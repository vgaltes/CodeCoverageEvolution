﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.TestManagement.Client;
using PlainConcepts.CodeCoverage.Web.Models;

namespace PlainConcepts.CodeCoverage.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var buildsCoverage = new Dictionary<string, Module>();

            return View(buildsCoverage);
        }
        //https://tfs2012.plainconcepts.com/tfs/Bilbao

        public JsonResult SelectCollection(string collectionUrl)
        {
            Uri collectionUri = new Uri(collectionUrl);
            var teamProjects = new TeamProjectsModel();

            using (var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri))
            {
                var commonStruct = tfsTeamProjectCollection.GetService<ICommonStructureService>();
                var teamProjectInfos = commonStruct.ListAllProjects();

                teamProjects.Status = true;
                foreach (var teamProjectInfo in teamProjectInfos)
                {
                    teamProjects.Projects.Add(new TeamProject(teamProjectInfo.Name, teamProjectInfo.Uri));
                }
            }

            return Json(teamProjects, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SelectProject(string collectionUrl, string projectUri)
        {
            Uri collectionUri = new Uri(collectionUrl);
            var buildListModel = new BuildListModel();

            using (var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri))
            {
                var buildService = tfsTeamProjectCollection.GetService<IBuildServer>();
                var builds = buildService.QueryBuildDefinitions(projectUri);
                buildListModel.Status = true;

                foreach (var build in builds)
                {
                    buildListModel.Builds.Add(new BuildModel(build.Name, build.Name));
                }
            }

            return Json(buildListModel, JsonRequestBehavior.AllowGet);
            
        }

        public JsonResult GetCodeCoverage(string collectionUrl, string projectName, string buildName)
        {
            Uri collectionUri = new Uri(collectionUrl);
            Dictionary<string, Module> buildsCoverage = new Dictionary<string, Module>();

            using (var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri))
            {
                var builds = GetBuilds(tfsTeamProjectCollection, projectName, buildName);
                foreach (var buildResult in builds.Builds)
                {
                    GetBuildCodeCoverage(tfsTeamProjectCollection, projectName, buildResult, buildsCoverage);
                }
            }

            var buildCoverageOrdered = buildsCoverage.OrderBy(b => b.Value.Builds.First().Name).ToList();

            return Json(buildCoverageOrdered, JsonRequestBehavior.AllowGet);
        }

        private static IBuildQueryResult GetBuilds(TfsTeamProjectCollection tfsTeamProjectCollection, string teamProject,
                                                   string buildName)
        {
            var buildService = tfsTeamProjectCollection.GetService<IBuildServer>();
            var buildSpec = buildService.CreateBuildDetailSpec(teamProject);
            buildSpec.MinFinishTime = DateTime.Now.AddDays(-30);
            buildSpec.Status = BuildStatus.All;
            buildSpec.QueryOrder = BuildQueryOrder.FinishTimeAscending;
            buildSpec.DefinitionSpec.Name = buildName;
            buildSpec.MaxBuildsPerDefinition = 150;

            var builds = buildService.QueryBuilds(buildSpec);
            return builds;
        }


        private void GetBuildCodeCoverage(TfsTeamProjectCollection tfsCollection, string teamProject, IBuildDetail build, Dictionary<string, Module> buildsCoverage)
        {
            var tcm = (ITestManagementService)tfsCollection.GetService(typeof(ITestManagementService));
            var testManagementTeamProject = tcm.GetTeamProject(teamProject);

            var coverageAnalysisManager = testManagementTeamProject.CoverageAnalysisManager;

            var queryBuildCoverage = coverageAnalysisManager.QueryBuildCoverage(build.Uri.ToString(), CoverageQueryFlags.Modules);

            foreach (var buildCoverage in queryBuildCoverage)
            {
                foreach (var moduleInfo in buildCoverage.Modules)
                {
                    if (buildsCoverage.ContainsKey(moduleInfo.Name))
                    {
                        AddBuildToModule(build, moduleInfo, buildsCoverage[moduleInfo.Name]);
                    }
                    else
                    {
                        Module module = new Module(moduleInfo.Name);
                        AddBuildToModule(build, moduleInfo, module);
                        buildsCoverage.Add(moduleInfo.Name, module);
                    }
                }
            }
        }

        private static void AddBuildToModule(IBuildDetail build, IModuleCoverage moduleInfo, Module module)
        {
            double coverage = ((double) moduleInfo.Statistics.BlocksCovered/
                               (double)
                               (moduleInfo.Statistics.BlocksCovered + moduleInfo.Statistics.BlocksNotCovered))*
                              100.0;
            Build buildPlain = new Build(build.BuildNumber, coverage);

            module.Builds.Add(buildPlain);
        }
    }
}
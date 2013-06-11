namespace PlainConcepts.CodeCoverage.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Web.Mvc;
    using System.Xml;
    using Microsoft.TeamFoundation;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.Server;
    using Microsoft.TeamFoundation.TestManagement.Client;
    using Models;

    /// <summary>
    /// Home controller
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Index action
        /// </summary>
        /// <returns>The frontend code for the application</returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Selects a collection in a TFS server
        /// </summary>
        /// <param name="collectionUrl">The collection's url</param>
        /// <param name="userName">Username to connect to TFS</param>
        /// <param name="password">User's password</param>
        /// <returns>A list of projects</returns>
        public JsonResult SelectCollection(string collectionUrl, string userName, string password)
        {
            Uri collectionUri = new Uri(collectionUrl);
            var teamProjects = new TeamProjectsModel();

            using (var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri))
            {
                try
                {
                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                    {
                        tfsTeamProjectCollection.ClientCredentials =
                            new TfsClientCredentials(new WindowsCredential(new NetworkCredential(userName, password)));
                        tfsTeamProjectCollection.Authenticate();
                    }

                    var commonStruct = tfsTeamProjectCollection.GetService<ICommonStructureService>();
                    var teamProjectInfos = commonStruct.ListAllProjects();

                    teamProjects.Status = true;
                    foreach (var teamProjectInfo in teamProjectInfos)
                    {
                        teamProjects.Projects.Add(new TeamProject(teamProjectInfo.Name, teamProjectInfo.Uri));
                    }
                }
                catch (TeamFoundationServerUnauthorizedException unauthorizedException)
                {
                    teamProjects.Status = false;
                }
            }

            return Json(teamProjects, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Selects a project in a TFS Server
        /// </summary>
        /// <param name="collectionUrl">The collection's url</param>
        /// <param name="projectUri">The project Uri</param>
        /// <param name="userName">Username to connect to TFS</param>
        /// <param name="password">User's password</param>
        /// <returns>A list of builds</returns>
        public JsonResult SelectProject(string collectionUrl, string projectUri, string userName, string password)
        {
            Uri collectionUri = new Uri(collectionUrl);
            var buildListModel = new BuildListModel();

            using (var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri))
            {
                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                {
                    tfsTeamProjectCollection.ClientCredentials =
                        new TfsClientCredentials(new WindowsCredential(new NetworkCredential(userName, password)));
                    tfsTeamProjectCollection.Authenticate();
                }

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


        /// <summary>
        /// Gets the code coverage for a build in a TFS server
        /// </summary>
        /// <param name="collectionUrl">The collection's url</param>
        /// <param name="projectName">The project name</param>
        /// <param name="buildName">The build name</param>
        /// <param name="userName">Username to connect to TFS</param>
        /// <param name="password">User's password</param>
        /// <returns></returns>
        public JsonResult GetCodeCoverage(string collectionUrl, string projectName, string buildName, string userName, string password)
        {
            var buildCoverageOrdered = CalculateBuildCoverageOrdered(collectionUrl, projectName, buildName, userName, password);

            var buildCoverage = CalculateBuildTotalCodeCoverage(buildCoverageOrdered);

            BuildCodeCoverageModel result = new BuildCodeCoverageModel();
            result.TotalCoverage = buildCoverage;
            result.Modules = buildCoverageOrdered;
            result.BuildName = buildName;

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Batch action
        /// </summary>
        /// <returns>The frontend code to make a batch request</returns>
        public ActionResult Batch()
        {
            return View();
        }

        /// <summary>
        /// Returns a group of code coverage results
        /// </summary>
        /// <param name="parameters">Parameters that define the bulds to use to make the request</param>
        /// <returns>The json representation of the code coverage results</returns>
        /// <remarks>The parameters should be lines like this 
        /// (<collection_url>,<ProjectName>,<buildName>,<user>,<password></password>) separated by ;
        /// </remarks>
        public JsonResult BatchCoverage(string parameters)
        {
            var result = new List<BuildCodeCoverageModel>();

            var buildsToCalculateCoverage = parameters.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var buildToCalculateCoverage in buildsToCalculateCoverage)
            {
                var buildParameters = buildToCalculateCoverage.Split(new string[] { "," },
                                                                     StringSplitOptions.RemoveEmptyEntries);

                if (buildParameters != null && buildParameters.Count() == 5)
                {
                    var collectionUrl = buildParameters[0];
                    var projectName = buildParameters[1];
                    var buildName = buildParameters[2];
                    var userName = buildParameters[3];
                    var password = buildParameters[4];

                    var buildCoverageOrdered = CalculateBuildCoverageOrdered(collectionUrl, projectName, buildName, userName, password);

                    var buildCoverage = CalculateBuildTotalCodeCoverage(buildCoverageOrdered);

                    var model = CreateBuildCodeCoverageModel(buildName, buildCoverageOrdered, buildCoverage);

                    result.Add(model);
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private static BuildCodeCoverageModel CreateBuildCodeCoverageModel(string buildName, List<KeyValuePair<string, Module>> buildCoverageOrdered,
                                                                           double buildCoverage)
        {
            var model = new BuildCodeCoverageModel();
            model.BuildName = buildName;
            model.Modules = buildCoverageOrdered;
            model.TotalCoverage = buildCoverage;
            return model;
        }

        private static double CalculateBuildTotalCodeCoverage(List<KeyValuePair<string, Module>> buildCoverageOrdered)
        {
            double blocksCovered = 0;
            double blocksNotCovered = 0;

            foreach (var build in buildCoverageOrdered)
            {
                blocksCovered += build.Value.Builds.Last().BlocksCovered;
                blocksNotCovered += build.Value.Builds.Last().BlocksNotCovered;
            }

            var buildCoverage = blocksCovered * 100 / (blocksCovered + blocksNotCovered);
            return buildCoverage;
        }

        private List<KeyValuePair<string, Module>> CalculateBuildCoverageOrdered(string collectionUrl, string projectName, string buildName, string userName,
                                                   string password)
        {
            Uri collectionUri = new Uri(collectionUrl);
            var buildsCoverage = new Dictionary<string, Module>();

            using (var tfsTeamProjectCollection = new TfsTeamProjectCollection(collectionUri))
            {
                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                {
                    tfsTeamProjectCollection.ClientCredentials =
                        new TfsClientCredentials(new WindowsCredential(new NetworkCredential(userName, password)));
                    tfsTeamProjectCollection.Authenticate();
                }

                var buildService = tfsTeamProjectCollection.GetService<IBuildServer>();
                var commonStructureService = tfsTeamProjectCollection.GetService<ICommonStructureService4>();
                ProjectInfo projectInfo = commonStructureService.GetProjectFromName(projectName);

                var iterationDates = GetIterationDates(commonStructureService, projectInfo.Uri);

                foreach (var iterationDate in iterationDates)
                {
                    var builds = GetSprintBuilds(buildService, projectName, buildName, iterationDate.StartDate.Value, iterationDate.EndDate.Value);
                    if (builds.Builds.Any())
                        GetBuildCodeCoverage(tfsTeamProjectCollection, projectName, builds.Builds.Last(), buildsCoverage);
                }
            }

            var buildCoverageOrdered = buildsCoverage.OrderBy(b => b.Value.Builds.First().Name).ToList();
            return buildCoverageOrdered;
        }

        private static IBuildQueryResult GetSprintBuilds(IBuildServer buildService, string teamProject,
                                                   string buildName, DateTime startDate, DateTime endDate)
        {
            var buildSpec = buildService.CreateBuildDetailSpec(teamProject);
            buildSpec.MinFinishTime = startDate;
            buildSpec.MaxFinishTime = endDate;
            buildSpec.Status = BuildStatus.All;
            buildSpec.QueryOrder = BuildQueryOrder.FinishTimeAscending;
            buildSpec.DefinitionSpec.Name = buildName;
            buildSpec.MaxBuildsPerDefinition = 1500;

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
                        var module = new Module(moduleInfo.Name);
                        AddBuildToModule(build, moduleInfo, module);
                        buildsCoverage.Add(moduleInfo.Name, module);
                    }
                }
            }
        }

        private static void AddBuildToModule(IBuildDetail build, IModuleCoverage moduleInfo, Module module)
        {
            double coverage = ((double)moduleInfo.Statistics.BlocksCovered /
                               (double)
                               (moduleInfo.Statistics.BlocksCovered + moduleInfo.Statistics.BlocksNotCovered)) *
                              100.0;
            Build buildPlain = new Build(build.BuildNumber, coverage, moduleInfo.Statistics.BlocksCovered, moduleInfo.Statistics.BlocksNotCovered);

            module.Builds.Add(buildPlain);
        }

        private static IEnumerable<ScheduleInfo> GetIterationDates(ICommonStructureService4 css, string projectUri)
        {
            NodeInfo[] structures = css.ListStructures(projectUri);
            NodeInfo iterations = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
            List<ScheduleInfo> schedule = null;

            if (iterations != null)
            {
                string projectName = css.GetProject(projectUri).Name;

                XmlElement iterationsTree = css.GetNodesXml(new[] { iterations.Uri }, true);
                GetIterationDates(iterationsTree.ChildNodes[0], projectName, ref schedule);
            }

            return schedule.Where(s => s.StartDate != null && s.EndDate != null);
        }

        private static void GetIterationDates(XmlNode node, string projectName, ref List<ScheduleInfo> schedule)
        {
            if (schedule == null)
                schedule = new List<ScheduleInfo>();

            if (node != null)
            {
                string iterationPath = node.Attributes["Path"].Value;
                if (!string.IsNullOrEmpty(iterationPath))
                {
                    // Attempt to read the start and end dates if they exist.
                    string strStartDate = (node.Attributes["StartDate"] != null) ? node.Attributes["StartDate"].Value : null;
                    string strEndDate = (node.Attributes["FinishDate"] != null) ? node.Attributes["FinishDate"].Value : null;

                    DateTime? startDate = null, endDate = null;

                    if (!string.IsNullOrEmpty(strStartDate) && !string.IsNullOrEmpty(strEndDate))
                    {
                        bool datesValid = true;

                        // Both dates should be valid.
                        DateTime tempStartDate, tempEndDate;
                        datesValid &= DateTime.TryParse(strStartDate, out tempStartDate);
                        datesValid &= DateTime.TryParse(strEndDate, out tempEndDate);

                        // Clear the dates unless both are valid.
                        if (datesValid)
                        {
                            startDate = tempStartDate;
                            endDate = tempEndDate;
                        }
                    }

                    schedule.Add(new ScheduleInfo
                    {
                        Path = iterationPath.Replace(string.Concat("\\", projectName, "\\Iteration"), projectName),
                        StartDate = startDate,
                        EndDate = endDate
                    });
                }

                // Visit any child nodes (sub-iterations).
                if (node.FirstChild != null)
                {
                    // The first child node is the <Children> tag, which we'll skip.
                    for (int nChild = 0; nChild < node.ChildNodes[0].ChildNodes.Count; nChild++)
                        GetIterationDates(node.ChildNodes[0].ChildNodes[nChild], projectName, ref schedule);
                }
            }
        }
    }
}

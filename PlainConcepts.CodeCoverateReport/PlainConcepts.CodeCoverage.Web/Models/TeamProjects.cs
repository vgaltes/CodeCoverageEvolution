using System.Collections.Generic;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class TeamProject
    {
        public readonly string Name;
        public readonly string ProjectUri;

        public TeamProject(string name, string projectUri)
        {
            Name = name;
            ProjectUri = projectUri;
        }
    }

    public class TeamProjectsModel
    {
        public bool Status { get; set; }
        public List<TeamProject> Projects { get; set; }

        public TeamProjectsModel()
        {
            Projects = new List<TeamProject>();
        }
    }
}
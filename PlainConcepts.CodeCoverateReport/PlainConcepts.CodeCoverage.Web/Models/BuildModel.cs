using System.Collections.Generic;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class BuildListModel
    {
        public bool Status { get; set; }

        public List<BuildModel> Builds { get; set; } 

        public BuildListModel()
        {
            Builds = new List<BuildModel>();
        }
    }
    public class BuildModel
    {
        public readonly string Name;
        public readonly string BuildUri;

        public BuildModel(string name, string buildUri)
        {
            Name = name;
            BuildUri = buildUri;
        }
    }
}
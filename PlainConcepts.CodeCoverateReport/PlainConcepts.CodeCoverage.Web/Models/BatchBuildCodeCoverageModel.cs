using System.Collections.Generic;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class BatchBuildCodeCoverageModel
    {
        public List<KeyValuePair<string, Module>> CodeCoverage { get; set; }
        public string BuildName { get; set; }
    }
}
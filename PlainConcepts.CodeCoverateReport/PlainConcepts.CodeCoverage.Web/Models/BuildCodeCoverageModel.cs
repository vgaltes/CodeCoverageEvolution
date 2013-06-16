using System.Collections.Generic;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class BuildCodeCoverageModel
    {
        public double TotalCoverage { get; set; }

        public List<KeyValuePair<string, Module>> Modules { get; set; }

        public string BuildName { get; set; }
    }
}
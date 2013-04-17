using System.Collections.Generic;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class Build
    {
        public readonly string Name;
        public readonly double Coverage;
        public readonly int BlocksCovered;
        public readonly int BlocksNotCovered;

        public Build(string name, double coverage, int blocksCovered, int blocksNotCovered)
        {
            Name = name;
            Coverage = coverage;
            BlocksCovered = blocksCovered;
            BlocksNotCovered = blocksNotCovered;
        }
    }

    public class Module
    {
        public readonly string Name;

        public List<Build> Builds { get; set; }

        public Module(string name)
        {
            Name = name;
            Builds = new List<Build>();
        }

    }
}
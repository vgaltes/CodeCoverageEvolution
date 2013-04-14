using System.Collections.Generic;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class Build
    {
        public readonly string Name;
        public readonly double Coverage;

        public Build(string name, double coverage)
        {
            Name = name;
            Coverage = coverage;
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
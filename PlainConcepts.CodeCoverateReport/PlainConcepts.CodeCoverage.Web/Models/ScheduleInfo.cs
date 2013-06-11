using System;

namespace PlainConcepts.CodeCoverage.Web.Models
{
    public class ScheduleInfo
    {
        public string Path { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
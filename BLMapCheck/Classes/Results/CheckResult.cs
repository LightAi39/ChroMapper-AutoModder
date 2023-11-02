using BLMapCheck.Classes.MapVersion.Difficulty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.Results
{
    #nullable enable

    public class CheckResult
    {
        public string Name { get; set; } = "";
        public Severity Severity { get; set; }
        public string CheckType { get; set; } = "";
        public string Description { get; set; } = "";
        public List<BeatmapObject>? BeatmapObjects { get; set; }
        public List<(string key, string value)>? ResultData { get; set; }
    }

    public enum Severity
    {
        Passed,
        Info,
        Suggestion,
        Warning,
        Error,
        Inconclusive
    }
}

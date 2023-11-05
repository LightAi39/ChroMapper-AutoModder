using Parser.Map.Difficulty.V3.Base;
using System.Collections.Generic;

namespace BLMapCheck.Classes.Results
{
    #nullable enable

    public class CheckResult
    {
        public string Name { get; set; } = "";
        public string? Difficulty { get; set; }
        public string? Characteristic { get; set; }
        public Severity Severity { get; set; }
        public string CheckType { get; set; } = "";
        public string Description { get; set; } = "";
        public List<BeatmapObject>? BeatmapObjects { get; set; }
        public List<KeyValuePair>? ResultData { get; set; }
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

    public class KeyValuePair
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";

        public KeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}

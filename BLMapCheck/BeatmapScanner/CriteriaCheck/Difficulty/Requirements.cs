using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Requirements
    {
        public static CritResult Check(List<string> requirements)
        {
            var issue = CritResult.Success;

            if (requirements != null && requirements.Any())
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Requirements",
                    Severity = Severity.Error,
                    CheckType = "Requirements",
                    Description = "Any map that is dependent on other mods or programs is not allowed.",
                    ResultData = new() { new("Requirements", "Has " + string.Join(",", requirements.ToArray())) }
                });
                issue = CritResult.Fail;
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Requirements",
                    Severity = Severity.Passed,
                    CheckType = "Requirements",
                    Description = "Map doesn't have any mod requirement.",
                    ResultData = new() { new("Requirements", "None") }
                });
            }

            return issue;
        }
    }
}

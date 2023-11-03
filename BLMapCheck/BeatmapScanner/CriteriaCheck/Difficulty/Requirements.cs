using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Requirements
    {
        public static CritResult Check(List<string> Requirements)
        {
            var issue = CritResult.Success;

            if (Requirements.Any())
            {
                //CreateSongInfoComment("R1C - " + diff.BeatmapFilename + " has " + req + " requirement", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = BSMapCheck.Characteristic,
                    Difficulty = BSMapCheck.Difficulty,
                    Name = "Requirements",
                    Severity = Severity.Error,
                    CheckType = "Requirements",
                    Description = "Any map that is dependent on other mods or programs is not allowed.",
                    ResultData = new() { new("Requirements", "Has " + string.Join(",", Requirements.ToArray())) }
                });
                issue = CritResult.Fail;
            }

            return issue;
        }
    }
}

using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class HotStart
    {
        // Detect objects that are too early in the map, configurable setting is available
        public static CritResult Check(List<BeatmapGridObject> Objects, List<Obstacle> Walls)
        {
            var issue = CritResult.Success;
            var limit = BeatPerMinute.BPM.ToBeatTime((float)Instance.HotStartDuration, true);
            foreach (var c in Objects)
            {
                if (c.b < limit)
                {
                    //CreateDiffCommentNote("R1E - Hot Start", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hot Start",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 1.33 seconds of time before any interactable objects.",
                        ResultData = new() { new("HotStart", "Minimum beat is: " + limit.ToString() + " Current object is at: " + c.b.ToString()) },
                        BeatmapObjects = new() { c }
                    });
                    issue = CritResult.Fail;
                }
                else break;
            }
            foreach (var w in Walls)
            {
                if (w.b < limit && ((w.x + w.w >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hot Start",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 1.33 seconds of time before any interactable objects.",
                        ResultData = new() { new("HotStart", "Minimum beat is: " + limit.ToString() + " Current object is at: " + w.b.ToString()) },
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                else break;
            }
            return issue;
        }
    }
}

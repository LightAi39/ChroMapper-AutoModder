using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ColdEnd
    {
        public static CritResult Check(List<BeatmapGridObject> Objects, List<Obstacle> Walls, float LoadedSongLength)
        {
            var issue = CritResult.Success;
            var limit = BeatPerMinute.BPM.ToBeatTime(LoadedSongLength - (float)ColdEndDuration, true);
            foreach (var obj in Objects)
            {
                if (obj.b > limit)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Cold End",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 2 seconds of time after the last interactable objects.",
                        ResultData = new() { new("ColdEnd", "Maximum beat is: " + limit.ToString() + " Current object is at: " + obj.b.ToString()) },
                        BeatmapObjects = new() { obj }
                    });
                    issue = CritResult.Fail;
                }
                else break;
            }
            foreach (var w in Walls)
            {
                if (w.b + w.d > limit && ((w.x + w.w >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    //CreateDiffCommentObstacle("R1E - Cold End", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Cold End",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 2 seconds of time after the last interactable objects.",
                        ResultData = new() { new("ColdEnd", "Maximum beat is: " + limit.ToString() + " Current obstacle is at: " + (w.b + w.d).ToString()) },
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

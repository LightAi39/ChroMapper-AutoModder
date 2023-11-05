using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ColdEnd
    {
        public static CritResult Check(List<BeatmapGridObject> objects, List<Obstacle> walls, float songLength)
        {
            var issue = CritResult.Success;
            var limit = BeatPerMinute.BPM.ToBeatTime(songLength - (float)Instance.ColdEndDuration, true);

            foreach (var obj in objects)
            {
                if (obj.b > limit)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Cold End",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 2 seconds of time after the last interactable object.",
                        ResultData = new() { new("ColdEnd", "Maximum beat is: " + limit.ToString() + " Current object is at: " + obj.b.ToString()) },
                        BeatmapObjects = new() { obj }
                    });
                    issue = CritResult.Fail;
                }
            }
            foreach (var w in walls)
            {
                if (w.b + w.d > limit && ((w.x + w.w >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    //CreateDiffCommentObstacle("R1E - Cold End", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Cold End",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 2 seconds of time after the last interactable object.",
                        ResultData = new() { new("ColdEnd", "Maximum beat is: " + limit.ToString() + " Current obstacle is at: " + (w.b + w.d).ToString()) },
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Cold End",
                    Severity = Severity.Passed,
                    CheckType = "Duration",
                    Description = "There is at least 2 seconds of time after the last interactable object.",
                    ResultData = new() { new("ColdEnd", "Success") }
                });
            }

            return issue;
        }
    }
}

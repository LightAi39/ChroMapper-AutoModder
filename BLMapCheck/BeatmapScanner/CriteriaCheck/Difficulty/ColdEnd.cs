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
        public static CritResult Check(List<BeatmapGridObject> objects, List<Wall> walls, float songLength)
        {
            var issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;
            var limit = timescale.BPM.ToBeatTime(songLength - (float)Instance.ColdEndDuration, true);

            foreach (var obj in objects)
            {
                if (obj.Beats > limit)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Cold End",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 2 seconds of time after the last interactable object.",
                        ResultData = new() { new("ColdEnd", "Maximum beat is: " + limit.ToString() + " Current object is at: " + obj.Beats.ToString()) },
                        BeatmapObjects = new() { obj }
                    });
                    issue = CritResult.Fail;
                }
            }
            foreach (var w in walls)
            {
                if (w.Beats + w.DurationInBeats > limit && ((w.x + w.Width >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
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
                        ResultData = new() { new("ColdEnd", "Maximum beat is: " + limit.ToString() + " Current obstacle is at: " + (w.Beats + w.DurationInBeats).ToString()) },
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

using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class HotStart
    {
        // Detect objects that are too early in the map, configurable setting is available
        public static CritResult Check(List<BeatmapGridObject> objects, List<Wall> walls)
        {
            var issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;
            var limit = timescale.BPM.ToBeatTime((float)Instance.HotStartDuration, true);
            foreach (var c in objects)
            {
                if (c.Beats < limit)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hot Start",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 1.33 seconds of time before any interactable objects.",
                        ResultData = new() { new("HotStart", "Minimum beat is: " + limit.ToString() + " Current object is at: " + c.Beats.ToString()) },
                        BeatmapObjects = new() { c }
                    });
                    issue = CritResult.Fail;
                }
                else break;
            }
            foreach (var w in walls)
            {
                if (w.Beats < limit && ((w.x + w.Width >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hot Start",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "There must be at least 1.33 seconds of time before any interactable objects.",
                        ResultData = new() { new("HotStart", "Minimum beat is: " + limit.ToString() + " Current object is at: " + w.ToString()) },
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                else break;
            }

            if(issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Hot Start",
                    Severity = Severity.Passed,
                    CheckType = "Duration",
                    Description = "There is at least 1.33 seconds of time before any interactable objects.",
                    ResultData = new() { new("HotStart", "Success") }
                });
            }

            return issue;
        }
    }
}

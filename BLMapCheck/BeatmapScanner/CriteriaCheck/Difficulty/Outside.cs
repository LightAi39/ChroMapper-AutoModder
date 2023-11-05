using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public static CritResult Check(float songLength, List<Colornote> notes, List<Burstslider> chains, List<Bombnote> bombs, List<Obstacle> walls)
        {
            var issue = CritResult.Success;

            var end = BeatPerMinute.BPM.ToBeatTime(songLength, true);
            if (notes.Exists(c => c.b < 0 || c.b > end) || chains.Exists(c => c.b < 0 || c.b > end)
                || bombs.Exists(b => b.b < 0 || b.b > end) || walls.Exists(w => w.b < 0 || w.b + w.d > end))
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Outside",
                    Severity = Severity.Error,
                    CheckType = "Outside",
                    Description = "Object cannot exist outside of playable length.",
                    ResultData = new() { new("Outside", "Boundary: 0 to" + end.ToString()) },
                });
                issue = CritResult.Fail;
            }

            if(issue == CritResult.Success) 
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Outside",
                    Severity = Severity.Passed,
                    CheckType = "Outside",
                    Description = "No object detected outside of the playable length.",
                    ResultData = new() { new("Outside", "Success") }
                });
            }

            return issue;
        }
    }
}

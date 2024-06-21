using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public static CritResult Check(float songLength, List<Note> notes, List<Chain> chains, List<Bomb> bombs, List<Wall> walls)
        {
            var issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            var end = timescale.BPM.ToBeatTime(songLength, true);
            if (notes.Exists(c => c.Beats < 0 || c.Beats > end) || chains.Exists(c => c.Beats < 0 || c.TailInBeats < 0 || c.Beats > end || c.TailInBeats > end)
                || bombs.Exists(b => b.Beats < 0 || b.Beats > end) || walls.Exists(w => w.Beats < 0 || w.Beats + w.DurationInBeats > end))
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Outside",
                    Severity = Severity.Error,
                    CheckType = "Outside",
                    Description = "Object cannot exist outside of playable length.",
                    ResultData = new(),
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
                    ResultData = new()
                });
            }

            return issue;
        }
    }
}

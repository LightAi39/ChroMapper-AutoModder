using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public static CritResult Check(float songLength, List<Note> notes, List<Chain> chains, List<Bomb> bombs, List<Wall> walls)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Outside";
            string checkType = "Outside";
            CritResult criteria = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            if (songLength == 0)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "Outside check error, SongLength is 0. Make sure to use an ogg file");
                criteria = CritResult.Fail;
            }

            // Calculate end duration in beat
            var end = timescale.BPM.ToBeatTime(songLength, true);

            // Check for existing objects before 0 or after end
            if (notes.Exists(c => c.Beats < 0 || c.Beats > end) || chains.Exists(c => c.Beats < 0 || c.TailInBeats < 0 || c.Beats > end || c.TailInBeats > end)
                || bombs.Exists(b => b.Beats < 0 || b.Beats > end) || walls.Exists(w => w.Beats < 0 || w.Beats + w.DurationInBeats > end))
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "Object cannot exist outside of playable length");
                criteria = CritResult.Fail;
            }

            if(criteria == CritResult.Success) 
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Passed, checkType, "No object detected outside of the playable length");
            }

            return criteria;
        }
    }
}

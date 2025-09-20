using beatleader_parser.Timescale;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public static CritResult Check(string characteristic, string difficulty, Timescale timescale, float songLength, List<Note> notes, List<Chain> chains, List<Bomb> bombs, List<Wall> walls)
        {
            CritResult criteria = CritResult.Success;

            if (songLength == 0)
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                    "Outside", Severity.Error, "Outside", "Outside check error, SongLength is 0. Make sure to use an ogg file");
                criteria = CritResult.Fail;
            }

            // Calculate end duration in beat
            var end = timescale.BPM.ToBeatTime(songLength, true);

            // Check for existing objects before 0 or after end
            if (notes.Exists(c => c.Beats < 0 || c.Beats > end) || chains.Exists(c => c.Beats < 0 || c.TailInBeats < 0 || c.Beats > end || c.TailInBeats > end)
                || bombs.Exists(b => b.Beats < 0 || b.Beats > end) || walls.Exists(w => w.Beats < 0 || w.Beats + w.DurationInBeats > end))
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                    "Outside", Severity.Error, "Outside", "Object cannot exist outside of playable length");
                criteria = CritResult.Fail;
            }

            if(criteria == CritResult.Success) 
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                    "Outside", Severity.Passed, "Outside", "No object detected outside of the playable length");
            }

            return criteria;
        }
    }
}

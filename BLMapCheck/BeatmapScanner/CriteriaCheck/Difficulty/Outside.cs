using beatleader_parser.Timescale;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public static CritResult Check(CheckContext ctx, float songLength, List<Note> notes, List<Chain> chains, List<Bomb> bombs, List<Wall> walls)
        {
            CritResult criteria = CritResult.Success;
            Timescale timescale = ctx.Timescale;

            if (songLength == 0)
            {
                ctx.Results.CreateDiffResult("Outside", Severity.Error, "Outside", "Outside check error, SongLength is 0. Make sure to use an ogg file");
                return CritResult.Fail;
            }

            // Calculate end duration in beat
            var end = timescale.BPM.ToBeatTime(songLength, true);

            // Check for existing objects before 0 or after end
            List<BeatmapObject> outside = new();
            outside.AddRange(notes.Where(c => c.Beats < 0 || c.Beats > end));
            outside.AddRange(chains.Where(c => c.Beats < 0 || c.TailInBeats < 0 || c.Beats > end || c.TailInBeats > end));
            outside.AddRange(bombs.Where(b => b.Beats < 0 || b.Beats > end));
            outside.AddRange(walls.Where(w => w.Beats < 0 || w.Beats + w.DurationInBeats > end));
            if (outside.Count > 0)
            {
                ctx.Results.CreateDiffResult("Outside", Severity.Error, "Outside", "Object cannot exist outside of playable length", new(), outside);
                criteria = CritResult.Fail;
            }

            if(criteria == CritResult.Success) 
            {
                ctx.Results.CreateDiffResult("Outside", Severity.Passed, "Outside", "No object detected outside of the playable length");
            }

            return criteria;
        }
    }
}

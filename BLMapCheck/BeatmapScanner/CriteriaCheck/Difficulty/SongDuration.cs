using beatleader_parser.Timescale;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SongDuration
    {
        // Detect if the mapped duration is above the minimum required, from first note to last note or chain link
        public static CritResult Check(CheckContext ctx, List<Note> notes, List<Chain> chains)
        {
            CritResult criteria = CritResult.Success;
            Timescale timescale = ctx.Timescale;

            if(notes.Any())
            {
                var first = notes.Min(n => n.Beats);
                var last = notes.Max(n => n.Beats);
                if (chains.Any()) last = Math.Max(last, chains.Max(c => c.TailInBeats));
                // Convert both ends separately, the beat difference alone would ignore BPM changes
                var duration = timescale.BPM.ToRealTime(last, true) - timescale.BPM.ToRealTime(first, true);

                if (duration < ctx.Config.MinSongDuration)
                {
                    ctx.Results.CreateDiffResult("Mapped Duration", Severity.Error, "Duration", "The map from first note to last note must be at least " + ctx.Config.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", ctx.Config.MinSongDuration.ToString() + "s") });
                    criteria = CritResult.Fail;
                }
                else
                {
                    ctx.Results.CreateDiffResult("Mapped Duration", Severity.Passed, "Duration", "The map from first note to last note must be at least " + ctx.Config.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", ctx.Config.MinSongDuration.ToString() + "s") });
                }  
            }
            else
            {
                ctx.Results.CreateDiffResult("Mapped Duration", Severity.Error, "Duration", "The map from first note to last note must be at least " + ctx.Config.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", "0s"), new("MinimumDuration", ctx.Config.MinSongDuration.ToString() + "s") });
                criteria = CritResult.Fail;
            }

            return criteria;
        }
    }
}

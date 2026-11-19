using beatleader_parser.Timescale;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class HotStart
    {
        // Detect objects that are too early in the map
        public static CritResult Check(CheckContext ctx, List<BeatmapGridObject> objects, List<Wall> walls)
        {
            CritResult criteria = CritResult.Success;
            Timescale timescale = ctx.Timescale;

            // Calculate the minimum duration in beats
            var limit = timescale.BPM.ToBeatTime((float)ctx.Config.HotStartDuration, true);

            foreach (var c in objects)
            {
                if (c.Beats < limit)
                {
                    ctx.Results.CreateDiffResult("Hot Start", Severity.Error, "Duration", "There must be at least " + ctx.Config.HotStartDuration.ToString() + " seconds of time before any interactable objects",
                        new() { new("CurrentBeat", c.Beats.ToString()), new("MinimumBeat", limit.ToString()) }, new() { c });
                    criteria = CritResult.Fail;
                }
                else break;
            }

            // Need to take into consideration wall placement
            foreach (var w in walls)
            {
                if (w.Beats >= limit) break;
                // Walls that stay in the outer lanes are not interactable
                if ((w.x + w.Width >= 2 && w.x < 2) || w.x == 1 || w.x == 2)
                {
                    ctx.Results.CreateDiffResult("Hot Start", Severity.Error, "Duration", "There must be at least " + ctx.Config.HotStartDuration.ToString() + " seconds of time before any interactable objects",
                        new() { new("CurrentBeat", w.Beats.ToString()), new("MinimumBeat", limit.ToString()) }, new() { w });
                    criteria = CritResult.Fail;
                }
            }

            if(criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Hot Start", Severity.Passed, "Duration", "There is at least " + ctx.Config.HotStartDuration.ToString() + " seconds of time before any interactable objects");
            }

            return criteria;
        }
    }
}

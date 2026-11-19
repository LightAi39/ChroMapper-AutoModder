using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal class ChainConsistency
    {
        // Compared chain duration with a specific value, flag mismatch.
        public static void Check(CheckContext ctx, List<Chain> chains, double expectedDuration = 0.0625)
        {
            if (ctx.Config.ChainPrecision != 0) expectedDuration = 1 / ctx.Config.ChainPrecision;

            foreach (Chain chain in chains)
            {
                double duration = chain.TailInBeats - chain.Beats;
                if (duration >= expectedDuration - 0.001 && duration <= expectedDuration + 0.001) continue;

                ctx.Results.CreateDiffResult("Chain Consistency", Severity.Data, "Chain", "Chain duration doesn't match expected value",
                        new List<Classes.Results.KeyValuePair>() { new("CurrentPrecision:", duration.ToString()) }, new() { chain });
            }
        }
    }
}

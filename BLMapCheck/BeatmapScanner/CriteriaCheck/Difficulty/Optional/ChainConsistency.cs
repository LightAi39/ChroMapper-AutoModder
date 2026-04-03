using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal class ChainConsistency
    {
        // Compared chain duration with a specific value, flag mismatch.
        public static void Check(List<Chain> chains, double expectedDuration = 0.0625)
        {
            if (Config.Instance.ChainPrecision != 0) expectedDuration = 1 / Config.Instance.ChainPrecision;

            foreach (Chain chain in chains)
            {
                double duration = chain.TailInBeats - chain.Beats;
                if (duration >= expectedDuration - 0.001 && duration <= expectedDuration + 0.001) continue;

                CheckResults.Instance.CreateDiffResult("Chain Consistency", Severity.Data, "Chain", "Chain duration doesn't match expected value",
                        new List<Classes.Results.KeyValuePair>() { new("CurrentPrecision:", duration.ToString()) }, new() { chain });
            }
        }
    }
}

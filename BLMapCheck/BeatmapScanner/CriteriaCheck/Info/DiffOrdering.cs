using BLMapCheck.Classes.Results;
using Parser.Map;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class DiffOrdering
    {
        // Run this per characteristic
        public static CritResult Check(List<DifficultySet> difficulties, float BeatsPerMinute, string characteristic = "")
        {
            var ordered = new List<double>();

            // If no characteristic is sent, default out to a specific order
            if (characteristic == "")
            {
                if (difficulties.Exists(x => x.Characteristic == "Standard")) characteristic = "Standard";
                else if (difficulties.Exists(x => x.Characteristic == "OneSaber")) characteristic = "OneSaber";
                else characteristic = difficulties.Select(x => x.Characteristic).FirstOrDefault().ToString();
            }

            foreach (var difficulty in difficulties.Where(x => x.Characteristic == characteristic))
            {
                if(difficulty.Data.Notes.Count >= 20)
                {
                    _Difficultybeatmaps difficultyBeatmap = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == difficulty.Characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == difficulty.Difficulty);
                    var data = BLMapChecker.analyzer.GetRating(difficulty.Data, difficulty.Characteristic, difficulty.Difficulty, BeatsPerMinute, difficultyBeatmap._noteJumpMovementSpeed);
                    ordered.Add(data[0].Pass);
                }
            }

            var order = ordered.OrderBy(x => x).ToList();
            if (ordered.SequenceEqual(order))
            {
                CheckResults.Instance.CreateInfoResult("Difficulty Ordering", Severity.Passed, "SongInfo", "Difficulty ordering is correct", 
                    new() { new("CurrentOrder", string.Join(",", ordered.ToArray())) });
                return CritResult.Success;
            }

            CheckResults.Instance.CreateInfoResult("Difficulty Ordering", Severity.Error, "SongInfo", "Difficulty ordering is wrong",
                    new() { new("CurrentOrder", string.Join(",", ordered.ToArray())), new("ExpectedOrder", string.Join(",", order.ToArray())) });
            return CritResult.Fail;
        }
    }
}

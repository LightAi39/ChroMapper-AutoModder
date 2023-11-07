using BLMapCheck.Classes.Results;
using Parser.Map;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using beatleader_analyzer;
using JoshaParity;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class DiffOrdering
    {
        // Run this per characteristic
        public static CritResult Check(List<DifficultySet> difficulties, float BeatsPerMinute)
        {
            var passStandard = new List<double>();

            foreach (var difficulty in difficulties.Where(x => x.Characteristic == CriteriaCheckManager.Characteristic))
            {
                if(difficulty.Data.Notes.Count >= 20)
                {
                    var data = BLMapChecker.analyzer.GetRating(difficulty.Data, difficulty.Characteristic, difficulty.Difficulty, BeatsPerMinute);
                    passStandard.Add(data[0].Pass);
                }
            }

            var order = passStandard.ToList();
            order.Sort();
            if (passStandard.SequenceEqual(order))
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Difficulty Ordering",
                    Severity = Severity.Passed,
                    CheckType = "SongInfo",
                    Description = "Difficulty ordering is correct.",
                    ResultData = new() { new("CurrentOrder", string.Join(",", passStandard.ToArray())) }
                });
                return CritResult.Success;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Difficulty Ordering",
                Severity = Severity.Error,
                CheckType = "SongInfo",
                Description = $"Difficulty ordering is wrong.",
                ResultData = new() { new("CurrentOrder", string.Join(",", passStandard.ToArray())), new("ExpectedOrder", string.Join(",", order.ToArray())) }
            });

            return CritResult.Fail;
        }
    }
}

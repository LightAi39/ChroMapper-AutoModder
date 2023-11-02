using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class DiffOrdering
    {
        // Run this per characteristic
        public static CritResult Check(List<DifficultyV3> difficulties, float BeatsPerMinute)
        {
            var passStandard = new List<double>();

            foreach (var difficulty in difficulties)
            {
                if (difficulty.colorNotes.Any())
                {
                    var notes = difficulty.colorNotes.Where(n => n.c == 0 || n.c == 1).ToList();
                    notes = notes.OrderBy(o => o.b).ToList();

                    if (notes.Count > 0)
                    {
                        List<Burstslider> chains = difficulty.burstSliders.OrderBy(o => o.b).ToList();
                        List<Bombnote> bombs = difficulty.bombNotes.OrderBy(o => o.b).ToList();
                        List<Obstacle> obstacles = difficulty.obstacles.OrderBy(o => o.b).ToList();
                        var data = BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatsPerMinute);
                        passStandard.Add(data.diff);
                    }
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

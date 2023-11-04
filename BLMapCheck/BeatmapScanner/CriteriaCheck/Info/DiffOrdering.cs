using BLMapCheck.Classes.MapVersion;
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
        public static CritResult Check(List<DifficultySet> difficulties, float BeatsPerMinute)
        {
            var passStandard = new List<double>();

            foreach (var difficulty in difficulties.Where(x => x.Characteristic == CriteriaCheckManager.Characteristic))
            {
                if (difficulty.Data.colorNotes.Any())
                {
                    var notes = difficulty.Data.colorNotes.Where(n => n.c == 0 || n.c == 1).ToList();
                    notes = notes.OrderBy(o => o.b).ToList();

                    if (notes.Count > 0)
                    {
                        List<Burstslider> chains = difficulty.Data.burstSliders.OrderBy(o => o.b).ToList();
                        List<Bombnote> bombs = difficulty.Data.bombNotes.OrderBy(o => o.b).ToList();
                        List<Obstacle> obstacles = difficulty.Data.obstacles.OrderBy(o => o.b).ToList();
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

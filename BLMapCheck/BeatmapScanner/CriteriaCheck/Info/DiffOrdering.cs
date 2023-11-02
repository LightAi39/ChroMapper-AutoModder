using BLMapCheck.Classes.MapVersion.Difficulty;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class DiffOrdering
    {
        // Run this per characteristic
        public static Severity Check(List<DifficultyV3> difficulties, float BeatsPerMinute)
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
                return Severity.Success;
            }

            //CreateSongInfoComment("R7E - Difficulty Ordering is wrong\nCurrent order: " + string.Join(",", passStandard.ToArray()) + "\nExpected order: " +
            //        string.Join(",", order.ToArray()), CommentTypesEnum.Issue); TODO: USE NEW METHOD
            return Severity.Fail;
        }
    }
}

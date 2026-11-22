using BLMapCheck.Classes.Results;
using Parser.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class DiffSimilarity
    {
        // There has to be a significant difference from one difficulty to another. A difficulty at least 40% identical to a difficulty
        // in another mapset isn't rankable; other mapsets aren't known, so the same limit is applied within the mapset, as a warning.
        // Identical notes have the same beat, cell, color and direction. The share is of the difficulty with the most notes.
        private const double MaxIdentical = 0.4;
        private static readonly string[] Rankable = { "Standard", "OneSaber" };

        public static CritResult Check(CheckContext ctx, List<DifficultySet> difficulties)
        {
            CritResult criteria = CritResult.Success;

            var diffs = difficulties.Where(x => Rankable.Contains(x.Characteristic) && x.Data.Notes.Count >= 20)
                .Select(x => (Set: x, Notes: x.Data.Notes.Where(n => n.Color == 0 || n.Color == 1)
                    .Select(n => (Beat: Math.Round(n.Beats, 3), n.x, n.y, n.Color, n.CutDirection)).ToHashSet()))
                .ToList();

            for (int i = 0; i < diffs.Count; i++)
            {
                for (int j = i + 1; j < diffs.Count; j++)
                {
                    var a = diffs[i];
                    var b = diffs[j];
                    var identical = a.Notes.Count(n => b.Notes.Contains(n));
                    var share = (double)identical / Math.Max(a.Notes.Count, b.Notes.Count);
                    if (share < MaxIdentical) continue;

                    ctx.Results.CreateInfoResult("Difficulty Similarity", Severity.Warning, "SongInfo", "Difficulties must be significantly different, these share at least 40% identical notes",
                        new() { new("Difficulties", $"{a.Set.Characteristic} {a.Set.Difficulty}, {b.Set.Characteristic} {b.Set.Difficulty}"), new("Identical", Math.Round(share * 100) + "%") });
                    criteria = CritResult.Warning;
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateInfoResult("Difficulty Similarity", Severity.Passed, "SongInfo", "Difficulties are significantly different");
            }

            return criteria;
        }
    }
}

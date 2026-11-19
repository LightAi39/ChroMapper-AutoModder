using BLMapCheck.BeatmapScanner.CriteriaCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.Helper
{
    internal class DifficultyTimings
    {
        public static void Compare(CheckContext ctx, string characteristic, string difficulty, int difficultyRank, string targetChar, string targetDiff)
        {
            ctx.SetDifficulty(characteristic, difficulty, difficultyRank);

            DifficultyV3 target = ctx.Map.Difficulties.FirstOrDefault(x => x.Characteristic == targetChar && x.Difficulty == targetDiff)?.Data;
            DifficultyV3 current = ctx.Map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic)?.Data;
            if (current != null && target != null)
            {
                var targetBeats = target.Notes.Select(x => x.Beats).Distinct().OrderBy(x => x).ToList();
                foreach (var note in current.Notes)
                {
                    var index = targetBeats.BinarySearch(note.Beats);
                    if (index >= 0) continue;
                    // Index of the closest timing after this note
                    index = ~index;
                    List<Classes.Results.KeyValuePair> results = new();
                    if (index > 0) results.Add(new("Previous:", targetBeats[index - 1].ToString()));
                    if (index < targetBeats.Count) results.Add(new("Next:", targetBeats[index].ToString()));

                    ctx.Results.CreateDiffResult("Timing", Severity.Info, "Timing", "Timing doesn't exist in compared diff", results, new() { note });
                }
            }

        }
    }
}

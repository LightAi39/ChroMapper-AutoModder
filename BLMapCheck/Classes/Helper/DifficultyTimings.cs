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
        public static void Compare(string characteristic, string difficulty, int difficultyRank, string targetChar, string targetDiff)
        {
            CriteriaCheckManager.Difficulty = difficulty;
            CriteriaCheckManager.Characteristic = characteristic;
            CriteriaCheckManager.DifficultyRank = difficultyRank;

            DifficultyV3 target = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Characteristic == targetChar && x.Difficulty == targetDiff).Data;
            DifficultyV3 current = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;
            if (current != null && target != null)
            {
                foreach (var note in current.Notes)
                {
                    if (target.Notes.Exists(x => x.Beats == note.Beats)) continue;
                    var previous = target.Notes.Where(x => x.Beats < note.Beats)?.Select(x => x.Beats).DefaultIfEmpty().Aggregate((x, y) => Math.Abs(x - note.Beats) < Math.Abs(y - note.Beats) ? x : y);
                    var next = target.Notes.Where(x => x.Beats > note.Beats)?.Select(x => x.Beats).DefaultIfEmpty().Aggregate((x, y) => Math.Abs(x - note.Beats) < Math.Abs(y - note.Beats) ? x : y);
                    List<Classes.Results.KeyValuePair> results = new();
                    if (previous != null) results.Add(new("Previous:", previous.ToString()));
                    if (next != null) results.Add(new("Next:", next.ToString()));

                    CheckResults.Instance.CreateDiffResult("Timing", Severity.Info, "Timing", "Timing doesn't exist in compared diff", results, new() { note });
                }
            }

            CheckResults.Instance.CheckFinished = true;
        }
    }
}

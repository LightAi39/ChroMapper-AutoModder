using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class AngleOffset
    {
        // Flag existing notes with angle offset as info type
        public static void Check(string characteristic, string difficulty, List<Note> notes)
        {
            foreach (Note note in notes.Where(o => o.AngleOffset != 0))
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                    "AngleOffset Note", Severity.Info, "AngleOffset", "AngleOffset");
            }
        }
    }
}

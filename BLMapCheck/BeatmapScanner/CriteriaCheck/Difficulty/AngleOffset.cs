using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class AngleOffset
    {
        // Flag existing notes with angle offset as info type
        public static void Check(List<Note> notes)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "AngleOffset Note";
            string checkType = "AngleOffset";

            foreach (Note note in notes.Where(o => o.AngleOffset != 0))
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Info, checkType, checkType);
            }
        }
    }
}

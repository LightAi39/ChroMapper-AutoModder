using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal class AngleOffset
    {
        // Flag existing notes with angle offset as info type
        public static void Check(List<Note> notes)
        {
            if (notes.Any())
            {
                foreach (Note note in notes.Where(o => o.AngleOffset != 0))
                {
                    CheckResults.Instance.CreateDiffResult("AngleOffset Note", Severity.Data, "AngleOffset", "AngleOffset", 
                        new() { new("AngleOffset", note.AngleOffset.ToString()) }, new() { note });
                }
            }  
        }
    }
}

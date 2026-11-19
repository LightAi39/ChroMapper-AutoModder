using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal class AngleOffset
    {
        // Flag existing notes with angle offset as info type, 45 and -45 can be ignored
        public static void Check(CheckContext ctx, List<Note> notes)
        {
            if (notes.Any())
            {
                foreach (Note note in notes.Where(o => o.AngleOffset != 0 && !(ctx.Config.AngleOffsetIgnore45 && System.Math.Abs(o.AngleOffset) == 45)))
                {
                    ctx.Results.CreateDiffResult("AngleOffset Note", Severity.Data, "AngleOffset", "AngleOffset", 
                        new() { new("AngleOffset", note.AngleOffset.ToString()) }, new() { note });
                }
            }  
        }
    }
}

using BLMapCheck.Classes.Results;
using System;
using System.Linq;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal static class Offbeat
    {
        public static readonly float[] AllowedSnap = { 0, 0.125f, 0.167f, 0.25f, 0.333f, 0.375f, 0.5f, 0.625f, 0.667f, 0.75f, 0.833f, 0.875f };

        // Detect notes that are mapped to an uncommon precision.
        public static void Check()
        {
            var notes = NotesData.Where(n => (n.Note.Color == 0 || n.Note.Color == 1) && (n.Head || !n.Pattern)).ToList();

            if (notes.Count >= 2)
            {
                for (int i = 0; i < notes.Count - 1; i++)
                {
                    var note = notes[i];
                    var precision = (float)Math.Round(note.Note.Beats % 1, 3);
                    if (!AllowedSnap.Contains(precision))
                    {
                        var reality = RealToFraction(precision, 0.01);
                        CheckResults.Instance.CreateDiffResult("Offbeat Note", Severity.Data, "Offbeat", "Uncommon precision",
                                new() { new("Precision", reality.N.ToString() + "/" + reality.D.ToString()) }, new() { note.Note });
                    }
                }
            }
        }
    }
}

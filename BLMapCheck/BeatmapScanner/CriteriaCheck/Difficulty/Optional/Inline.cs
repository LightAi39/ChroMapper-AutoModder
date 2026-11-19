using beatleader_parser.VNJS;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal class Inline
    {
        // Detect and highlight notes that are too close to eachother, based on beat distance
        public static void Check(CheckContext ctx, List<Note> notes)
        {

            // Calculate the maximum distance in beats considered for inline
            double maxDistance = 0.251;
            if (ctx.Config.InlineBeatPrecision != 0) maxDistance = 1 / ctx.Config.InlineBeatPrecision + 0.001;

            // List to store already found inline
            HashSet<Note> highlighted = new();

            for (int j = 0; j < notes.Count; j++)
            {
                var note = notes[j];
                // Find all the existing inline for said note based on the precision in the config file. Notes are sorted by beat.
                List<Note> inlines = new();
                for (int k = j + 1; k < notes.Count && notes[k].Beats - note.Beats <= maxDistance; k++)
                {
                    var n = notes[k];
                    if (n.Beats - note.Beats > 0 && n.x == note.x && n.y == note.y) inlines.Add(n);
                }
                for (int i = 0; i < inlines.Count; i++)
                {
                    var inline = inlines[i];
                    // We only want to highlight once.
                    if (!highlighted.Contains(inline))
                    {
                        // Calculate the distance between the two closest notes in meters
                        float distance = 0;
                        if (i != 0) distance = ctx.BeatsToSeconds(inline.Beats - inlines[i - 1].Beats, inline.Beats) * inline.njs;
                        else distance = ctx.BeatsToSeconds(inline.Beats - note.Beats, inline.Beats) * inline.njs;
                        
                        ctx.Results.CreateDiffResult("Inline Note", Severity.Data, "Inline", "Inline", new() { new("Distance", Math.Round(distance, 3).ToString() + "m") }, new() { inline });

                        // Add to list of found inline
                        highlighted.Add(inline);
                    }
                }
            }
        }
    }
}

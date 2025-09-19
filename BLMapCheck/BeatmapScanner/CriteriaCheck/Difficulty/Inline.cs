using beatleader_parser.VNJS;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Inline
    {
        // Detect and highlight notes that are too close to eachother, based on beat distance
        public static void Check(List<Note> notes)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Inline Note";
            string checkType = "Inline";
            var timescale = CriteriaCheckManager.timescale;

            double maxDistance = 0.251;
            if (Configs.Config.Instance.InlineBeatPrecision != 0) maxDistance = (1 / Configs.Config.Instance.InlineBeatPrecision) + 0.001;

            // List to store already found inline
            List<Note> highlighted = new();

            foreach (var note in notes)
            {
                // Find all the existing inline for said note based on the precision in the config file.
                var inlines = notes.Where(n => n != note && n.Beats - note.Beats <= maxDistance && n.Beats - note.Beats > 0 && n.x == note.x && n.y == note.y).ToList();
                for (int i = 0; i < inlines.Count; i++)
                {
                    var inline = inlines[i];
                    // We only want to highlight once.
                    if (!highlighted.Contains(inline))
                    {
                        // Calculate the distance between the two closest notes in meters
                        float distance = 0;
                        if (i != 0) distance = (inline.Beats - inlines[i - 1].Beats) / (timescale.BPM.GetValue() / 60) * inline.njs;
                        else distance = (inline.Beats - note.Beats) / (timescale.BPM.GetValue() / 60) * inline.njs;
                        
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Info, checkType, checkType, new() { new("Distance", Math.Round(distance, 3).ToString() + "m") }, new() { inline });

                        // Add to list of found inline
                        highlighted.Add(inline);
                    }
                }
            }
        }
    }
}

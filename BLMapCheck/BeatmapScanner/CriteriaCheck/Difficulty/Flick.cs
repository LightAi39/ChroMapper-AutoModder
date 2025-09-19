using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Flick
    {
        // Detect and flag flicks, based on beat distance
        public static void Check(List<Note> notes)
        {
            if (notes.Any())
            {
                string characteristic = CriteriaCheckManager.Characteristic;
                string difficulty = CriteriaCheckManager.Difficulty;
                string name = "Flick Note";
                string checkType = "Flick";
                double maxDistance = 0.251f;
                if (Configs.Config.Instance.FlickBeatPrecision != 0) maxDistance = (1 / Configs.Config.Instance.FlickBeatPrecision) + 0.001;

                var red = NotesData.Where(n => n.Note.Color == 0 && (n.Head || !n.Pattern)).ToList();
                var blue = NotesData.Where(n => n.Note.Color == 1 && (n.Head || !n.Pattern)).ToList();

                List<NoteData> flicks = CheckDuration(red, maxDistance);
                flicks.AddRange(CheckDuration(blue, maxDistance));

                foreach (var flick in flicks)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty, name, Severity.Info, checkType, checkType, 
                        new List<Classes.Results.KeyValuePair>() { new("Maximum distance", (maxDistance - 0.001).ToString()) }, new() { flick.Note });
                }
            }
        }

        public static List<NoteData> CheckDuration(List<NoteData> notes, double maxDistance)
        {
            if (notes.Count > 2)
            {
                List<NoteData> flicks = new();

                if (notes[1].Note.Beats - notes[0].Note.Beats <= maxDistance && notes[1].Note.Beats - notes[0].Note.Beats > 0)
                {
                    if (notes[2].Note.Beats - notes[1].Note.Beats > maxDistance)
                    {
                        flicks.Add(notes[1]);
                    }
                }
                for (int i = 2; i < notes.Count - 1; i++)
                {
                    var prev = notes[i - 2];
                    var note = notes[i - 1];
                    var next = notes[i];
                    if (next.Note.Beats - note.Note.Beats <= maxDistance && next.Note.Beats - note.Note.Beats > 0)
                    {
                        if (note.Note.Beats - prev.Note.Beats > maxDistance && notes[i + 1].Note.Beats - next.Note.Beats > maxDistance) flicks.Add(next);
                    }
                    else if (i == notes.Count - 2)
                    {
                        if (notes.Last().Note.Beats - next.Note.Beats <= maxDistance && notes.Last().Note.Beats - next.Note.Beats > 0) flicks.Add(notes.Last());
                    }
                }

                return flicks;
            }

            return new();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Flick
    {
        // Detect and highlight flicks, based on beat distance
        public static void Check(List<Note> notes)
        {
            if (Configs.Config.Instance.DisplayFlick)
            {
                if (notes.Any())
                {
                    double maxDistance = 0.251;
                    if (Configs.Config.Instance.FlickBeatPrecision != 0) maxDistance = (1 / Configs.Config.Instance.FlickBeatPrecision) + 0.001;

                    var red = NotesData.Where(n => n.Note.Color == 0 && (n.Head || !n.Pattern)).ToList();
                    var blue = NotesData.Where(n => n.Note.Color == 1 && (n.Head || !n.Pattern)).ToList();

                    List<NoteData> flicks = new();
                    if(red.Count > 2)
                    {
                        if (red[1].Note.Beats - red[0].Note.Beats <= maxDistance && red[1].Note.Beats - red[0].Note.Beats > 0)
                        {
                            if (red[2].Note.Beats - red[1].Note.Beats > maxDistance)
                            {
                                flicks.Add(red[1]);
                            }
                        }
                        for (int i = 2; i < red.Count - 1; i++)
                        {
                            var prev = red[i - 2];
                            var note = red[i - 1];
                            var next = red[i];
                            if (next.Note.Beats - note.Note.Beats <= maxDistance && next.Note.Beats - note.Note.Beats > 0)
                            {
                                if (note.Note.Beats - prev.Note.Beats > maxDistance && red[i + 1].Note.Beats - next.Note.Beats > maxDistance) flicks.Add(next);
                            }
                            else if (i == red.Count - 2)
                            {
                                if (red.Last().Note.Beats - next.Note.Beats <= maxDistance && red.Last().Note.Beats - next.Note.Beats > 0) flicks.Add(red.Last());
                            }
                        }
                    }
                    if (blue.Count > 2)
                    {
                        if (blue[1].Note.Beats - blue[0].Note.Beats <= maxDistance && blue[1].Note.Beats - blue[0].Note.Beats > 0)
                        {
                            if (blue[2].Note.Beats - blue[1].Note.Beats > maxDistance)
                            {
                                flicks.Add(blue[1]);
                            }
                        }
                        for (int i = 2; i < blue.Count - 1; i++)
                        {
                            var prev = blue[i - 2];
                            var note = blue[i - 1];
                            var next = blue[i];
                            if (next.Note.Beats - note.Note.Beats <= maxDistance && next.Note.Beats - note.Note.Beats > 0)
                            {
                                if (note.Note.Beats - prev.Note.Beats > maxDistance && blue[i + 1].Note.Beats - next.Note.Beats > maxDistance)
                                {
                                    flicks.Add(next);
                                }
                            }
                            else if (i == blue.Count - 2)
                            {
                                if (blue.Last().Note.Beats - next.Note.Beats <= maxDistance && blue.Last().Note.Beats - next.Note.Beats > 0) flicks.Add(blue.Last());
                            }
                        }
                    }
                    
                    foreach (var flick in flicks)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Flick Note",
                            Severity = Severity.Info,
                            CheckType = "Flick",
                            Description = "Flick",
                            ResultData = new() { new("Maximum distance", (maxDistance - 0.001).ToString()) },
                            BeatmapObjects = new() { flick.Note }
                        });
                    }
                }
            }
        }
    }
}

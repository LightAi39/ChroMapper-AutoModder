using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Handclap
    {
        // Attempt to detect specific note and angle placement based on BeatLeader criteria
        public static CritResult Check(List<Note> notes)
        {
            var issue = CritResult.Success;

            if (notes.Any())
            {
                Note previous = notes[0];
                Note[] lastNote = { null, null };
                List<List<Note>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<Note>();
                for (int i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (note.CutDirection == 8)
                    {
                        continue;
                    }
                    if (lastNote[note.Color] != null)
                    {
                        if (note.Beats != lastNote[note.Color].Beats)
                        {
                            swingNoteArray[note.Color].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Color + 1) % 2])
                    {
                        if (other.CutDirection == 8)
                        {
                            continue;
                        }
                        if (other.Color != 0 && other.Color != 1)
                        {
                            continue;
                        }
                        if (note.Beats != other.Beats)
                        {
                            continue;
                        }
                        var d = Math.Sqrt(Math.Pow(note.x - other.x, 2) + Math.Pow(note.y - other.y, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {

                            if (other.x == note.x)
                            {
                                if ((SwingType.Up.Contains(note.CutDirection) && SwingType.Down.Contains(other.CutDirection)) ||
                                    (SwingType.Down.Contains(note.CutDirection) && SwingType.Up.Contains(other.CutDirection)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                            else if (other.y == note.y)
                            {
                                if ((SwingType.Left.Contains(note.CutDirection) && SwingType.Right.Contains(other.CutDirection)) ||
                                    (SwingType.Right.Contains(note.CutDirection) && SwingType.Left.Contains(other.CutDirection)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d >= 1.001 && d < 2) // Diagonal
                        {
                            if (((note.CutDirection == 6 && note.y > other.y && note.x > other.x) ||
                                (note.CutDirection == 7 && note.y > other.y && note.x < other.x) ||
                                (note.CutDirection == 4 && note.y < other.y && note.x > other.x) ||
                                (note.CutDirection == 5 && note.y < other.y && note.x < other.x)) && Reverse.Get(note.CutDirection) == other.CutDirection)
                            {
                                arr.Add(other);
                                arr.Add(note);
                                break;
                            }
                        }
                        else if (d >= 2 && d <= 2.99) // 1-2 wide
                        {
                            if (NoteDirection.Move(note) == NoteDirection.Move(other))
                            {
                                if ((note.Color == 0 && note.x > other.x) || (note.Color == 1 && note.x < other.x)) // Crossover
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                                else if ((note.x == other.x + 2 && note.y == other.y + 2) || (other.x == note.x + 2 && other.y == note.y + 2) // Facing directly
                                || (note.x == other.x + 2 && note.y == other.y - 2) || (other.x == note.x + 2 && other.y == note.y - 2)
                                || (note.x == other.x && note.y == other.y + 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (note.x == other.x && note.y == other.y - 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (other.y == note.y && other.x == note.x + 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (other.y == note.y && other.x == note.x - 2 && Reverse.Get(note.CutDirection) == other.CutDirection))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d > 2.99 && ((note.Color == 0 && note.x > 2) || (note.Color == 1 && note.x < 1))) // 3-wide
                        {
                            // TODO: This is trash, could easily be done better
                            if (other.y == note.y)
                            {
                                if (((SwingType.Up_Left.Contains(note.CutDirection) && SwingType.Up_Right.Contains(other.CutDirection) && note.Color == 1) ||
                                    (SwingType.Up_Right.Contains(note.CutDirection) && SwingType.Up_Left.Contains(other.CutDirection) && note.Color == 0)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                                if ((SwingType.Down_Left.Contains(note.CutDirection) && SwingType.Down_Right.Contains(other.CutDirection) && note.Color == 1) ||
                                (SwingType.Down_Right.Contains(note.CutDirection) && SwingType.Down_Left.Contains(other.CutDirection) && note.Color == 0))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }

                            }
                        }
                    }
                    lastNote[note.Color] = note;
                    swingNoteArray[note.Color].Add(note);
                }

                foreach (var item in arr)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hand Clap",
                        Severity = Severity.Warning,
                        CheckType = "Handclap",
                        Description = "Patterns must not encourage hand clapping.",
                        ResultData = new() { new("Handclap", "Warning") },
                        BeatmapObjects = new() { item }
                    });
                    issue = CritResult.Warning;
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Hand Clap",
                    Severity = Severity.Passed,
                    CheckType = "Handclap",
                    Description = "No handclap pattern detected.",
                    ResultData = new(),
                });
            }

            return issue;
        }
    }
}

using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Handclap
    {
        // Attempt to detect specific note and angle placement based on BeatLeader criteria
        public static CritResult Check(List<Colornote> Notes)
        {
            var issue = CritResult.Success;

            if (Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                Colornote previous = Notes[0];
                Colornote[] lastNote = { null, null };
                List<List<Colornote>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<Colornote>();
                var arr2 = new List<Colornote>();
                for (int i = 0; i < Notes.Count; i++)
                {
                    var note = Notes[i];
                    if (note.d == 8)
                    {
                        continue;
                    }
                    if (lastNote[note.c] != null)
                    {
                        if (note.b != lastNote[note.c].b)
                        {
                            swingNoteArray[note.c].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.c + 1) % 2])
                    {
                        if (other.d == 8)
                        {
                            continue;
                        }
                        if (other.c != 0 && other.c != 1)
                        {
                            continue;
                        }
                        if (note.b != other.b)
                        {
                            continue;
                        }
                        var d = Math.Sqrt(Math.Pow(note.x - other.x, 2) + Math.Pow(note.y - other.y, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {

                            if (other.x == note.x)
                            {
                                if ((SwingType.Up.Contains(note.d) && SwingType.Down.Contains(other.d)) ||
                                    (SwingType.Down.Contains(note.d) && SwingType.Up.Contains(other.d)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                            else if (other.y == note.y)
                            {
                                if ((SwingType.Left.Contains(note.d) && SwingType.Right.Contains(other.d)) ||
                                    (SwingType.Right.Contains(note.d) && SwingType.Left.Contains(other.d)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d >= 1.001 && d < 2) // Diagonal
                        {
                            if (((note.d == 6 && note.y > other.y && note.x > other.x) ||
                                (note.d == 7 && note.y > other.y && note.x < other.x) ||
                                (note.d == 4 && note.y < other.y && note.x > other.x) ||
                                (note.d == 5 && note.y < other.y && note.x < other.x)) && Reverse.Get(note.d) == other.d)
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
                                if ((note.c == 0 && note.x > other.x) || (note.c == 1 && note.x < other.x)) // Crossover
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                                else if ((note.x == other.x + 2 && note.y == other.y + 2) || (other.x == note.x + 2 && other.y == note.y + 2) // Facing directly
                                || (note.x == other.x + 2 && note.y == other.y - 2) || (other.x == note.x + 2 && other.y == note.y - 2)
                                || (note.x == other.x && note.y == other.y + 2 && Reverse.Get(note.d) == other.d) ||
                                (note.x == other.x && note.y == other.y - 2 && Reverse.Get(note.d) == other.d) ||
                                (other.y == note.y && other.x == note.x + 2 && Reverse.Get(note.d) == other.d) ||
                                (other.y == note.y && other.x == note.x - 2 && Reverse.Get(note.d) == other.d))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d > 2.99 && ((note.c == 0 && note.x > 2) || (note.c == 1 && note.x < 1))) // 3-wide
                        {
                            // TODO: This is trash, could easily be done better
                            if (other.y == note.y)
                            {
                                if (((SwingType.Up_Left.Contains(note.d) && SwingType.Up_Right.Contains(other.d) && note.c == 1) ||
                                    (SwingType.Up_Right.Contains(note.d) && SwingType.Up_Left.Contains(other.d) && note.c == 0)))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }
                                if ((SwingType.Down_Left.Contains(note.d) && SwingType.Down_Right.Contains(other.d) && note.c == 1) ||
                                (SwingType.Down_Right.Contains(note.d) && SwingType.Down_Left.Contains(other.d) && note.c == 0))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }

                            }
                        }
                    }
                    lastNote[note.c] = note;
                    swingNoteArray[note.c].Add(note);
                }

                foreach (var item in arr2)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Hand Clap",
                        Severity = Severity.Warning,
                        CheckType = "Chain",
                        Description = "Patterns must not encourage hand clapping.",
                        ResultData = new() { new("Handclap", "True") },
                        BeatmapObjects = new() { item }
                    });
                    issue = CritResult.Warning;
                }

                foreach (var item in arr)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Hand Clap",
                        Severity = Severity.Error,
                        CheckType = "Handclap",
                        Description = "Patterns must not encourage hand clapping.",
                        ResultData = new() { new("Handclap", "True") },
                        BeatmapObjects = new() { item }
                    });
                    issue = CritResult.Fail;
                }
            }

            return issue;
        }
    }
}

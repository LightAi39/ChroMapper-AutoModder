using BLMapCheck.BeatmapScanner.MapCheck;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using System.Collections.Generic;
using System.Linq;
using System;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Hitbox
    {
        // Implementation of Kival Evan hitboxInline.ts, hitboxStair.ts and hitboxReverseStaircase.ts
        public static CritResult HitboxCheck(List<Note> notes, float njs)
        {
            var issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            if (notes.Any())
            {
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
                    if (lastNote[note.Color] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Color], timescale.BPM.GetValue(), swingNoteArray[note.Color]))
                        {
                            swingNoteArray[note.Color].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Color + 1) % 2])
                    {
                        var isInline = false;
                        var distance = Math.Sqrt(Math.Pow(note.x - other.x, 2) + Math.Pow(note.y - other.y, 2));
                        if (distance <= 0.5)
                        {
                            isInline = true;
                        }
                        if (njs < 1.425 / ((60 * (note.Beats - other.Beats)) / timescale.BPM.GetValue()) && isInline)
                        {
                            arr.Add(note);
                            break;
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
                        Name = "Inline",
                        Severity = Severity.Info,
                        CheckType = "Hitbox",
                        Description = "Low NJS Inline.",
                        ResultData = new(),
                        BeatmapObjects = new() { item }
                    });
                }

                var hitboxTime = (0.15 * timescale.BPM.GetValue()) / 60;
                int[] lastNoteDirection = { -1, -1 };
                double[] lastSpeed = { -1, -1 };
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                NoteData[] noteOccupy = { new(), new() };
                arr.Clear();
                for (int i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (lastNote[note.Color] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Color], timescale.BPM.GetValue(), swingNoteArray[note.Color]))
                        {
                            lastSpeed[note.Color] = note.Beats - lastNote[note.Color].Beats;
                            if (note.CutDirection != NoteDirection.ANY)
                            {
                                noteOccupy[note.Color].Line = note.x + NoteDirectionSpace.Get(note.CutDirection)[0];
                                noteOccupy[note.Color].Layer = note.y + NoteDirectionSpace.Get(note.CutDirection)[1];
                            }
                            else
                            {
                                noteOccupy[note.Color].Line = -1;
                                noteOccupy[note.Color].Layer = -1;
                            }
                            swingNoteArray[note.Color].Clear();
                            lastNoteDirection[note.Color] = note.CutDirection;
                        }
                        else if (MapCheck.Parity.IsEnd(note, lastNote[note.Color], lastNoteDirection[note.Color]))
                        {
                            if (note.CutDirection != NoteDirection.ANY)
                            {
                                noteOccupy[note.Color].Line = note.x + NoteDirectionSpace.Get(note.CutDirection)[0];
                                noteOccupy[note.Color].Layer = note.y + NoteDirectionSpace.Get(note.CutDirection)[1];
                                lastNoteDirection[note.Color] = note.CutDirection;
                            }
                            else
                            {
                                noteOccupy[note.Color].Line = note.x + NoteDirectionSpace.Get(lastNoteDirection[note.Color])[0];
                                noteOccupy[note.Color].Layer = note.y + NoteDirectionSpace.Get(lastNoteDirection[note.Color])[1];
                            }
                        }
                        if (lastNote[(note.Color + 1) % 2] != null)
                        {
                            if (note.Beats - lastNote[(note.Color + 1) % 2].Beats != 0 &&
                                note.Beats - lastNote[(note.Color + 1) % 2].Beats < Math.Min(hitboxTime, lastSpeed[(note.Color + 1) % 2]))
                            {
                                if (note.x == noteOccupy[(note.Color + 1) % 2].Line && note.y == noteOccupy[(note.Color + 1) % 2].Layer && !Swing.IsDouble(note, notes, i))
                                {
                                    arr.Add(note);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (note.CutDirection != NoteDirection.ANY)
                        {
                            noteOccupy[note.Color].Line = note.x + NoteDirectionSpace.Get(note.CutDirection)[0];
                            noteOccupy[note.Color].Layer = note.y + NoteDirectionSpace.Get(note.CutDirection)[1];
                        }
                        else
                        {
                            noteOccupy[note.Color].Line = -1;
                            noteOccupy[note.Color].Layer = -1;
                        }
                        lastNoteDirection[note.Color] = note.CutDirection;
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
                        Name = "Staircase",
                        Severity = Severity.Warning,
                        CheckType = "Staircase",
                        Description = "Potential Hitbox issue.",
                        ResultData = new() { new("Type", "Staircase") },
                        BeatmapObjects = new() { item }
                    });
                    issue = CritResult.Warning;
                }

                var constant = 0.03414823529;
                var constantDiagonal = 0.03414823529;
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                arr.Clear();
                for (int i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (lastNote[note.Color] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Color], timescale.BPM.GetValue(), swingNoteArray[note.Color]))
                        {
                            swingNoteArray[note.Color].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Color + 1) % 2])
                    {
                        if (other.Color != 0 && other.Color != 1)
                        {
                            continue;
                        }
                        if (other.CutDirection != NoteDirection.ANY)
                        {
                            if (!((note.Beats / timescale.BPM.GetValue() * 60) > (other.Beats / timescale.BPM.GetValue() * 60) + 0.01))
                            {
                                continue;
                            }
                            var isDiagonal = Swing.NoteDirectionAngle[other.CutDirection] % 90 > 15 && Swing.NoteDirectionAngle[other.CutDirection] % 90 < 75;
                            double[,] value = { { 15, 1.5 } };
                            if (njs < 1.425 / ((60 * (note.Beats - other.Beats)) / timescale.BPM.GetValue() + (isDiagonal ? constantDiagonal : constant)) &&
                                Swing.IsIntersect(note, other, value, 1))
                            {
                                arr.Add(other);
                                break;
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
                        Name = "Reverse Staircase",
                        Severity = Severity.Warning,
                        CheckType = "Staircase",
                        Description = "Potential Hitbox issue.",
                        ResultData = new() { new("Type", "Reverse Staircase") },
                        BeatmapObjects = new() { item }
                    });
                    issue = CritResult.Warning;
                }
            }

            if(issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Staircase",
                    Severity = Severity.Passed,
                    CheckType = "Staircase",
                    Description = "No staircase issue detected.",
                    ResultData = new(),
                });
            }

            return issue;
        }

    }
}

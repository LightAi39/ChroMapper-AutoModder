using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Hitbox
    {
        // Implementation of Kival Evan hitboxInline.ts, hitboxStair.ts and hitboxReverseStaircase.ts
        public static CritResult HitboxCheck(List<Note> notes, float njs)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            CritResult criteria = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            if (notes.Any())
            {
                // Store last left and right note
                Note[] lastNote = { null, null };
                List<List<Note>> swingNoteArray = new()
                {
                    new(),
                    new()
                };

                // Store potential hitbox issue
                var hitbox = new List<Note>();

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
                            hitbox.Add(note);
                            break;
                        }
                    }
                    lastNote[note.Color] = note;
                    swingNoteArray[note.Color].Add(note);
                }

                foreach (var item in hitbox)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Inline", Severity.Warning, "Hitbox", "Low NJS Inline", new(), new() { item });
                    criteria = CritResult.Warning;
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
                hitbox.Clear();

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
                                    hitbox.Add(note);
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

                foreach (var item in hitbox)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Staircase", Severity.Warning, "Staircase", "Potential Hitbox issue",
                        new List<Classes.Results.KeyValuePair>() { new("Type", "Staircase") }, new() { item });
                    criteria = CritResult.Warning;
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
                hitbox.Clear();

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
                                hitbox.Add(other);
                                break;
                            }
                        }

                    }

                    lastNote[note.Color] = note;
                    swingNoteArray[note.Color].Add(note);
                }

                foreach (var item in hitbox)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Reverse Staircase", Severity.Warning, "Staircase", "Potential Hitbox issue", 
                        new List<Classes.Results.KeyValuePair>() { new("Type", "Reverse Staircase") }, new() { item });
                    criteria = CritResult.Warning;
                }
            }

            if(criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Staircase", Severity.Passed, "Staircase", "No hitbox issue detected");
            }

            return criteria;
        }
    }
}

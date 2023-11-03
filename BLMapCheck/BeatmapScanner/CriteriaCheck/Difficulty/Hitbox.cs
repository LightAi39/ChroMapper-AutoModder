using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.ChroMapper;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using System.Collections.Generic;
using System.Linq;
using System;
using BLMapCheck.Classes.MapVersion.Difficulty;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Hitbox
    {
        // Implementation of Kival Evan hitboxInline.ts, hitboxStair.ts and hitboxReverseStaircase.ts
        public static CritSeverity HitboxCheck(List<Colornote> Notes, float NoteJumpSpeed)
        {
            var issue = CritSeverity.Success;

            if (Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                Colornote[] lastNote = { null, null };
                List<List<Colornote>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<Colornote>();

                for (int i = 0; i < Notes.Count; i++)
                {
                    var note = Notes[i];
                    if (lastNote[note.c] != null)
                    {
                        if (Swing.Next(note, lastNote[note.c], BeatPerMinute.BPM.GetValue(), swingNoteArray[note.c]))
                        {
                            swingNoteArray[note.c].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.c + 1) % 2])
                    {
                        var isInline = false;
                        var distance = Math.Sqrt(Math.Pow(note.x - other.x, 2) + Math.Pow(note.y - other.y, 2));
                        if (distance <= 0.5)
                        {
                            isInline = true;
                        }
                        if (NoteJumpSpeed < 1.425 / ((60 * (note.b - other.b)) / BeatPerMinute.BPM.GetValue()) && isInline)
                        {
                            arr.Add(note);
                            break;
                        }
                    }
                    lastNote[note.c] = note;
                    swingNoteArray[note.c].Add(note);
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3G - Low NJS Inline", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.b && c.c == item.c
                    //                    && item.x == c.Line && item.y == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }

                var hitboxTime = (0.15 * BeatPerMinute.BPM.GetValue()) / 60;
                int[] lastNoteDirection = { -1, -1 };
                double[] lastSpeed = { -1, -1 };
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                Cube[] noteOccupy = { new(), new() };
                arr.Clear();
                for (int i = 0; i < Notes.Count; i++)
                {
                    var note = Notes[i];
                    if (lastNote[note.c] != null)
                    {
                        if (Swing.Next(note, lastNote[note.c], BeatPerMinute.BPM.GetValue(), swingNoteArray[note.c]))
                        {
                            lastSpeed[note.c] = note.b - lastNote[note.c].b;
                            if (note.d != NoteDirection.ANY)
                            {
                                noteOccupy[note.c].Line = note.x + NoteDirectionSpace.Get(note.d)[0];
                                noteOccupy[note.c].Layer = note.y + NoteDirectionSpace.Get(note.d)[1];
                            }
                            else
                            {
                                noteOccupy[note.c].Line = -1;
                                noteOccupy[note.c].Layer = -1;
                            }
                            swingNoteArray[note.c].Clear();
                            lastNoteDirection[note.c] = note.d;
                        }
                        else if (MapCheck.Parity.IsEnd(note, lastNote[note.c], lastNoteDirection[note.c]))
                        {
                            if (note.d != NoteDirection.ANY)
                            {
                                noteOccupy[note.c].Line = note.x + NoteDirectionSpace.Get(note.d)[0];
                                noteOccupy[note.c].Layer = note.y + NoteDirectionSpace.Get(note.d)[1];
                                lastNoteDirection[note.c] = note.d;
                            }
                            else
                            {
                                noteOccupy[note.c].Line = note.x + NoteDirectionSpace.Get(lastNoteDirection[note.c])[0];
                                noteOccupy[note.c].Layer = note.y + NoteDirectionSpace.Get(lastNoteDirection[note.c])[1];
                            }
                        }
                        if (lastNote[(note.c + 1) % 2] != null)
                        {
                            if (note.b - lastNote[(note.c + 1) % 2].b != 0 &&
                                note.b - lastNote[(note.c + 1) % 2].b < Math.Min(hitboxTime, lastSpeed[(note.c + 1) % 2]))
                            {
                                if (note.x == noteOccupy[(note.c + 1) % 2].Line && note.y == noteOccupy[(note.c + 1) % 2].Layer && !Swing.IsDouble(note, Notes, i))
                                {
                                    arr.Add(note);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (note.d != NoteDirection.ANY)
                        {
                            noteOccupy[note.c].Line = note.x + NoteDirectionSpace.Get(note.d)[0];
                            noteOccupy[note.c].Layer = note.y + NoteDirectionSpace.Get(note.d)[1];
                        }
                        else
                        {
                            noteOccupy[note.c].Line = -1;
                            noteOccupy[note.c].Layer = -1;
                        }
                        lastNoteDirection[note.c] = note.d;
                    }
                    lastNote[note.c] = note;
                    swingNoteArray[note.c].Add(note);
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3G - Staircase", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.b && c.c == item.c
                    //                    && item.x == c.Line && item.y == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
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
                for (int i = 0; i < Notes.Count; i++)
                {
                    var note = Notes[i];
                    if (lastNote[note.c] != null)
                    {
                        if (Swing.Next(note, lastNote[note.c], BeatPerMinute.BPM.GetValue(), swingNoteArray[note.c]))
                        {
                            swingNoteArray[note.c].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.c + 1) % 2])
                    {
                        if (other.c != 0 && other.c != 1)
                        {
                            continue;
                        }
                        if (other.d != NoteDirection.ANY)
                        {
                            if (!((note.b / BeatPerMinute.BPM.GetValue() * 60) > (other.b / BeatPerMinute.BPM.GetValue() * 60) + 0.01))
                            {
                                continue;
                            }
                            var isDiagonal = Swing.NoteDirectionAngle[other.d] % 90 > 15 && Swing.NoteDirectionAngle[other.d] % 90 < 75;
                            double[,] value = { { 15, 1.5 } };
                            if (NoteJumpSpeed < 1.425 / ((60 * (note.b - other.b)) / BeatPerMinute.BPM.GetValue() + (isDiagonal ? constantDiagonal : constant)) &&
                                Swing.IsIntersect(note, other, value, 1))
                            {
                                arr.Add(other);
                                break;
                            }
                        }

                    }
                    lastNote[note.c] = note;
                    swingNoteArray[note.c].Add(note);
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3G - Reverse Staircase", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.b && c.c == item.c
                    //                    && item.x == c.Line && item.y == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }
            }

            return issue;
        }

    }
}

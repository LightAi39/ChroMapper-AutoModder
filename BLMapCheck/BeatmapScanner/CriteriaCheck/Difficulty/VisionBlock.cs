using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class VisionBlock
    {
        // Detect notes and bombs VB based on BeatLeader current criteria
        // Most of the minimum and maximum duration are configurable
        public static CritResult Check(List<Colornote> Notes, List<Bombnote> Bombs, double PassRating, double TechRating)
        {
            CritResult issue = CritResult.Success;
            List<BeatmapGridObject> beatmapGridObjects = new();
            beatmapGridObjects.AddRange(Notes);
            beatmapGridObjects.AddRange(Bombs);
            if (beatmapGridObjects.Any())
            {
                List<BeatmapGridObject> lastMidL = new();
                List<BeatmapGridObject> lastMidR = new();
                List<BeatmapGridObject> arr = new();
                for (var i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var note = beatmapGridObjects[i];
                    BeatPerMinute.BPM.SetCurrentBPM(note.b);
                    var MaxBottomNoteTime = BeatPerMinute.BPM.ToBeatTime((float)VBMinBottomNoteTime);
                    var MaxOuterNoteTime = BeatPerMinute.BPM.ToBeatTime((float)VBMaxOuterNoteTime);
                    var Overall = BeatPerMinute.BPM.ToBeatTime((float)VBMinimum);
                    var MinTimeWarning = BeatPerMinute.BPM.ToBeatTime((float)((800 - 300) * Math.Pow(Math.E, -PassRating / 7.6 - TechRating * 0.04) + 300) / 1000);
                    lastMidL.RemoveAll(l => note.b - l.b > MinTimeWarning);
                    lastMidR.RemoveAll(l => note.b - l.b > MinTimeWarning);
                    if (lastMidL.Count > 0)
                    {
                        if (note.b - lastMidL.First().b >= Overall) // Further than 0.025
                        {
                            if (note.b - lastMidL.First().b <= MinTimeWarning) // Warning
                            {
                                if (note.x == 0 && note.b - lastMidL.First().b <= MaxOuterNoteTime) // Closer than 0.15 in outer lane
                                {
                                    // Fine
                                }
                                else if (note.x == 1 && note.y == 0 && note.b - lastMidL.First().b <= MaxBottomNoteTime) // Closer than 0.075 at bottom layer
                                {
                                    // Also fine
                                }
                                else if (note.x < 2)
                                {
                                    arr.Add(note);
                                    if (note is Colornote)
                                    {
                                        CheckResults.Instance.AddResult(new CheckResult()
                                        {
                                            Characteristic = CriteriaCheckManager.Characteristic,
                                            Difficulty = CriteriaCheckManager.Difficulty,
                                            Name = "Vision Block",
                                            Severity = Severity.Warning,
                                            CheckType = "Vision",
                                            Description = "Notes must be placed to give the player acceptable time to react.",
                                            ResultData = new() { new("VisionBlock", "Possible VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                            BeatmapObjects = new() { note }
                                        });
                                        issue = CritResult.Warning;
                                    }
                                }
                            }
                        }
                    }
                    if (lastMidR.Count > 0)
                    {
                        if (note.b - lastMidR.First().b >= Overall)
                        {
                            if (note.b - lastMidR.First().b <= MinTimeWarning)
                            {
                                if (note.x == 3 && note.b - lastMidR.First().b <= MaxOuterNoteTime)
                                {
                                    // Fine
                                }
                                else if (note.x == 2 && note.y == 0 && note.b - lastMidR.First().b <= MaxBottomNoteTime)
                                {
                                    // Also fine
                                }
                                else if (note.x > 1)
                                {
                                    arr.Add(note);
                                    if (note is Colornote)
                                    {
                                        CheckResults.Instance.AddResult(new CheckResult()
                                        {
                                            Characteristic = CriteriaCheckManager.Characteristic,
                                            Difficulty = CriteriaCheckManager.Difficulty,
                                            Name = "Vision Block",
                                            Severity = Severity.Warning,
                                            CheckType = "Vision",
                                            Description = "Notes must be placed to give the player acceptable time to react.",
                                            ResultData = new() { new("VisionBlock", "Possible VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                            BeatmapObjects = new() { note }
                                        });
                                        issue = CritResult.Warning;
                                    }
                                }
                            }
                        }
                    }
                    if (note.y == 1 && note.x == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.y == 1 && note.x == 2)
                    {
                        lastMidR.Add(note);
                    }
                }

                // Bombs
                lastMidL = new List<BeatmapGridObject>();
                lastMidR = new List<BeatmapGridObject>();
                arr = new();
                for (var i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var note = beatmapGridObjects[i];
                    if (note is Bombnote)
                    {
                        BeatPerMinute.BPM.SetCurrentBPM(note.b);
                        var MaxTimeBomb = BeatPerMinute.BPM.ToBeatTime((float)VBMaxBombTime);
                        var MinTimeBomb = BeatPerMinute.BPM.ToBeatTime((float)VBMinBombTime);
                        var Overall = BeatPerMinute.BPM.ToBeatTime((float)VBMinimum);
                        var left = (Colornote)beatmapGridObjects.Where(x => x.b < note.b && x is Colornote no && no.c == 0).OrderBy(o => o.b).LastOrDefault();
                        var right = (Colornote)beatmapGridObjects.Where(x => x.b < note.b && x is Colornote no && no.c == 1).OrderBy(o => o.b).LastOrDefault();
                        lastMidL.RemoveAll(l => note.b - l.b > MinTimeBomb);
                        lastMidR.RemoveAll(l => note.b - l.b > MinTimeBomb);
                        if (lastMidL.Count > 0)
                        {
                            if (note.b - lastMidL.First().b <= MinTimeBomb) // Closer than 0.20
                            {
                                if (note.x == 0 && note.b - lastMidL.First().b <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((note.x != 1 || note.y != 1) && note.b - lastMidL.First().b <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (note.x < 2)
                                {
                                    if (left != null)
                                    {
                                        if (left.d == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.x - left.x, 2) + Math.Pow(note.y - left.y, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CheckResults.Instance.AddResult(new CheckResult()
                                                {
                                                    Characteristic = CriteriaCheckManager.Characteristic,
                                                    Difficulty = CriteriaCheckManager.Difficulty,
                                                    Name = "Vision Block",
                                                    Severity = Severity.Error,
                                                    CheckType = "Vision",
                                                    Description = "Bombs must be placed to give the player acceptable time to react.",
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { note }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.x, left.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.d))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.x - pos.x, 2) + Math.Pow(note.y - pos.y, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CheckResults.Instance.AddResult(new CheckResult()
                                            {
                                                Characteristic = CriteriaCheckManager.Characteristic,
                                                Difficulty = CriteriaCheckManager.Difficulty,
                                                Name = "Vision Block",
                                                Severity = Severity.Error,
                                                CheckType = "Vision",
                                                Description = "Bombs must be placed to give the player acceptable time to react.",
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { note }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.d == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.x - right.x, 2) + Math.Pow(note.y - right.y, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CheckResults.Instance.AddResult(new CheckResult()
                                                {
                                                    Characteristic = CriteriaCheckManager.Characteristic,
                                                    Difficulty = CriteriaCheckManager.Difficulty,
                                                    Name = "Vision Block",
                                                    Severity = Severity.Error,
                                                    CheckType = "Vision",
                                                    Description = "Bombs must be placed to give the player acceptable time to react.",
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { note }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.x, right.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.d))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.x - pos.x, 2) + Math.Pow(note.y - pos.y, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CheckResults.Instance.AddResult(new CheckResult()
                                            {
                                                Characteristic = CriteriaCheckManager.Characteristic,
                                                Difficulty = CriteriaCheckManager.Difficulty,
                                                Name = "Vision Block",
                                                Severity = Severity.Error,
                                                CheckType = "Vision",
                                                Description = "Bombs must be placed to give the player acceptable time to react.",
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { note }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        if (lastMidR.Count > 0)
                        {
                            if (note.b - lastMidR.First().b <= MinTimeBomb) // Closer than 0.20
                            {
                                if (note.x == 3 && note.b - lastMidR.First().b <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((note.x != 2 || note.y != 1) && note.b - lastMidR.First().b <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (note.x > 1)
                                {
                                    if (left != null)
                                    {
                                        if (left.d == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.x - left.x, 2) + Math.Pow(note.y - left.y, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CheckResults.Instance.AddResult(new CheckResult()
                                                {
                                                    Characteristic = CriteriaCheckManager.Characteristic,
                                                    Difficulty = CriteriaCheckManager.Difficulty,
                                                    Name = "Vision Block",
                                                    Severity = Severity.Error,
                                                    CheckType = "Vision",
                                                    Description = "Bombs must be placed to give the player acceptable time to react.",
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { note }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.x, left.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.d))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.x - pos.x, 2) + Math.Pow(note.y - pos.y, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CheckResults.Instance.AddResult(new CheckResult()
                                            {
                                                Characteristic = CriteriaCheckManager.Characteristic,
                                                Difficulty = CriteriaCheckManager.Difficulty,
                                                Name = "Vision Block",
                                                Severity = Severity.Error,
                                                CheckType = "Vision",
                                                Description = "Bombs must be placed to give the player acceptable time to react.",
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { note }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.d == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.x - right.x, 2) + Math.Pow(note.y - right.y, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CheckResults.Instance.AddResult(new CheckResult()
                                                {
                                                    Characteristic = CriteriaCheckManager.Characteristic,
                                                    Difficulty = CriteriaCheckManager.Difficulty,
                                                    Name = "Vision Block",
                                                    Severity = Severity.Error,
                                                    CheckType = "Vision",
                                                    Description = "Bombs must be placed to give the player acceptable time to react.",
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { note }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.x, right.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.d))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.x - pos.x, 2) + Math.Pow(note.y - pos.y, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CheckResults.Instance.AddResult(new CheckResult()
                                            {
                                                Characteristic = CriteriaCheckManager.Characteristic,
                                                Difficulty = CriteriaCheckManager.Difficulty,
                                                Name = "Vision Block",
                                                Severity = Severity.Error,
                                                CheckType = "Vision",
                                                Description = "Bombs must be placed to give the player acceptable time to react.",
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(BeatPerMinute.BPM.ToRealTime(note.b - lastMidL.First().b) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { note }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (note.y == 1 && note.x == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.y == 1 && note.x == 2)
                    {
                        lastMidR.Add(note);
                    }
                }
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }

    }
}

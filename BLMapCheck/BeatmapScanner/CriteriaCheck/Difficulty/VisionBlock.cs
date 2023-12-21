using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class VisionBlock
    {
        // Detect notes and bombs VB based on BeatLeader current criteria
        // Most of the minimum and maximum duration are configurable
        public static CritResult Check(List<BeatmapGridObject> beatmapGridObjects, double pass, double tech)
        {
            CritResult issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;
            if (beatmapGridObjects.Any())
            {
                List<BeatmapGridObject> lastMidL = new();
                List<BeatmapGridObject> lastMidR = new();
                List<BeatmapGridObject> arr = new();
                for (var i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var note = beatmapGridObjects[i];
                    timescale.BPM.SetCurrentBPM(note.Beats);
                    var MaxBottomNoteTime = timescale.BPM.ToBeatTime((float)Instance.VBMinBottomNoteTime);
                    var MaxOuterNoteTime = timescale.BPM.ToBeatTime((float)Instance.VBMaxOuterNoteTime);
                    var Overall = timescale.BPM.ToBeatTime((float)Instance.VBMinimum);
                    var MinTimeWarning = timescale.BPM.ToBeatTime((float)((800 - 300) * Math.Pow(Math.E, -pass / 7.6 - tech * 10 * 0.04) + 300) / 1000);
                    lastMidL.RemoveAll(l => note.Beats - l.Beats > MinTimeWarning);
                    lastMidR.RemoveAll(l => note.Beats - l.Beats > MinTimeWarning);
                    if (lastMidL.Count > 0)
                    {
                        if (note.Beats - lastMidL.First().Beats >= Overall) // Further than 0.025
                        {
                            if (note.Beats - lastMidL.First().Beats <= MinTimeWarning) // Warning
                            {
                                if (note.x == 0 && note.Beats - lastMidL.First().Beats <= MaxOuterNoteTime) // Closer than 0.15 in outer lane
                                {
                                    // Fine
                                }
                                else if (note.x == 1 && note.y == 0 && note.Beats - lastMidL.First().Beats <= MaxBottomNoteTime) // Closer than 0.075 at bottom layer
                                {
                                    // Also fine
                                }
                                else if (note.x < 2)
                                {
                                    arr.Add(note);
                                    if (note is Note)
                                    {
                                        CheckResults.Instance.AddResult(new CheckResult()
                                        {
                                            Characteristic = CriteriaCheckManager.Characteristic,
                                            Difficulty = CriteriaCheckManager.Difficulty,
                                            Name = "Vision Block",
                                            Severity = Severity.Warning,
                                            CheckType = "Vision",
                                            Description = "Notes must be placed with enough time to react.",
                                            ResultData = new() {
                                                new("currentReactionTime", Math.Round(timescale.BPM.ToRealTime(note.Beats - lastMidL.First().Beats) * 1000, 0).ToString()),
                                                new("targetReactionTime", Math.Round(timescale.BPM.ToRealTime(MinTimeWarning) * 1000, 0).ToString()),
                                            },
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
                        if (note.Beats - lastMidR.First().Beats >= Overall)
                        {
                            if (note.Beats - lastMidR.First().Beats <= MinTimeWarning)
                            {
                                if (note.x == 3 && note.Beats - lastMidR.First().Beats <= MaxOuterNoteTime)
                                {
                                    // Fine
                                }
                                else if (note.x == 2 && note.y == 0 && note.Beats - lastMidR.First().Beats <= MaxBottomNoteTime)
                                {
                                    // Also fine
                                }
                                else if (note.x > 1)
                                {
                                    arr.Add(note);
                                    if (note is Note)
                                    {
                                        CheckResults.Instance.AddResult(new CheckResult()
                                        {
                                            Characteristic = CriteriaCheckManager.Characteristic,
                                            Difficulty = CriteriaCheckManager.Difficulty,
                                            Name = "Vision Block",
                                            Severity = Severity.Warning,
                                            CheckType = "Vision",
                                            Description = "Notes must be placed with enough time to react.",
                                            ResultData = new() {
                                                new("currentReactionTime", Math.Round(timescale.BPM.ToRealTime(note.Beats - lastMidR.First().Beats) * 1000, 0).ToString()),
                                                new("targetReactionTime", Math.Round(timescale.BPM.ToRealTime(MinTimeWarning) * 1000, 0).ToString()),
                                            },
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
                    var bomb = beatmapGridObjects[i];
                    if (bomb is Bomb)
                    {
                        timescale.BPM.SetCurrentBPM(bomb.Beats);
                        var MaxTimeBomb = timescale.BPM.ToBeatTime((float)Instance.VBMaxBombTime);
                        var MinTimeBomb = timescale.BPM.ToBeatTime((float)Instance.VBMinBombTime);
                        var Overall = timescale.BPM.ToBeatTime((float)Instance.VBMinimum);
                        var left = (Note)beatmapGridObjects.Where(x => x.Beats < bomb.Beats && x is Note no && no.Color == 0).OrderBy(o => o.Beats).LastOrDefault();
                        var right = (Note)beatmapGridObjects.Where(x => x.Beats < bomb.Beats && x is Note no && no.Color == 1).OrderBy(o => o.Beats).LastOrDefault();
                        lastMidL.RemoveAll(l => bomb.Beats - l.Beats > MinTimeBomb);
                        lastMidR.RemoveAll(l => bomb.Beats - l.Beats > MinTimeBomb);
                        if (lastMidL.Count > 0)
                        {
                            if (bomb.Beats - lastMidL.First().Beats <= MinTimeBomb) // Closer than 0.20
                            {
                                if (bomb.x == 0 && bomb.Beats - lastMidL.First().Beats <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((bomb.x != 1 || bomb.y != 1) && bomb.Beats - lastMidL.First().Beats <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (bomb.x < 2)
                                {
                                    if (left != null)
                                    {
                                        if (left.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(bomb.x - left.x, 2) + Math.Pow(bomb.y - left.y, 2));
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
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidL.First().Beats) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { bomb }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.x, left.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.CutDirection))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(bomb.x - pos.x, 2) + Math.Pow(bomb.y - pos.y, 2));
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
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidL.First().Beats) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { bomb }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(bomb.x - right.x, 2) + Math.Pow(bomb.y - right.y, 2));
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
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidL.First().Beats) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { bomb }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.x, right.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.CutDirection))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(bomb.x - pos.x, 2) + Math.Pow(bomb.y - pos.y, 2));
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
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidL.First().Beats) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { bomb }
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
                            if (bomb.Beats - lastMidR.First().Beats <= MinTimeBomb) // Closer than 0.20
                            {
                                if (bomb.x == 3 && bomb.Beats - lastMidR.First().Beats <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((bomb.x != 2 || bomb.y != 1) && bomb.Beats - lastMidR.First().Beats <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (bomb.x > 1)
                                {
                                    if (left != null)
                                    {
                                        if (left.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(bomb.x - left.x, 2) + Math.Pow(bomb.y - left.y, 2));
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
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidR.First().Beats) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { bomb }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.x, left.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.CutDirection))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(bomb.x - pos.x, 2) + Math.Pow(bomb.y - pos.y, 2));
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
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidR.First().Beats) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { bomb }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(bomb.x - right.x, 2) + Math.Pow(bomb.y - right.y, 2));
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
                                                    ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidR.First().Beats) * 1000, 0) + "ms") },
                                                    BeatmapObjects = new() { bomb }
                                                });
                                                issue = CritResult.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.x, right.y);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.CutDirection))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(bomb.x - pos.x, 2) + Math.Pow(bomb.y - pos.y, 2));
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
                                                ResultData = new() { new("VisionBlock", "VB - " + Math.Round(timescale.BPM.ToRealTime(bomb.Beats - lastMidR.First().Beats) * 1000, 0) + "ms") },
                                                BeatmapObjects = new() { bomb }
                                            });
                                            issue = CritResult.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (bomb.y == 1 && bomb.x == 1)
                    {
                        lastMidL.Add(bomb);
                    }
                    if (bomb.y == 1 && bomb.x == 2)
                    {
                        lastMidR.Add(bomb);
                    }
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Vision Block",
                    Severity = Severity.Passed,
                    CheckType = "Vision",
                    Description = "No issue with vision from notes and bombs detected.",
                    ResultData = new() { new("VisionBlock", "Success") }
                });
            }

            timescale.BPM.ResetCurrentBPM();
            return issue;
        }

    }
}

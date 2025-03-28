﻿using BLMapCheck.Classes.Results;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Parity
    {
        // JoshaParity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public static CritResult Check(List<SwingData> swings, List<Parser.Map.Difficulty.V3.Grid.Note> notes)
        {
            bool hadIssue = false;
            bool hadWarning = false;

            foreach (var swing in swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                swing.notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Parity",
                    Severity = Severity.Error,
                    CheckType = "Parity",
                    Description = "Parity error.",
                    ResultData = new() { new("ErrorType", "Reset") },
                    BeatmapObjects = new(colornotes) { }
                });
                hadIssue = true;
            }
            foreach (var swing in swings.Where(x => x.swingEBPM == float.PositiveInfinity).ToList())
            {
                List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                swing.notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Parity Mismatch",
                    Severity = Severity.Error,
                    CheckType = "Parity",
                    Description = "Parity mismatch on the same beat.",
                    ResultData = new() { new("ErrorType", "swingEBPM is equal PositiveInfinity") },
                    BeatmapObjects = new(colornotes) { }
                });
                hadIssue = true;
            }

            List<SwingData> rightHandSwings = swings.Where(x => x.rightHand).ToList();
            List<SwingData> leftHandSwings = swings.Where(x => !x.rightHand).ToList();

            for (int i = 0; i < rightHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    if (i == rightHandSwings.Count - 1 && rightHandSwings[i].notes.Last().d == 8) break;
                    float difference = rightHandSwings[i].startPos.rotation - rightHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Instance.ParityWarningAngle)
                    {
                        List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                        rightHandSwings[i].notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Parity Warning",
                            Severity = Severity.Warning,
                            CheckType = "Parity",
                            Description = "Parity degree difference.",
                            ResultData = new() { new("WarningType", Math.Abs(difference) + " degree difference") },
                            BeatmapObjects = new(colornotes) { }
                        });
                        hadWarning = true;
                    }
                    else if (Math.Abs(rightHandSwings[i].startPos.rotation) > 135 || Math.Abs(rightHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Instance.ParityInvertedWarning)
                        {
                            List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                            rightHandSwings[i].notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Parity Inverted",
                                Severity = Severity.Inconclusive,
                                CheckType = "Parity",
                                Description = "Parity playing inverted.",
                                ResultData = new() { new("WarningType", "Playing inverted") },
                                BeatmapObjects = new(colornotes) { }
                            });
                        }
                        hadWarning = true;
                    }
                }
            }

            for (int i = 0; i < leftHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    if (i == leftHandSwings.Count - 1 && leftHandSwings[i].notes.Last().d == 8) break;
                    float difference = leftHandSwings[i].startPos.rotation - leftHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Instance.ParityWarningAngle)
                    {
                        List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                        leftHandSwings[i].notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Parity Warning",
                            Severity = Severity.Warning,
                            CheckType = "Parity",
                            Description = "Parity degree difference.",
                            ResultData = new() { new("WarningType", Math.Abs(difference).ToString() + " degree difference") },
                            BeatmapObjects = new(colornotes) { }
                        });
                        hadWarning = true;
                    }
                    else if (Math.Abs(leftHandSwings[i].startPos.rotation) > 135 || Math.Abs(leftHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Instance.ParityInvertedWarning)
                        {
                            List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                            leftHandSwings[i].notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Parity Inverted",
                                Severity = Severity.Inconclusive,
                                CheckType = "Parity",
                                Description = "Parity playing inverted.",
                                ResultData = new() { new("WarningType", "Playing inverted") },
                                BeatmapObjects = new(colornotes) { }
                            });
                        }
                        hadWarning = true;
                    }
                }
            }

            if (Instance.ParityDebug)
            {
                foreach (var swing in swings)
                {
                    var swingWithoutNotes = swing;
                    Severity commentType = Severity.Info;
                    if (swing.resetType == ResetType.Rebound) commentType = Severity.Error;
                    if (Math.Abs(swing.endPos.rotation) > 135 || Math.Abs(swing.endPos.rotation) > 135) commentType = Severity.Inconclusive;

                    List<KeyValuePair> resultData = new()
                    {
                        new("swingParity", swing.swingParity.ToString()),
                        new("resetType", swing.resetType.ToString()),
                        new("swingStartBeat", swing.swingStartBeat.ToString()),
                        new("swingEndBeat", swing.swingEndBeat.ToString()),
                        new("swingEBPM", swing.swingEBPM.ToString()),
                        new("notes", JsonConvert.SerializeObject(swing.notes, Formatting.Indented)),
                        new("startPos", JsonConvert.SerializeObject(swing.startPos, Formatting.Indented)),
                        new("endPos", JsonConvert.SerializeObject(swing.endPos, Formatting.Indented)),
                        new("rightHand", swing.rightHand.ToString())
                    };
                    
                    List<Parser.Map.Difficulty.V3.Grid.Note> colornotes = new();
                    swing.notes.ForEach(note => colornotes.Add(notes.Where(n => n.Beats == note.b && n.CutDirection == note.d && n.x == note.x && n.y == note.y && n.Color == note.c).FirstOrDefault()));
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Parity Debug",
                        Severity = commentType,
                        CheckType = "Parity",
                        Description = "Parity Debug.",
                        ResultData = resultData,
                        BeatmapObjects = new(colornotes) { }
                    });
                }
            }

            if (hadIssue)
            {
                return CritResult.Fail;
            }
            else if (hadWarning)
            {
                return CritResult.Warning;
            }
            else
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Parity",
                    Severity = Severity.Passed,
                    CheckType = "Parity",
                    Description = "No possible parity issue detected.",
                    ResultData = new()
                });
                return CritResult.Success;
            }
        }
    }
}

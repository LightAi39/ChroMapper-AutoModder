using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Parity
    {
        // JoshaParity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public static CritResult Check(List<SwingData> Swings, List<Colornote> Notes)
        {
            bool hadIssue = false;
            bool hadWarning = false;

            foreach (var swing in Swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                List<Colornote> colornotes = new();
                swing.notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Parity",
                    Severity = Severity.Error,
                    CheckType = "Parity",
                    Description = "Parity error.",
                    ResultData = new() { new("Parity", "Reset Type: Rebound") },
                    BeatmapObjects = new(colornotes) { }
                });
                hadIssue = true;
            }
            foreach (var swing in Swings.Where(x => x.swingEBPM == float.PositiveInfinity).ToList())
            {
                List<Colornote> colornotes = new();
                swing.notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Parity Mismatch",
                    Severity = Severity.Error,
                    CheckType = "Parity",
                    Description = "Parity mismatch on the same beat.",
                    ResultData = new() { new("ParityMismatch", "swingEBPM is equal PositiveInfinity") },
                    BeatmapObjects = new(colornotes) { }
                });
                hadIssue = true;
            }

            List<SwingData> rightHandSwings = Swings.Where(x => x.rightHand).ToList();
            List<SwingData> leftHandSwings = Swings.Where(x => !x.rightHand).ToList();

            for (int i = 0; i < rightHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = rightHandSwings[i].startPos.rotation - rightHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Instance.ParityWarningAngle)
                    {
                        List<Colornote> colornotes = new();
                        rightHandSwings[i].notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Parity Warning",
                            Severity = Severity.Warning,
                            CheckType = "Parity",
                            Description = "Parity degree difference.",
                            ResultData = new() { new("ParityWarning", "Parity Warning - " + Math.Abs(difference) + " degree difference") },
                            BeatmapObjects = new(colornotes) { }
                        });
                        hadWarning = true;
                    }
                    else if (Math.Abs(rightHandSwings[i].startPos.rotation) > 135 || Math.Abs(rightHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Instance.ParityInvertedWarning)
                        {
                            List<Colornote> colornotes = new();
                            rightHandSwings[i].notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Parity Inverted",
                                Severity = Severity.Inconclusive,
                                CheckType = "Parity",
                                Description = "Parity playing inverted.",
                                ResultData = new() { new("ParityInverted", "Parity Warning - Playing inverted") },
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
                    float difference = leftHandSwings[i].startPos.rotation - leftHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Instance.ParityWarningAngle)
                    {
                        List<Colornote> colornotes = new();
                        leftHandSwings[i].notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Parity Warning",
                            Severity = Severity.Warning,
                            CheckType = "Parity",
                            Description = "Parity degree difference.",
                            ResultData = new() { new("ParityWarning", "Parity Warning - " + Math.Abs(difference).ToString() + " degree difference") },
                            BeatmapObjects = new(colornotes) { }
                        });
                        hadWarning = true;
                    }
                    else if (Math.Abs(leftHandSwings[i].startPos.rotation) > 135 || Math.Abs(leftHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Instance.ParityInvertedWarning)
                        {
                            List<Colornote> colornotes = new();
                            leftHandSwings[i].notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Parity Inverted",
                                Severity = Severity.Inconclusive,
                                CheckType = "Parity",
                                Description = "Parity playing inverted.",
                                ResultData = new() { new("ParityInverted", "Parity Warning - Playing inverted") },
                                BeatmapObjects = new(colornotes) { }
                            });
                        }
                        hadWarning = true;
                    }
                }
            }

            if (Instance.ParityDebug)
            {
                foreach (var swing in Swings)
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
                    
                    List<Colornote> colornotes = new();
                    swing.notes.ForEach(note => colornotes.Add(Notes.Where(n => n.b == note.b && n.d == note.d && n.x == note.x && n.y == note.y && n.c == note.c).FirstOrDefault()));
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
                return CritResult.Success;
            }
        }
    }
}

using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Chains
    {
        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public static CritResult Check(List<Chain> chains, List<Note> notes)
        {
            var issue = CritResult.Success;

            if (notes.Count >= 16)
            {
                var link = chains.Where(l => l.Beats <= notes[15].Beats).ToList();
                if(link.Any())
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Early Chain",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chains cannot be part of the first 16 notes of the map.",
                        ResultData = new() { new("EarlyChain", "Found" )},
                        BeatmapObjects = new(link) { }
                    });
                    issue = CritResult.Fail;
                }
            }
            else if (chains.Any())
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Early Chain",
                    Severity = Severity.Error,
                    CheckType = "Chain",
                    Description = "Chains cannot be part of the first 16 notes of the map.",
                    ResultData = new() { new("EarlyChain", "Found") },
                    BeatmapObjects = new(chains) { }
                });
                issue = CritResult.Fail;
            }

            // TODO: Make this mess better idk
            foreach (var l in chains)
            {
                var chain = l;
                var x = Math.Abs(l.tx - l.x) * chain.Squish;
                var y = Math.Abs(l.ty - l.y) * chain.Squish;
                var distance = Math.Sqrt(x * x + y * y);
                var value = distance / (chain.Segment - 1);
                // Difference between expected and current distance, multiplied by current squish to know maximum value
                double max;
                if (l.ty == l.y) max = Math.Round(Instance.ChainLinkVsAir / value * chain.Squish, 2);
                else max = Math.Round(Instance.ChainLinkVsAir * 1.1 / value * chain.Squish, 2);
                if (chain.Squish - 0.01 > max)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Squish",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chains must be at least 12.5% links versus air/empty-space.",
                        ResultData = new() { new("ChainSquish", "Current squish:" + chain.Squish.ToString() + " Maximum squish for placement:" + max.ToString()) },
                        BeatmapObjects = new() { chain }
                    });
                    issue = CritResult.Fail;
                }
                var newX = l.x + (l.tx - l.x) * chain.Squish;
                var newY = l.y + (l.ty - l.y) * chain.Squish;
                if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Lead",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chain cannot lead too far off the grid.",
                        ResultData = new() { new("ChainLead", "X: " + newX.ToString() + " Y: " + newY.ToString()) },
                        BeatmapObjects = new() { l }
                    });
                    issue = CritResult.Fail;
                }
                if (l.TailInBeats < l.Beats)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Reversed Chain",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chain cannot have a reverse direction.",
                        ResultData = new() { new("ChainReverse", "Current duration: " + (l.Beats - l.TailInBeats).ToString()) },
                        BeatmapObjects = new() { l }
                    });
                    issue = CritResult.Fail;
                }
                var note = notes.FirstOrDefault(x => x.Beats >= l.TailInBeats && x.Color == l.Color);
                if (note != null)
                {
                    if (note.Beats - l.TailInBeats < l.TailInBeats - l.Beats)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Chain Flick",
                            Severity = Severity.Error,
                            CheckType = "Chain",
                            Description = "The duration between the chain tail and next note must be at least the duration of the chain",
                            ResultData = new() { new("ChainFlick", "Chain duration: " + (l.TailInBeats - l.Beats).ToString() + " Duration between:" + (note.Beats - l.TailInBeats).ToString()) },
                            BeatmapObjects = new() { l }
                        });
                        issue = CritResult.Fail;
                    }
                }
                var temp = new NoteData()
                {
                    Direction = Mod(DirectionToDegree[l.Direction], 360),
                    Line = l.x,
                    Layer = l.y
                };
                var temp2 = new NoteData()
                {
                    Line = l.tx,
                    Layer = l.ty
                };
                var temp3 = new List<NoteData>
                {
                    temp,
                    temp2
                };
                if (!IsSameDirection(ReverseCutDirection(FindAngleViaPosition(temp3, 0, 1)), temp.Direction, Instance.MaxChainRotation) && !IsSameDirection(FindAngleViaPosition(temp3, 0, 1), temp.Direction, Instance.MaxChainRotation))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Rotation",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chains cannot rotate over 45 degrees.",
                        ResultData = new() { new("ChainRotation", "True") },
                        BeatmapObjects = new() { l }
                    });
                    issue = CritResult.Fail;
                }
            }

            if(issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Chain",
                    Severity = Severity.Passed,
                    CheckType = "Chain",
                    Description = "Chains spacing, lead, placement and rotation are all proper.",
                    ResultData = new() { new("Chain", "Success") }
                });
            }

            return issue;
        }
    }
}

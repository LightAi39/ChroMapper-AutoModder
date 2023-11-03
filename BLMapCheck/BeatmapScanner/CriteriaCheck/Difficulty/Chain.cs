using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Chain
    {
        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public static CritResult Check()
        {
            var issue = CritResult.Success;
            var links = BeatmapScanner.Chains.OrderBy(c => c.b).ToList();
            var notes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();

            if (notes.Count >= 16)
            {
                var link = links.Where(l => l.b <= notes[15].Time).ToList();
                if(link.Any())
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
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
            else if (links.Any())
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = BSMapCheck.Characteristic,
                    Difficulty = BSMapCheck.Difficulty,
                    Name = "Early Chain",
                    Severity = Severity.Error,
                    CheckType = "Chain",
                    Description = "Chains cannot be part of the first 16 notes of the map.",
                    ResultData = new() { new("EarlyChain", "Found") },
                    BeatmapObjects = new(links) { }
                });
                issue = CritResult.Fail;
            }

            // TODO: Make this mess better idk
            foreach (var l in links)
            {
                var chain = l;
                var x = Math.Abs(l.tx - l.x) * chain.s;
                var y = Math.Abs(l.ty - l.y) * chain.s;
                var distance = Math.Sqrt(x * x + y * y);
                var value = distance / (chain.sc - 1);
                // Difference between expected and current distance, multiplied by current squish to know maximum value
                double max;
                if (l.ty == l.y) max = Math.Round(ChainLinkVsAir / value * chain.s, 2);
                else max = Math.Round(ChainLinkVsAir * 1.1 / value * chain.s, 2);
                if (chain.s - 0.01 > max)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Chain Squish",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chains must be at least 12.5% links versus air/empty-space.",
                        ResultData = new() { new("ChainSquish", "Current squish:" + chain.s.ToString() + " Maximum squish for placement:" + max.ToString()) },
                        BeatmapObjects = new() { chain }
                    });
                    issue = CritResult.Fail;
                }
                var newX = l.x + (l.tx - l.x) * chain.s;
                var newY = l.y + (l.ty - l.y) * chain.s;
                if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                {
                    //CreateDiffCommentLink("R2D - Lead too far", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Chain Lead",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chain cannot lead too far off the grid.",
                        ResultData = new() { new("ChainLead", "X: " + newX.ToString() + " Y: " + newY.ToString()) },
                        BeatmapObjects = new() { l }
                    });
                    issue = CritResult.Fail;
                }
                if (l.tb < l.b)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
                        Name = "Reversed Chain",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Chain cannot have a reverse direction.",
                        ResultData = new() { new("ChainReverse", "Current duration: " + (l.b - l.tb).ToString()) },
                        BeatmapObjects = new() { l }
                    });
                    issue = CritResult.Fail;
                }
                var note = notes.Find(x => x.Time >= l.tb && x.Type == l.c);
                if (note != null)
                {
                    if (l.tb + (l.tb - l.b) > note.Time)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Chain Flick",
                            Severity = Severity.Error,
                            CheckType = "Chain",
                            Description = "The duration between the chain tail and next note must be at least the duration of the chain",
                            ResultData = new() { new("ChainFlick", "Chain duration: " + (l.tb - l.b).ToString() + " Duration between:" + (note.Time - l.tb).ToString()) },
                            BeatmapObjects = new() { l }
                        });
                        issue = CritResult.Fail;
                    }
                }

                var temp = new Cube(notes.First())
                {
                    Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[l.d], 360),
                    Line = l.x,
                    Layer = l.y
                };
                var temp2 = new Cube(notes.First())
                {
                    Line = l.tx,
                    Layer = l.ty
                };
                var temp3 = new List<Cube>
                {
                    temp,
                    temp2
                };
                if (!ScanMethod.IsSameDirection(ScanMethod.ReverseCutDirection(ScanMethod.FindAngleViaPosition(temp3, 0, 1)), temp.Direction, MaxChainRotation))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = BSMapCheck.Characteristic,
                        Difficulty = BSMapCheck.Difficulty,
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

            return issue;
        }
    }
}

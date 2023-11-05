using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ProlongedSwing
    {
        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public static CritResult Check(List<Colornote> notes, List<Burstslider> chains)
        {
            if(Slider.AverageSliderDuration == -1)
            {
                Slider.Check();
            }

            var duration = false;
            var head = false;
            var issue = false;
            var unsure = false;

            foreach (var ch in chains)
            {
                if (ch.tb - ch.b >= Slider.AverageSliderDuration * 4.2)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Duration",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Maximum chains duration must be similar to the average window sliders duration * 2.",
                        ResultData = new() { new("ChainDuration", "Current duration: " + (ch.tb - ch.b).ToString() + " Maximum duration: " + (Slider.AverageSliderDuration * 4.2).ToString()) },
                        BeatmapObjects = new() { ch }
                    });
                    issue = true;
                    duration = true;
                }
                else if (ch.tb - ch.b >= Slider.AverageSliderDuration * 3.15)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Duration",
                        Severity = Severity.Inconclusive,
                        CheckType = "Chain",
                        Description = "Maximum chains duration must be similar to the average window sliders duration * 2.",
                        ResultData = new() { new("ChainDuration", "Current duration: " + (ch.tb - ch.b).ToString() + " Recommended maximum duration: " + (Slider.AverageSliderDuration * 3.15).ToString()) },
                        BeatmapObjects = new() { ch }
                    });
                    unsure = true;
                    duration = true;
                }
                if (!notes.Exists(c => c.b == ch.b && c.c == ch.c && c.x == ch.x && c.y == ch.y))
                {
                    // Link spam maybe idk
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Head",
                        Severity = Severity.Inconclusive,
                        CheckType = "Chain",
                        Description = "Chain must have an head note.",
                        ResultData = new() { new("ChainHead", "No head note at: " + ch.b + " " + ch.x + "/" + ch.y) },
                        BeatmapObjects = new() { ch }
                    });
                    issue = true;
                    head = true;
                }
            }

            if (!duration)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Chain Duration",
                    Severity = Severity.Passed,
                    CheckType = "Chain",
                    Description = "Chains duration match the map sliders duration * 2.",
                    ResultData = new() { new("ChainDuration", "Success") }
                });
            }
            if(!head)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Chain Head",
                    Severity = Severity.Passed,
                    CheckType = "Chain",
                    Description = "All chains in the map have an head note.",
                    ResultData = new() { new("ChainHead", "Success") }
                });
            }
            
            // Dot spam and pauls maybe
            var leftNotes = notes.Where(d => d.c == 0).ToList();
            var rightNotes = notes.Where(d => d.c == 1).ToList();
            Colornote previous = null;
            foreach (var left in leftNotes)
            {
                if (previous != null)
                {
                    if (left.b - previous.b <= 0.25 && left.b != previous.b && left.x == previous.x && left.y == previous.y)
                    {
                        if (left.d == 8)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Inconclusive,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "Inconclusive") },
                                BeatmapObjects = new() { left }
                            });
                            unsure = true;
                        }
                        else
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "Error") },
                                BeatmapObjects = new() { left }
                            });
                            issue = true;
                        }
                    }
                }

                previous = left;
            }

            previous = null;
            foreach (var right in rightNotes)
            {
                if (previous != null)
                {
                    if (right.b - previous.b <= 0.25  && right.b != previous.b && right.x == previous.x && right.y == previous.y)
                    {
                        if (right.d == 8)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Inconclusive,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "Inconclusive") },
                                BeatmapObjects = new() { right }
                            });
                            unsure = true;
                        }
                        else
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "Error") },
                                BeatmapObjects = new() { right }
                            });
                            issue = true;
                        }
                    }
                }

                previous = right;
            }

            if (issue)
            {
                return CritResult.Fail;
            }
            else if (unsure)
            {
                return CritResult.Warning;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Characteristic = CriteriaCheckManager.Characteristic,
                Difficulty = CriteriaCheckManager.Difficulty,
                Name = "Dot Spam",
                Severity = Severity.Passed,
                CheckType = "Swing",
                Description = "Map doesn't have any prolonged swing duration.",
                ResultData = new() { new("DotSpam", "Success") }
            });

            return CritResult.Success;
        }
    }
}

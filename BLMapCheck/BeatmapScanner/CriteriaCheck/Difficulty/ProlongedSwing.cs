using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ProlongedSwing
    {
        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public static CritResult Check(List<Note> notes, List<Chain> chains)
        {
            var duration = false;
            var head = false;
            var issue = false;
            var unsure = false;

            if (Config.Instance.AutomaticSliderPrecision)
            {
                SetAutoSliderPrecision();
            }

            foreach (var ch in chains)
            {
                if (ch.TailInBeats - ch.Beats >= Config.Instance.SliderPrecision * 4.2)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Chain Duration",
                        Severity = Severity.Error,
                        CheckType = "Chain",
                        Description = "Maximum chains duration must be similar to the average window sliders duration * 2.",
                        ResultData = new() { new("CurrentDuration", (ch.TailInBeats - ch.Beats).ToString()), new("MaximumDuration", (Config.Instance.SliderPrecision * 4.2).ToString()) },
                        BeatmapObjects = new() { ch }
                    });
                    issue = true;
                    duration = true;
                }
                if (!notes.Exists(c => c.Beats == ch.Beats && c.Color == ch.Color && c.x == ch.x && c.y == ch.y))
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
                        ResultData = new() { new("IssueType", "No head note at: " + ch.Beats + " " + ch.x + "/" + ch.y) },
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
                    ResultData = new()
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
                    ResultData = new()
                });
            }
            
            // Dot spam and pauls maybe
            var leftNotes = notes.Where(d => d.Color == 0).ToList();
            var rightNotes = notes.Where(d => d.Color == 1).ToList();
            Note previous = null;
            foreach (var left in leftNotes)
            {
                if (previous != null)
                {
                    if (left.Beats - previous.Beats <= 0.125 && left.Beats != previous.Beats && left.x == previous.x && left.y == previous.y)
                    {
                        if (left.CutDirection == 8)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("Type", "Dot Spam") },
                                BeatmapObjects = new() { left }
                            });
                            unsure = true;
                        }
                        else if(previous.CutDirection != 8 && IsSameDirection(DirectionToDegree[previous.CutDirection], DirectionToDegree[left.CutDirection]))
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("Type", "Dot Spam") },
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
                    if (right.Beats - previous.Beats <= 0.125  && right.Beats != previous.Beats && right.x == previous.x && right.y == previous.y)
                    {
                        if (right.CutDirection == 8)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("Type", "Dot Spam") },
                                BeatmapObjects = new() { right }
                            });
                            unsure = true;
                        }
                        else if (previous.CutDirection != 8 && IsSameDirection(DirectionToDegree[previous.CutDirection], DirectionToDegree[right.CutDirection]))
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("Type", "Dot Spam") },
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
                ResultData = new()
            });

            return CritResult.Success;
        }
    }
}

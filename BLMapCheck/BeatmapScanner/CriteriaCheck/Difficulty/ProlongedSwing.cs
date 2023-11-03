using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ProlongedSwing
    {
        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public static CritResult Check(List<Colornote> Notes)
        {
            if(Slider.AverageSliderDuration == -1)
            {
                Slider.Check(Notes);
            }

            var issue = false;
            var unsure = false;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.b).ToList();
            var slider = false;
            if (cubes.Exists(x => x.Slider))
            {
                slider = true;
            }
            foreach (var ch in chains)
            {
                if (ch.tb - ch.b >= Slider.AverageSliderDuration * 4.2)
                {
                    if (slider)
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
                    }
                    else if (ch.tb - ch.b > 0.125)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Chain Duration",
                            Severity = Severity.Inconclusive,
                            CheckType = "Chain",
                            Description = "Maximum chains duration must be similar to the average window sliders duration * 2.",
                            ResultData = new() { new("ChainDuration", "Current duration: " + (ch.tb - ch.b).ToString() + " Recommended maximum duration: " +  (0.125).ToString()) },
                            BeatmapObjects = new() { ch }
                        });
                        unsure = true;
                    }
                }
                else if (ch.tb - ch.b >= Slider.AverageSliderDuration * 3.15)
                {
                    if (slider)
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
                    }
                    else if (ch.tb - ch.b > 0.125)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Chain Duration",
                            Severity = Severity.Inconclusive,
                            CheckType = "Chain",
                            Description = "Maximum chains duration must be similar to the average window sliders duration * 2.",
                            ResultData = new() { new("ChainDuration", "Current duration: " + (ch.tb - ch.b).ToString() + " Recommended maximum duration: " + (0.125).ToString()) },
                            BeatmapObjects = new() { ch }
                        });
                        unsure = true;
                    }
                }
                if (!cubes.Exists(c => c.Time == ch.b && c.Type == ch.c && c.Line == ch.x && c.Layer == ch.y))
                {
                    // Link spam maybe idk
                    //CreateDiffCommentLink("R2D - No head note", CommentTypesEnum.Issue, ch); TODO: USE NEW METHOD
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
                }
            }
            // Dot spam and pauls maybe
            var leftCube = cubes.Where(d => d.Type == 0).ToList();
            var rightCube = cubes.Where(d => d.Type == 1).ToList();
            Cube previous = null;
            foreach (var left in leftCube)
            {
                if (previous != null)
                {
                    if (((left.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(left.Direction, previous.Direction, 67.5)) || (left.Time - previous.Time <= 0.142857)) && left.Time != previous.Time && left.Line == previous.Line && left.Layer == previous.Layer)
                    {
                        if (left.CutDirection == 8)
                        {
                            var note = Notes.Where(n => n.b == left.Time && n.d == left.Direction && n.x == left.Line && n.y == left.Layer && n.c == left.Type).FirstOrDefault();
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Inconclusive,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "True") },
                                BeatmapObjects = new() { note }
                            });
                            unsure = true;
                        }
                        else
                        {
                            var note = Notes.Where(n => n.b == left.Time && n.d == left.Direction && n.x == left.Line && n.y == left.Layer && n.c == left.Type).FirstOrDefault();
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "True") },
                                BeatmapObjects = new() { note }
                            });
                            issue = true;
                        }
                    }
                }

                previous = left;
            }

            previous = null;
            foreach (var right in rightCube)
            {
                if (previous != null)
                {
                    if (((right.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(right.Direction, previous.Direction, 67.5)) || (right.Time - previous.Time <= 0.142857)) && right.Time != previous.Time && right.Line == previous.Line && right.Layer == previous.Layer)
                    {
                        if (right.CutDirection == 8)
                        {
                            var note = Notes.Where(n => n.b == right.Time && n.d == right.Direction && n.x == right.Line && n.y == right.Layer && n.c == right.Type).FirstOrDefault();
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Inconclusive,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "True") },
                                BeatmapObjects = new() { note }
                            });
                            unsure = true;
                        }
                        else
                        {
                            var note = Notes.Where(n => n.b == right.Time && n.d == right.Direction && n.x == right.Line && n.y == right.Layer && n.c == right.Type).FirstOrDefault();
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Dot Spam",
                                Severity = Severity.Error,
                                CheckType = "Swing",
                                Description = "Swing duration should be consistent throughout the map.",
                                ResultData = new() { new("DotSpam", "True") },
                                BeatmapObjects = new() { note }
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

            return CritResult.Success;
        }
    }
}

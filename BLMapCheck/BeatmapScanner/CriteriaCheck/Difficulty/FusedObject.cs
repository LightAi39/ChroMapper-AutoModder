using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class FusedObject
    {
        // Detect if objects are too close. Configurable margin (in ms)
        // TODO: There's probably a way better way to do this, can someone clean this mess
        public static CritResult Check(List<Colornote> notes, List<Bombnote> bombs, List<Obstacle> obstacles, List<Burstslider> chains, float njs)
        {
            var issue = CritResult.Success;

            foreach (var w in obstacles)
            {
                foreach (var c in notes)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(c.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (c.b >= w.b - max && c.b <= w.b + w.d + max && c.x <= w.x + w.w - 1 && c.x >= w.x && c.y < w.y + w.h && c.y >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { c, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var b in bombs)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(b.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (b.b >= w.b - max && b.b <= w.b + w.d + max && b.x <= w.x + w.w - 1 && b.x >= w.x && b.y < w.y + w.h && b.y >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { b, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(c.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (c.b >= w.b - max && c.b <= w.b + w.d + max && c.tx <= w.x + w.w - 1 && c.tx >= w.x && c.ty < w.y + w.h && c.ty >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { c, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < notes.Count; i++)
            {
                var c = notes[i];
                for (int j = i + 1; j < notes.Count; j++)
                {
                    var c2 = notes[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.x == c2.x && c.y == c2.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { c, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
                for (int j = 0; j < bombs.Count; j++)
                {
                    var b = bombs[j];
                    BeatPerMinute.BPM.SetCurrentBPM(b.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= b.b - max && c.b <= b.b + max && c.x == b.x && c.y == b.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { c, b }
                        });
                        issue = CritResult.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.x == c2.tx && c.y == c2.ty)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { c, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < bombs.Count; i++)
            {
                var b = bombs[i];
                for (int j = i + 1; j < bombs.Count; j++)
                {
                    var b2 = bombs[j];
                    BeatPerMinute.BPM.SetCurrentBPM(b2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b2.b - b.b >= max)
                    {
                        break;
                    }
                    if (b.b >= b2.b - max && b.b <= b2.b + max && b.x == b2.x && b.y == b2.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { b, b2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.b - b.b >= max)
                    {
                        break;
                    }
                    if (b.b >= c2.b - max && b.b <= c2.b + max && b.x == c2.tx && b.y == c2.ty)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { b, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < chains.Count; i++)
            {
                var c = chains[i];
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.tx == c2.tx && c.ty == c2.ty)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "Error") },
                            BeatmapObjects = new() { c, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Fused Object",
                    Severity = Severity.Passed,
                    CheckType = "Fused",
                    Description = "No fused objects detected.",
                    ResultData = new() { new("FusedObject", "Success") },
                });
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

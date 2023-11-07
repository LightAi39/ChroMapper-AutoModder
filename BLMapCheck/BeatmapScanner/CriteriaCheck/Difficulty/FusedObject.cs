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
        public static CritResult Check(List<Note> notes, List<Bomb> bombs, List<Wall> obstacles, List<Chain> chains, float njs)
        {
            var issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            foreach (var w in obstacles)
            {
                foreach (var c in notes)
                {
                    timescale.BPM.SetCurrentBPM(c.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.Beats - (w.Beats + w.DurationInBeats) >= max)
                    {
                        break;
                    }
                    if (c.Beats >= w.Beats - max && c.Beats <= w.Beats + w.DurationInBeats + max && c.x <= w.x + w.Width - 1 && c.x >= w.x && c.y < w.y + w.Height && c.y >= w.y - 1)
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
                    timescale.BPM.SetCurrentBPM(b.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b.Beats - (w.Beats + w.DurationInBeats) >= max)
                    {
                        break;
                    }
                    if (b.Beats >= w.Beats - max && b.Beats <= w.Beats + w.DurationInBeats + max && b.x <= w.x + w.Width - 1 && b.x >= w.x && b.y < w.y + w.Height && b.y >= w.y - 1)
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
                    timescale.BPM.SetCurrentBPM(c.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.Beats - (w.Beats + w.DurationInBeats) >= max)
                    {
                        break;
                    }
                    if (c.Beats >= w.Beats - max && c.Beats <= w.Beats + w.DurationInBeats + max && c.tx <= w.x + w.Width - 1 && c.tx >= w.x && c.ty < w.y + w.Height && c.ty >= w.y - 1)
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
                    timescale.BPM.SetCurrentBPM(c2.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.Beats - c.Beats >= max)
                    {
                        break;
                    }
                    if (c.Beats >= c2.Beats - max && c.Beats <= c2.Beats + max && c.x == c2.x && c.y == c2.y)
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
                    timescale.BPM.SetCurrentBPM(b.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b.Beats - c.Beats >= max)
                    {
                        break;
                    }
                    if (c.Beats >= b.Beats - max && c.Beats <= b.Beats + max && c.x == b.x && c.y == b.y)
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
                    timescale.BPM.SetCurrentBPM(c2.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.Beats - c.Beats >= max)
                    {
                        break;
                    }
                    if (c.Beats >= c2.Beats - max && c.Beats <= c2.Beats + max && c.x == c2.tx && c.y == c2.ty)
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
                    timescale.BPM.SetCurrentBPM(b2.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b2.Beats - b.Beats >= max)
                    {
                        break;
                    }
                    if (b.Beats >= b2.Beats - max && b.Beats <= b2.Beats + max && b.x == b2.x && b.y == b2.y)
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
                    timescale.BPM.SetCurrentBPM(c2.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.Beats - b.Beats >= max)
                    {
                        break;
                    }
                    if (b.Beats >= c2.Beats - max && b.Beats <= c2.Beats + max && b.x == c2.tx && b.y == c2.ty)
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
                    timescale.BPM.SetCurrentBPM(c2.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c2.Beats - c.Beats >= max)
                    {
                        break;
                    }
                    if (c.Beats >= c2.Beats - max && c.Beats <= c2.Beats + max && c.tx == c2.tx && c.ty == c2.ty)
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

            timescale.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;
using static BLMapCheck.Classes.Helper.Helper;

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
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
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
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { b, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    timescale.BPM.SetCurrentBPM(c.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.TailInBeats - (w.Beats + w.DurationInBeats) >= max)
                    {
                        break;
                    }
                    var pre = w.Beats - max;
                    var post = w.Beats + w.DurationInBeats + max;
                    if ((c.Beats >= pre || c.TailInBeats >= pre) && (c.Beats <= post || c.TailInBeats <= post) && c.tx <= w.x + w.Width - 1 && c.tx >= w.x && c.ty < w.y + w.Height && c.ty >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { c, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < notes.Count; i++)
            {
                var n = notes[i];
                for (int j = i + 1; j < notes.Count; j++)
                {
                    var n2 = notes[j];
                    timescale.BPM.SetCurrentBPM(n2.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (n2.Beats - n.Beats >= max)
                    {
                        break;
                    }
                    if (n.Beats >= n2.Beats - max && n.Beats <= n2.Beats + max && n.x == n2.x && n.y == n2.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { n, n2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var b in bombs)
                {
                    timescale.BPM.SetCurrentBPM(b.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (b.Beats - n.Beats >= max)
                    {
                        break;
                    }
                    if (n.Beats >= b.Beats - max && n.Beats <= b.Beats + max && n.x == b.x && n.y == b.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { n, b }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    timescale.BPM.SetCurrentBPM(c.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.TailInBeats - n.Beats >= max)
                    {
                        break;
                    }
                    if (n.x == c.x && n.y == c.y) // Head
                    {
                        break;
                    }
                    var pre = n.Beats - max;
                    var post = n.Beats + max;
                    var dir = c.CutDirection;
                    if ((c.Beats >= pre || c.TailInBeats >= pre) && (c.Beats <= post || c.TailInBeats <= post) && IsPointBetween(n, c))
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { n, c }
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
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { b, b2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    timescale.BPM.SetCurrentBPM(c.Beats);
                    var max = Math.Round(timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
                    if (c.TailInBeats - b.Beats >= max)
                    {
                        break;
                    }
                    if (b.x == c.x && b.y == c.y) // Head
                    {
                        break;
                    }
                    var pre = b.Beats - max;
                    var post = b.Beats + max;
                    var dir = c.CutDirection;
                    if ((c.Beats >= pre || c.TailInBeats >= pre) && (c.Beats <= post || c.TailInBeats <= post) && IsPointBetween(b, c))
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
                            BeatmapObjects = new() { b, c }
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
                    if (Math.Abs(c2.TailInBeats - c.Beats) >= max || Math.Abs(c2.TailInBeats - c.TailInBeats) >= max || Math.Abs(c2.Beats - c.Beats) >= max || Math.Abs(c2.Beats - c.TailInBeats) >= max)
                    {
                        break;
                    }
                    var pre = c.Beats - max;
                    var post = c.Beats + max;
                    if ((c2.Beats >= pre || c2.TailInBeats >= pre) && (c2.Beats <= post || c2.TailInBeats <= post) && DoLinesIntersect(c, c2))
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within " + max.ToString() + " in the same line",
                            ResultData = new(),
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
                    ResultData = new(),
                });
            }

            timescale.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

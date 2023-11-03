using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class FusedObject
    {
        // Detect if objects are too close. Configurable margin (in ms)
        // TODO: There's probably a way better way to do this, can someone clean this mess
        public static CritResult Check(List<Colornote> Notes, List<Bombnote> Bombs, List<Obstacle> Obstacles, List<Burstslider> Chains, float NoteJumpSpeed)
        {
            var issue = CritResult.Success;

            foreach (var w in Obstacles)
            {
                foreach (var c in Notes)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(c.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (c.b >= w.b - max && c.b <= w.b + w.d + max && c.x <= w.x + w.w - 1 && c.x >= w.x && c.y < w.y + w.h && c.y >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { c, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var b in Bombs)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(b.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (b.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (b.b >= w.b - max && b.b <= w.b + w.d + max && b.x <= w.x + w.w - 1 && b.x >= w.x && b.y < w.y + w.h && b.y >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { b, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
                foreach (var c in Chains)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(c.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (c.b >= w.b - max && c.b <= w.b + w.d + max && c.tx <= w.x + w.w - 1 && c.tx >= w.x && c.ty < w.y + w.h && c.ty >= w.y - 1)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { c, w }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < Notes.Count; i++)
            {
                var c = Notes[i];
                for (int j = i + 1; j < Notes.Count; j++)
                {
                    var c2 = Notes[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.x == c2.x && c.y == c2.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { c, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
                for (int j = 0; j < Bombs.Count; j++)
                {
                    var b = Bombs[j];
                    BeatPerMinute.BPM.SetCurrentBPM(b.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (b.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= b.b - max && c.b <= b.b + max && c.x == b.x && c.y == b.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { c, b }
                        });
                        issue = CritResult.Fail;
                    }
                }
                for (int j = i + 1; j < Chains.Count; j++)
                {
                    var c2 = Chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.x == c2.tx && c.y == c2.ty)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { c, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < Bombs.Count; i++)
            {
                var b = Bombs[i];
                for (int j = i + 1; j < Bombs.Count; j++)
                {
                    var b2 = Bombs[j];
                    BeatPerMinute.BPM.SetCurrentBPM(b2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (b2.b - b.b >= max)
                    {
                        break;
                    }
                    if (b.b >= b2.b - max && b.b <= b2.b + max && b.x == b2.x && b.y == b2.y)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { b, b2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
                for (int j = i + 1; j < Chains.Count; j++)
                {
                    var c2 = Chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - b.b >= max)
                    {
                        break;
                    }
                    if (b.b >= c2.b - max && b.b <= c2.b + max && b.x == c2.tx && b.y == c2.ty)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { b, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            for (int i = 0; i < Chains.Count; i++)
            {
                var c = Chains[i];
                for (int j = i + 1; j < Chains.Count; j++)
                {
                    var c2 = Chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.tx == c2.tx && c.ty == c2.ty)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Fused Object",
                            Severity = Severity.Error,
                            CheckType = "Fused",
                            Description = "Objects cannot collide within" + max.ToString() + " in the same line",
                            ResultData = new() { new("FusedObject", "True") },
                            BeatmapObjects = new() { c, c2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

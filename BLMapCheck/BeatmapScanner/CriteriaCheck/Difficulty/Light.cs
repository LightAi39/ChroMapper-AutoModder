using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using Parser.Map.Difficulty.V3.Grid;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Light
    {
        public class EventState
        {
            public bool State { get; set; } = false;
            public double Time { get; set; } = 0;
            public double FadeTime { get; set; } = 0;

            public EventState(bool state, double time, double fade)
            {
                State = state;
                Time = time;
                FadeTime = fade;
            }
        }
        public class EventLitTime
        {
            public double Time { get; set; } = 0;
            public bool State { get; set; } = false;

            public EventLitTime(double time, bool state)
            {
                Time = time;
                State = state;
            }
        }

        // Fetch the average event per beat, and compare it to a configurable value
        // Also check for well-lit bombs
        public static CritResult Check(float songLength, List<Basicbeatmapevent> events, List<Lightcoloreventboxgroup> v3events, List<Bombnote> bombs)
        {
            var issue = CritResult.Success;
            var end = BeatPerMinute.BPM.ToBeatTime(songLength, true);
            var lit = true;
            if (!events.Any() || !events.Exists(e => e.et >= 0 && e.et <= 5))
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Light",
                    Severity = Severity.Error,
                    CheckType = "Light",
                    Description = "The map must have sufficient lighting throughout.",
                    ResultData = new() { new("Light", "Error") }
                });
                return CritResult.Fail;
            }
            else
            {
                var lights = events.Where(e => e.et >= 0 && e.et <= 5).OrderBy(e => e.b).ToList();
                var average = lights.Count() / end;
                if (v3events.Count > 0)
                {
                    average = v3events.Count() / end;
                }
                if (average < Instance.AverageLightPerBeat)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Average Light",
                        Severity = Severity.Error,
                        CheckType = "Light",
                        Description = "The map must have sufficient lighting throughout.",
                        ResultData = new() { new("AverageLight", "Current average per beat: " + average.ToString() + " Required: " + Instance.AverageLightPerBeat.ToString()) }
                    });
                    issue = CritResult.Fail;
                }

                if (issue == CritResult.Success)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Light",
                        Severity = Severity.Passed,
                        CheckType = "Light",
                        Description = "Map has light.",
                        ResultData = new() { new("Light", "Passed") }
                    });
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Average Light",
                        Severity = Severity.Passed,
                        CheckType = "Light",
                        Description = "Map has enough light per beat in average.",
                        ResultData = new() { new("AverageLight", "Current average per beat: " + average.ToString() + " Required: " + Instance.AverageLightPerBeat.ToString()) }
                    });
                }

                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var eventLitTime = new List<List<EventLitTime>>();
                if (v3events.Count > 0)
                {
                    //ExtendOverallComment("R6A - Warning - V3 Lights detected. Bombs visibility won't be checked."); TODO: USE NEW METHOD
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Bomb Lit",
                        Severity = Severity.Inconclusive,
                        CheckType = "Light",
                        Description = "V3 Lights detected. Bombs visibility won't be checked.",
                        ResultData = new() { new("BombLit", "Inconclusive") },
                        BeatmapObjects = new(bombs) { }
                    });
                    return CritResult.Warning;
                }
                else
                {
                    for (var i = 0; i < 12; i++)
                    {
                        eventLitTime.Add(new());
                    }
                    for (int i = 0; i < lights.Count; i++)
                    {
                        var ev = lights[i];
                        BeatPerMinute.BPM.SetCurrentBPM(ev.b);
                        var fadeTime = BeatPerMinute.BPM.ToBeatTime((float)Instance.LightFadeDuration, true);
                        var reactTime = BeatPerMinute.BPM.ToBeatTime((float)Instance.LightBombReactionTime, true);
                        if (ev.IsOn || ev.IsFlash || ev.IsFade)
                        {
                            eventLitTime[ev.et].Add(new(ev.b, true));
                            if (ev.IsFade)
                            {
                                eventLitTime[ev.et].Add(new(ev.b + fadeTime, false));
                            }
                        }
                        if (ev.f < 0.25 || ev.IsOff)
                        {
                            eventLitTime[ev.et].Add(new(ev.b + reactTime, false));
                        }
                    }
                    foreach (var elt in eventLitTime)
                    {
                        elt.Reverse();
                    }
                    for (int i = 0; i < bombs.Count; i++)
                    {
                        var bomb = bombs[i];
                        var isLit = false;
                        foreach (var el in eventLitTime)
                        {
                            var t = el.Find(e => e.Time < bomb.b);
                            if (t != null)
                            {
                                isLit = isLit || t.State;
                            }
                        }
                        if (!isLit)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Bomb Lit",
                                Severity = Severity.Inconclusive,
                                CheckType = "Light",
                                Description = "There must be sufficient lighting whenever bombs are present.",
                                ResultData = new() { new("BombLit", isLit.ToString()) },
                                BeatmapObjects = new() { bomb }
                            });
                            lit = false;
                            issue = CritResult.Fail;
                        }
                    }
                }
            }

            if(lit)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Bomb Lit",
                    Severity = Severity.Passed,
                    CheckType = "Light",
                    Description = "Bombs in the map are properly lit (or the map has v3 lights).",
                    ResultData = new() { new("BombLit", "Success") }
                });
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

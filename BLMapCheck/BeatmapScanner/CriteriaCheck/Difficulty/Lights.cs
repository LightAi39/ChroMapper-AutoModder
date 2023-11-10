using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;
using System.Diagnostics;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Lights
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
        public static CritResult Check(float songLength, List<Light> events, List<Lightcoloreventboxgroup> v3events, List<Bomb> bombs)
        {
            var timescale = CriteriaCheckManager.timescale;

            var issue = CritResult.Success;
            var end = timescale.BPM.ToBeatTime(songLength, true);
            var lit = true;
            if ((!events.Any() || !events.Exists(e => e.Type >= 0 && e.Type <= 5)) && !v3events.Any())
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
                var lights = events.Where(e => e.Type >= 0 && e.Type <= 5).OrderBy(e => e.Beats).ToList();
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
                        Description = "Map has enough light per beat in average.",
                        ResultData = new() { new("AverageLight", "Current average per beat: " + average.ToString() + " Required: " + Instance.AverageLightPerBeat.ToString()) }
                    });
                }

                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var eventLitTime = new List<List<EventLitTime>>();
                if (v3events.Any())
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
                    float fadeTime = 0f;
                    float reactTime = 0f;
                    for (var i = 0; i < 12; i++)
                    {
                        eventLitTime.Add(new());
                    }
                    for (int i = 0; i < lights.Count; i++)
                    {
                        var ev = lights[i];
                        timescale.BPM.SetCurrentBPM(ev.Beats);
                        fadeTime = timescale.BPM.ToBeatTime((float)Instance.LightFadeDuration, true);
                        reactTime = timescale.BPM.ToBeatTime((float)Instance.LightBombReactionTime, true);
                        if (ev.isOn || ev.isFlash || ev.isFade)
                        {
                            eventLitTime[ev.Type].Add(new(ev.Beats, true));
                            if (ev.isFade)
                            {
                                eventLitTime[ev.Type].Add(new(ev.Beats + fadeTime, false));
                            }
                        }
                        if (ev.f < 0.25 || ev.isOff)
                        {
                            eventLitTime[ev.Type].Add(new(ev.Beats + reactTime, false));
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
                            var t = el.Find(e => e.Time < bomb.Beats - reactTime);
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
                                Severity = Severity.Error,
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
                    Description = "Bombs in the map are properly lit.",
                    ResultData = new() { new("BombLit", "Success") }
                });
            }

            timescale.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

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
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Light";
            string checkType = "Light";
            CritResult criteria = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            if (songLength == 0)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "Light check error, SongLength is 0. Make sure to use an ogg file");
                return CritResult.Fail;
            }

            var end = timescale.BPM.ToBeatTime(songLength, true);
            var lit = true;

            // Check if there's lights in the map
            if ((!events.Any() || !events.Exists(e => e.Type >= 0 && e.Type <= 5)) && !v3events.Any())
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "The map must have sufficient lighting throughout");
                return CritResult.Fail;
            }
            else
            {
                // Calculate the average amount of light per beat
                var lights = events.Where(e => e.Type >= 0 && e.Type <= 5).OrderBy(e => e.Beats).ToList();
                var average = lights.Count() / end;
                if (v3events.Count > 0)
                {
                    average = v3events.Count() / end;
                }
                if (average < Instance.AverageLightPerBeat)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Average Light", Severity.Error, checkType, "The map must have sufficient lighting throughout", 
                        new() { new("CurrentAvgLightPerBeat", average.ToString()), new("RequiredAvgLightPerBeat", Instance.AverageLightPerBeat.ToString()) });
                    criteria = CritResult.Fail;
                }

                if (criteria == CritResult.Success)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "Map has enough light per beat in average",
                        new() { new("CurrentAvgLightPerBeat", average.ToString()), new("RequiredAvgLightPerBeat", Instance.AverageLightPerBeat.ToString()) });
                }

                // Bombs must be sufficiently lit during their presence
                name = "Bomb Lit";
                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var eventLitTime = new List<List<EventLitTime>>();
                // If V3 lights exist, bomb won't be checked
                if (v3events.Any())
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Inconclusive, checkType, "V3 Lights detected. Bombs visibility won't be checked",
                        new() { new("BombLit", "Inconclusive") });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;
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
                            lit = false;
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                                name, Severity.Error, checkType, "Bombs must be sufficiently lit during their presence",
                                new() { new("BombLit", isLit.ToString()) }, new() { bomb });
                            criteria = CritResult.Fail;
                        }
                    }
                }
            }

            if(lit)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Passed, checkType, "Bombs in the map are properly lit");
            }
            
            timescale.BPM.ResetCurrentBPM();

            return criteria;
        }
    }
}

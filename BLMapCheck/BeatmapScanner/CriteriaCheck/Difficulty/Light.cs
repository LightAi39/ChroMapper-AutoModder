﻿using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

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
        public static CritResult Check(float LoadedSongLength, List<Basicbeatmapevent> Events, List<Lightcoloreventboxgroup> V3Events)
        {
            var issue = CritResult.Success;
            var end = BeatPerMinute.BPM.ToBeatTime(LoadedSongLength, true);
            var bombs = BeatmapScanner.Bombs.OrderBy(b => b.b).ToList();
            if (!Events.Any() || !Events.Exists(e => e.et >= 0 && e.et <= 5))
            {
                //ExtendOverallComment("R6A - Map has no light"); TODO: USE NEW METHOD
                return CritResult.Fail;
            }
            else
            {
                var lights = Events.Where(e => e.et >= 0 && e.et <= 5).OrderBy(e => e.b).ToList();
                var average = lights.Count() / end;
                if (V3Events.Count > 0)
                {
                    average = V3Events.Count() / end;
                }
                if (average < AverageLightPerBeat)
                {
                    //ExtendOverallComment("R6A - Map doesn't have enough light"); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var eventLitTime = new List<List<EventLitTime>>();
                if (V3Events.Count > 0)
                {
                    //ExtendOverallComment("R6A - Warning - V3 Lights detected. Bombs visibility won't be checked."); TODO: USE NEW METHOD
                    issue = CritResult.Warning;
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
                        var fadeTime = BeatPerMinute.BPM.ToBeatTime((float)LightFadeDuration, true);
                        var reactTime = BeatPerMinute.BPM.ToBeatTime((float)LightBombReactionTime, true);
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
                            //CreateDiffCommentBomb("R5B - Light missing for bomb", CommentTypesEnum.Issue, bomb); TODO: USE NEW METHOD
                            issue = CritResult.Fail;
                        }
                    }
                }
            }
            bpm.ResetCurrentBPM();
            return issue;
        }
    }
}

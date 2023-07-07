using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChroMapper_LightModding.BeatmapScanner.MapCheck
{
    // https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/beatmap/shared/bpm.ts
    internal class BeatPerMinute
    {
        private double _bpm { get; set; }
        private List<IBPMChange> _bpmChange { get; set; }
        private List<IBPMTimeScale> _timeScale { get; set; }
        private double _offset { get; set; }

        public BeatPerMinute(double bpm, List<IBPMChange> bpmChange, double offset)
        {
            _bpm = bpm;
            _offset = offset;
            _timeScale = GetTimeScale(bpmChange);
            _bpmChange = GetBpmChangeTime(bpmChange);
        }

        public static BeatPerMinute Create(double bpm, List<BaseBpmEvent> bpmChange, double offset)
        {
            List<IBPMChange> change = new();
            foreach (var bpmEvent in bpmChange)
            {
                change.Add(new(bpmEvent));
            }
            return new BeatPerMinute(bpm, change, offset);
        }

        public double GetValue()
        {
            return _bpm;
        }

        public void SetValue(double bpm)
        {
            _bpm = bpm;
        }

        public List<IBPMChange> GetChange()
        {
            return _bpmChange;
        }

        public void SetChange(List<IBPMChange> bpmChange)
        {
            _bpmChange = bpmChange;
        }

        public List<IBPMTimeScale> GetTimescale()
        {
            return _timeScale;
        }

        public void SetTimescale(List<IBPMTimeScale> timescale)
        {
            _timeScale = timescale;
        }

        public double GetOffset()
        {
            return _offset * 1000;
        }

        public void SetOffset(double offset)
        {
            _offset = offset / 1000;
        }

        public List<IBPMChange> GetBpmChangeTime(List<IBPMChange> bpmc)
        {
            IBPMChange temp = null;
            List<IBPMChange> bpmChange = new();
            bpmc.OrderBy(b => b.b);
            for (int i = 0; i < bpmc.Count; i++)
            {
                var curBPMC = bpmc[i];
                if (temp != null)
                {
                    curBPMC.newTime = Math.Ceiling(((curBPMC.b - temp.b) / _bpm) * temp.m + temp.newTime - 0.01);
                }
                else
                {
                    curBPMC.newTime = Math.Ceiling(curBPMC.b - (_offset * _bpm) / 60 - 0.01);
                }
                bpmChange.Add(curBPMC);
                temp = curBPMC;
            }

            return bpmChange;
        }

        public List<IBPMTimeScale> GetTimeScale(List<IBPMTimeScale> bpmc)
        {
            bpmc = bpmc.OrderBy(b => b.time).ToList();
            List<IBPMTimeScale> timeScale = new();
            IBPMTimeScale ibpm = new();
            foreach (var bpm in bpmc)
            {
                ibpm = new()
                {
                    time = bpm.time,
                    scale = bpm.scale
                };
                timeScale.Add(ibpm);
            }

            return timeScale;
        }

        public List<IBPMTimeScale> GetTimeScale(List<IBPMChange> bpmc)
        {
            bpmc = bpmc.OrderBy(b => b.b).ToList();
            List<IBPMTimeScale> timeScale = new();
            IBPMTimeScale ibpm = new();
            foreach (var bpm in bpmc)
            {
                ibpm = new()
                {
                    time = bpm.b,
                    scale = _bpm / bpm.m
                };
                timeScale.Add(ibpm);
            }

            return timeScale;
        }

        public double OffsetBegone(double beat)
        {
            return ToBeatTime(ToRealTime(beat, false) - _offset);
        }

        public double ToRealTime(double beat, bool timescale = true)
        {
            if (!timescale)
            {
                return (beat / _bpm) * 60;
            }
            double calculatedBeat = 0;
            for (int i = _timeScale.Count - 1; i >= 0; i--)
            {
                if (beat > _timeScale[i].time)
                {
                    calculatedBeat += (beat - _timeScale[i].time) * _timeScale[i].scale;
                    beat = _timeScale[i].time;
                }
            }
            return ((beat + calculatedBeat) / _bpm) * 60;
        }

        public double ToBeatTime(double seconds, bool timescale = false)
        {
            if (!timescale)
            {
                return (seconds * _bpm) / 60;
            }
            double calculatedSecond = 0;
            for (int i = _timeScale.Count - 1; i >= 0; i--)
            {
                var currentSeconds = ToRealTime(_timeScale[i].time);
                if (seconds > currentSeconds)
                {
                    calculatedSecond += (seconds - currentSeconds) / _timeScale[i].scale;
                    seconds = currentSeconds;
                }
            }
            return ToBeatTime(seconds + calculatedSecond);
        }

        public double ToJsonTime(double beat)
        {
            for (int i = _bpmChange.Count - 1; i >= 0; i--)
            {
                if (beat > _bpmChange[i].newTime)
                {
                    return (((beat - _bpmChange[i].newTime) / _bpmChange[i].m) * _bpm + _bpmChange[i].b);
                }
            }
            return ToBeatTime(ToRealTime(beat, false) + _offset);
        }

        public double AdjustTime(double beat)
        {
            for (int i = _bpmChange.Count - 1; i >= 0; i--)
            {
                if (beat > _bpmChange[i].b)
                {
                    return (((beat - _bpmChange[i].b) / _bpm) * _bpmChange[i].m + _bpmChange[i].newTime);
                }
            }
            return OffsetBegone(beat);
        }

        public void SetCurrentBPM(double beat)
        {
            for (int i = 0; i < _bpmChange.Count; i++)
            {
                if (beat > _bpmChange[i].b)
                {
                    _bpm = _bpmChange[i].m;
                }
            }
        }

        public void ResetCurrentBPM()
        {
            if (_bpmChange.Count > 0)
            {
                _bpm = _bpmChange[0].m;
                return;
            }
        }
    }

    internal class IBPMTimeScale
    {
        public double time { get; set; }
        public double scale { get; set; }
    }

    internal class IBPMChange
    {
        public double b { get; set; }
        public double m { get; set; }
        public double p { get; set; }
        public double o { get; set; }
        public double newTime { get; set; }

        public IBPMChange(BaseBpmEvent ev)
        {
            b = ev.JsonTime;
            m = ev.Bpm;
            p = 0;
            o = 0;
            newTime = ev.JsonTime;
        }
    }
}

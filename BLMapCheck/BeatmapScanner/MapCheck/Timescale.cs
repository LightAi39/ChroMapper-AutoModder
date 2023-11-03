using BLMapCheck.Classes.MapVersion.Difficulty;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.MapCheck
{
    // https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/beatmap/shared/bpm.ts
    internal class BeatPerMinute
    {
        public static BeatPerMinute BPM { get; set; }
        private float _bpm { get; set; }
        private List<IBPMChange> _bpmChange { get; set; }
        private List<IBPMTimeScale> _timeScale { get; set; }
        private float _offset { get; set; }

        public BeatPerMinute(float bpm, List<IBPMChange> bpmChange, float offset)
        {
            BPM = this;
            _bpm = bpm;
            _offset = offset;
            _timeScale = GetTimeScale(bpmChange);
            _bpmChange = GetBpmChangeTime(bpmChange);
        }

        public static BeatPerMinute Create(float bpm, List<Bpmevent> bpmChange, float offset)
        {
            List<IBPMChange> change = new();
            foreach (var bpmEvent in bpmChange)
            {
                change.Add(new(bpmEvent));
            }
            return new BeatPerMinute(bpm, change, offset);
        }

        public float GetValue()
        {
            return _bpm;
        }

        public void SetValue(float bpm)
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

        public float GetOffset()
        {
            return _offset * 1000;
        }

        public void SetOffset(float offset)
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
                    curBPMC.newTime = (float)Math.Ceiling(((curBPMC.b - temp.b) / _bpm) * temp.m + temp.newTime - 0.01);
                }
                else
                {
                    curBPMC.newTime = (float)Math.Ceiling(curBPMC.b - (_offset * _bpm) / 60 - 0.01);
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

        public float OffsetBegone(float beat)
        {
            return ToBeatTime(ToRealTime(beat, false) - _offset);
        }

        public float ToRealTime(float beat, bool timescale = true)
        {
            if (!timescale)
            {
                return (beat / _bpm) * 60;
            }
            float calculatedBeat = 0;
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

        public float ToBeatTime(float seconds, bool timescale = false)
        {
            if (!timescale)
            {
                return (seconds * _bpm) / 60;
            }
            float calculatedSecond = 0;
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

        public float ToJsonTime(float beat)
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

        public float AdjustTime(float beat)
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

        public void SetCurrentBPM(float beat)
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
        public float time { get; set; }
        public float scale { get; set; }
    }

    internal class IBPMChange
    {
        public float b { get; set; }
        public float m { get; set; }
        public float p { get; set; }
        public float o { get; set; }
        public float newTime { get; set; }

        public IBPMChange(Bpmevent ev)
        {
            b = ev.b;
            m = ev.m;
            p = 0;
            o = 0;
            newTime = ev.b;
        }
    }
}

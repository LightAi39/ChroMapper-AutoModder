using System;

namespace BLMapCheck.Classes.MapVersion.Difficulty
{
    public class DifficultyV2
    {
        public string _version { get; set; } = "";
        public NoteV2[] _notes { get; set; } = Array.Empty<NoteV2>();
        public SliderV2[] _sliders { get; set; } = Array.Empty<SliderV2>();
        public ObstacleV2[] _obstacles { get; set; } = Array.Empty<ObstacleV2>();
        public EventsV2[] _events { get; set; } = Array.Empty<EventsV2>();
    }

    public class NoteV2
    {
        public float _time { get; set; }
        public int _lineIndex { get; set; }
        public int _lineLayer { get; set; }
        public int _type { get; set; }
        public int _cutDirection { get; set; }
    }

    public class ObstacleV2
    {
        public float _time { get; set; }
        public int _lineIndex { get; set; }
        public int _type { get; set; }
        public float _duration { get; set; }
        public int _width { get; set; }
    }

    public class EventsV2
    {
        public float _time { get; set; }
        public int _type { get; set; }
        public int _value { get; set; }
        public float _floatValue { get; set; }
    }

    public class SliderV2
    {
        public int colorType { get; set; }
        public float _headTime { get; set; }
        public int _headLineIndex { get; set; }
        public int _headLineLayer { get; set; }
        public float _headControlPointLengthMultiplier { get; set; }
        public int _headCutDirection { get; set; }
        public float _tailTime { get; set; }
        public int _tailLineIndex { get; set; }
        public int _tailLineLayer { get; set; }
        public float _tailControlPointLengthMultiplier { get; set; }
        public int _tailCutDirection { get; set; }
        public int _sliderMidAnchorMode { get; set; }
    }
}

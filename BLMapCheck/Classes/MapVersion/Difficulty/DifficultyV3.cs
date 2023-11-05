using System.Collections.Generic;

namespace BLMapCheck.Classes.MapVersion.Difficulty
{
    public class DifficultyV3
    {
        public string version { get; set; }
        public List<Bpmevent> bpmEvents { get; set; }
        public object[] rotationEvents { get; set; }
        public List<Colornote> colorNotes { get; set; }
        public List<Bombnote> bombNotes { get; set; }
        public List<Obstacle> obstacles { get; set; }
        public List<Slider> sliders { get; set; }
        public List<Burstslider> burstSliders { get; set; }
        public object[] waypoints { get; set; }
        public List<Basicbeatmapevent> basicBeatmapEvents { get; set; }
        public List<Colorboostbeatmapevent> colorBoostBeatmapEvents { get; set; }
        public List<Lightcoloreventboxgroup> lightColorEventBoxGroups { get; set; }
        public List<Lightrotationeventboxgroup> lightRotationEventBoxGroups { get; set; }
        public List<Lighttranslationeventboxgroup> lightTranslationEventBoxGroups { get; set; }
        public Basiceventtypeswithkeywords basicEventTypesWithKeywords { get; set; }
        public bool useNormalEventsAsCompatibleEvents { get; set; }
        public Customdata customData { get; set; }
        public object[] vfxEventBoxGroups { get; set; }
        public _Fxeventscollection _fxEventsCollection { get; set; }

        public static DifficultyV3 V2toV3(DifficultyV2 v2)
        {
            DifficultyV3 difficultyV3 = new()
            {
                version = "3.0.0",
                colorNotes = new(),
                bombNotes = new(),
                burstSliders = new(),
                sliders = new(),
                obstacles = new(),
                basicBeatmapEvents = new(),
                lightColorEventBoxGroups = new(),
                bpmEvents = new(),
            };
            foreach (var note in v2._notes)
            {
                if (note._type == 0 || note._type == 1)
                {
                    Colornote colornote = new()
                    {
                        d = note._cutDirection,
                        c = note._type,
                        b = note._time,
                        x = note._lineIndex,
                        y = note._lineLayer,
                        a = 0
                    };
                    difficultyV3.colorNotes.Add(colornote);
                }
                else if (note._type == 3)
                {
                    Bombnote bombnote = new()
                    {
                        b = note._time,
                        x = note._lineIndex,
                        y = note._lineLayer,
                    };
                    difficultyV3.bombNotes.Add(bombnote);
                }
            }
            foreach (var obstacle in v2._obstacles)
            {
                Obstacle obs = new()
                {
                    b = obstacle._time,
                    x = obstacle._lineIndex,
                    w = obstacle._width,
                    d = obstacle._duration
                };
                if (obstacle._type == 0)
                {
                    obs.y = 0;
                    obs.h = 5;
                }
                else if (obstacle._type == 1)
                {
                    obs.y = 2;
                    obs.h = 3;
                }
                difficultyV3.obstacles.Add(obs);
            }
            foreach (var arc in v2._sliders)
            {
                Slider slider = new()
                {
                    c = arc.colorType,
                    d = arc._headCutDirection,
                    b = arc._headTime,
                    tb = arc._tailTime,
                    x = arc._headLineIndex,
                    tx = arc._tailLineIndex,
                    y = arc._headLineLayer,
                    ty = arc._tailLineLayer,
                    m = arc._sliderMidAnchorMode,
                    mu = arc._headControlPointLengthMultiplier,
                    tmu = arc._tailControlPointLengthMultiplier
                };
                difficultyV3.sliders.Add(slider);
            }
            foreach (var ev in v2._events)
            {
                Basicbeatmapevent basic = new Basicbeatmapevent
                {
                    b = ev._time,
                    et = ev._type,
                    i = ev._value,
                    f = ev._floatValue
                };
                difficultyV3.basicBeatmapEvents.Add(basic);
            }
            return difficultyV3;
        }
    }

    public class Basiceventtypeswithkeywords
    {
        public object[] d { get; set; }
    }

    public class Customdata
    {
        public float time { get; set; }
        public List<Bookmark> bookmarks { get; set; }
        public bool bookmarksUseOfficialBpmEvents { get; set; }
        public List<Environment> environment { get; set; }
    }

    public class Bookmark
    {
        public float b { get; set; }
        public string n { get; set; }
        public float[] c { get; set; }
    }

    public class Environment
    {
        public string id { get; set; }
        public string lookupMethod { get; set; }
        public Components components { get; set; }
        public bool active { get; set; }
        public float[] position { get; set; }
        public float[] scale { get; set; }
        public int[] rotation { get; set; }
        public Geometry geometry { get; set; }
        public int duplicate { get; set; }
    }

    public class Components
    {
        public Bloomfogenvironment BloomFogEnvironment { get; set; }
    }

    public class Bloomfogenvironment
    {
        public float attenuation { get; set; }
        public int startY { get; set; }
        public int height { get; set; }
        public string track { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public Material material { get; set; }
    }

    public class Material
    {
        public float[] color { get; set; }
        public string shader { get; set; }
    }

    public class _Fxeventscollection
    {
        public object[] _il { get; set; }
        public object[] _fl { get; set; }
    }

    public class Bpmevent : BeatmapObject
    {
        public float m { get; set; }

        public Bpmevent(float bpm, float beat = 0)
        {
            b = beat;
            m = bpm;
        }
    }

    public class Colornote : BeatmapGridObject
    {
        public int a { get; set; }
        public int c { get; set; }
        public int d { get; set; }
    }

    public class Bombnote : BeatmapGridObject
    {

    }

    public class Obstacle : BeatmapGridObject
    {
        public float d { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }

    public class Slider : BeatmapGridObject
    {

        public int c { get; set; }
        public int d { get; set; }
        public float mu { get; set; }
        public float tb { get; set; }
        public int tx { get; set; }
        public int ty { get; set; }
        public int tc { get; set; }
        public float tmu { get; set; }
        public int m { get; set; }
    }

    public class Burstslider : BeatmapGridObject
    {

        public int c { get; set; }
        public int d { get; set; }
        public float tb { get; set; }
        public int tx { get; set; }
        public int ty { get; set; }
        public int sc { get; set; }
        public float s { get; set; }
    }

    public class Basicbeatmapevent : BeatmapObject
    {
        public int et { get; set; }
        public int i { get; set; }
        public float f { get; set; }
        public Customdata1 customData { get; set; }
        public bool IsBlue => et == 1 || et == 2 || et == 3 || et == 4;
        public bool IsRed => et == 5 || et == 6 || et == 7 || et == 8;
        public bool IsWhite => et == 9 || et == 10 || et == 11 || et == 12;
        public bool IsOff => et == 0;
        public bool IsOn => et == 1 || et == 5 || et == 9;
        public bool IsFlash => et == 2 || et == 6 || et == 10;
        public bool IsFade => et == 3 || et == 7 || et == 11;
        public bool IsTransition => et == 4 || et == 8 || et == 12;
    }

    public class Customdata1
    {
        public float[] color { get; set; }
        public int rotation { get; set; }
        public int prop { get; set; }
        public float speed { get; set; }
        public float step { get; set; }
        public int[] lightID { get; set; }
        public int direction { get; set; }
        public bool lockRotation { get; set; }
        public string lerpType { get; set; }
        public string easing { get; set; }
    }

    public class Colorboostbeatmapevent
    {
        public float b { get; set; }
        public bool o { get; set; }
    }

    public class Lightcoloreventboxgroup
    {
        public float b { get; set; }
        public int g { get; set; }
        public E[] e { get; set; }
    }

    public class E
    {
        public F f { get; set; }
        public float w { get; set; }
        public int d { get; set; }
        public float r { get; set; }
        public int t { get; set; }
        public int b { get; set; }
        public int i { get; set; }
        public E1[] e { get; set; }
    }

    public class F
    {
        public int f { get; set; }
        public int p { get; set; }
        public int t { get; set; }
        public float r { get; set; }
        public int c { get; set; }
        public int n { get; set; }
        public float s { get; set; }
        public float l { get; set; }
        public int d { get; set; }
    }

    public class E1
    {
        public float b { get; set; }
        public int c { get; set; }
        public float s { get; set; }
        public int i { get; set; }
        public int f { get; set; }
    }

    public class Lightrotationeventboxgroup
    {
        public float b { get; set; }
        public int g { get; set; }
        public E2[] e { get; set; }
    }

    public class E2
    {
        public F1 f { get; set; }
        public float w { get; set; }
        public int d { get; set; }
        public float s { get; set; }
        public int t { get; set; }
        public int b { get; set; }
        public int a { get; set; }
        public float r { get; set; }
        public int i { get; set; }
        public L[] l { get; set; }
    }

    public class F1
    {
        public int f { get; set; }
        public int p { get; set; }
        public int t { get; set; }
        public float r { get; set; }
        public int c { get; set; }
        public int n { get; set; }
        public float s { get; set; }
        public float l { get; set; }
        public int d { get; set; }
    }

    public class L
    {
        public float b { get; set; }
        public float r { get; set; }
        public int o { get; set; }
        public int e { get; set; }
        public float l { get; set; }
        public int p { get; set; }
    }

    public class Lighttranslationeventboxgroup
    {
        public float b { get; set; }
        public int g { get; set; }
        public E3[] e { get; set; }
    }

    public class E3
    {
        public F2 f { get; set; }
        public float w { get; set; }
        public int d { get; set; }
        public float s { get; set; }
        public int t { get; set; }
        public int b { get; set; }
        public int a { get; set; }
        public float r { get; set; }
        public int i { get; set; }
        public L1[] l { get; set; }
    }

    public class F2
    {
        public int f { get; set; }
        public int p { get; set; }
        public int t { get; set; }
        public float r { get; set; }
        public int c { get; set; }
        public int n { get; set; }
        public float s { get; set; }
        public float l { get; set; }
        public int d { get; set; }
    }

    public class L1
    {
        public float b { get; set; }
        public int p { get; set; }
        public int e { get; set; }
        public float t { get; set; }
    }

    public class BeatmapObject
    {
        public float b { get; set; }
    }

    public class BeatmapGridObject : BeatmapObject
    {
        public int x { get; set; }
        public int y { get; set; }
    }
}

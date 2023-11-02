using Newtonsoft.Json;

namespace BLMapCheck.Classes.MapVersion.Difficulty
{
    public class V3
    {
        public string version { get; set; }
        public Bpmevent[] bpmEvents { get; set; }
        public object[] rotationEvents { get; set; }
        public Colornote[] colorNotes { get; set; }
        public Bombnote[] bombNotes { get; set; }
        public Obstacle[] obstacles { get; set; }
        public Slider[] sliders { get; set; }
        public Burstslider[] burstSliders { get; set; }
        public object[] waypoints { get; set; }
        public Basicbeatmapevent[] basicBeatmapEvents { get; set; }
        public object[] colorBoostBeatmapEvents { get; set; }
        public object[] lightColorEventBoxGroups { get; set; }
        public object[] lightRotationEventBoxGroups { get; set; }
        public object[] lightTranslationEventBoxGroups { get; set; }
        public Basiceventtypeswithkeywords basicEventTypesWithKeywords { get; set; }
        public bool useNormalEventsAsCompatibleEvents { get; set; }
        public Customdata customData { get; set; }
        public object[] vfxEventBoxGroups { get; set; }
        public _Fxeventscollection _fxEventsCollection { get; set; }
    }

    public class Basiceventtypeswithkeywords
    {
        public object[] d { get; set; }
    }

    public class Customdata
    {
        public float time { get; set; }
        public Bookmark[] bookmarks { get; set; }
        public bool bookmarksUseOfficialBpmEvents { get; set; }
        public Environment[] environment { get; set; }
    }

    public class Bookmark
    {
        public int b { get; set; }
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

    public class Bpmevent
    {
        public int b { get; set; }
        public int m { get; set; }
    }

    public class Colornote
    {
        public float b { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int a { get; set; }
        public int c { get; set; }
        public int d { get; set; }
    }

    public class Bombnote
    {
        public float b { get; set; }
        public int x { get; set; }
        public int y { get; set; }
    }

    public class Obstacle
    {
        public float b { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public float d { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }

    public class Slider
    {
        public float b { get; set; }
        public int c { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int d { get; set; }
        public float mu { get; set; }
        public float tb { get; set; }
        public int tx { get; set; }
        public int ty { get; set; }
        public int tc { get; set; }
        public float tmu { get; set; }
        public int m { get; set; }
    }

    public class Burstslider
    {
        public float b { get; set; }
        public int c { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int d { get; set; }
        public float tb { get; set; }
        public int tx { get; set; }
        public int ty { get; set; }
        public int sc { get; set; }
        public int s { get; set; }
    }

    public class Basicbeatmapevent
    {
        public float b { get; set; }
        public int et { get; set; }
        public int i { get; set; }
        public int f { get; set; }
        public Customdata1 customData { get; set; }
    }

    public class Customdata1
    {
        public float[] color { get; set; }
        public int rotation { get; set; }
        public int prop { get; set; }
        public float speed { get; set; }
        public int step { get; set; }
        public int[] lightID { get; set; }
        public int direction { get; set; }
        public bool lockRotation { get; set; }
        public string lerpType { get; set; }
        public string easing { get; set; }
    }
}

using BLMapCheck.Classes.MapVersion.Difficulty;

namespace BLMapCheck.BeatmapScanner.Data
{
    internal class Cube
    {
        public float Time { get; set; } = 0;
        public int Line { get; set; } = 0;
        public int Layer { get; set; } = 0;
        public int Type { get; set; } = 0;
        public int CutDirection { get; set; } = 0;
        public double AngleOffset { get; set; } = 0;
        public double Direction { get; set; } = 8;
        public bool Head { get; set; } = false;
        public bool Pattern { get; set; } = false;
        public bool Slider { get; set; } = false;
        public double Precision { get; set; } = 0;
        public double Spacing { get; set; } = 0;
        public bool Linear { get; set; } = false;

        public Cube()
        {
        }

        public Cube(Cube cube)
        {
            AngleOffset = cube.AngleOffset;
            CutDirection = cube.CutDirection;
            Type = cube.Type;
            Time = cube.Time;
            Line = cube.Line;
            Layer = cube.Layer;
            Direction = cube.Direction;
        }


        public Cube(Colornote note)
        {
            AngleOffset = note.a;
            CutDirection = note.d;
            Type = note.c;
            Time = note.b;
            Line = note.x;
            Layer = note.y;
            Direction = note.d;
        }
    }
}

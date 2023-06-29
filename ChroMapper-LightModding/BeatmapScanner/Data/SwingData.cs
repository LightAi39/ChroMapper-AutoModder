namespace ChroMapper_LightModding.BeatmapScanner.Data
{
    internal class SwingData
    {
        public double Time { get; set; } = 0;
        public double Angle { get; set; } = 0;
        public (double x, double y) EntryPosition { get; set; } = (0, 0);
        public (double x, double y) ExitPosition { get; set; } = (0, 0);
        public double SwingFrequency { get; set; } = 0;
        public double SwingDiff { get; set; } = 0;
        public bool Forehand { get; set; } = true;
        public bool Reset { get; set; } = false;
        public double PathStrain { get; set; } = 0;
        public double AngleStrain { get; set; } = 0;
        public double AnglePathStrain { get; set; } = 0;
        public double PreviousDistance { get; set; } = 0;
        public double PositionComplexity { get; set; } = 0;
        public double CurveComplexity { get; set; } = 0;

        public SwingData()
        {

        }

        public SwingData(double beat, double angle)
        {
            Time = beat;
            Angle = angle;
        }
    }

    internal class SData
    {
        public double HitDistance { get; set; } = 0;
        public double HitDiff { get; set; } = 0;
        public double Stress { get; set; } = 0;
        public double SwingSpeed { get; set; } = 0;

        public SData(double ss)
        {
            SwingSpeed = ss;
        }
    }
}

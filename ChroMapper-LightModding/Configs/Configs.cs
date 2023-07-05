namespace ChroMapper_LightModding.Configs
{
    public class Configs
    {
        public int MaxChar { get; set; } = 30;
        public double HotStartDuration { get; set; } = 1.33;
        public double ColdEndDuration { get; set; } = 2;
        public double MinSongDuration { get; set; } = 45;
        public double FusedObjectDuration { get; set; } = 0.03;
        public double AverageLightPerBeat { get; set; } = 1;
        public double LightFadeDuration { get; set; } = 1;
        public double LightBombReactionTime { get; set; } = 0.25;
        public double MinimumWallDuration { get; set; } = 0.0138;
        public double ShortWallTrailDuration { get; set; } = 0.25;
        public double MaximumDodgeWallPerSecond { get; set; } = 3.5;
        public double SubjectiveDodgeWallPerSecond { get; set; } = 2;
        public double MaxChainRotation { get; set; } = 30;
        public double MaxChainBeatLength { get; set; } = 0.45;
        public double ChainLinkVsAir { get; set; } = 0.75;
        public double VBMinBottomNoteTime { get; set; } = 0.075;
        public double VBMaxOuterNoteTime { get; set; } = 0.15;
        public double VBMinPatternTime { get; set; } = 0.20;
        public double VBMinNoteTime { get; set; } = 0.25;
        public double VBMaxBombTime { get; set; } = 0.15;
        public double VBMinBombTime { get; set; } = 0.20;
        public double VBMinimum { get; set; } = 0.025;
        public double ParityWarningAngle { get; set; } = 180;
        public bool ParityDebug { get; set; } = false;
    }
}

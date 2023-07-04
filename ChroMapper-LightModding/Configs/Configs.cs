namespace ChroMapper_LightModding.Configs
{
    public class Configs
    {
        public int MaxChar { get; set; } = 30;
        public double HotStartDuration { get; set; } = 1.33;
        public double ColdEndDuration { get; set; } = 2;
        public double MinSongDuration { get; set; } = 45;
        public double FusedElementDuration { get; set; } = 0.03;
        public double AverageLightPerBeat { get; set; } = 1;
        public double LightFadeDuration { get; set; } = 1;
        public double LightBombReactionTime { get; set; } = 0.25;
        public double MinimumWallDuration { get; set; } = 0.0138;
        public double ShortWallTrailDuration { get; set; } = 0.25;
        public double MaximumDodgeWallPerSecond { get; set; } = 2;
        public int MaxChainRotation { get; set; } = 30;
        public double MaxChainBeatLength { get; set; } = 0.4;
        public double ChainLinkVsAir { get; set; } = 1.5;
        public double VBMinimumNoteTime { get; set; } = 0.15;
        public double VBMaximumNoteTime { get; set; } = 0.25;
        public double VBMinimumBombTime { get; set; } = 0.15;
        public double VBMaximumBombTime { get; set; } = 0.20;
        public double VBAllowedMinimum { get; set; } = 0.05;
        public int ParityWarningAngle { get; set; } = 180;
        public bool ParityDebug { get; set; } = false;
    }
}

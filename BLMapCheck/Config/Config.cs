namespace BLMapCheck.Config
{
    public static class Config
    {
        public static int Version { get; set; } = 0; // Do not change this
        public static int MaxChar { get; set; } = 30;
        public static double HotStartDuration { get; set; } = 1.33;
        public static double ColdEndDuration { get; set; } = 2;
        public static double MinSongDuration { get; set; } = 45;
        public static double FusedDistance { get; set; } = 0.5;
        public static double AverageLightPerBeat { get; set; } = 1;
        public static double LightFadeDuration { get; set; } = 1;
        public static double LightBombReactionTime { get; set; } = 0.25;
        public static double MinimumWallDuration { get; set; } = 0.0138;
        public static double ShortWallTrailDuration { get; set; } = 0.25;
        public static double MaximumDodgeWallPerSecond { get; set; } = 3.5;
        public static double SubjectiveDodgeWallPerSecond { get; set; } = 2.5;
        public static double MaxChainRotation { get; set; } = 30;
        public static double ChainLinkVsAir { get; set; } = 1.333;
        public static double VBMinBottomNoteTime { get; set; } = 0.075;
        public static double VBMaxOuterNoteTime { get; set; } = 0.15;
        public static double VBMaxBombTime { get; set; } = 0.15;
        public static double VBMinBombTime { get; set; } = 0.20;
        public static double VBMinimum { get; set; } = 0.025;
        public static double ParityWarningAngle { get; set; } = 180;
        public static bool DisplayBadcut { get; set; } = true;
        public static bool HighlightOffbeat { get; set; } = true;
        public static bool DisplayFlick { get; set; } = true;
        public static bool ParityInvertedWarning { get; set; } = true;
        public static bool ParityDebug { get; set; } = false;
    }
}

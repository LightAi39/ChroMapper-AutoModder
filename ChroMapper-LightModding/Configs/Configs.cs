namespace ChroMapper_LightModding.Configs
{
    internal static class Configs
    {
        public static int MaxChar { get; set; } = 30;
        public static double HotStartDuration { get; set; } = 1.33;
        public static double ColdEndDuration { get; set; } = 2;
        public static double MinSongDuration { get; set; } = 45;
        public static double FusedElementDuration { get; set; } = 0.03;
        public static double AverageLightPerBeat { get; set; } = 1;
        public static double LightFadeDuration { get; set; } = 1;
        public static double LightBombReactionTime { get; set; } = 0.25;
        public static double MinimumWallDuration { get; set; } = 0.0138;
        public static double ShortWallTrailDuration { get; set; } = 0.25;
        public static double MaximumDodgeWallPerSecond { get; set; } = 2;
        public static int MaxChainRotation { get; set; } = 30;
        public static double MaxChainBeatLength { get; set; } = 0.4;
        public static int ParityWarningThreshold { get; set; } = 60;
        public static int ParityErrorThreshold { get; set; } = 45;
        public static int ParityAllowedRotation { get; set; } = 90;
        public static double VBMinimumNoteTime { get; set; } = 0.15;
        public static double VBMaximumNoteTime { get; set; } = 0.25;
        public static double VBMinimumBombTime { get; set; } = 0.15;
        public static double VBMaximumBombTime { get; set; } = 0.20;
        public static double VBAllowedMinimum { get; set; } = 0.05;
    }
}

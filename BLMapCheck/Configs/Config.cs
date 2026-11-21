namespace BLMapCheck.Configs
{
    // Settings of a BLMapChecker. A new Config contains the default values.
    public class Config
    {
        public int Version { get; set; } = 0; // Do not change this
        public int MaxChar { get; set; } = 10;
        public double HotStartDuration { get; set; } = 1.5;
        public double ColdEndDuration { get; set; } = 2;
        public double MinSongDuration { get; set; } = 45;
        public double FusedDistance { get; set; } = 0.5;
        public double AverageLightPerBeat { get; set; } = 1;
        public double LightFadeDuration { get; set; } = 1;
        public double LightBombReactionTime { get; set; } = 0.25;
        public double LightBombMinimumBrightness { get; set; } = 0.5;
        public double LightBombOffGrace { get; set; } = 0.25;
        public double BombSwingPathBeats { get; set; } = 0.125;
        public double MinimumWallDuration { get; set; } = 0.0138;
        public double ShortWallTrailDuration { get; set; } = 0.25;
        public double ShortWallAnyTrailDuration { get; set; } = 0.1;
        public double CrouchWallVisionBlockBeats { get; set; } = 0.5;
        public double SubjectiveDodgeWallPerSecond { get; set; } = 2;
        public double WallMinimumAvoidTime { get; set; } = 0.25;
        public double WallExceptionalAvoidTime { get; set; } = 0.235;
        public double VNJSHittableRange { get; set; } = 1.5;
        public double VNJSFarDistance { get; set; } = 3;
        public double MaxChainRotation { get; set; } = 45;
        public double ChainLinkVsAir { get; set; } = 1.333;
        public double VBMinBottomNoteTime { get; set; } = 0.075;
        public double VBMaxOuterNoteTime { get; set; } = 0.15;
        public double VBMaxBombTime { get; set; } = 0.15;
        public double VBMinBombTime { get; set; } = 0.20;
        public double VBMinimum { get; set; } = 0.025;
        public bool UseMapRT { get; set; } = false;
        public double ParityWarningAngle { get; set; } = 180;
        public bool HighlightOffbeat { get; set; } = false;
        public bool HighlightInline { get; set; } = false;
        public double InlineBeatPrecision { get; set; } = 3;
        public bool DisplayFlick { get; set; } = false;
        public double FlickBeatPrecision { get; set; } = 4;
        public bool DisplayShrado { get; set; } = false;
        public double ShradoMaxBeat { get; set; } = 0.75;
        public bool AutomaticSliderPrecision { get; set; } = true;
        public double SliderPrecision { get; set; } = 0.0625;
        public bool ChainConsistency { get; set; } = false;
        public double ChainPrecision { get; set; } = 16;
        public bool DisplayAngleOffset { get; set; } = false;
        public bool AngleOffsetIgnore45 { get; set; } = false;
        public bool DetectModdedObjects { get; set; } = false;
        public bool DetectSongBpm { get; set; } = true;
        public bool ParityInvertedWarning { get; set; } = true;
        public bool ParityDebug { get; set; } = false;
    }
}

namespace BLMapCheck.Configs
{
    public class Config
    {

        private static Config _instance;

        private Config() { }

        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Config();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public Config Reset()
        {
            _instance = new();
            return _instance;
        }

        public int Version { get; set; } = 0; // Do not change this
        public int MaxChar { get; set; } = 10;
        public double HotStartDuration { get; set; } = 1.5;
        public double ColdEndDuration { get; set; } = 2;
        public double MinSongDuration { get; set; } = 45;
        public double FusedDistance { get; set; } = 0.5;
        public double AverageLightPerBeat { get; set; } = 1;
        public double LightFadeDuration { get; set; } = 1;
        public double LightBombReactionTime { get; set; } = 0.25;
        public double MinimumWallDuration { get; set; } = 0.0138;
        public double ShortWallTrailDuration { get; set; } = 0.25;
        public double MaximumDodgeWallPerSecond { get; set; } = 3.5;
        public double SubjectiveDodgeWallPerSecond { get; set; } = 2.5;
        public double MaxChainRotation { get; set; } = 45;
        public double ChainLinkVsAir { get; set; } = 1.333;
        public double VBMinBottomNoteTime { get; set; } = 0.075;
        public double VBMaxOuterNoteTime { get; set; } = 0.15;
        public double VBMaxBombTime { get; set; } = 0.15;
        public double VBMinBombTime { get; set; } = 0.20;
        public double VBMinimum { get; set; } = 0.025;
        public bool UseMapRT { get; set; } = false;
        public double ParityWarningAngle { get; set; } = 180;
        public bool DisplayBadcut { get; set; } = false;
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
        public bool ParityInvertedWarning { get; set; } = true;
        public bool ParityDebug { get; set; } = false;
    }
}

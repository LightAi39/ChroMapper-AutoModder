using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.Data.Criteria
{
    public class DiffCrit
    {
        public CritSeverity HotStart { get; set; } = CritSeverity.Fail;
        public CritSeverity ColdEnd { get; set; } = CritSeverity.Fail;
        public CritSeverity MinSongDuration { get; set; } = CritSeverity.Fail;
        public CritSeverity Outside { get; set; } = CritSeverity.Fail;
        public CritSeverity ProlongedSwing { get; set; } = CritSeverity.Fail;
        public CritSeverity VisionBlock { get; set; } = CritSeverity.Fail;
        public CritSeverity Parity { get; set; } = CritSeverity.Fail;
        public CritSeverity Chain { get; set; } = CritSeverity.Fail;
        public CritSeverity FusedObject { get; set; } = CritSeverity.Fail;
        public CritSeverity Loloppe { get; set; } = CritSeverity.Fail;
        public CritSeverity HandClap { get; set; } = CritSeverity.Fail;
        public CritSeverity SwingPath { get; set; } = CritSeverity.Fail;
        public CritSeverity Hitbox { get; set; } = CritSeverity.Fail;
        public CritSeverity Slider { get; set; } = CritSeverity.Fail;
        public CritSeverity Wall { get; set; } = CritSeverity.Fail;
        public CritSeverity Light { get; set; } = CritSeverity.Fail;
        public CritSeverity DifficultyLabelSize { get; set; } = CritSeverity.Fail;
        public CritSeverity DifficultyName { get; set; } = CritSeverity.Fail;
        public CritSeverity Requirement { get; set; } = CritSeverity.Fail;
        public CritSeverity NJS { get; set; } = CritSeverity.Fail;

        public CritSeverity HighestSeverityCheck()
        {
            DiffCrit diffCrit = this;
            var properties = typeof(DiffCrit).GetProperties();
            CritSeverity highestSeverity = CritSeverity.Success;

            foreach (var property in properties)
            {
                CritSeverity propertySeverity = (CritSeverity)property.GetValue(diffCrit);
                if (propertySeverity > highestSeverity)
                {
                    highestSeverity = propertySeverity;
                }
            }

            return highestSeverity;
        }

    }
}

using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;

namespace ChroMapper_LightModding.BeatmapScanner.Data.Criteria
{
    public class DiffCrit
    {
        public Severity HotStart { get; set; } = Severity.Fail;
        public Severity ColdEnd { get; set; } = Severity.Fail;
        public Severity MinSongDuration { get; set; } = Severity.Fail;
        public Severity Outside { get; set; } = Severity.Fail;
        public Severity ProlongedSwing { get; set; } = Severity.Fail;
        public Severity VisionBlock { get; set; } = Severity.Fail;
        public Severity Parity { get; set; } = Severity.Fail;
        public Severity Chain { get; set; } = Severity.Fail;
        public Severity FusedObject { get; set; } = Severity.Fail;
        public Severity Loloppe { get; set; } = Severity.Fail;
        public Severity HandClap { get; set; } = Severity.Fail;
        public Severity SwingPath { get; set; } = Severity.Fail;
        public Severity Hitbox { get; set; } = Severity.Fail;
        public Severity Slider { get; set; } = Severity.Fail;
        public Severity Wall { get; set; } = Severity.Fail;
        public Severity Light { get; set; } = Severity.Fail;
        public Severity DifficultyLabelSize { get; set; } = Severity.Fail;
        public Severity DifficultyName { get; set; } = Severity.Fail;
        public Severity Requirement { get; set; } = Severity.Fail;
        public Severity NJS { get; set; } = Severity.Fail;

        public Severity HighestSeverityCheck()
        {
            DiffCrit diffCrit = this;
            var properties = typeof(DiffCrit).GetProperties();
            Severity highestSeverity = Severity.Success;

            foreach (var property in properties)
            {
                Severity propertySeverity = (Severity)property.GetValue(diffCrit);
                if (propertySeverity > highestSeverity)
                {
                    highestSeverity = propertySeverity;
                }
            }

            return highestSeverity;
        }

    }
}

using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;

namespace ChroMapper_LightModding.BeatmapScanner.Data.Criteria
{
    public class DiffCrit
    {
        public Severity HotStart { get; set; } = Severity.Fail;
        public Severity ColdEnd { get; set; } = Severity.Fail;
        public Severity MinSongDuration { get; set; } = Severity.Fail;
        public Severity Outside { get; set; } = Severity.Fail;
        public Severity ProlongedSwing { get; set; } = Severity.Fail; // automated
        public Severity VisionBlock { get; set; } = Severity.Fail; // semi-automated, will detect some Severity.Fail positive that the user will have to check. min and max ms should be configurable.
        public Severity Parity { get; set; } = Severity.Fail; // semi-automated, will detect some Severity.Fail positive that the user will have to check.
        public Severity Chain { get; set; } = Severity.Fail;
        public Severity FusedElement { get; set; } = Severity.Fail;
        public Severity Loloppe { get; set; } = Severity.Fail; // automated, no parallel notes.
        public Severity HandClap { get; set; } = Severity.Fail; // semi-automated, mark specific position + angle as issue, otherwise subjective
        public Severity SwingPath { get; set; } = Severity.Fail; // automated, no note or bomb blocking path of another note
        public Severity Hitbox { get; set; } = Severity.Fail; // automated, no bad/good hitbox overlap by more than 20%
        public Severity Slider { get; set; } = Severity.Fail;
        public Severity Wall { get; set; } = Severity.Fail;
        public Severity Light { get; set; } = Severity.Fail;
        public Severity DifficultyLabelSize { get; set; } = Severity.Fail;
        public Severity DifficultyName { get; set; } = Severity.Fail;
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

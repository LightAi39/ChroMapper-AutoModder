using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.Data.Criteria
{
    public class DiffCrit
    {
        public CritResult HotStart { get; set; } = CritResult.Fail;
        public CritResult ColdEnd { get; set; } = CritResult.Fail;
        public CritResult MinSongDuration { get; set; } = CritResult.Fail;
        public CritResult Outside { get; set; } = CritResult.Fail;
        public CritResult ProlongedSwing { get; set; } = CritResult.Fail;
        public CritResult VisionBlock { get; set; } = CritResult.Fail;
        public CritResult Parity { get; set; } = CritResult.Fail;
        public CritResult Chain { get; set; } = CritResult.Fail;
        public CritResult FusedObject { get; set; } = CritResult.Fail;
        public CritResult Loloppe { get; set; } = CritResult.Fail;
        public CritResult HandClap { get; set; } = CritResult.Fail;
        public CritResult SwingPath { get; set; } = CritResult.Fail;
        public CritResult Hitbox { get; set; } = CritResult.Fail;
        public CritResult Slider { get; set; } = CritResult.Fail;
        public CritResult Wall { get; set; } = CritResult.Fail;
        public CritResult Light { get; set; } = CritResult.Fail;
        public CritResult DifficultyLabelSize { get; set; } = CritResult.Fail;
        public CritResult DifficultyName { get; set; } = CritResult.Fail;
        public CritResult Requirement { get; set; } = CritResult.Fail;
        public CritResult NJS { get; set; } = CritResult.Fail;
        public CritResult Bomb { get; set; } = CritResult.Fail;

        // Every criterion set to NotChecked
        public static DiffCrit NotChecked()
        {
            DiffCrit diffCrit = new();
            foreach (var property in typeof(DiffCrit).GetProperties())
            {
                property.SetValue(diffCrit, CritResult.NotChecked);
            }
            return diffCrit;
        }

        public CritResult HighestSeverityCheck()
        {
            DiffCrit diffCrit = this;
            var properties = typeof(DiffCrit).GetProperties();
            if (properties.All(p => (CritResult)p.GetValue(diffCrit) == CritResult.NotChecked)) return CritResult.NotChecked;
            CritResult highestSeverity = CritResult.Success;

            foreach (var property in properties)
            {
                CritResult propertySeverity = (CritResult)property.GetValue(diffCrit);
                if (propertySeverity > highestSeverity)
                {
                    highestSeverity = propertySeverity;
                }
            }

            return highestSeverity;
        }

    }
}

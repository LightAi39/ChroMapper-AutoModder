using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SongDuration
    {
        // Detect if the mapped duration is above the minimum required, from first note to last note, configurable setting is available
        public static CritResult Check()
        {
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderBy(c => c.Time).ToList();
            var duration = BeatPerMinute.BPM.ToRealTime(cube.Last().Time - cube.First().Time, true);
            if (duration < MinSongDuration)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Mapped Duration",
                    Severity = Severity.Error,
                    CheckType = "Duration",
                    Description = "The map from first note to last note must be at least 45 seconds in length.",
                    ResultData = new() { new("MappedDuration", "Current map duration is " + duration.ToString() + "s. Minimum required duration is " + MinSongDuration.ToString() + "s.") },
                });
                return CritResult.Fail;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Characteristic = CriteriaCheckManager.Characteristic,
                Difficulty = CriteriaCheckManager.Difficulty,
                Name = "Mapped Duration",
                Severity = Severity.Passed,
                CheckType = "Duration",
                Description = "The map from first note to last note must be at least 45 seconds in length.",
                ResultData = new() { new("MappedDuration", "Current map duration is " + duration.ToString() + "s. Minimum required duration is " + MinSongDuration.ToString() + "s.") },
            });

            return CritResult.Success;
        }
    }
}

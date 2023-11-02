using BLMapCheck.BeatmapScanner.MapCheck;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SongDuration
    {
        // Detect if the mapped duration is above the minimum required, from first note to last note, configurable setting is available
        public static Severity Check()
        {
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderBy(c => c.Time).ToList();
            var duration = BeatPerMinute.BPM.ToRealTime(cube.Last().Time - cube.First().Time, true);
            if (duration < MinSongDuration)
            {
                //ExtendOverallComment("R1F - Current map duration is " + duration.ToString() + "s. Minimum required duration is " + config.MinSongDuration.ToString() + "s."); TODO: USE NEW METHOD
                return Severity.Fail;
            }

            return Severity.Success;
        }
    }
}

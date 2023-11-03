using BLMapCheck.BeatmapScanner.MapCheck;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public CritSeverity Check(float LoadedSongLength)
        {
            var issue = CritSeverity.Success;
            var cubes = BeatmapScanner.Cubes;
            var chains = BeatmapScanner.Chains;
            var bombs = BeatmapScanner.Bombs;
            var walls = BeatmapScanner.Walls;

            var end = BeatPerMinute.BPM.ToBeatTime(LoadedSongLength, true);
            if (cubes.Exists(c => c.Time < 0 || c.Time > end) || chains.Exists(c => c.b < 0 || c.b > end)
                || bombs.Exists(b => b.b < 0 || b.b > end) || walls.Exists(w => w.b < 0 || w.b + w.d > end))
            {
                //ExtendOverallComment("R1B - Object outside of playable length"); TODO: USE NEW METHOD
                issue = CritSeverity.Fail;
            }

            return issue;
        }
    }
}

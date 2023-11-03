using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Outside
    {
        // Detect objects that are outside of the audio boundary
        public CritResult Check(float LoadedSongLength)
        {
            var issue = CritResult.Success;
            var cubes = BeatmapScanner.Cubes;
            var chains = BeatmapScanner.Chains;
            var bombs = BeatmapScanner.Bombs;
            var walls = BeatmapScanner.Walls;

            var end = BeatPerMinute.BPM.ToBeatTime(LoadedSongLength, true);
            if (cubes.Exists(c => c.Time < 0 || c.Time > end) || chains.Exists(c => c.b < 0 || c.b > end)
                || bombs.Exists(b => b.b < 0 || b.b > end) || walls.Exists(w => w.b < 0 || w.b + w.d > end))
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = BSMapCheck.Characteristic,
                    Difficulty = BSMapCheck.Difficulty,
                    Name = "Outside",
                    Severity = Severity.Error,
                    CheckType = "Outside",
                    Description = "Object cannot exist outside of playable length.",
                    ResultData = new() { new("Outside", "Boundary: 0 to" + end.ToString()) },
                });
                issue = CritResult.Fail;
            }

            return issue;
        }
    }
}

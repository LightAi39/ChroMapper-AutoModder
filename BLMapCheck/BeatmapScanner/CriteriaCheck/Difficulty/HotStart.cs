using BLMapCheck.BeatmapScanner.MapCheck;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class HotStart
    {
        // Detect objects that are too early in the map, configurable setting is available
        public static Severity Check()
        {
            var issue = Severity.Success;
            var cube = BeatmapScanner.Cubes;
            cube = cube = cube.OrderBy(c => c.Time).ToList();
            var wall = BeatmapScanner.Walls;
            wall = wall.OrderBy(w => w.b).ToList();
            var limit = BeatPerMinute.BPM.ToBeatTime(HotStartDuration, true);
            foreach (var c in cube)
            {
                if (c.Time < limit)
                {
                    //CreateDiffCommentNote("R1E - Hot Start", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                    issue = Severity.Fail;
                }
                else break;
            }
            foreach (var w in wall)
            {
                if (w.b < limit && ((w.x + w.w >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    //CreateDiffCommentObstacle("R1E - Hot Start", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = Severity.Fail;
                }
                else break;
            }
            return issue;
        }
    }
}

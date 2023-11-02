using BLMapCheck.BeatmapScanner.MapCheck;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ColdEnd
    {
        public static Severity Check(float LoadedSongLength)
        {
            var issue = Severity.Success;
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderByDescending(c => c.Time).ToList();
            var wall = BeatmapScanner.Walls;
            wall = wall.OrderByDescending(w => w.b).ToList();
            var limit = BeatPerMinute.BPM.ToBeatTime(LoadedSongLength - ColdEndDuration, true);
            foreach (var c in cube)
            {
                if (c.Time > limit)
                {
                    //CreateDiffCommentNote("R1E - Cold End", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                    issue = Severity.Fail;
                }
                else break;
            }
            foreach (var w in wall)
            {
                if (w.b + w.d > limit && ((w.x + w.w >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    //CreateDiffCommentObstacle("R1E - Cold End", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = Severity.Fail;
                }
                else break;
            }
            return issue;
        }
    }
}

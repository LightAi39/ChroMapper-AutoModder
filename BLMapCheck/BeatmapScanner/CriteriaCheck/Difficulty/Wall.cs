using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.ChroMapper;
using BLMapCheck.Classes.MapVersion.Difficulty;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Wall
    {
        // Calculate dodge wall per seconds, objects hidden behind walls, walls that force players outside of boundary, walls that are too short in middle lane and negative walls.
        // Subjective and max dodge wall, min wall d and trail d is configurable
        public static CritResult Check()
        {
            var issue = CritResult.Success;

            var walls = BeatmapScanner.Walls;
            var notes = BeatmapScanner.Cubes;
            var bombs = BeatmapScanner.Bombs;

            var leftWall = walls.Where(w => w.x == 1 && w.w == 1);
            var rightWall = walls.Where(w => w.x == 2 && w.w == 1);

            foreach (var w in leftWall)
            {
                var note = notes.Where(n => n.Line == 0 && !(n.Layer == 0 && w.y == 0 && w.h == 1) && ((n.Layer >= w.y && n.Layer < w.y + w.h) || (n.Layer >= 0 && w.y == 0 && w.h > 1)) && n.Time > w.b && n.Time <= w.b + w.d && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    //CreateDiffCommentNote("R3B - Hidden behind wall", CommentTypesEnum.Issue, n); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                var bomb = bombs.Where(b => b.x == 0 && !(b.y == 0 && w.y == 0 && w.h == 1) && ((b.y >= w.y && b.y < w.y + w.h) || (b.y >= 0 && w.y == 0 && w.h > 1)) && b.b > w.b && b.b <= w.b + w.d).ToList();
                foreach (var b in bomb)
                {
                    //CreateDiffCommentBomb("R5E - Hidden behind wall", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
            }

            foreach (var w in rightWall)
            {
                var note = notes.Where(n => n.Line == 3 && !(n.Layer == 0 && w.y == 0 && w.h == 1) && ((n.Layer >= w.y && n.Layer < w.y + w.h) || (n.Layer >= 0 && w.y == 0 && w.h > 1)) && n.Time > w.b && n.Time <= w.b + w.d && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    //CreateDiffCommentNote("R3B - Hidden behind wall", CommentTypesEnum.Issue, n); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                var bomb = bombs.Where(b => b.x == 3 && !(b.y == 0 && w.y == 0 && w.h == 1) && ((b.y >= w.y && b.y < w.y + w.h) || (b.y >= 0 && w.y == 0 && w.h > 1)) && b.b > w.b && b.b <= w.b + w.d).ToList();
                foreach (var b in bomb)
                {
                    //CreateDiffCommentBomb("R5E - Hidden behind wall", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
            }

            Obstacle previous = null;
            foreach (var w in walls)
            {
                BeatPerMinute.BPM.SetCurrentBPM(w.b);
                var min = BeatPerMinute.BPM.ToBeatTime((float)MinimumWallDuration);
                var max = BeatPerMinute.BPM.ToBeatTime((float)ShortWallTrailDuration);

                if (w.y <= 0 && w.h > 1 && ((w.x + w.w == 2 && walls.Exists(wa => wa != w && wa.y == 0 && wa.h > 0 && wa.x + wa.w == 3 && wa.b <= w.b + w.d && wa.b >= w.b)) ||
                    (w.x + w.w == 3 && walls.Exists(wa => wa != w && wa.y == 0 && wa.h > 0 && wa.x + wa.w == 2 && wa.b <= w.b + w.d && wa.b >= w.b))))
                {
                    //CreateDiffCommentObstacle("R4C - Force the player to move into the outer lanes", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                else if (w.y <= 0 && w.h > 1 && ((w.w >= 3 && (w.x + w.w == 2 || w.x + w.w == 3 || w.x == 1)) || (w.w >= 2 && w.x == 1 && w.y == 0 && w.h > 0) || (w.w >= 4 && w.x + w.w >= 4 && w.x <= 0 && w.y == 0)))
                {
                    //CreateDiffCommentObstacle("R4C - Force the player to move into the outer lanes", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                if (w.w <= 0 || w.d <= 0 || // Negative w or d
                    (w.h <= 0 && w.x >= 0 && w.x <= 3 && (w.y > 0 || w.y + w.h >= 0)) // In or above with negative h
                    || ((w.x == 1 || w.x == 2 || (w.x + w.w >= 2 && w.x <= 3)) && w.h < 0)  // Under middle lane with negative h
                    || (w.x + w.w >= 1 && w.x <= 4) && w.y + w.h >= 0 && w.h < 0) // Stretch above with negative h
                {
                    //CreateDiffCommentObstacle("R4D - Must have positive w, h and d", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                if (w.d < min && (w.x + w.w == 2 || w.x + w.w == 3) && w.y + w.h > 1 &&
                    !walls.Exists(wa => wa != w && wa.x + wa.w >= w.x + w.w && wa.x <= w.x + w.w && wa.d >= min && w.b >= wa.b && w.b <= wa.b + wa.d + max))
                {
                    //CreateDiffCommentObstacle("R4E - Shorter than 13.8ms in the middle two lanes", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }

                previous = w;
            }

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                var dodge = 0d;
                var side = 0;
                var w = walls[i];
                BeatPerMinute.BPM.SetCurrentBPM(w.b);
                var sec = BeatPerMinute.BPM.ToBeatTime(1);
                // All the walls under 1 second
                var wallinsec = walls.Where(x => x.b < w.b && x.b >= w.b - sec).ToList();
                wallinsec.Reverse();
                if (w.x + w.w == 2 && w.y <= 2 && w.y + w.h >= 3)
                {
                    side = 2;
                    dodge++;
                }
                else if (w.x == 2 && w.y <= 2 && w.y + w.h >= 3)
                {
                    side = 1;
                    dodge++;
                }
                if (dodge == 1) // Ignore non-dodge walls
                {
                    // Count the amount of dodge in the last second
                    foreach (var wall in wallinsec)
                    {
                        if (wall.x + wall.w == 2 && side != 2 && wall.y <= 2 && wall.y + wall.h >= 3)
                        {
                            side = 2;
                            dodge++;
                        }
                        else if (wall.x == 2 && side != 1 && wall.y <= 2 && wall.y + wall.h >= 3)
                        {
                            side = 1;
                            dodge++;
                        }
                    }
                    if (dodge >= MaximumDodgeWallPerSecond)
                    {
                        //CreateDiffCommentObstacle("R4B - Over the " + config.MaximumDodgeWallPerSecond + " dodge per second limit", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                        issue = CritResult.Fail;
                    }
                    else if (dodge >= SubjectiveDodgeWallPerSecond)
                    {
                        //CreateDiffCommentObstacle("Y4A - " + Plugin.configs.SubjectiveDodgeWallPerSecond + "+ dodge per second need justification", CommentTypesEnum.Suggestion, w); TODO: USE NEW METHOD
                        issue = CritResult.Warning;
                    }
                }
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

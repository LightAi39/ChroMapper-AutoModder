using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Wall
    {
        // Calculate dodge wall per seconds, objects hidden behind walls, walls that force players outside of boundary, walls that are too short in middle lane and negative walls.
        // Subjective and max dodge wall, min wall d and trail d is configurable
        public static CritResult Check(List<Colornote> ColorNotes)
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
                    var no = ColorNotes.Where(c => c.b == n.Time && c.c == n.Type && n.Line == c.x && n.Layer == c.y).FirstOrDefault();
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hidden",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Notes cannot be hidden behind walls.",
                        ResultData = new() { new("Hidden", "Error") },
                        BeatmapObjects = new() { no }
                    });
                    issue = CritResult.Fail;
                }
                var bomb = bombs.Where(b => b.x == 0 && !(b.y == 0 && w.y == 0 && w.h == 1) && ((b.y >= w.y && b.y < w.y + w.h) || (b.y >= 0 && w.y == 0 && w.h > 1)) && b.b > w.b && b.b <= w.b + w.d).ToList();
                foreach (var b in bomb)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hidden",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Bombs cannot be hidden behind walls.",
                        ResultData = new() { new("Hidden", "Error") },
                        BeatmapObjects = new() { b }
                    });
                    issue = CritResult.Fail;
                }
            }

            foreach (var w in rightWall)
            {
                var note = notes.Where(n => n.Line == 3 && !(n.Layer == 0 && w.y == 0 && w.h == 1) && ((n.Layer >= w.y && n.Layer < w.y + w.h) || (n.Layer >= 0 && w.y == 0 && w.h > 1)) && n.Time > w.b && n.Time <= w.b + w.d && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    var no = ColorNotes.Where(c => c.b == n.Time && c.c == n.Type && n.Line == c.x && n.Layer == c.y).FirstOrDefault();
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hidden",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Notes cannot be hidden behind walls.",
                        ResultData = new() { new("Hidden", "Error") },
                        BeatmapObjects = new() { no }
                    });
                    issue = CritResult.Fail;
                }
                var bomb = bombs.Where(b => b.x == 3 && !(b.y == 0 && w.y == 0 && w.h == 1) && ((b.y >= w.y && b.y < w.y + w.h) || (b.y >= 0 && w.y == 0 && w.h > 1)) && b.b > w.b && b.b <= w.b + w.d).ToList();
                foreach (var b in bomb)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hidden",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Bombs cannot be hidden behind walls.",
                        ResultData = new() { new("Hidden", "Error") },
                        BeatmapObjects = new() { b }
                    });
                    issue = CritResult.Fail;
                }
            }

            Obstacle previous = null;
            foreach (var w in walls)
            {
                BeatPerMinute.BPM.SetCurrentBPM(w.b);
                var min = BeatPerMinute.BPM.ToBeatTime((float)Instance.MinimumWallDuration);
                var max = BeatPerMinute.BPM.ToBeatTime((float)Instance.ShortWallTrailDuration);

                if (w.y <= 0 && w.h > 1 && ((w.x + w.w == 2 && walls.Exists(wa => wa != w && wa.y == 0 && wa.h > 0 && wa.x + wa.w == 3 && wa.b <= w.b + w.d && wa.b >= w.b)) ||
                    (w.x + w.w == 3 && walls.Exists(wa => wa != w && wa.y == 0 && wa.h > 0 && wa.x + wa.w == 2 && wa.b <= w.b + w.d && wa.b >= w.b))))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Forced Movement",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls cannot force the player to move into the outer lanes.",
                        ResultData = new() { new("ForcedMovement", "Error") },
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                else if (w.y <= 0 && w.h > 1 && ((w.w >= 3 && (w.x + w.w == 2 || w.x + w.w == 3 || w.x == 1)) || (w.w >= 2 && w.x == 1 && w.y == 0 && w.h > 0) || (w.w >= 4 && w.x + w.w >= 4 && w.x <= 0 && w.y == 0)))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Forced Movement",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls cannot force the player to move into the outer lanes.",
                        ResultData = new() { new("ForcedMovement", "Error") },
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                if (w.w <= 0 || w.d <= 0 || // Negative w or d
                    (w.h <= 0 && w.x >= 0 && w.x <= 3 && (w.y > 0 || w.y + w.h >= 0)) // In or above with negative h
                    || ((w.x == 1 || w.x == 2 || (w.x + w.w >= 2 && w.x <= 3)) && w.h < 0)  // Under middle lane with negative h
                    || (w.x + w.w >= 1 && w.x <= 4) && w.y + w.h >= 0 && w.h < 0) // Stretch above with negative h
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Wall Size",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls must have positive width, height and duration.",
                        ResultData = new() { new("WallSize", "Error") },
                        BeatmapObjects = new() { w }
                    });
                    //CreateDiffCommentObstacle("R4D - Must have positive w, h and d", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                if (w.d < min && (w.x + w.w == 2 || w.x + w.w == 3) && w.y + w.h > 1 &&
                    !walls.Exists(wa => wa != w && wa.x + wa.w >= w.x + w.w && wa.x <= w.x + w.w && wa.d >= min && w.b >= wa.b && w.b <= wa.b + wa.d + max))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Wall Length",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls cannot be shorter than 13.8ms in the middle two lanes.",
                        ResultData = new() { new("WallLength", "Current length: " + w.d.ToString() + " Minimum required: " + min.ToString()) },
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }

                previous = w;
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Wall",
                    Severity = Severity.Passed,
                    CheckType = "Wall",
                    Description = "No issue with hidden objects, movement, wall size and duration detected.",
                    ResultData = new() { new("Wall", "Success") }
                });
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
                    if (dodge >= Instance.MaximumDodgeWallPerSecond)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Wall Dodge",
                            Severity = Severity.Error,
                            CheckType = "Wall",
                            Description = "Dodge walls must not force the players head to move more than " + Instance.MaximumDodgeWallPerSecond.ToString() + " times per second.",
                            ResultData = new() { new("WallDodge", dodge.ToString() + " is over the " + Instance.MaximumDodgeWallPerSecond.ToString() + " limit.") },
                            BeatmapObjects = new() { w }
                        });
                        issue = CritResult.Fail;
                    }
                    else if (dodge >= Instance.SubjectiveDodgeWallPerSecond)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Wall Dodge",
                            Severity = Severity.Warning,
                            CheckType = "Wall",
                            Description = "Dodge walls that force the players head to move more than " + Instance.SubjectiveDodgeWallPerSecond.ToString() + " per second need justification.",
                            ResultData = new() { new("WallDodge", dodge.ToString() + " is over " + Instance.SubjectiveDodgeWallPerSecond.ToString()) },
                            BeatmapObjects = new() { w }
                        });
                        issue = CritResult.Warning;
                    }
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Wall Dodge",
                    Severity = Severity.Passed,
                    CheckType = "Wall",
                    Description = "No issue with dodge wall found.",
                    ResultData = new() { new("WallDodge", "Success") }
                });
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

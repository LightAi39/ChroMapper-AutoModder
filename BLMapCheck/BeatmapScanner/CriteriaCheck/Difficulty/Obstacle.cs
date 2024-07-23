using beatleader_parser.Timescale;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Obstacle
    {
        // Calculate dodge wall per seconds, objects hidden behind walls, walls that force players outside of boundary, walls that are too short in middle lane and negative walls.
        // Subjective and max dodge wall, min wall d and trail d is configurable
        public static CritResult Check(List<Note> notes, List<Wall> walls, List<Bomb> bombs)
        {
            var issue = CritResult.Success;

            var leftWall = walls.Where(w => w.x == 1 && w.Width == 1);
            var rightWall = walls.Where(w => w.x == 2 && w.Width == 1);
            var data = Helper.NotesData;

            List<Note> above = new();
            List<Note> toCheck = notes.ToList();
            foreach (var wall in walls)
            {
                toCheck.RemoveAll(x => x.Beats < wall.Beats);
                var found = toCheck.Where(x => x.Beats >= wall.Beats && x.Beats <= wall.Beats + wall.DurationInBeats + 0.25 &&
                x.x >= wall.x && x.x <= wall.x + wall.Width - 1 && x.y > wall.y + wall.Height - 1);
                foreach (var note in found)
                {
                    if (!above.Contains(note)) above.Add(note);
                }
            }

            foreach (var note in above)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Visibility",
                    Severity = Severity.Warning,
                    CheckType = "Wall",
                    Description = "Reduced visibility due to wall",
                    ResultData = new(),
                    BeatmapObjects = new() { note }
                });
                issue = CritResult.Warning;
            }

            foreach (var w in leftWall)
            {
                var note = notes.Where(n => n.x == 0 && !(n.y == 0 && w.y == 0 && w.Height == 1) && ((n.y >= w.y - 1 && n.y < w.y + w.Height) || (n.y >= 0 && w.y == 0 && w.Height > 1)) && n.Beats > w.Beats && n.Beats <= w.Beats + w.DurationInBeats + 0.25 && (data.FirstOrDefault(d => d.Note == n).Head || !data.FirstOrDefault(d => d.Note == n).Pattern)).ToList();
                
                foreach (var no in note)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hidden",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Notes cannot be hidden behind walls.",
                        ResultData = new(),
                        BeatmapObjects = new() { no }
                    });
                    issue = CritResult.Fail;
                }
                
                var bomb = bombs.Where(b => b.x == 0 && !(b.y == 0 && w.y == 0 && w.Height == 1) && ((b.y >= w.y - 1 && b.y < w.y + w.Height) || (b.y >= 0 && w.y == 0 && w.Height > 1)) && b.Beats > w.Beats && b.Beats <= w.Beats + w.DurationInBeats + 0.25).ToList();
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
                        ResultData = new(),
                        BeatmapObjects = new() { b }
                    });
                    issue = CritResult.Fail;
                }
            }

            foreach (var w in rightWall)
            {
                var note = notes.Where(n => n.x == 3 && !(n.y == 0 && w.y == 0 && w.Height == 1) && ((n.y >= w.y - 1 && n.y < w.y + w.Height) || (n.y >= 0 && w.y == 0 && w.Height > 1)) && n.Beats > w.Beats && n.Beats <= w.Beats + w.DurationInBeats + 0.25 && (data.FirstOrDefault(d => d.Note == n).Head || !data.FirstOrDefault(d => d.Note == n).Pattern)).ToList();
                foreach (var n in note)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Hidden",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Notes cannot be hidden behind walls.",
                        ResultData = new(),
                        BeatmapObjects = new() { n }
                    });
                    issue = CritResult.Fail;
                }
                var bomb = bombs.Where(b => b.x == 3 && !(b.y == 0 && w.y == 0 && w.Height == 1) && ((b.y >= w.y - 1 && b.y < w.y + w.Height) || (b.y >= 0 && w.y == 0 && w.Height > 1)) && b.Beats > w.Beats && b.Beats <= w.Beats + w.DurationInBeats + 0.25).ToList();
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
                        ResultData = new(),
                        BeatmapObjects = new() { b }
                    });
                    issue = CritResult.Fail;
                }
            }

            Wall previous = null;
            Timescale timescale = CriteriaCheckManager.timescale;
            foreach (var w in walls)
            {
                timescale.BPM.SetCurrentBPM(w.Beats);
                var min = timescale.BPM.ToBeatTime((float)Instance.MinimumWallDuration);
                var max = timescale.BPM.ToBeatTime((float)Instance.ShortWallTrailDuration);

                if (w.y <= 1 && w.Height > 1 && ((w.x + w.Width == 3 && walls.Exists(wa => wa != w && wa.y <= 1 && wa.Height > 0 && wa.x + wa.Width >= 2 && wa.x <= 1 && wa.Beats <= w.Beats + w.DurationInBeats && wa.Beats + wa.DurationInBeats >= w.Beats)) ||
                    (w.x + w.Width == 2 && walls.Exists(wa => wa != w && wa.y <= 1 && wa.Height > 0 && wa.x == 2 && wa.Beats <= w.Beats + w.DurationInBeats && wa.Beats + wa.DurationInBeats >= w.Beats))))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Forced Movement",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls cannot force the player to move into the outer lanes.",
                        ResultData = new(),
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                else if (w.y <= 1 && w.Height > 1 && ((w.Width >= 3 && (w.x + w.Width == 3 || w.x == 1)) || (w.Width >= 2 && w.x == 1 && w.y <= 1 && w.Height > 0) || (w.Width >= 4 && w.x + w.Width >= 4 && w.x <= 0 && w.y <= 1)))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Forced Movement",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls cannot force the player to move into the outer lanes.",
                        ResultData = new(),
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                if (w.Width <= 0 || w.DurationInBeats <= 0 || // Negative w or d
                    (w.Height <= 0 && w.x >= 0 && w.x <= 3 && (w.y > 0 || w.y + w.Height >= 0)) // In or above with negative h
                    || ((w.x == 1 || w.x == 2 || (w.x + w.Width >= 2 && w.x <= 3)) && w.Height < 0)  // Under middle lane with negative h
                    || (w.x + w.Width >= 1 && w.x <= 3) && w.y + w.Height >= 0 && w.Height < 0) // Stretch above with negative h
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Wall Size",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls must have positive width, height and duration.",
                        ResultData = new(),
                        BeatmapObjects = new() { w }
                    });
                    issue = CritResult.Fail;
                }
                if (w.DurationInBeats < min && (w.x + w.Width == 2 || w.x + w.Width == 3) && w.y + w.Height > 1 &&
                    !walls.Exists(wa => wa != w && wa.x + wa.Width >= w.x + w.Width && wa.x <= w.x + w.Width && wa.DurationInBeats >= min && w.Beats >= wa.Beats && w.Beats <= wa.Beats + wa.DurationInBeats + max))
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Wall Length",
                        Severity = Severity.Error,
                        CheckType = "Wall",
                        Description = "Walls cannot be shorter than 13.8ms in the middle two lanes.",
                        ResultData = new() { new("CurrentLength", w.DurationInBeats.ToString()), new("MinimumLength", min.ToString()) },
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
                    ResultData = new()
                });
            }

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                var dodge = 0d;
                var side = 0;
                var w = walls[i];
                timescale.BPM.SetCurrentBPM(w.Beats);
                var sec = timescale.BPM.ToBeatTime(1);
                // All the walls under 1 second
                var wallinsec = walls.Where(x => x.Beats < w.Beats && x.Beats >= w.Beats - sec).ToList();
                wallinsec.Reverse();
                if (w.x + w.Width == 2 && w.y <= 2 && w.y + w.Height >= 3)
                {
                    side = 2;
                    dodge++;
                }
                else if (w.x == 2 && w.y <= 2 && w.y + w.Height >= 3)
                {
                    side = 1;
                    dodge++;
                }
                if (dodge == 1) // Ignore non-dodge walls
                {
                    // Count the amount of dodge in the last second
                    foreach (var wall in wallinsec)
                    {
                        if (wall.x + wall.Width == 2 && side != 2 && wall.y <= 2 && wall.y + wall.Height >= 3)
                        {
                            side = 2;
                            dodge++;
                        }
                        else if (wall.x == 2 && side != 1 && wall.y <= 2 && wall.y + wall.Height >= 3)
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
                            ResultData = new() { new("CurrentDodgeAmount", dodge.ToString()), new("MaxDodgeAmount", Instance.MaximumDodgeWallPerSecond.ToString()) },
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
                            ResultData = new() { new("CurrentDodgeAmount", dodge.ToString()), new("ReccomendedDodgeAmount", Instance.SubjectiveDodgeWallPerSecond.ToString()) },
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
                    ResultData = new()
                });
            }

            timescale.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

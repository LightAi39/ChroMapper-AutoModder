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
        public static CritResult Check(List<Note> notes, List<Wall> walls, List<Bomb> bombs)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string checkType = "Wall";
            CritResult criteria = CritResult.Success;
            Timescale timescale = CriteriaCheckManager.timescale;

            var middleWall = walls.Where(w => (w.x == 1 || w.x == 2) && w.Width == 1);
            // Preprocessed note data
            var data = Helper.NotesData;

            // Check for notes above walls
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
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    "Visibility", Severity.Warning, checkType, "Reduced visibility due to wall", new(), new() { note });
                criteria = CritResult.Warning;
            }

            // Compare walls
            foreach (var w in middleWall)
            {
                int line;
                if (w.x == 1) line = 0;
                else line = 3;

                // to notes, take into consideration sliders, towers, stacks, etc.
                var note = notes.Where(n => n.x == line && !(n.y == 0 && w.y == 0 && w.Height == 1) && ((n.y >= w.y - 1 && n.y < w.y + w.Height) || (n.y >= 0 && w.y == 0 && w.Height > 1)) && n.Beats > w.Beats && n.Beats <= w.Beats + w.DurationInBeats + 0.25 && (data.FirstOrDefault(d => d.Note == n).Head || !data.FirstOrDefault(d => d.Note == n).Pattern)).ToList();
                
                foreach (var n in note)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Hidden", Severity.Error, checkType, "Notes cannot be hidden behind walls", new(), new() { n });
                    criteria = CritResult.Fail;
                }

                // to bombs.
                var bomb = bombs.Where(b => b.x == line && !(b.y == 0 && w.y == 0 && w.Height == 1) && ((b.y >= w.y - 1 && b.y < w.y + w.Height) || (b.y >= 0 && w.y == 0 && w.Height > 1)) && b.Beats > w.Beats && b.Beats <= w.Beats + w.DurationInBeats + 0.25).ToList();
                foreach (var b in bomb)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Hidden", Severity.Error, checkType, "Bombs cannot be hidden behind walls", new(), new() { b });
                    criteria = CritResult.Fail;
                }
            }

            Wall previous = null;
            
            foreach (var w in walls)
            {
                timescale.BPM.SetCurrentBPM(w.Beats);

                // Calculate duration based on BPM
                var min = timescale.BPM.ToBeatTime((float)Instance.MinimumWallDuration);
                var max = timescale.BPM.ToBeatTime((float)Instance.ShortWallTrailDuration);

                // Detect if a wall force a player to move into the outer lanes.
                if (((w.Height >= 3 && w.y <= 0) || (w.Height >= 2 && w.y == 1)) && ((w.x + w.Width == 3 && walls.Exists(wa => wa != w && wa.y <= 1 && wa.Height > 0 && ((wa.Height >= 3 && wa.y <= 0) || (wa.Height >= 2 && wa.y == 1)) && wa.x + wa.Width >= 2 && wa.x <= 1 && wa.Beats <= w.Beats + w.DurationInBeats && wa.Beats + wa.DurationInBeats >= w.Beats)) ||
                    (w.x + w.Width == 2 && walls.Exists(wa => wa != w && wa.y <= 1 && wa.Height > 0 && ((wa.Height >= 3 && wa.y <= 0) || (wa.Height >= 2 && wa.y == 1)) && wa.x == 2 && wa.Beats <= w.Beats + w.DurationInBeats && wa.Beats + wa.DurationInBeats >= w.Beats))))
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Forced Movement", Severity.Error, checkType, "Walls cannot force the player to move into the outer lanes", new(), new() { w });
                    criteria = CritResult.Fail;
                }
                else if (((w.Height >= 3 && w.y <= 0) || (w.Height >= 2 && w.y == 1)) && ((w.Width >= 3 && (w.x + w.Width == 3 || w.x == 1)) || (w.Width >= 2 && w.x == 1 && w.y <= 1 && w.Height > 0) || (w.Width >= 4 && w.x + w.Width >= 4 && w.x <= 0 && w.y <= 1)))
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Forced Movement", Severity.Error, checkType, "Walls cannot force the player to move into the outer lanes", new(), new() { w });
                    criteria = CritResult.Fail;
                }

                // Wall width and duration may not be set to values of 0 or lower. Wall height may not be set to 0.
                // Wall height may be negative as long as the wall does not reach inside the 4 standard lanes of the mapping grid.
                if (w.Width <= 0 || w.DurationInBeats <= 0 || // Negative w or d
                    (w.Height <= 0 && w.x >= 0 && w.x <= 3 && (w.y > 0 || w.y + w.Height >= 0)) // In or above with negative h
                    || (((w.x >= 0 && w.x <= 3) || (w.x + w.Width >= 1 && w.x <= 3)) && w.Height < 0)  // Under grid with negative h
                    || (w.x + w.Width >= 1 && w.x <= 3) && w.y + w.Height >= 0 && w.Height < 0) // Stretch above with negative h
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Wall Size", Severity.Error, checkType, "Walls must have positive width, height and duration", new(), new() { w });
                    criteria = CritResult.Fail;
                }

                // Walls must be legal or be placed behind a legal wall
                if (w.DurationInBeats < min &&
                    !walls.Exists(wa => wa != w && wa.x + wa.Width >= w.x + w.Width && wa.x <= w.x && wa.DurationInBeats >= min && w.Beats >= wa.Beats && w.Beats <= wa.Beats + wa.DurationInBeats + max))
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Wall Length", Severity.Error, checkType, "Walls cannot be shorter than 13.8ms", 
                        new() { new("CurrentLength", w.DurationInBeats.ToString()), new("MinimumLength", min.ToString()) }, new() { w });
                    criteria = CritResult.Fail;
                }

                previous = w;
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Wall", Severity.Passed, checkType, "No issue with hidden objects, movement, wall size and duration detected");
            }

            bool issue = false;

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
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Wall Dodge", Severity.Error, checkType, "Dodge walls must not force the players head to move more than " + Instance.MaximumDodgeWallPerSecond.ToString() + " times per second",
                            new() { new("CurrentDodgeAmount", dodge.ToString()), new("MaxDodgeAmount", Instance.MaximumDodgeWallPerSecond.ToString()) }, new() { w });
                        criteria = CritResult.Fail;
                        issue = true;
                    }
                    else if (dodge >= Instance.SubjectiveDodgeWallPerSecond)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Wall Dodge", Severity.Warning, checkType, "Dodge walls that force the players head to move more than " + Instance.SubjectiveDodgeWallPerSecond.ToString() + " per second need justification",
                            new() { new("CurrentDodgeAmount", dodge.ToString()), new("ReccomendedDodgeAmount", Instance.SubjectiveDodgeWallPerSecond.ToString()) }, new() { w });
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                        issue = true;
                    }
                }
            }

            if (!issue)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Wall Dodge", Severity.Passed, checkType, "No issue with dodge wall found");
            }

            timescale.BPM.ResetCurrentBPM();

            return criteria;
        }
    }
}

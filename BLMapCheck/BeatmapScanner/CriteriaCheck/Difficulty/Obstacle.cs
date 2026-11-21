using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Obstacle
    {
        // Seconds between walls under which the player has no time to stand up, a crouch wall then forces a crouch
        private const double ForcedCrouchGap = 0.05;

        // Calculate dodge wall per seconds, objects hidden behind walls, walls that force players outside of boundary, walls that are too short in middle lane and negative walls.
        public static CritResult Check(CheckContext ctx, List<Note> notes, List<Wall> walls, List<Bomb> bombs, List<Chain> chains)
        {
            CritResult criteria = CritResult.Success;

            // Preprocessed note data
            var data = new Dictionary<Note, NoteData>();
            foreach (var d in ctx.NotesData)
            {
                if (d.Note != null) data[d.Note] = d;
            }

            var crouch = GetCrouchPeriods(walls);
            var objects = new List<BeatmapGridObject>();
            objects.AddRange(notes);
            objects.AddRange(chains);
            objects.AddRange(bombs);
            objects = objects.OrderBy(o => o.Beats).ToList();

            // Objects may not be hidden by walls: the player stays in a center lane, on the other side of a dodge wall,
            // and only crouches if necessary. A player beneath a crouch wall is not on the opposite side of it.
            HashSet<BeatmapGridObject> hidden = new();
            foreach (var w in walls)
            {
                int side = DodgeSide(w);
                if (side < 0) continue;
                if (IsCrouch(w) && crouch.Any(c => c.Start < w.Beats + w.DurationInBeats && c.End > w.Beats)) continue;

                hidden.UnionWith(InRange(objects, w.Beats, w.Beats + w.DurationInBeats + 0.25).Where(o =>
                    o.Beats > w.Beats &&
                    (side == 0 ? o.x < w.x : o.x > w.x + w.Width - 1) && // Beyond the wall, away from the player
                    !(o.y == 0 && w.y == 0 && w.Height == 1) && ((o.y >= w.y - 1 && o.y < w.y + w.Height) || (o.y >= 0 && w.y == 0 && w.Height > 1)) &&
                    (o is not Note n || IsStandalone(data, n)))); // Notes of a pattern are only checked through their head
            }

            foreach (var o in hidden.OrderBy(o => o.Beats))
            {
                ctx.Results.CreateDiffResult("Hidden", Severity.Error, "Wall", o is Bomb ? "Bombs cannot be hidden behind walls" : "Notes cannot be hidden behind walls", new(), new() { o });
                criteria = CritResult.Fail;
            }

            // Walls covering the bottom of a lane hide the spawn animation of the objects in that lane,
            // from their start to 0.25 beats after their end. Objects inside the wall are fused objects instead.
            HashSet<BeatmapGridObject> spawnHidden = new();
            foreach (var w in walls.Where(w => w.y == 0 && w.Height > 0))
            {
                double end = w.Beats + w.DurationInBeats;
                spawnHidden.UnionWith(InRange(objects, w.Beats, end + 0.25).Where(o =>
                    o.x >= w.x && o.x <= w.x + w.Width - 1 && !hidden.Contains(o) &&
                    (o.Beats > end || o.y >= w.y + w.Height)));
            }

            foreach (var o in spawnHidden.OrderBy(o => o.Beats))
            {
                ctx.Results.CreateDiffResult("Visibility", Severity.Warning, "Wall", "The spawn of this object is hidden by a wall covering the bottom of its lane, it requires justification", new(), new() { o });
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            Wall previous = null;
            List<Wall> highWalls = new();
            
            foreach (var w in walls)
            {
                // Calculate duration based on BPM
                var min = ctx.SecondsToBeats(ctx.Config.MinimumWallDuration, w.Beats);
                var max = ctx.SecondsToBeats(ctx.Config.ShortWallTrailDuration, w.Beats);
                var anyMax = ctx.SecondsToBeats(ctx.Config.ShortWallAnyTrailDuration, w.Beats);
                var interactable = IsInteractable(w);

                // Detect if a wall force a player to move into the outer lanes.
                if (interactable)
                {
                    if (((w.Height >= 3 && w.y <= 0) || (w.Height >= 2 && w.y == 1)) && ((w.x + w.Width == 3 && walls.Exists(wa => wa != w && wa.y <= 1 && wa.Height > 0 && ((wa.Height >= 3 && wa.y <= 0) || (wa.Height >= 2 && wa.y == 1)) && wa.x + wa.Width >= 2 && wa.x <= 1 && wa.Beats <= w.Beats + w.DurationInBeats && wa.Beats + wa.DurationInBeats >= w.Beats)) ||
                    (w.x + w.Width == 2 && walls.Exists(wa => wa != w && wa.y <= 1 && wa.Height > 0 && ((wa.Height >= 3 && wa.y <= 0) || (wa.Height >= 2 && wa.y == 1)) && wa.x == 2 && wa.Beats <= w.Beats + w.DurationInBeats && wa.Beats + wa.DurationInBeats >= w.Beats))))
                    {
                        ctx.Results.CreateDiffResult("Forced Movement", Severity.Error, "Wall", "Walls cannot force the player to move into the outer lanes", new(), new() { w });
                        criteria = CritResult.Fail;
                    }
                    else if (((w.Height >= 3 && w.y <= 0) || (w.Height >= 2 && w.y == 1)) && ((w.Width >= 3 && (w.x + w.Width == 3 || w.x == 1)) || (w.Width >= 2 && w.x == 1 && w.y <= 1 && w.Height > 0) || (w.Width >= 4 && w.x + w.Width >= 4 && w.x <= 0 && w.y <= 1)))
                    {
                        ctx.Results.CreateDiffResult("Forced Movement", Severity.Error, "Wall", "Walls cannot force the player to move into the outer lanes", new(), new() { w });
                        criteria = CritResult.Fail;
                    }

                    // Walls must be legal or be placed behind a legal wall
                    // A short wall may trail a legal wall of the same lane by 250ms, or any wall of the same lane by 100ms
                    if (w.DurationInBeats < min &&
                        !walls.Exists(wa => wa != w && wa.x + wa.Width >= w.x + w.Width && wa.x <= w.x &&
                            ((wa.DurationInBeats >= min && w.Beats >= wa.Beats && w.Beats <= wa.Beats + wa.DurationInBeats + max) ||
                            (w.Beats > wa.Beats && w.Beats <= wa.Beats + wa.DurationInBeats + anyMax))))
                    {
                        ctx.Results.CreateDiffResult("Wall Length", Severity.Error, "Wall", "Walls cannot be shorter than 13.8ms",
                            new() { new("CurrentLength", w.DurationInBeats.ToString()), new("MinimumLength", min.ToString()) }, new() { w });
                        criteria = CritResult.Fail;
                    }
                }
                
                // Wall width and duration may not be set to values of 0 or lower. Wall height may not be set to 0.
                // Wall height may be negative as long as the wall does not reach inside the 4 standard lanes of the mapping grid.
                if (w.Width <= 0 || w.DurationInBeats <= 0 || // Negative w or d
                    (w.Height <= 0 && w.x >= 0 && w.x <= 3 && (w.y > 0 || w.y + w.Height >= 0)) // In or above with negative h
                    || (((w.x >= 0 && w.x <= 3) || (w.x + w.Width >= 1 && w.x <= 3)) && w.Height < 0)  // Under grid with negative h
                    || (w.x + w.Width >= 1 && w.x <= 3) && w.y + w.Height >= 0 && w.Height < 0) // Stretch above with negative h
                {
                    ctx.Results.CreateDiffResult("Wall Size", Severity.Error, "Wall", "Walls must have positive width, height and duration", new(), new() { w });
                    criteria = CritResult.Fail;
                }

                // y 3 and 4 are only valid in the standard lanes from map format 4.1.0, the parser doesn't keep the format version
                if ((w.y == 3 || w.y == 4) && w.x + w.Width > 0 && w.x < 4)
                {
                    highWalls.Add(w);
                }

                previous = w;
            }

            if (highWalls.Count > 0)
            {
                ctx.Results.CreateDiffResult("Wall Height", Severity.Warning, "Wall", "Walls with a y value of 3 or 4 require map format 4.1.0 or above, and disable score submission below game version 1.41.1",
                    new(), new(highWalls));
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            // Crouch wall rules
            if (crouch.Count > 0)
            {
                foreach (var obj in objects.Where(o => !(o is Bomb)))
                {
                    // During a crouch wall, the middle layer is the highest rankable note placement in all lanes
                    if (obj.y >= 2 && crouch.Any(c => obj.Beats >= c.Start && obj.Beats <= c.End))
                    {
                        ctx.Results.CreateDiffResult("Crouch Wall", Severity.Error, "Wall", "During a crouch wall, the middle layer is the highest rankable note placement", new(), new() { obj });
                        criteria = CritResult.Fail;
                    }
                    // During or after a crouch wall for 0.5 beats, the notes on the bottom row in the 2 middle columns are vision blocks
                    else if (obj.y == 0 && (obj.x == 1 || obj.x == 2) && crouch.Any(c => obj.Beats >= c.Start && obj.Beats <= c.End + ctx.Config.CrouchWallVisionBlockBeats))
                    {
                        ctx.Results.CreateDiffResult("Crouch Wall", Severity.Warning, "Wall", "Notes in the bottom middle lanes during or right after a crouch wall are considered vision blocks", new(), new() { obj });
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Wall", Severity.Passed, "Wall", "No issue with hidden objects, movement, wall size and duration detected");
            }

            bool issue = false;

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                var dodge = 0d;
                var side = 0;
                var w = walls[i];
                var sec = ctx.SecondsToBeats(1, w.Beats);
                // All the walls under 1 second, closest first
                var wallinsec = new List<Wall>();
                for (int j = i - 1; j >= 0 && walls[j].Beats >= w.Beats - sec; j--)
                {
                    if (walls[j].Beats < w.Beats) wallinsec.Add(walls[j]);
                }
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
                    // The hard limit comes from the minimum time to avoid walls below
                    if (dodge >= ctx.Config.SubjectiveDodgeWallPerSecond)
                    {
                        ctx.Results.CreateDiffResult("Wall Dodge", Severity.Warning, "Wall", "Dodge walls that force the players head to move more than " + (System.Math.Ceiling(ctx.Config.SubjectiveDodgeWallPerSecond) - 1).ToString() + " times per second need justification",
                            new() { new("CurrentDodgeAmount", dodge.ToString()), new("ReccomendedDodgeAmount", ctx.Config.SubjectiveDodgeWallPerSecond.ToString()) }, new() { w });
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                        issue = true;
                    }
                }
            }

            // Walls may not give the player less than 250ms (235ms in exceptional cases) to avoid their boundaries,
            // from the end of a dodge wall to the start of the next dodge wall on the other side.
            // A crouch wall starting before the previous walls end, or within 50ms of it, forces the player to crouch instead of moving to a side
            double[] sideEnd = { double.MinValue, double.MinValue };
            double previousEnd = double.MinValue;
            foreach (var w in walls)
            {
                bool forcedCrouch = IsCrouch(w) && ctx.Timescale.BPM.ToRealTime(w.Beats) - previousEnd <= ForcedCrouchGap;
                previousEnd = System.Math.Max(previousEnd, ctx.Timescale.BPM.ToRealTime(w.Beats + w.DurationInBeats));
                int side = DodgeSide(w);
                if (side < 0 || forcedCrouch) continue;
                var start = ctx.Timescale.BPM.ToRealTime(w.Beats);
                var gap = start - sideEnd[1 - side];
                if (gap >= 0 && gap < ctx.Config.WallMinimumAvoidTime)
                {
                    bool exceptional = gap >= ctx.Config.WallExceptionalAvoidTime;
                    ctx.Results.CreateDiffResult("Wall Dodge", exceptional ? Severity.Warning : Severity.Error, "Wall",
                        exceptional ? "Dodge walls giving less than 250ms to avoid them require justification" : "Dodge walls must give at least 250ms to avoid them",
                        new() { new("CurrentTime", System.Math.Round(gap * 1000).ToString() + "ms"), new("MinimumTime", System.Math.Round(ctx.Config.WallMinimumAvoidTime * 1000).ToString() + "ms") }, new() { w });
                    if (exceptional && CritResult.Warning > criteria) criteria = CritResult.Warning;
                    if (!exceptional) criteria = CritResult.Fail;
                    issue = true;
                }
                sideEnd[side] = System.Math.Max(sideEnd[side], ctx.Timescale.BPM.ToRealTime(w.Beats + w.DurationInBeats));
            }

            if (!issue)
            {
                ctx.Results.CreateDiffResult("Wall Dodge", Severity.Passed, "Wall", "No issue with dodge wall found");
            }

            return criteria;
        }

        // Walls that only cover the outer lanes, or only the bottom layer, can't hit the player
        public static bool IsInteractable(Wall wall)
        {
            if (wall.x + wall.Width <= 1 || wall.x >= 3) return false;
            if (wall.y + wall.Height <= 1) return false;

            return true;
        }

        // Dodge walls at head height covering only one center lane: 0 if the head must go right, 1 if it must go left, -1 otherwise
        private static int DodgeSide(Wall w)
        {
            if (w.DurationInBeats <= 0 || w.y > 2 || w.y + w.Height < 3) return -1;
            bool lane1 = Covers(w, 1), lane2 = Covers(w, 2);
            if (lane1 && !lane2) return 0;
            if (lane2 && !lane1) return 1;
            return -1;
        }

        // Walls that block a lane at head height, the player has to move to another lane
        private static bool IsFullHeight(Wall w) => (w.Height >= 3 && w.y <= 0) || (w.Height >= 2 && w.y == 1);

        // Walls that start at the top layer, the player can crouch under them
        private static bool IsCrouch(Wall w) => w.y == 2 && w.Height > 0;

        private static bool Covers(Wall w, int lane) => w.x <= lane && w.x + w.Width > lane && w.DurationInBeats > 0;

        // Periods (in beats) where the player must crouch: both center lanes are covered, by a crouch wall or a wall
        // that blocks the lane, and at least one of them by a crouch wall. Two half walls count as a crouch wall.
        private static List<(double Start, double End)> GetCrouchPeriods(List<Wall> walls)
        {
            List<(double Start, double End)> Periods(System.Func<Wall, bool> filter) =>
                Merge(walls.Where(filter).Select(w => ((double)w.Beats, (double)(w.Beats + w.DurationInBeats))));

            var crouchLane1 = Periods(w => IsCrouch(w) && Covers(w, 1));
            var crouchLane2 = Periods(w => IsCrouch(w) && Covers(w, 2));
            var coveredLane1 = Periods(w => (IsCrouch(w) || IsFullHeight(w)) && Covers(w, 1));
            var coveredLane2 = Periods(w => (IsCrouch(w) || IsFullHeight(w)) && Covers(w, 2));

            return Intersect(Intersect(coveredLane1, coveredLane2), Merge(crouchLane1.Concat(crouchLane2)));
        }

        private static List<(double Start, double End)> Merge(IEnumerable<(double Start, double End)> periods)
        {
            var merged = new List<(double Start, double End)>();
            foreach (var p in periods.OrderBy(p => p.Start))
            {
                if (merged.Count > 0 && p.Start <= merged[merged.Count - 1].End)
                {
                    merged[merged.Count - 1] = (merged[merged.Count - 1].Start, System.Math.Max(merged[merged.Count - 1].End, p.End));
                }
                else merged.Add(p);
            }
            return merged;
        }

        // Both lists must be merged and sorted
        private static List<(double Start, double End)> Intersect(List<(double Start, double End)> a, List<(double Start, double End)> b)
        {
            var result = new List<(double Start, double End)>();
            int i = 0, j = 0;
            while (i < a.Count && j < b.Count)
            {
                double start = System.Math.Max(a[i].Start, b[j].Start);
                double end = System.Math.Min(a[i].End, b[j].End);
                if (start < end) result.Add((start, end));
                if (a[i].End < b[j].End) i++;
                else j++;
            }
            return result;
        }

        // Notes that are part of a pattern are only checked through their head
        private static bool IsStandalone(Dictionary<Note, NoteData> data, Note note)
        {
            return !data.TryGetValue(note, out var d) || d.Head || !d.Pattern;
        }
    }
}

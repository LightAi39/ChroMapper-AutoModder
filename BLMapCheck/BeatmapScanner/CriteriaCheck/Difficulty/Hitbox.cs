using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Hitbox
    {
        // Hitboxes in meters: grid cells are 0.6 apart, the good cut box is 0.8 wide and 0.5 along the cut direction, the bad cut box is 0.35.
        // Both turn with the note, dots like a down note.
        private const double GridSize = 0.6, GoodAlong = 0.25, GoodAcross = 0.4, BadHalf = 0.175;
        private const double MaxBadOverlap = 0.2;

        // The top of a note's good hitbox cannot overlap the top of an opposite colored note's bad hitbox by more than 20%, for notes on
        // the same beat or extremely close in time (SameTimeSeconds).
        // The sabers must go through each other's notes: the follow-through of one swing, its good cut box carried on one cell in the cut
        // direction, crosses the bad cut box of the other note, and the approach of the other swing, its good cut box carried back one
        // cell, crosses the bad cut box of the first note. The overlap is the smaller of the two shares of bad cut box crossed, face-on.
        // Dots can be hit from any side, they're skipped.
        private const double SameTimeSeconds = 0.01, Reach = GridSize;

        public static CritResult Check(CheckContext ctx, List<Note> notes, float baseNjs, List<SwingInfo> swings, List<Chain> chains)
        {
            CritResult criteria = CritResult.Success;

            var sorted = notes.Where(n => n.Color == 0 || n.Color == 1).OrderBy(n => n.Seconds).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = i + 1; j < sorted.Count && sorted[j].Seconds - sorted[i].Seconds <= SameTimeSeconds; j++)
                {
                    Note a = sorted[i], b = sorted[j];
                    if (a.Color == b.Color || a.CutDirection == 8 || b.CutDirection == 8) continue;
                    var overlap = Math.Max(Crossing(a, b), Crossing(b, a));
                    if (overlap <= MaxBadOverlap + 0.0001) continue;

                    ctx.Results.CreateDiffResult("Hitbox", Severity.Error, "Hitbox", "The good cut hitbox of a note cannot overlap the bad cut hitbox of an opposite colored note by more than 20%",
                        new() { new("Overlap", Math.Round(overlap * 100) + "%") }, new() { a, b });
                    criteria = CritResult.Fail;
                }
            }

            if (CheckMultiNoteSwingPath(ctx, notes, swings, chains)) criteria = CritResult.Fail;

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Hitbox", Severity.Passed, "Hitbox", "No hitbox issue detected");
            }

            return criteria;
        }

        // The follow-through of the first note's swing over the second note's bad cut box, and the approach of the second note's swing
        // over the first note's bad cut box
        private static double Crossing(Note first, Note second) => Math.Min(SwingOverBad(first, second, Reach), SwingOverBad(second, first, -Reach));

        // Share of the bad cut box of a note crossed by the good cut box of another carried on in the cut direction (or back when negative)
        private static double SwingOverBad(Note good, Note bad, double reach)
        {
            var center = ToMeters(good.x, good.y);
            var radians = ConvertDegreesToRadians(BoxAngle(good));
            var shift = reach / 2;
            var swing = Box((center.X + shift * Math.Cos(radians), center.Y + shift * Math.Sin(radians)), BoxAngle(good), GoodAlong + Math.Abs(shift), GoodAcross);
            var badBox = Box(ToMeters(bad.x, bad.y), BoxAngle(bad), BadHalf, BadHalf);
            return Area(Clip(badBox, swing)) / (4 * BadHalf * BadHalf);
        }

        // In a multi-note swing (a slider, notes swung in the same movement on different beats, or a multi-note hit, on the same beat),
        // the swing path of one hand cannot overlap the bad cut hitbox of the other hand's notes by more than 20%.
        // The swing path is every path the saber can take through the good cut hitboxes of the swing: between two points of the swing,
        // the area between the face it leaves the first good cut box by and the face it enters the next one by. That's the pre-swing of
        // the next point: the other hand's notes block it unless they were already hit when the saber passes them, later notes always count.
        // After the last point is the post-swing, which isn't checked.
        // Returns true if an issue was found
        private static bool CheckMultiNoteSwingPath(CheckContext ctx, List<Note> notes, List<SwingInfo> swings, List<Chain> chains)
        {
            var issue = false;
            double Seconds(float beats) => ctx.Timescale.BPM.ToRealTime(beats);

            foreach (var (swingNotes, path) in Slider.SwingPaths(swings, chains))
            {
                var color = swingNotes[0].Color;
                var first = Seconds(path[0].Beats);
                var last = Seconds(path[path.Count - 1].Beats);
                // Notes of the other hand during the swing
                var others = notes.Where(n => n.Color == 1 - color && Seconds(n.Beats) >= first - 0.01 && Seconds(n.Beats) <= last + 0.01).ToList();
                if (others.Count == 0) continue;

                // Points on the same beat are cut along the direction of the swing
                var start = path.FirstOrDefault(p => p.Cut != null)?.Cut ?? ConvertRadiansToDegrees(Math.Atan2(path[path.Count - 1].Y - path[0].Y, path[path.Count - 1].X - path[0].X));
                double dirX = Math.Cos(ConvertDegreesToRadians(start)), dirY = Math.Sin(ConvertDegreesToRadians(start));
                var ordered = path.OrderBy(p => Math.Round(p.Beats * 1000)).ThenBy(p => p.X * dirX + p.Y * dirY).ToList();

                (double Overlap, Note Note) worst = (0, null);
                for (int i = 1; i < ordered.Count; i++)
                {
                    var a = ToMeters(ordered[i - 1].X, ordered[i - 1].Y);
                    var b = ToMeters(ordered[i].X, ordered[i].Y);
                    if (a == b) continue;
                    // The saber leaves a good cut box and enters the next one by their faces turned the most towards each other
                    var travel = (X: b.X - a.X, Y: b.Y - a.Y);
                    var corridor = ConvexHull(Face(Box(a, PointAngle(ordered[i - 1]), GoodAlong, GoodAcross), a, travel)
                        .Concat(Face(Box(b, PointAngle(ordered[i]), GoodAlong, GoodAcross), b, (-travel.X, -travel.Y))).ToList());
                    double ta = Seconds(ordered[i - 1].Beats), tb = Seconds(ordered[i].Beats), length = travel.X * travel.X + travel.Y * travel.Y;
                    foreach (var other in others)
                    {
                        var center = ToMeters(other.x, other.y);
                        var bad = Box(center, BoxAngle(other), BadHalf, BadHalf);
                        var overlap = Area(Clip(bad, corridor)) / (4 * BadHalf * BadHalf);
                        if (overlap <= worst.Overlap) continue;
                        // Already hit when the saber passes it
                        var s = Math.Max(0, Math.Min(1, ((center.X - a.X) * travel.X + (center.Y - a.Y) * travel.Y) / length));
                        if (Seconds(other.Beats) < ta + s * (tb - ta) - 0.0001) continue;
                        worst = (overlap, other);
                    }
                }

                if (worst.Overlap > MaxBadOverlap)
                {
                    ctx.Results.CreateDiffResult("Multi-Note Hitbox", Severity.Error, "Hitbox", "The swing path of a multi-note swing cannot overlap the bad cut hitbox of the other hand's notes by more than 20%",
                        new() { new("Overlap", Math.Round(worst.Overlap * 100) + "%") }, new() { swingNotes[0], worst.Note });
                    issue = true;
                }
            }

            return issue;
        }

        private static (double X, double Y) ToMeters(double x, double y) => (x * GridSize, y * GridSize);

        // Rotation of a note's hitboxes, dots like a down note
        private static double BoxAngle(Note note) =>
            note.CutDirection >= 0 && note.CutDirection < DirectionToDegree.Length ? Slider.CutAngle(note) : 270;

        // Rotation of the hitboxes of a point of a multi-note swing, chain links turn like their head
        private static double PointAngle(Slider.PathPoint point) =>
            point.Cut ?? (point.Object is Note note ? BoxAngle(note)
                : point.Object is Chain chain && chain.CutDirection >= 0 && chain.CutDirection < DirectionToDegree.Length ? DirectionToDegree[chain.CutDirection] : 270);

        // Corners, counterclockwise, of a box turned by angle with the given half sizes along and across that direction
        private static List<(double X, double Y)> Box((double X, double Y) center, double angle, double along, double across)
        {
            double cos = Math.Cos(ConvertDegreesToRadians(angle)), sin = Math.Sin(ConvertDegreesToRadians(angle));
            (double X, double Y) Corner(double u, double v) => (center.X + u * cos - v * sin, center.Y + u * sin + v * cos);
            return new() { Corner(-along, -across), Corner(along, -across), Corner(along, across), Corner(-along, across) };
        }

        // The two corners of the face of a box whose outward normal is the closest to a direction
        private static List<(double X, double Y)> Face(List<(double X, double Y)> box, (double X, double Y) center, (double X, double Y) direction)
        {
            int best = 0;
            double bestDot = double.MinValue;
            for (int i = 0; i < box.Count; i++)
            {
                var p = box[i];
                var q = box[(i + 1) % box.Count];
                double nx = (p.X + q.X) / 2 - center.X, ny = (p.Y + q.Y) / 2 - center.Y;
                double dot = (nx * direction.X + ny * direction.Y) / Math.Sqrt(nx * nx + ny * ny);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = i;
                }
            }
            return new() { box[best], box[(best + 1) % box.Count] };
        }

        // Counterclockwise convex hull (monotone chain)
        private static List<(double X, double Y)> ConvexHull(List<(double X, double Y)> points)
        {
            var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
            var hull = new List<(double X, double Y)>();
            foreach (var pass in new[] { sorted, Enumerable.Reverse(sorted).ToList() })
            {
                int start = hull.Count;
                foreach (var p in pass)
                {
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 1e-12) hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }
            return hull;
        }

        // Part of a polygon inside a counterclockwise convex polygon (Sutherland-Hodgman)
        private static List<(double X, double Y)> Clip(List<(double X, double Y)> polygon, List<(double X, double Y)> convex)
        {
            var result = polygon;
            for (int e = 0; e < convex.Count && result.Count > 0; e++)
            {
                var a = convex[e];
                var b = convex[(e + 1) % convex.Count];
                double Side((double X, double Y) p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
                var input = result;
                result = new List<(double X, double Y)>();
                for (int i = 0; i < input.Count; i++)
                {
                    var current = input[i];
                    var previous = input[(i + input.Count - 1) % input.Count];
                    double sc = Side(current), sp = Side(previous);
                    if (sc >= 0)
                    {
                        if (sp < 0) result.Add(Intersect(previous, current, sp, sc));
                        result.Add(current);
                    }
                    else if (sp >= 0) result.Add(Intersect(previous, current, sp, sc));
                }
            }
            return result;
        }

        private static (double X, double Y) Intersect((double X, double Y) p, (double X, double Y) q, double sp, double sq)
        {
            var t = sp / (sp - sq);
            return (p.X + (q.X - p.X) * t, p.Y + (q.Y - p.Y) * t);
        }

        private static double Area(List<(double X, double Y)> polygon)
        {
            double area = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                var q = polygon[(i + 1) % polygon.Count];
                area += p.X * q.Y - q.X * p.Y;
            }
            return Math.Abs(area) / 2;
        }
    }
}

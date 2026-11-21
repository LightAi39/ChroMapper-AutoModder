using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Slider
    {
        // Notes of a multi-note swing within this many beats are on the same beat (a multi-note hit)
        private const float SameTime = 0.001f;
        // Maximum deviation from the starting angle, and the deviation under which the swing is considered straight
        private const double MaxDeviation = 45;
        private const double Straight = 1;

        // Get the average sliders precision and warn if it's not applied to all sliders in the map.
        // Multi-note swings must be hittable with a smooth swing path.
        public static CritResult Check(CheckContext ctx, List<SwingInfo> swings, List<Chain> chains)
        {
            CritResult criteria = CheckSwingPath(ctx, swings, chains);

            // Fetch non-head slider notes from preprocessed data
            var sliders = ctx.NotesData.Where(c => c.Pattern && !c.Head && c.Precision != 0).ToList();

            for (int i = 0; i < sliders.Count(); i++)
            {
                NoteData note = sliders[i];

                // The minimum effective slider precision is 1/16th (or 1/8th for a 2-note window,
                // as the missing note should be counted for effective precision.) relative to the mapping precision in that section.
                if (note.Precision - 0.01 > (note.Spacing + 1) * 0.0625)
                {
                    ctx.Results.CreateDiffResult("Slider Precision", Severity.Error, "Slider", "Slider cannot be slower than 1/16", new(), new() { note.Note });
                    criteria = CritResult.Fail;
                }

                // Slider Swing speed must be consistent per section of the map. This may be overruled with sufficient justification.
                if (note.Precision - 0.01 > (note.Spacing + 1) * ctx.SliderPrecision)
                {
                    var expected = DoubleToFraction((note.Spacing + 1) * ctx.SliderPrecision);

                    ctx.Results.CreateDiffResult("Slider Precision", Severity.Warning, "Slider", "Slider Swing speed must be consistent per section of the map", 
                        new() { new("ExpectedSliderPrecision", expected.num.ToString() + "/" + expected.den.ToString()) }, new() { note.Note });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;

                    continue;
                }

                // Slider Swing speed must be consistent per section of the map. This may be overruled with sufficient justification.
                if (!(note.Precision <= ((note.Spacing + 1) * ctx.SliderPrecision) + 0.01 && note.Precision >= ((note.Spacing + 1) * ctx.SliderPrecision) - 0.01))
                {
                    var expected = DoubleToFraction((note.Spacing + 1) * ctx.SliderPrecision);

                    ctx.Results.CreateDiffResult("Slider Precision", Severity.Warning, "Slider", "Slider Swing speed must be consistent per section of the map",
                        new() { new("ExpectedSliderPrecision", expected.num.ToString() + "/" + expected.den.ToString()) }, new() { note.Note });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;

                    continue;
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Slider Precision", Severity.Passed, "Slider", "No issue with slider precision detected");
            }

            return criteria;
        }

        // Swings must be able to hit all notes of the multi-note swing at their cut angle in the center of the note without deviating
        // more than 45 degrees from the starting angle, and may only deviate in one direction along the entire swing path.
        // Notes are cut in the order they are placed, notes on the same beat (multi-note hits) in the order that passes best.
        private static CritResult CheckSwingPath(CheckContext ctx, List<SwingInfo> swings, List<Chain> chains)
        {
            CritResult criteria = CritResult.Success;

            foreach (var (notes, points) in SwingPaths(swings, chains))
            {
                // Groups of points on the same beat, cut one by one in the order that passes best
                var groups = new List<List<PathPoint>>();
                foreach (var point in points)
                {
                    if (groups.Count > 0 && point.Beats - groups[groups.Count - 1][0].Beats < SameTime) groups[groups.Count - 1].Add(point);
                    else groups.Add(new() { point });
                }

                (double Worst, bool BothSides, BeatmapGridObject Object)? best = null;
                foreach (var order in Orders(groups))
                {
                    var result = Evaluate(order);
                    if (result == null) continue;
                    if (best == null || Score(result.Value) < Score(best.Value)) best = result;
                    if (Score(best.Value) == 0) break;
                }
                if (best == null) continue;

                if (best.Value.Worst > MaxDeviation + 0.01)
                {
                    ctx.Results.CreateDiffResult("Multi-Note Swing", Severity.Error, "Slider", "Multi-note swings cannot deviate more than 45 degrees from their starting angle",
                        new() { new("Deviation", Math.Round(best.Value.Worst).ToString()) }, new() { notes[0], best.Value.Object });
                    criteria = CritResult.Fail;
                }
                else if (best.Value.BothSides)
                {
                    ctx.Results.CreateDiffResult("Multi-Note Swing", Severity.Error, "Slider", "Multi-note swings may only deviate in one direction along the swing path",
                        new(), new() { notes[0], points[points.Count - 1].Object });
                    criteria = CritResult.Fail;
                }
            }

            return criteria;
        }

        // Multi-note swings: sliders (notes swung in the same movement on different beats, close enough in time) and multi-note hits
        // (notes on the same beat), with the points of their swing path in time order: the notes, with their cut angle for arrows,
        // and the links of the chains they lead into (chain heads and links follow the same rules)
        internal static IEnumerable<(List<Note> Notes, List<PathPoint> Points)> SwingPaths(List<SwingInfo> swings, List<Chain> chains)
        {
            foreach (var notes in swings.Where(s => s.IsMultiNote).SelectMany(s => SplitSliders(s.Notes)).Where(n => n.Count > 1))
            {
                var points = notes.Select(n => new PathPoint(n.x, n.y, n.Beats, n,
                    n.CutDirection >= 0 && n.CutDirection < DirectionToDegree.Length ? CutAngle(n) : null)).ToList();
                foreach (var chain in chains.Where(c => notes.Any(n => n.Color == c.Color && n.x == c.x && n.y == c.y && Math.Abs(n.Beats - c.Beats) < SameTime)))
                {
                    var positions = FindChainLinksPosition(chain);
                    for (int i = 0; i < positions.Count; i++)
                    {
                        var beat = chain.Beats + (chain.TailInBeats - chain.Beats) * (i + 1) / (chain.SliceCount - 1);
                        points.Add(new PathPoint(positions[i].x, positions[i].y, beat, chain, null));
                    }
                }
                yield return (notes, points.OrderBy(p => p.Beats).ToList());
            }
        }

        // Point of a swing path, with the cut angle of arrows
        internal class PathPoint
        {
            public double X, Y;
            public float Beats;
            public BeatmapGridObject Object;
            public double? Cut;

            public PathPoint(double x, double y, float beats, BeatmapGridObject obj, double? cut)
            {
                X = x; Y = y; Beats = beats; Object = obj; Cut = cut;
            }
        }

        // Passing orders first: no deviation over the maximum and only on one side, then the smallest deviation
        private static double Score((double Worst, bool BothSides, BeatmapGridObject Object) result) =>
            (result.Worst > MaxDeviation + 0.01 ? 1000 : 0) + (result.BothSides ? 500 : 0) + (result.Worst > MaxDeviation + 0.01 ? result.Worst : 0);

        // Every order the swing can cut its points in: points on the same beat in any order, capped for big groups
        private static IEnumerable<List<PathPoint>> Orders(List<List<PathPoint>> groups)
        {
            IEnumerable<List<PathPoint>> orders = new[] { new List<PathPoint>() };
            foreach (var group in groups)
            {
                var permutations = group.Count <= 4 ? Permutations(group).ToList() : new List<List<PathPoint>> { group };
                orders = orders.SelectMany(o => permutations.Select(p => o.Concat(p).ToList()));
            }
            return orders.Take(2000);
        }

        private static IEnumerable<List<T>> Permutations<T>(List<T> items)
        {
            if (items.Count <= 1)
            {
                yield return new List<T>(items);
                yield break;
            }
            for (int i = 0; i < items.Count; i++)
            {
                var rest = items.Where((_, j) => j != i).ToList();
                foreach (var p in Permutations(rest))
                {
                    p.Insert(0, items[i]);
                    yield return p;
                }
            }
        }

        // Largest deviation from the starting angle (the cut angle of the first arrow, or the first direction travelled) of the cut
        // angles and the directions travelled between points, and whether it deviates on both sides. Null if no angle is known
        private static (double Worst, bool BothSides, BeatmapGridObject Object)? Evaluate(List<PathPoint> order)
        {
            var travel = new List<(double Angle, BeatmapGridObject Object)>();
            for (int i = 1; i < order.Count; i++)
            {
                double dx = order[i].X - order[i - 1].X, dy = order[i].Y - order[i - 1].Y;
                if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001) continue;
                travel.Add((Mod(ConvertRadiansToDegrees(Math.Atan2(dy, dx)), 360), order[i].Object));
            }

            var head = order.FirstOrDefault(p => p.Cut != null);
            double start;
            if (head != null) start = head.Cut.Value;
            else if (travel.Count > 0) start = travel[0].Angle;
            else return null;

            var deviations = order.Where(p => p.Cut != null).Select(p => (Deviation: AngleDifference(start, p.Cut.Value), p.Object))
                .Concat(travel.Select(t => (Deviation: AngleDifference(start, t.Angle), t.Object))).ToList();
            var worst = deviations.OrderByDescending(d => Math.Abs(d.Deviation)).First();
            bool bothSides = deviations.Any(d => d.Deviation > Straight) && deviations.Any(d => d.Deviation < -Straight);
            return (Math.Abs(worst.Deviation), bothSides, worst.Object);
        }

        // Notes further apart than the slowest allowed slider precision ((spacing + 1) * 1/16 beat) are separate swings for the swing path,
        // the Analyzer sometimes groups them
        private static IEnumerable<List<Note>> SplitSliders(List<Note> swingNotes)
        {
            var current = new List<Note>();
            foreach (var note in swingNotes.OrderBy(n => n.Beats))
            {
                if (current.Count > 0)
                {
                    var prev = current[current.Count - 1];
                    var spacing = Math.Max(Math.Max(Math.Abs(note.x - prev.x), Math.Abs(note.y - prev.y)) - 1, 0);
                    if (note.Beats - prev.Beats > (spacing + 1) * 0.0625 + 0.01)
                    {
                        yield return current;
                        current = new List<Note>();
                    }
                }
                current.Add(note);
            }
            yield return current;
        }

        internal static double CutAngle(Note note) => Mod(DirectionToDegree[note.CutDirection] + note.AngleOffset, 360);

        // Signed difference from a to b in degrees, from -180 to 180
        private static double AngleDifference(double a, double b) => Mod(b - a + 180, 360) - 180;
    }
}

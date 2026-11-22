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

        // Slider precision rules, per section of the map. Multi-note swings must be hittable with a smooth swing path.
        public static CritResult Check(CheckContext ctx, List<SwingInfo> swings, List<Chain> chains)
        {
            CritResult criteria = CheckSwingPath(ctx, swings, chains);
            var sections = Sections(ctx, swings);

            // Every step of the sliders, notes the Analyzer grouped while they're too far apart to be a slider are separate swings
            foreach (var step in SliderSteps(swings))
            {
                var section = SectionAt(sections, step.Note.Beats);
                var gap = step.Effective * (step.Spacing + 1);

                // The minimum effective slider precision is 1/16th (or 1/8th for a 2-note window,
                // as the missing note should be counted for effective precision.) relative to the mapping precision in that section.
                if (gap - 0.01 > (step.Spacing + 1) * section.Minimum)
                {
                    ctx.Results.CreateDiffResult("Slider Precision", Severity.Error, "Slider", "Slider cannot be slower than " + Fraction(section.Minimum) + " in this section",
                        new() { new("MinimumSliderPrecision", Fraction(section.Minimum)) }, new() { step.Note });
                    criteria = CritResult.Fail;
                }

                // Slider Swing speed must be consistent per section of the map. This may be overruled with sufficient justification.
                var expected = (step.Spacing + 1) * section.SliderPrecision;
                if (gap > expected + 0.01 || gap < expected - 0.01)
                {
                    ctx.Results.CreateDiffResult("Slider Precision", Severity.Warning, "Slider", "Slider Swing speed must be consistent per section of the map",
                        new() { new("ExpectedSliderPrecision", Fraction(expected)) }, new() { step.Note });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                }
            }

            if (CheckSingleSpeed(ctx, swings)) criteria = CritResult.Fail;
            if (CheckWindowSliders(ctx, swings, sections)) criteria = CritResult.Fail;

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Slider Precision", Severity.Passed, "Slider", "No issue with slider precision detected");
            }

            return criteria;
        }

        // A section of the map starts at a significant BPM change or after a break. Its mapping precision is the most common time
        // between swings of both hands, the minimum effective slider precision is a quarter of it (1/16 at 1/4, 1/32 at 1/8), never
        // above 1/16. Its slider precision is the most common effective precision of its sliders (the time between two notes, the left
        // out notes of a window count), or the expected slider precision without sliders
        private class Section
        {
            public float Start;
            public double Minimum = 0.0625;
            public double SliderPrecision;
        }

        private const float SectionBreakBeats = 4;
        private const double SectionBpmChange = 0.05;

        private static List<Section> Sections(CheckContext ctx, List<SwingInfo> swings)
        {
            var groups = new List<List<float>>();
            foreach (var beat in swings.Select(s => s.Notes[0].Beats).OrderBy(b => b))
            {
                var current = groups.Count > 0 ? groups[groups.Count - 1] : null;
                if (current == null || beat - current[current.Count - 1] >= SectionBreakBeats
                    || Math.Abs(ctx.BpmAt(beat) / ctx.BpmAt(current[0]) - 1) > SectionBpmChange)
                {
                    groups.Add(new() { beat });
                }
                else current.Add(beat);
            }

            var steps = SliderSteps(swings).ToList();
            var sections = new List<Section>();
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var section = new Section { Start = group[0] };
                var gap = group.Zip(group.Skip(1), (a, b) => b - a).Where(g => g > SameTime)
                    .GroupBy(g => Math.Round(g, 3)).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
                if (gap != null) section.Minimum = Math.Min(0.0625, gap.Value / 4);
                var end = i + 1 < groups.Count ? groups[i + 1][0] : float.MaxValue;
                var sectionSteps = steps.Where(x => x.Note.Beats >= group[0] && x.Note.Beats < end)
                    .Select(x => new NoteData(x.Note) { Pattern = true, Precision = x.Effective * (x.Spacing + 1), Spacing = x.Spacing }).ToList();
                section.SliderPrecision = DetectSliderPrecision(sectionSteps) ?? ctx.SliderPrecision;
                sections.Add(section);
            }
            if (sections.Count == 0) sections.Add(new Section { Start = float.MinValue, SliderPrecision = ctx.SliderPrecision });
            return sections;
        }

        // Chain duration cannot exceed 400% of the effective slider precision of its section (the time between two notes of a slider)
        internal static Func<float, double> MaxChainDuration(CheckContext ctx, List<SwingInfo> swings)
        {
            var sections = Sections(ctx, swings);
            return beat => SectionAt(sections, beat).SliderPrecision * 4;
        }

        private static Section SectionAt(List<Section> sections, float beat) => sections.LastOrDefault(s => s.Start <= beat) ?? sections[0];

        private static string Fraction(double value)
        {
            var (num, den) = DoubleToFraction(value, 256);
            return num + "/" + den;
        }

        // Multi-note patterns must have a single swing speed throughout: every step of a slider has the same effective precision.
        // Returns true if an issue was found
        private static bool CheckSingleSpeed(CheckContext ctx, List<SwingInfo> swings)
        {
            var issue = false;
            foreach (var notes in MultiNoteSwings(swings))
            {
                var steps = SliderSteps(notes).ToList();
                if (steps.Count < 2 || steps.All(s => SamePrecision(s.Effective, steps[0].Effective))) continue;
                var odd = steps.First(s => !SamePrecision(s.Effective, steps[0].Effective));
                ctx.Results.CreateDiffResult("Slider Speed", Severity.Error, "Slider", "Multi-note patterns must have a single swing speed throughout",
                    new() { new("EffectivePrecision", "1/" + Math.Round(1 / steps[0].Effective)), new("OtherPrecision", "1/" + Math.Round(1 / odd.Effective)) }, new() { notes[0], odd.Note });
                issue = true;
            }
            return issue;
        }

        // Sliders and window sliders must have the same effective precision within a section: the time between two notes divided by
        // the cells travelled, the left out notes of a window count. A window slider must match the nearest slider of its section
        // before or after it. Returns true if an issue was found
        private static bool CheckWindowSliders(CheckContext ctx, List<SwingInfo> swings, List<Section> sections)
        {
            var steps = SliderSteps(swings).ToList();
            var regular = steps.Where(s => s.Spacing == 0).OrderBy(s => s.Note.Beats).ToList();
            var issue = false;

            foreach (var window in steps.Where(s => s.Spacing > 0))
            {
                var section = SectionAt(sections, window.Note.Beats);
                var nearest = new[]
                {
                    regular.LastOrDefault(s => s.Note.Beats <= window.Note.Beats && SectionAt(sections, s.Note.Beats) == section),
                    regular.FirstOrDefault(s => s.Note.Beats >= window.Note.Beats && SectionAt(sections, s.Note.Beats) == section)
                }.Where(s => s.Note != null).ToList();
                if (nearest.Count == 0 || nearest.Any(s => SamePrecision(s.Effective, window.Effective))) continue;

                ctx.Results.CreateDiffResult("Window Slider Precision", Severity.Error, "Slider", "Sliders and window sliders must have the same effective precision within a section",
                    new() { new("EffectivePrecision", "1/" + Math.Round(1 / window.Effective)), new("SliderPrecision", "1/" + Math.Round(1 / nearest[0].Effective)) }, new() { window.Note });
                issue = true;
            }

            return issue;
        }

        // Every step between two notes of a slider at different times, with the cells skipped and the effective precision
        private static IEnumerable<(Note Note, int Spacing, double Effective)> SliderSteps(List<SwingInfo> swings) =>
            swings.Where(s => s.IsMultiNote).SelectMany(s => SplitSliders(s.Notes)).SelectMany(SliderSteps);

        private static IEnumerable<(Note Note, int Spacing, double Effective)> SliderSteps(List<Note> notes)
        {
            var beats = notes.GroupBy(n => Math.Round(n.Beats / SameTime)).OrderBy(g => g.Key).ToList();
            for (int i = 1; i < beats.Count; i++)
            {
                foreach (var note in beats[i])
                {
                    // From the closest note of the previous beat
                    var spacing = beats[i - 1].Min(p => Math.Max(Math.Max(Math.Abs(note.x - p.x), Math.Abs(note.y - p.y)) - 1, 0));
                    var gap = note.Beats - beats[i - 1].First().Beats;
                    yield return (note, spacing, gap / (spacing + 1));
                }
            }
        }

        // Within 5%, or 0.002 beats for beats saved with 3 decimals
        private static bool SamePrecision(double a, double b) => Math.Abs(a - b) <= Math.Max(0.05 * Math.Max(a, b), 0.002);

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
            foreach (var notes in MultiNoteSwings(swings))
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

        // Notes of each multi-note swing in the order they are cut, split where the Analyzer grouped notes too far apart to be a slider
        internal static IEnumerable<List<Note>> MultiNoteSwings(List<SwingInfo> swings) =>
            swings.Where(s => s.IsMultiNote).SelectMany(s => SplitSliders(s.Notes)).Where(n => n.Count > 1);

        // Whether the Analyzer grouped notes too far apart to be a slider into one swing
        internal static bool HasSeparateSwings(List<Note> swingNotes) => SplitSliders(swingNotes).Count() > 1;

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

        // Turned like the game does for windows (WindowSnap)
        internal static double CutAngle(Note note) => WindowSnap.Angle(note) ?? Mod(DirectionToDegree[note.CutDirection] + note.AngleOffset, 360);

        // Signed difference from a to b in degrees, from -180 to 180
        private static double AngleDifference(double a, double b) => Mod(b - a + 180, 360) - 180;
    }
}

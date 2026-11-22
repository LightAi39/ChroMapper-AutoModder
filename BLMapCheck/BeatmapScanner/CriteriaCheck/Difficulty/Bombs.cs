using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Bombs
    {
        // Periods in seconds where at least one light is bright enough, from basic and V3 light events, merged and sorted
        private static List<(double Start, double End)> GetLitTimes(CheckContext ctx, List<Light> events, List<Lightcoloreventboxgroup> v3events)
        {
            double minimum = ctx.Config.LightBombMinimumBrightness;
            var periods = GetV3LitTimes(ctx, v3events);
            foreach (var group in events.Where(e => Lights.LightTypes.Contains(e.Type)).GroupBy(e => e.Type))
            {
                var lights = group.OrderBy(e => e.Seconds).ToList();
                for (int i = 0; i < lights.Count; i++)
                {
                    var ev = lights[i];
                    // Each event lasts until the next event of the same type
                    double next = i + 1 < lights.Count ? lights[i + 1].Seconds : double.MaxValue;
                    double duration = 0;
                    if (ev.isOn || ev.isFlash || ev.isTransition)
                    {
                        if (ev.f >= minimum) duration = double.MaxValue;
                    }
                    else if (ev.isFade && ev.f > minimum)
                    {
                        // Fades linearly to 0, it stays bright enough until it reaches the minimum
                        duration = ctx.Config.LightFadeDuration * (1 - minimum / ev.f);
                    }
                    double end = duration == double.MaxValue ? next : System.Math.Min(next, ev.Seconds + duration);
                    // A light switched off or replaced is still counted for a short time, so strobes count as lit
                    if (duration > 0 && end < ev.Seconds + duration)
                    {
                        end = System.Math.Min(ev.Seconds + duration, end + ctx.Config.LightBombOffGrace);
                    }
                    if (end > ev.Seconds) periods.Add((ev.Seconds, end));
                }
            }

            var merged = new List<(double Start, double End)>();
            foreach (var period in periods.OrderBy(p => p.Start))
            {
                if (merged.Count > 0 && period.Start <= merged[merged.Count - 1].End)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = (last.Start, System.Math.Max(last.End, period.End));
                }
                else merged.Add(period);
            }
            return merged;
        }

        // V3 light groups: each index filter of a group is followed on its own, a filter covering the whole group applies to all its lights.
        // The brightest light counts: the brightness distribution adds to it, a strobe counts at its brightest. Lights switch one after
        // the other over the beat distribution, so the last one turns off that much later. Transitions are linear, easings are ignored
        private static List<(double Start, double End)> GetV3LitTimes(CheckContext ctx, List<Lightcoloreventboxgroup> v3events)
        {
            double minimum = ctx.Config.LightBombMinimumBrightness;
            var periods = new List<(double Start, double End)>();
            double Seconds(float beats) => ctx.Timescale.BPM.ToRealTime(beats);

            foreach (var group in v3events.GroupBy(g => g.Group))
            {
                // Events of each filter, the whole group ones are added to every filter. On the same beat, the box placed last wins
                var timelines = new Dictionary<(int, int, int, int, int, int, int, float, int), List<(float Beats, float Brightness, int Transition, float Spread, int Order)>>();
                var whole = new List<(float Beats, float Brightness, int Transition, float Spread, int Order)>();
                var order = 0;
                foreach (var boxGroup in group.OrderBy(g => g.Beats))
                {
                    foreach (var box in boxGroup.EventBoxGroup ?? new())
                    {
                        if (box.e == null) continue;
                        var f = box.Filter;
                        var key = f == null ? default : (f.f, f.p, f.t, f.r, f.c, f.n, f.s, f.l, f.d);
                        var boxOrder = order++;
                        var events = box.e.OrderBy(e => e.b).Select((e, index) =>
                        {
                            var brightness = e.s + (box.b == 1 || index > 0 ? Math.Max(0, box.r) : 0);
                            if (e.f > 0) brightness = Math.Max(brightness, e.sb);
                            return (boxGroup.Beats + e.b, brightness, e.i, box.w, boxOrder);
                        });
                        // Whole group: a single division, or every light from the first one (a step of 0 is a single light), without a limit
                        if (f == null || (((f.f == 1 && f.p <= 1) || (f.f == 2 && f.p == 0 && f.t == 1)) && (f.l <= 0 || f.l >= 1))) whole.AddRange(events);
                        else
                        {
                            if (!timelines.TryGetValue(key, out var list)) timelines[key] = list = new();
                            list.AddRange(events);
                        }
                    }
                }

                foreach (var timeline in timelines.Values.Select(t => t.Concat(whole)).Append(whole))
                {
                    double? from = null;
                    (float Beats, float Brightness) previous = (0, 0);
                    foreach (var ev in timeline.OrderBy(e => e.Beats).ThenBy(e => e.Order))
                    {
                        if (ev.Transition == 2) continue; // Extend keeps the previous state
                        double start = Seconds(previous.Beats), end = Seconds(ev.Beats);
                        if (ev.Transition == 1 && previous.Brightness != ev.Brightness)
                        {
                            // Bright enough on the part of the transition above the minimum
                            var crossing = start + (end - start) * (minimum - previous.Brightness) / (ev.Brightness - previous.Brightness);
                            if (previous.Brightness >= minimum && ev.Brightness < minimum)
                            {
                                periods.Add((from ?? start, crossing + Seconds(ev.Beats + ev.Spread) - end + ctx.Config.LightBombOffGrace));
                                from = null;
                            }
                            else if (previous.Brightness < minimum && ev.Brightness >= minimum) from = crossing;
                        }
                        else if (previous.Brightness >= minimum && ev.Brightness < minimum)
                        {
                            // Switched off: the last light follows after the distribution, strobes count as lit
                            periods.Add((from ?? start, Seconds(ev.Beats + ev.Spread) + ctx.Config.LightBombOffGrace));
                            from = null;
                        }
                        else if (previous.Brightness < minimum && ev.Brightness >= minimum) from = end;
                        previous = (ev.Beats, ev.Brightness);
                    }
                    if (from != null) periods.Add((from.Value, double.MaxValue));
                }
            }

            return periods;
        }

        public static CritResult Check(CheckContext ctx, List<Bomb> bombs, List<Note> notes, List<Chain> chains, List<SwingInfo> swings, List<Light> events, List<Lightcoloreventboxgroup> v3events)
        {
            CritResult criteria = CritResult.Success;

            // Bombs must be sufficiently lit during their presence
            if (bombs.Count > 0)
            {
                // A light of at least 50 brightness must be active from 250ms before a bomb until it has passed
                var litTimes = GetLitTimes(ctx, events, v3events);
                int index = 0;
                foreach (var bomb in bombs.OrderBy(b => b.Seconds))
                {
                    var from = System.Math.Max(0, bomb.Seconds - ctx.Config.LightBombReactionTime);
                    while (index < litTimes.Count && litTimes[index].End < bomb.Seconds) index++;
                    bool isLit = index < litTimes.Count && litTimes[index].Start <= from && litTimes[index].End >= bomb.Seconds;
                    if (!isLit)
                    {
                        ctx.Results.CreateDiffResult("Bomb Lit", Severity.Error, "Bomb", $"Bombs must be lit by a light of at least {ctx.Config.LightBombMinimumBrightness * 100} brightness from {ctx.Config.LightBombReactionTime * 1000}ms before until they have passed",
                            new() { new("BombLit", isLit.ToString()) }, new() { bomb });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Bombs cannot be placed in a way that forces a saber outside the 4x3 grid, which happens when bombs fill the whole grid
            // at once: bombs closer than 0.5m in depth (time * NJS) are one layer, both sabers can share a free cell
            var sorted = bombs.OrderBy(b => b.Beats).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var layer = new List<Bomb>();
                for (int j = i; j < sorted.Count && (sorted[j].Seconds - sorted[i].Seconds) * sorted[i].njs < 0.5; j++) layer.Add(sorted[j]);

                var cells = layer.Where(b => b.x >= 0 && b.x <= 3 && b.y >= 0 && b.y <= 2).Select(b => (b.x, b.y)).Distinct().Count();
                if (cells == 12)
                {
                    ctx.Results.CreateDiffResult("Bomb Grid", Severity.Error, "Bomb", "Bombs cannot fill the whole 4x3 grid, it forces a saber outside of the grid", new(), new() { layer[0] });
                    criteria = CritResult.Fail;
                    // Once per layer
                    i += layer.Count - 1;
                }
            }

            if (CheckSwingPath(ctx, sorted, notes, chains, swings)) criteria = CritResult.Fail;

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Bomb", Severity.Passed, "Bomb", "Bombs are properly lit, are not in a swing path and don't force a saber outside of the grid");
            }

            return criteria;
        }

        // Bombs may not be placed directly in the pre or post swing path of a note or chain head/link.
        // Pre-swing: the cell behind the note, shortly before it. Post-swing: every cell up to the grid edge in the swing direction, shortly after it.
        // Returns true if a bomb was found
        private static bool CheckSwingPath(CheckContext ctx, List<Bomb> bombs, List<Note> notes, List<Chain> chains, List<SwingInfo> swings)
        {
            if (bombs.Count == 0) return false;
            var window = (float)ctx.Config.BombSwingPathBeats;

            // Dots only have a known direction as part of a slider
            var swingDirection = SwingInfo.DirectionByNote(swings);
            // The post-swing of a chain head is checked along its links instead
            var heads = new HashSet<(int, int, int)>(chains.Select(c => (c.Color, c.x, c.y)));
            bool IsHead(Note n) => heads.Contains((n.Color, n.x, n.y)) && chains.Any(c => c.Color == n.Color && c.x == n.x && c.y == n.y && Math.Abs(c.Beats - n.Beats) < 0.001);

            HashSet<Bomb> found = new();
            void Report(IEnumerable<Bomb> inPath, BeatmapGridObject obj, string description)
            {
                foreach (var bomb in inPath.ToList())
                {
                    if (!found.Add(bomb)) continue;
                    ctx.Results.CreateDiffResult("Bomb Swing Path", Severity.Error, "Bomb", description, new(), new() { bomb, obj });
                }
            }

            foreach (var note in notes)
            {
                var degree = CutDegree(note, swingDirection);
                if (degree == null) continue;
                var (dx, dy) = GridStep(degree.Value);

                var behind = (x: note.x - dx, y: note.y - dy);
                Report(InRange(bombs, note.Beats - window, note.Beats).Where(b => b.x == behind.x && b.y == behind.y), note,
                    "Bombs may not be placed in the pre-swing path of a note");

                if (IsHead(note)) continue;
                var path = CellsToEdge(note.x, note.y, dx, dy);
                Report(InRange(bombs, note.Beats, note.Beats + window).Where(b => path.Contains((b.x, b.y))), note,
                    "Bombs may not be placed in the post-swing path of a note");
            }

            foreach (var chain in chains)
            {
                var links = FusedObject.GetLinks(chain);
                if (links.Count == 0) continue;
                var tail = links[links.Count - 1];

                // The saber goes through each link cell around the time of that link
                foreach (var link in links.Where(l => l.X != chain.x || l.Y != chain.y))
                {
                    Report(InRange(bombs, link.Beats - window, link.Beats + window).Where(b => b.x == link.X && b.y == link.Y), chain,
                        "Bombs may not be placed in the swing path of a chain");
                }

                // The swing continues past the last link in the direction of the chain, or of the head if the tail is on the head
                double degree;
                if (chain.tx != chain.x || chain.ty != chain.y) degree = Math.Atan2(chain.ty - chain.y, chain.tx - chain.x) * 180 / Math.PI;
                else if (chain.CutDirection >= 0 && chain.CutDirection < DirectionToDegree.Length) degree = DirectionToDegree[chain.CutDirection];
                else continue;
                var (dx, dy) = GridStep(degree);
                var path = CellsToEdge(tail.X, tail.Y, dx, dy);
                Report(InRange(bombs, tail.Beats, tail.Beats + window).Where(b => path.Contains((b.x, b.y))), chain,
                    "Bombs may not be placed in the post-swing path of a chain");
            }

            return found.Count > 0;
        }
    }
}

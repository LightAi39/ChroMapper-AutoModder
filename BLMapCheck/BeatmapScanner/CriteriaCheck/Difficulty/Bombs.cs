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
        // Bombs placed at the same beat within this many beats are a single layer
        private const float SameBeat = 0.01f;

        // Periods in seconds where at least one light is bright enough, merged and sorted
        private static List<(double Start, double End)> GetLitTimes(CheckContext ctx, List<Light> events)
        {
            double minimum = ctx.Config.LightBombMinimumBrightness;
            var periods = new List<(double Start, double End)>();
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

        public static CritResult Check(CheckContext ctx, List<Bomb> bombs, List<Note> notes, List<Chain> chains, List<SwingInfo> swings, List<Light> events, List<Lightcoloreventboxgroup> v3events)
        {
            CritResult criteria = CritResult.Success;

            // Bombs must be sufficiently lit during their presence
            // If V3 lights exist, bomb won't be checked
            if (bombs.Count > 0 && v3events.Any())
            {
                ctx.Results.CreateDiffResult("Bomb Lit", Severity.Inconclusive, "Bomb", "V3 Lights detected. Bombs visibility won't be checked",
                    new() { new("BombLit", "Inconclusive") });
                criteria = CritResult.Warning;
            }
            else if (bombs.Count > 0)
            {
                // A light of at least 50 brightness must be active from 250ms before a bomb until it has passed
                var litTimes = GetLitTimes(ctx, events);
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

            // Bombs cannot be placed in a way that forces a saber outside the 4x3 grid,
            // which only happens when bombs fill the whole grid at the same time
            var sorted = bombs.OrderBy(b => b.Beats).ToList();
            for (int i = 0; i < sorted.Count;)
            {
                var layer = new List<Bomb>();
                int j = i;
                while (j < sorted.Count && sorted[j].Beats - sorted[i].Beats < SameBeat)
                {
                    layer.Add(sorted[j]);
                    j++;
                }

                var cells = layer.Where(b => b.x >= 0 && b.x <= 3 && b.y >= 0 && b.y <= 2).Select(b => (b.x, b.y)).Distinct().Count();
                if (cells == 12)
                {
                    ctx.Results.CreateDiffResult("Bomb Grid", Severity.Error, "Bomb", "Bombs cannot fill the whole 4x3 grid, it forces a saber outside of the grid", new(), new() { layer[0] });
                    criteria = CritResult.Fail;
                }

                i = j;
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

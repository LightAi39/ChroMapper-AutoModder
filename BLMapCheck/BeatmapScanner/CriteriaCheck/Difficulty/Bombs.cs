using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

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

        public static CritResult Check(CheckContext ctx, List<Bomb> bombs, List<Light> events, List<Lightcoloreventboxgroup> v3events)
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

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Bomb", Severity.Passed, "Bomb", "Bombs are properly lit and don't force a saber outside of the grid");
            }

            return criteria;
        }
    }
}

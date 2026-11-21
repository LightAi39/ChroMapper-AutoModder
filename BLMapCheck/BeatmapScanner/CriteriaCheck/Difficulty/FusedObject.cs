using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class FusedObject
    {
        // Objects may not be placed in a way where they intersect in the Z dimension.
        // Objects that are placed within 0.5m of one another in the Z dimension on the same grid cell are considered to intersect.
        public static CritResult Check(CheckContext ctx, List<Note> notes, List<Bomb> bombs, List<Wall> obstacles, List<Chain> chains)
        {
            CritResult criteria = CritResult.Success;

            // Notes and bombs can be considered the same for this
            List<BeatmapGridObject> objects = new();
            objects.AddRange(notes);
            objects.AddRange(bombs);
            objects = objects.OrderBy(x => x.Beats).ToList();

            // Position and beat of every chain link. The head is a note, so it's already compared with the other notes
            var chainLinks = chains.Select(c => (Chain: c, Links: GetLinks(c))).ToList();

            // Compare walls
            foreach (var o in obstacles)
            {
                // to all notes and bombs
                foreach (var n in objects)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(ctx, n.Beats, n.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (n.Beats - (o.Beats + o.DurationInBeats) >= max)
                    {
                        break;
                    }

                    // Need to take into account wall height and duration
                    if (n.Beats >= o.Beats - max && n.Beats <= o.Beats + o.DurationInBeats + max && n.x <= o.x + o.Width - 1 && n.x >= o.x && n.y < o.y + o.Height && n.y >= o.y - 1)
                    {
                        ctx.Results.CreateDiffResult("Fused Object", Severity.Error, "Fused", "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { n, o });
                        criteria = CritResult.Fail;
                    }
                }

                // to all chains (links)
                foreach (var (c, links) in chainLinks)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(ctx, c.Beats, c.njs);

                    // Chains are ordered by their head beat, so if the head is above the max limit, break out of this loop
                    if (c.Beats - (o.Beats + o.DurationInBeats) >= max)
                    {
                        break;
                    }

                    // Need to take into account wall height and duration
                    var pre = o.Beats - max;
                    var post = o.Beats + o.DurationInBeats + max;
                    if (links.Exists(l => l.Beats >= pre && l.Beats <= post && l.X <= o.x + o.Width - 1 && l.X >= o.x && l.Y < o.y + o.Height && l.Y >= o.y - 1))
                    {
                        ctx.Results.CreateDiffResult("Fused Object", Severity.Error, "Fused", "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { c, o });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Compare notes and bombs
            for (int i = 0; i < objects.Count; i++)
            {
                var n = objects[i];
                // to notes and bombs
                for (int j = i + 1; j < objects.Count; j++)
                {
                    var next = objects[j];
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(ctx, next.Beats, next.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (next.Beats - n.Beats >= max)
                    {
                        break;
                    }

                    // Compare beat, x and y position
                    if (n.Beats >= next.Beats - max && n.Beats <= next.Beats + max && n.x == next.x && n.y == next.y)
                    {
                        ctx.Results.CreateDiffResult("Fused Object", Severity.Error, "Fused", "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { n, next });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Compare notes and bombs to chain links
            foreach (var n in objects)
            {
                foreach (var (c, links) in chainLinks)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(ctx, c.Beats, c.njs);

                    // Chains are ordered by their head beat, so if the head is above the max limit, break out of this loop
                    if (c.Beats - n.Beats >= max)
                    {
                        break;
                    }

                    // Chain is already over
                    if (n.Beats - c.TailInBeats >= max)
                    {
                        continue;
                    }

                    // Head note of this chain
                    if (n is Note note && note.Beats == c.Beats && note.x == c.x && note.y == c.y && note.Color == c.Color)
                    {
                        continue;
                    }

                    if (links.Exists(l => Math.Abs(l.Beats - n.Beats) < max && l.X == n.x && l.Y == n.y))
                    {
                        ctx.Results.CreateDiffResult("Fused Object", Severity.Error, "Fused", "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { n, c });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Compare chains
            for (int i = 0; i < chainLinks.Count; i++)
            {
                var (c, links) = chainLinks[i];
                // Need to calculate 0.5m based on the NJS and BPM
                var max = CalculateMeter(ctx, c.Beats, c.njs);

                // to chains
                for (int j = i + 1; j < chainLinks.Count; j++)
                {
                    var (c2, links2) = chainLinks[j];

                    // Chains are ordered by their head beat, so if the head is after this chain end, break out of this loop
                    if (c2.Beats - c.TailInBeats >= max)
                    {
                        break;
                    }

                    if (links.Exists(l => links2.Exists(l2 => Math.Abs(l.Beats - l2.Beats) < max && l.X == l2.X && l.Y == l2.Y)))
                    {
                        ctx.Results.CreateDiffResult("Fused Object", Severity.Error, "Fused", "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { c, c2 });
                        criteria = CritResult.Fail;
                    }
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Fused Object", Severity.Passed, "Fused", "No fused objects detected");
            }

            return criteria;
        }

        // Links are snapped to the closest grid cell
        internal static List<(int X, int Y, float Beats)> GetLinks(Chain chain)
        {
            var positions = FindChainLinksPosition(chain);
            var links = new List<(int X, int Y, float Beats)>();
            for (int i = 0; i < positions.Count; i++)
            {
                var beat = chain.Beats + (chain.TailInBeats - chain.Beats) * (i + 1) / (chain.SliceCount - 1);
                links.Add(((int)Math.Round(positions[i].x), (int)Math.Round(positions[i].y), beat));
            }
            return links;
        }

        public static double CalculateMeter(CheckContext ctx, float beat, float njs)
        {
            return Math.Round(ctx.SecondsToBeats(1, beat) / njs * ctx.Config.FusedDistance, 3);
        }
    }
}

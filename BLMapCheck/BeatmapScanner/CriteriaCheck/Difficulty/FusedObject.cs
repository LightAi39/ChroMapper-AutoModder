using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class FusedObject
    {
        // Objects may not be placed in a way where they intersect in the Z dimension.
        // Objects that are placed within 0.5m of one another in the Z dimension on the same grid cell are considered to intersect.
        public static CritResult Check(List<Note> notes, List<Bomb> bombs, List<Wall> obstacles, List<Chain> chains)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Fused Object";
            string checkType = "Fused";
            CritResult criteria = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            // Notes and bombs can be considered the same for this
            List<BeatmapGridObject> objects = new();
            objects.AddRange(notes);
            objects.AddRange(bombs);
            objects = objects.OrderBy(x => x.Beats).ToList();

            // Compare walls
            foreach (var o in obstacles)
            {
                // to all notes and bombs
                foreach (var n in objects)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(n.Beats, n.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (n.Beats - (o.Beats + o.DurationInBeats) >= max)
                    {
                        break;
                    }

                    // Need to take into account wall height and duration
                    if (n.Beats >= o.Beats - max && n.Beats <= o.Beats + o.DurationInBeats + max && n.x <= o.x + o.Width - 1 && n.x >= o.x && n.y < o.y + o.Height && n.y >= o.y - 1)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Error, checkType, "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { n, o });
                        criteria = CritResult.Fail;
                    }
                }

                // to all chains (links)
                foreach (var c in chains)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(c.Beats, c.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (c.TailInBeats - (o.Beats + o.DurationInBeats) >= max)
                    {
                        break;
                    }

                    // Need to take into account wall height, duration and chain duration
                    var pre = o.Beats - max;
                    var post = o.Beats + o.DurationInBeats + max;
                    if ((c.Beats >= pre || c.TailInBeats >= pre) && (c.Beats <= post || c.TailInBeats <= post) && c.tx <= o.x + o.Width - 1 && c.tx >= o.x && c.ty < o.y + o.Height && c.ty >= o.y - 1)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Error, checkType, "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { c, o });
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
                    var max = CalculateMeter(next.Beats, next.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (next.Beats - n.Beats >= max)
                    {
                        break;
                    }

                    // Compare beat, x and y position
                    if (n.Beats >= next.Beats - max && n.Beats <= next.Beats + max && n.x == next.x && n.y == next.y)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Error, checkType, "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { n, next });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Compare notes
            foreach (var n in notes)
            {
                // to chains
                foreach (var c in chains)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(c.Beats, c.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (c.TailInBeats - n.Beats >= max)
                    {
                        break;
                    }

                    // Head note, break loop
                    if (n.Beats == c.Beats && n.x == c.x && n.y == c.y && n.Color == c.Color)
                    {
                        break;
                    }

                    // Need to take into account chain duration
                    var pre = n.Beats - max;
                    var post = n.Beats + max;
                    if ((c.Beats >= pre || c.TailInBeats >= pre) && (c.Beats <= post || c.TailInBeats <= post) && IsPointBetween(n, c))
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Error, checkType, "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { n, c });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Compare bombs
            for (int i = 0; i < bombs.Count; i++)
            {
                var b = bombs[i];
                // to chains
                foreach (var c in chains)
                {
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(c.Beats, c.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (c.TailInBeats - b.Beats >= max)
                    {
                        break;
                    }

                    // Need to take into account chain duration
                    var pre = b.Beats - max;
                    var post = b.Beats + max;
                    if ((c.Beats >= pre || c.TailInBeats >= pre) && (c.Beats <= post || c.TailInBeats <= post) && IsPointBetween(b, c))
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Error, checkType, "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { b, c });
                        criteria = CritResult.Fail;
                    }
                }
            }

            // Compare chains
            for (int i = 0; i < chains.Count; i++)
            {
                var c = chains[i];
                // to chains
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    // Need to calculate 0.5m based on the NJS and BPM
                    var max = CalculateMeter(c.Beats, c.njs);

                    // Objects are ordered in beat, so if the beat is above the max limit, break out of this loop
                    if (Math.Abs(c2.TailInBeats - c.Beats) >= max || Math.Abs(c2.TailInBeats - c.TailInBeats) >= max || Math.Abs(c2.Beats - c.Beats) >= max || Math.Abs(c2.Beats - c.TailInBeats) >= max)
                    {
                        break;
                    }

                    // Need to take into account chain duration
                    var pre = c.Beats - max;
                    var post = c.Beats + max;
                    if ((c2.Beats >= pre || c2.TailInBeats >= pre) && (c2.Beats <= post || c2.TailInBeats <= post) && DoLinesIntersect(c, c2))
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Error, checkType, "Objects cannot collide within " + max.ToString() + " in the same line", new(), new() { c, c2 });
                        criteria = CritResult.Fail;
                    }
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Passed, checkType, "No fused objects detected");
            }

            timescale.BPM.ResetCurrentBPM();

            return criteria;
        }

        public static double CalculateMeter(float beat, float njs)
        {
            CriteriaCheckManager.timescale.BPM.SetCurrentBPM(beat);
            return Math.Round(CriteriaCheckManager.timescale.BPM.ToBeatTime(1) / njs * Instance.FusedDistance, 3);
        }
    }
}

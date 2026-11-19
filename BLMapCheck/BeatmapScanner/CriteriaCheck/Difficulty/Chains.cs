using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Chains
    {
        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public static CritResult Check(CheckContext ctx, List<Chain> chains, List<Note> notes, List<Arc> arcs, List<Bomb> bombs)
        {
            CritResult criteria = CritResult.Success;

            // Arcs may not be connected to bombs
            // The result is placed on the bomb, the plugin can't put comments on arcs
            Bomb OnBomb(int x, int y, float beat) => bombs.FirstOrDefault(b => b.x == x && b.y == y && Math.Abs(b.Beats - beat) < 0.001);
            foreach (var arc in arcs ?? new())
            {
                var bomb = OnBomb(arc.x, arc.y, arc.Beats) ?? OnBomb(arc.tx, arc.ty, arc.TailInBeats);
                if (bomb != null)
                {
                    ctx.Results.CreateDiffResult("Arc Bomb", Severity.Error, "Chain", "Arcs may not be connected to bombs", new(), new() { bomb });
                    criteria = CritResult.Fail;
                }
            }

            // Chains may not be a part of the first 16 notes of the map
            if ((notes.Count >= 16 && chains.Where(c => c.Beats <= notes[15].Beats).Any()) || 
                (notes.Count < 16 && chains.Any()))
            {
                float beat;
                if (notes.Count >= 16) beat = notes[15].Beats;
                else beat = chains.LastOrDefault().Beats;

                ctx.Results.CreateDiffResult("Early Chain", Severity.Error, "Chain", "Chains cannot be part of the first 16 notes of the map", new(), new(chains.Where(c => c.Beats <= beat)));
                criteria = CritResult.Fail;
            }

            foreach (var chain in chains)
            {
                // Chain heads must be inside the 4x3 grid
                if (chain.x < 0 || chain.x > 3 || chain.y < 0 || chain.y > 2)
                {
                    ctx.Results.CreateDiffResult("Chain Head", Severity.Error, "Chain", "Chain heads must be inside the 4x3 grid",
                        new List<Classes.Results.KeyValuePair>() { new("ChainHead", "X: " + chain.x.ToString() + " Y: " + chain.y.ToString()) }, new() { chain });
                    criteria = CritResult.Fail;
                }

                // Chain squish factor may not be set to a value of zero or lower
                if (chain.Squish <= 0)
                {
                    ctx.Results.CreateDiffResult("Chain Squish", Severity.Error, "Chain", "Chain squish factor may not be set to a value of zero or lower",
                        new List<Classes.Results.KeyValuePair>() { new("CurrentSquish", chain.Squish.ToString()) }, new() { chain });
                    criteria = CritResult.Fail;
                    continue;
                }

                // Chains must be complete
                if (chain.SliceCount >= 2 && notes.Exists(n => n.Beats == chain.Beats && n.x == chain.x && n.y == chain.y && n.Color == chain.Color))
                {
                    // Chains must be at least 12.5% links versus air/empty-space
                    var x = Math.Abs(chain.tx - chain.x) * chain.Squish;
                    var y = Math.Abs(chain.ty - chain.y) * chain.Squish;
                    var distance = Math.Sqrt(x * x + y * y);
                    var value = distance / (chain.SliceCount - 1);

                    // Difference between expected and current distance, multiplied by current squish to know maximum value
                    double max;
                    if (chain.ty == chain.y) max = Math.Round(ctx.Config.ChainLinkVsAir / value * chain.Squish, 2);
                    else max = Math.Round(ctx.Config.ChainLinkVsAir * 1.1 / value * chain.Squish, 2);

                    if (chain.Squish - 0.01 > max)
                    {
                        ctx.Results.CreateDiffResult("Chain Squish", Severity.Error, "Chain", "Chains must be at least 12.5% links versus air/empty-space",
                            new List<Classes.Results.KeyValuePair>() { new("CurrentSquish", chain.Squish.ToString()), new("MaxSquish", max.ToString()) }, new() { chain });
                        criteria = CritResult.Fail;
                    }

                    // Chains may not lead outside the 4x3 grid by more than 1 lane width
                    var newX = chain.x + (chain.tx - chain.x) * chain.Squish;
                    var newY = chain.y + (chain.ty - chain.y) * chain.Squish;
                    if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                    {
                        ctx.Results.CreateDiffResult("Chain Lead", Severity.Error, "Chain", "Chains may not lead outside the 4x3 grid by more than 1 lane width",
                            new List<Classes.Results.KeyValuePair>() { new("ChainLead", "X: " + newX.ToString() + " Y: " + newY.ToString()) }, new() { chain });
                        criteria = CritResult.Fail;
                    }

                    // Chains must not be in reverse direction, the head must always precede the links in time
                    if (chain.TailInBeats < chain.Beats)
                    {
                        ctx.Results.CreateDiffResult("Reversed Chain", Severity.Error, "Chain", "Chain cannot have a reverse direction",
                            new List<Classes.Results.KeyValuePair>() { new("ChainReverse", "Current duration: " + (chain.Beats - chain.TailInBeats).ToString()) }, new() { chain });
                        criteria = CritResult.Fail;
                    }

                    // There must be sufficient time between chains (and notes?)
                    var note = notes.FirstOrDefault(x => x.Beats >= chain.TailInBeats && x.Color == chain.Color);
                    if (note != null)
                    {
                        if (note.Beats - chain.TailInBeats < chain.TailInBeats - chain.Beats)
                        {
                            ctx.Results.CreateDiffResult("Chain Flick", Severity.Error, "Chain", "The amount of beats between the last link of the preceding chain and the next note of the same hand must be equal or higher than the preceding chain’s duration",
                            new List<Classes.Results.KeyValuePair>() { new("ChainFlick", "Chain duration: " + (chain.TailInBeats - chain.Beats).ToString() + " Duration between:" + (note.Beats - chain.TailInBeats).ToString()) }, new() { chain });
                            criteria = CritResult.Fail;
                        }
                    }

                    // Chains cannot change in direction by more than 45 degrees from the starting position and angle of the head note
                    // TODO: Need a new algo, this is horrible
                    if (chain.CutDirection >= DirectionToDegree.Length)
                    {
                        ctx.Results.CreateDiffResult("Chain Rotation", Severity.Error, "Chain", "Chains must have a proper cut direction",
                            new List<Classes.Results.KeyValuePair>() { new("ChainDirection", "Error") }, new() { chain });
                        criteria = CritResult.Fail;
                        continue;
                    }
                    var temp = new NoteData()
                    {
                        Direction = Mod(DirectionToDegree[chain.CutDirection], 360),
                        Line = chain.x,
                        Layer = chain.y
                    };
                    var temp2 = new NoteData()
                    {
                        Line = chain.tx,
                        Layer = chain.ty
                    };
                    var temp3 = new List<NoteData>
                    {
                        temp,
                        temp2
                    };
                    if (!IsSameDirection(ReverseCutDirection(FindAngleViaPosition(temp3, 0, 1)), temp.Direction, ctx.Config.MaxChainRotation) && !IsSameDirection(FindAngleViaPosition(temp3, 0, 1), temp.Direction, ctx.Config.MaxChainRotation))
                    {
                        ctx.Results.CreateDiffResult("Chain Rotation", Severity.Error, "Chain", "Chains cannot change in direction by more than 45 degrees from the starting position and angle of the head note",
                            new List<Classes.Results.KeyValuePair>() { new("ChainExceedsRotation", "True") }, new() { chain });
                        criteria = CritResult.Fail;
                    }
                }
                else
                {
                    ctx.Results.CreateDiffResult("Chain Slice", Severity.Error, "Chain", "Chains must have a head note and at least one visible link (sc >= 2)",
                            new List<Classes.Results.KeyValuePair>() { new("CurrentSliceCount:", chain.SliceCount.ToString()) }, new() { chain });
                    criteria = CritResult.Fail;
                }
            }

            if(criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Chain", Severity.Passed, "Chain", "Chains are all proper");
            }

            return criteria;
        }
    }
}

using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Chains
    {
        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public static CritResult Check(List<Chain> chains, List<Note> notes)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string checkType = "Chain";
            CritResult criteria = CritResult.Success;

            // Chains may not be a part of the first 16 notes of the map
            if ((notes.Count >= 16 && chains.Where(c => c.Beats <= notes[15].Beats).Any()) || 
                (notes.Count < 16 && chains.Any()))
            {
                float beat;
                if (notes.Count >= 16) beat = notes[15].Beats;
                else beat = chains.LastOrDefault().Beats;

                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Early Chain", Severity.Error, checkType, "Chains cannot be part of the first 16 notes of the map", new(), new(chains.Where(c => c.Beats <= beat)));
                criteria = CritResult.Fail;
            }

            foreach (var chain in chains)
            {
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
                    if (chain.ty == chain.y) max = Math.Round(Instance.ChainLinkVsAir / value * chain.Squish, 2);
                    else max = Math.Round(Instance.ChainLinkVsAir * 1.1 / value * chain.Squish, 2);
                    if (chain.Squish - 0.01 > max)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Chain Squish", Severity.Error, checkType, "Chains must be at least 12.5% links versus air/empty-space",
                            new List<Classes.Results.KeyValuePair>() { new("CurrentSquish", chain.Squish.ToString()), new("MaxSquish", max.ToString()) }, new() { chain });
                        criteria = CritResult.Fail;
                    }

                    // Chains may not lead outside the 4x3 grid by more than 1 lane width
                    var newX = chain.x + (chain.tx - chain.x) * chain.Squish;
                    var newY = chain.y + (chain.ty - chain.y) * chain.Squish;
                    if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Chain Lead", Severity.Error, checkType, "Chains may not lead outside the 4x3 grid by more than 1 lane width",
                            new List<Classes.Results.KeyValuePair>() { new("ChainLead", "X: " + newX.ToString() + " Y: " + newY.ToString()) }, new() { chain });
                        criteria = CritResult.Fail;
                    }

                    // Chains must not be in reverse direction, the head must always precede the links in time
                    if (chain.TailInBeats < chain.Beats)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Reversed Chain", Severity.Error, checkType, "Chain cannot have a reverse direction",
                            new List<Classes.Results.KeyValuePair>() { new("ChainReverse", "Current duration: " + (chain.Beats - chain.TailInBeats).ToString()) }, new() { chain });
                        criteria = CritResult.Fail;
                    }

                    // There must be sufficient time between chains (and notes?)
                    var note = notes.FirstOrDefault(x => x.Beats >= chain.TailInBeats && x.Color == chain.Color);
                    if (note != null)
                    {
                        if (note.Beats - chain.TailInBeats < chain.TailInBeats - chain.Beats)
                        {
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Chain Flick", Severity.Error, checkType, "The amount of beats between the last link of the preceding chain and the next note of the same hand must be equal or higher than the preceding chain’s duration",
                            new List<Classes.Results.KeyValuePair>() { new("ChainFlick", "Chain duration: " + (chain.TailInBeats - chain.Beats).ToString() + " Duration between:" + (note.Beats - chain.TailInBeats).ToString()) }, new() { chain });
                            criteria = CritResult.Fail;
                        }
                    }

                    // Chains cannot change in direction by more than 45 degrees from the starting position and angle of the head note
                    // TODO: Need a new algo, this is horrible
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
                    if (!IsSameDirection(ReverseCutDirection(FindAngleViaPosition(temp3, 0, 1)), temp.Direction, Instance.MaxChainRotation) && !IsSameDirection(FindAngleViaPosition(temp3, 0, 1), temp.Direction, Instance.MaxChainRotation))
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Chain Rotation", Severity.Error, checkType, "Chains cannot change in direction by more than 45 degrees from the starting position and angle of the head note",
                            new List<Classes.Results.KeyValuePair>() { new("ChainExceedsRotation", "True") }, new() { chain });
                        criteria = CritResult.Fail;
                    }
                }
                else
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Chain Slice", Severity.Error, checkType, "Chains must have a head note and at least one visible link (sc >= 2)",
                            new List<Classes.Results.KeyValuePair>() { new("CurrentSliceCount:", chain.SliceCount.ToString()) }, new() { chain });
                    criteria = CritResult.Fail;
                }
            }

            if(criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Chain", Severity.Passed, checkType, "Chains are all proper");
            }

            return criteria;
        }

        // Compared chain duration with a specific value, flag mismatch.
        public static void Consistency(List<Chain> chains)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Chain Consistency";
            string checkType = "Chain";
            double expected = 0.0625;

            if (Config.Instance.ChainPrecision != 0) expected = 1 / Config.Instance.ChainPrecision;

            foreach (Chain chain in chains)
            {
                double duration = chain.TailInBeats - chain.Beats;
                if (duration >= expected - 0.001 && duration <= expected + 0.001) continue;

                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Info, checkType, "Chain duration doesn't match expected value",
                        new List<Classes.Results.KeyValuePair>() { new("CurrentPrecision:", duration.ToString()) }, new() { chain });
            }
        }
    }
}

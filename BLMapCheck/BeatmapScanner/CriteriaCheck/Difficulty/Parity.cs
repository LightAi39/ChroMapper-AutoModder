using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Parity
    {
        // The Analyzer parity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public static CritResult Check(CheckContext ctx, List<SwingInfo> swings)
        {
            CritResult criteria = CritResult.Success;

            // Resets are allowed, but they must be backed by the music and be readable (R5C), which needs to be reviewed manually
            foreach (var swing in swings.Where(x => x.IsRebound))
            {
                ctx.Results.CreateDiffResult("Parity", Severity.Warning, "Parity", "Reset, must be backed by a musical element and be readable", new() { new("ErrorType", "Reset") }, new(swing.Notes));
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            foreach (var swing in swings.Where(x => double.IsPositiveInfinity(x.EBPM)))
            {
                ctx.Results.CreateDiffResult("Parity Mismatch", Severity.Error, "Parity", "Swing has an infinite EBPM, notes of the same swing are likely mismatched",
                    new() { new("ErrorType", "swingEBPM is equal PositiveInfinity") }, new(swing.Notes));
                criteria = CritResult.Fail;
            }

            foreach (var hand in new[] { swings.Where(x => x.RightHand).ToList(), swings.Where(x => !x.RightHand).ToList() })
            {
                for (int i = 1; i < hand.Count; i++)
                {
                    if (i == hand.Count - 1 && hand[i].Notes.Last().CutDirection == 8) break;
                    var difference = Math.Abs(hand[i].StartRotation - hand[i - 1].EndRotation);
                    if (difference >= ctx.Config.ParityWarningAngle)
                    {
                        ctx.Results.CreateDiffResult("Parity Warning", Severity.Warning, "Parity", "Parity degree difference",
                            new() { new("WarningType", difference + " degree difference") }, new(hand[i].Notes));
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                    else if (Math.Abs(hand[i].StartRotation) > 135 || Math.Abs(hand[i].EndRotation) > 135)
                    {
                        if (ctx.Config.ParityInvertedWarning)
                        {
                            ctx.Results.CreateDiffResult("Parity Inverted", Severity.Warning, "Parity", "Parity playing inverted",
                                new() { new("WarningType", "Playing inverted") }, new(hand[i].Notes));
                        }
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                }
            }

            if (ctx.Config.ParityDebug)
            {
                foreach (var swing in swings)
                {
                    Severity commentType = Severity.Data;
                    if (Math.Abs(swing.StartRotation) > 135 || Math.Abs(swing.EndRotation) > 135) commentType = Severity.Inconclusive;

                    List<Classes.Results.KeyValuePair> resultData = new()
                    {
                        new("Parity", swing.Forehand ? "Forehand" : "Backhand"),
                        new("Direction", Helper.Mod(Math.Round(swing.Direction), 360).ToString()),
                        new("IsReset", swing.IsReset.ToString()),
                        new("IsBombReset", swing.IsBombReset.ToString()),
                        new("EBPM", Math.Round(swing.EBPM, 2).ToString())
                    };

                    ctx.Results.CreateDiffResult("Parity Debug", commentType, "Parity", "Parity Debug", resultData, new(swing.Notes));
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Parity", Severity.Passed, "Parity", "No possible parity issue detected");
            }

            return criteria;
        }
    }
}

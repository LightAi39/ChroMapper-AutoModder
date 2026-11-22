using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
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
        public static CritResult Check(CheckContext ctx, List<SwingInfo> swings, List<Arc> arcs)
        {
            CritResult criteria = CritResult.Success;

            // Resets are allowed, but they must be backed by the music and be readable (R5C), which needs to be reviewed manually
            foreach (var swing in Resets(swings).Select(r => r.Swing))
            {
                ctx.Results.CreateDiffResult("Parity", Severity.Warning, "Parity", "Reset, must be backed by a musical element and be readable", new() { new("ErrorType", "Reset") }, new(swing.Notes));
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            if (CheckResetReadability(ctx, swings, arcs)) criteria = CritResult.Fail;

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
                            new() { new("WarningType", Math.Round(difference, 2) + " degree difference") }, new(hand[i].Notes));
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

        private enum ResetType { Forehand, Backhand, Triangle }

        // Resets must have visual aid through bombs or arcs, or attempt to indicate how they are played by having each note within the reset
        // pattern rotated within 45 degrees of the previous note, and being 0.5 beats apart or moved along the x-axis. A reset pattern that
        // isn't aided must have an odd number of swings, or end on an aided reset. Ignored for the types of reset introduced before
        // 1/3 of the mapped length or 30 seconds, whichever is longer. Bomb resets aren't resets here. Returns true if an issue was found
        private static bool CheckResetReadability(CheckContext ctx, List<SwingInfo> swings, List<Arc> arcs)
        {
            if (swings.Count == 0) return false;
            double first = swings.Min(s => s.StartSeconds), last = swings.Max(s => s.EndSeconds);
            var introduction = first + Math.Max((last - first) / 3, 30);

            // Every reset with the note before it, its type, and whether it is aided or readable
            var resets = new List<(SwingInfo Swing, Note Previous, ResetType Type, bool Aided, bool Readable)>();
            foreach (var (swing, previousSwing) in Resets(swings))
            {
                var previous = previousSwing.Notes[previousSwing.Notes.Count - 1];
                var note = swing.Notes[0];
                double? turn = IsArrow(previous) && IsArrow(note) ? Math.Abs(Helper.Mod(Slider.CutAngle(note) - Slider.CutAngle(previous) + 180, 360) - 180) : null;
                // A reset turning more than 45 degrees comes from a triangle: notes hit without breaking parity until it has to
                var type = turn > 45 ? ResetType.Triangle : swing.Forehand ? ResetType.Forehand : ResetType.Backhand;
                var aided = arcs.Any(a => a.Color == note.Color && Math.Abs(a.TailInBeats - note.Beats) < 0.01 && a.tx == note.x && a.ty == note.y);
                // Moved along the x-axis in the horizontal direction of the arrow, any way for vertical arrows and dots
                var dx = note.x - previous.x;
                var horizontal = IsArrow(note) ? Math.Cos(Helper.ConvertDegreesToRadians(Slider.CutAngle(note))) : 0;
                var moved = dx != 0 && (Math.Abs(horizontal) < 0.1 || Math.Sign(dx) == Math.Sign(horizontal));
                var readable = (turn ?? 0) <= 45 && (note.Beats - previous.Beats >= 0.5 - 0.001 || moved);
                resets.Add((swing, previous, type, aided, readable));
            }

            // Types of reset introduced early enough
            var introduced = resets.GroupBy(r => r.Type).Where(g => g.Min(r => r.Swing.StartSeconds) < introduction).Select(g => g.Key).ToHashSet();

            var issue = false;
            foreach (var reset in resets.Where(r => !introduced.Contains(r.Type) && !r.Aided && !r.Readable).OrderBy(r => r.Swing.StartSeconds))
            {
                ctx.Results.CreateDiffResult("Reset", Severity.Error, "Parity",
                    $"{reset.Type} resets introduced after the first 30 seconds or 1/3 of the map need a visual aid (bombs or arcs), or a note within 45 degrees of the previous one, 0.5 beats after it or moved along the x-axis",
                    new() { new("ResetType", reset.Type.ToString()) }, new(reset.Swing.Notes));
                issue = true;
            }

            // Reset patterns: runs of consecutive resets of a hand, with the swing before them
            foreach (var hand in resets.GroupBy(r => r.Swing.RightHand))
            {
                var ordered = hand.OrderBy(r => r.Swing.StartSeconds).ToList();
                var handSwings = swings.Where(s => s.RightHand == hand.Key).ToList();
                for (int i = 0; i < ordered.Count;)
                {
                    int j = i + 1;
                    while (j < ordered.Count && handSwings.IndexOf(ordered[j].Swing) == handSwings.IndexOf(ordered[j - 1].Swing) + 1) j++;
                    var pattern = ordered.GetRange(i, j - i);
                    i = j;

                    if (pattern.All(r => introduced.Contains(r.Type)) || pattern.All(r => r.Aided)) continue;
                    var swingCount = pattern.Count + 1;
                    if (swingCount % 2 == 1 || pattern[pattern.Count - 1].Aided) continue;
                    ctx.Results.CreateDiffResult("Reset Pattern", Severity.Error, "Parity",
                        $"Reset patterns without visual aid must have an odd number of swings or end on a visually aided reset ({swingCount} swings)",
                        new() { new("Swings", swingCount.ToString()) }, pattern.SelectMany(r => r.Swing.Notes).Cast<Parser.Map.Difficulty.V3.Base.BeatmapObject>().ToList());
                    issue = true;
                }
            }

            return issue;
        }

        // Resets (bomb resets aren't) with the previous swing of the hand. A reset next to notes the Analyzer grouped into one swing while
        // they are too far apart to be a slider is left out: they are separate swings, and the parity around them can't be trusted
        internal static IEnumerable<(SwingInfo Swing, SwingInfo Previous)> Resets(List<SwingInfo> swings)
        {
            foreach (var hand in new[] { swings.Where(x => x.RightHand).ToList(), swings.Where(x => !x.RightHand).ToList() })
            {
                for (int i = 1; i < hand.Count; i++)
                {
                    if (!hand[i].IsRebound || Slider.HasSeparateSwings(hand[i].Notes) || Slider.HasSeparateSwings(hand[i - 1].Notes)) continue;
                    yield return (hand[i], hand[i - 1]);
                }
            }
        }

        private static bool IsArrow(Note note) => note.CutDirection >= 0 && note.CutDirection < Helper.DirectionToDegree.Length;
    }
}

using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ProlongedSwing
    {
        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public static CritResult Check(CheckContext ctx, List<Note> notes, List<Chain> chains, List<SwingInfo> swings)
        {
            CritResult criteria = CritResult.Success;

            var duration = false;
            var maxDuration = Slider.MaxChainDuration(ctx, swings);

            foreach (var ch in chains)
            {
                // Chain duration cannot exceed 400% of the average effective slider duration (first to last note) of its section
                var max = maxDuration(ch.Beats);
                if (ch.TailInBeats - ch.Beats > max + 0.01)
                {
                    ctx.Results.CreateDiffResult("Chain Duration", Severity.Error, "Chain", "Chain duration cannot exceed 400% of the average effective slider duration",
                        new() { new("CurrentDuration", System.Math.Round(ch.TailInBeats - ch.Beats, 3).ToString()), new("MaximumDuration", System.Math.Round(max, 3).ToString()) }, new() { ch });
                    criteria = CritResult.Fail;

                    duration = true;
                }
                // Missing head notes are reported by the Chains check (Chain Slice)
            }

            if (!duration)
            {
                ctx.Results.CreateDiffResult("Chain Duration", Severity.Passed, "Chain", "Chains duration are under 400% the average effective slider duration");
            }

            // Possible dots spam and pauls
            var leftNotes = notes.Where(d => d.Color == 0).ToList();
            var rightNotes = notes.Where(d => d.Color == 1).ToList();

            var spam = CheckForSpam(ctx, leftNotes);
            var rightSpam = CheckForSpam(ctx, rightNotes);
            if (rightSpam > spam) spam = rightSpam;
            if (spam > criteria) criteria = spam;

            if (spam == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Dot Spam", Severity.Passed, "Swing", "Map doesn't have any prolonged swing duration");
            }

            return criteria;
        }
        
        public static CritResult CheckForSpam(CheckContext ctx, List<Note> notes)
        {
            CritResult criteria = CritResult.Success;
            Note previous = null;

            foreach (var note in notes)
            {
                if (previous != null)
                {
                    // Under 1/8, not on same beat, same position
                    if (note.Beats - previous.Beats <= 0.125 && note.Beats != previous.Beats && note.x == previous.x && note.y == previous.y)
                    {
                        // Any direction
                        if (note.CutDirection == 8)
                        {
                            ctx.Results.CreateDiffResult("Dot Spam", Severity.Warning, "Swing", "Swing duration should be consistent throughout the map",
                                new() { new("Type", "Dot Spam") }, new() { note });
                            if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                        }
                        else if (previous.CutDirection < DirectionToDegree.Length && note.CutDirection < DirectionToDegree.Length && IsSameDirection(DirectionToDegree[previous.CutDirection] + previous.AngleOffset, DirectionToDegree[note.CutDirection] + note.AngleOffset)) // Same direction
                        {
                            ctx.Results.CreateDiffResult("Dot Spam", Severity.Error, "Swing", "Swing duration should be consistent throughout the map",
                                new() { new("Type", "Dot Spam") }, new() { note });
                            if (CritResult.Fail > criteria) criteria = CritResult.Fail;
                        }
                    }
                }

                previous = note;
            }

            return criteria;
        }
    }
}

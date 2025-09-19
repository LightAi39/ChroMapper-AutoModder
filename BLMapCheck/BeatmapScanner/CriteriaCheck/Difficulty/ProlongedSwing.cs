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
        public static CritResult Check(List<Note> notes, List<Chain> chains)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string checkType = "Chain";
            CritResult criteria = CritResult.Success;

            var duration = false;
            var head = false;

            if (Config.Instance.AutomaticSliderPrecision)
            {
                SetAutoSliderPrecision();
            }

            foreach (var ch in chains)
            {
                // The duration of chains should be similar to the average effective slider duration or 150% of that
                if (ch.TailInBeats - ch.Beats >= Config.Instance.SliderPrecision * 2.1)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Chain Duration", Severity.Warning, checkType, "The duration of chains should be similar to the average effective slider duration or 150% of that. A longer duration might require justification if deemed too slow by Ranking Staff.", 
                        new() { new("CurrentDuration", (ch.TailInBeats - ch.Beats).ToString()), new("MaximumDuration", (Config.Instance.SliderPrecision * 2.1).ToString()) }, new() { ch });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;

                    duration = true;
                }

                // Chains must have a head note
                if (!notes.Exists(c => c.Beats == ch.Beats && c.Color == ch.Color && c.x == ch.x && c.y == ch.y))
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Chain Head", Severity.Error, checkType, "Chain must have an head note", 
                        new() { new("IssueType", "No head note at: " + ch.Beats + " " + ch.x + "/" + ch.y) }, new() { ch });
                    criteria = CritResult.Fail;

                    head = true;
                }
            }

            if (!duration)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Chain Duration", Severity.Passed, checkType, "Chains duration are under 150% the average effective slider duration");
            }

            if(!head)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Chain Head", Severity.Passed, checkType, "All chains in the map have an head note");
            }

            // Possible dots spam and pauls
            var leftNotes = notes.Where(d => d.Color == 0).ToList();
            var rightNotes = notes.Where(d => d.Color == 1).ToList();

            CheckForSpam(leftNotes, criteria);
            CheckForSpam(rightNotes, criteria);

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Dot Spam", Severity.Passed, "Swing", "Map doesn't have any prolonged swing duration");
            }

            return criteria;
        }
        
        public static void CheckForSpam(List<Note> notes, CritResult criteria)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string checkType = "Swing";
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
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                                "Dot Spam", Severity.Warning, checkType, "Swing duration should be consistent throughout the map",
                                new() { new("Type", "Dot Spam") }, new() { note });
                            if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                        }
                        else if (previous.CutDirection != 8 && IsSameDirection(DirectionToDegree[previous.CutDirection] + previous.AngleOffset, DirectionToDegree[note.CutDirection] + note.AngleOffset)) // Same direction
                        {
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                                "Dot Spam", Severity.Error, checkType, "Swing duration should be consistent throughout the map",
                                new() { new("Type", "Dot Spam") }, new() { note });
                            if (CritResult.Fail > criteria) criteria = CritResult.Fail;
                        }
                    }
                }

                previous = note;
            }
        }
    }
}

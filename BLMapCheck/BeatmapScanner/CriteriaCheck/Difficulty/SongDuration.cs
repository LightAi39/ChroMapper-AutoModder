using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SongDuration
    {
        // Detect if the mapped duration is above the minimum required, from first note to last note
        public static CritResult Check(List<Note> notes)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Mapped Duration";
            string checkType = "Duration";
            CritResult criteria = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            if(notes.Any())
            {
                // Calculate duration from first to last note to real time
                var duration = timescale.BPM.ToRealTime(notes.Last().Beats - notes.First().Beats, true);

                if (duration < Instance.MinSongDuration)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") });
                    criteria = CritResult.Fail;
                }
                else
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Passed, checkType, "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") });
                }  
            }
            else
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", "0s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") });
                criteria = CritResult.Fail;
            }

            return criteria;
        }
    }
}

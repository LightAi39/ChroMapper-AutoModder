using beatleader_parser.Timescale;
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
        public static CritResult Check(string characteristic, string difficulty, Timescale timescale, List<Note> notes)
        {
            CritResult criteria = CritResult.Success;

            if(notes.Any())
            {
                // Calculate duration from first to last note to real time
                var duration = timescale.BPM.ToRealTime(notes.Last().Beats - notes.First().Beats, true);

                if (duration < Instance.MinSongDuration)
                {
                    CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Mapped Duration", Severity.Error, "Duration", "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") });
                    criteria = CritResult.Fail;
                }
                else
                {
                    CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Mapped Duration", Severity.Passed, "Duration", "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") });
                }  
            }
            else
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Mapped Duration", Severity.Error, "Duration", "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        new() { new("MappedDuration", "0s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") });
                criteria = CritResult.Fail;
            }

            return criteria;
        }
    }
}

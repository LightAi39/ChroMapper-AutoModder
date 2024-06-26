﻿using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SongDuration
    {
        // Detect if the mapped duration is above the minimum required, from first note to last note, configurable setting is available
        public static CritResult Check(List<Note> notes)
        {
            var timescale = CriteriaCheckManager.timescale;

            if(notes.Any())
            {
                var duration = timescale.BPM.ToRealTime(notes.Last().Beats - notes.First().Beats, true);
                if (duration < Instance.MinSongDuration)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Mapped Duration",
                        Severity = Severity.Error,
                        CheckType = "Duration",
                        Description = "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                        ResultData = new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") },
                    });
                    return CritResult.Fail;
                }

                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Mapped Duration",
                    Severity = Severity.Passed,
                    CheckType = "Duration",
                    Description = "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                    ResultData = new() { new("MappedDuration", duration.ToString() + "s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") }
                });

                return CritResult.Success;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Characteristic = CriteriaCheckManager.Characteristic,
                Difficulty = CriteriaCheckManager.Difficulty,
                Name = "Mapped Duration",
                Severity = Severity.Error,
                CheckType = "Duration",
                Description = "The map from first note to last note must be at least " + Instance.MinSongDuration.ToString() + " seconds in length.",
                ResultData = new() { new("MappedDuration", "0s"), new("MinimumDuration", Instance.MinSongDuration.ToString() + "s") },
            });
            return CritResult.Fail;
        }
    }
}

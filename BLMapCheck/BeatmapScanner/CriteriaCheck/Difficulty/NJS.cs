using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.ChroMapper;
using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class NJS
    {
        // Warn the user if the current NJS and RT set doesn't match BeatLeader recommended value chart.
        public static CritResult Check(List<JoshaParity.SwingData> swings, float LoadedSongLength, float NoteJumpSpeed, float NoteJumpStartBeatOffset)
        {
            var issue = CritResult.Success;
            List<double> sps = new();

            for (int i = 0; i < LoadedSongLength - 1; i++)
            {
                BeatPerMinute.BPM.SetCurrentBPM(BeatPerMinute.BPM.ToRealTime(i, true));
                var secInBeat = BeatPerMinute.BPM.GetValue() / 60;
                sps.Add(swings.Where(s => s.swingStartBeat > i * secInBeat && s.swingStartBeat < (i + 1) * secInBeat).Count());
            }
            sps.Sort();
            sps.Reverse();
            var peak = sps.Take((int)(6.25f / 100 * sps.Count())).Average();
            (double min, double max) NJS = (0, 0);
            (double min, double max) RT = (0, 0);

            foreach (var val in Recommended.Values)
            {
                if (val.SPS < peak)
                {
                    NJS = val.NJS;
                    RT = val.RT;
                }
            }

            if (NoteJumpSpeed <= 0)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Note Jump Speed",
                    Severity = Severity.Error,
                    CheckType = "NoteJumpSpeed",
                    Description = "Note Jump Speed cannot be lower or equal to 0.",
                    ResultData = new() { new("NoteJumpSpeed", "NJS is currently: " + NoteJumpSpeed.ToString()) }
                });
                issue = CritResult.Fail;
            }
            else
            {
                if (NoteJumpSpeed < NJS.min || NoteJumpSpeed > NJS.max)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Note Jump Speed",
                        Severity = Severity.Suggestion,
                        CheckType = "NoteJumpSpeed",
                        Description = "Note Jump Speed is outside of the recommended value.",
                        ResultData = new() { new("NoteJumpSpeed", "Recommended NJS is: " + NJS.min.ToString() + " - " + NJS.max.ToString()) }
                    });
                    issue = CritResult.Warning;
                }
                var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(NoteJumpSpeed, NoteJumpStartBeatOffset, BeatPerMinute.BPM.GetValue());
                var beatms = 60000 / BeatPerMinute.BPM.GetValue();
                var reactionTime = beatms * halfJumpDuration;
                if (reactionTime < RT.min || reactionTime > RT.max)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Reaction Time",
                        Severity = Severity.Suggestion,
                        CheckType = "ReactionTime",
                        Description = "Reaction Time is outside of the recommended value.",
                        ResultData = new() { new("ReactionTime", "Recommended RT is: " + RT.min.ToString() + " - " + RT.max.ToString()) }
                    });
                    issue = CritResult.Warning;
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Note Jump Speed",
                    Severity = Severity.Info,
                    CheckType = "NoteJumpSpeed",
                    Description = "Note Jump Speed is within the recommended value.",
                    ResultData = new() { new("NoteJumpSpeed", "Recommended NJS is: " + NJS.min.ToString() + " - " + NJS.max.ToString()) }
                });
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Reaction Time",
                    Severity = Severity.Info,
                    CheckType = "ReactionTime",
                    Description = "Reaction Time is within the recommended value.",
                    ResultData = new() { new("ReactionTime", "Recommended RT is: " + RT.min.ToString() + " - " + RT.max.ToString()) }
                });
            }

            BeatPerMinute.BPM.ResetCurrentBPM();

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Note Jump Speed",
                    Severity = Severity.Passed,
                    CheckType = "NoteJumpSpeed",
                    Description = "No issue with NJS detected.",
                    ResultData = new() { new("NoteJumpSpeed", "Success") }
                });
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Reaction Time",
                    Severity = Severity.Passed,
                    CheckType = "ReactionTime",
                    Description = "No issue with RT detected.",
                    ResultData = new() { new("ReactionTime", "Success") }
                });
            }

            return issue;
        }
    }
}

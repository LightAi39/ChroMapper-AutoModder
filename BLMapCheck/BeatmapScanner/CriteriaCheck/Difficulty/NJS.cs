using BLMapCheck.BeatmapScanner.Data.Criteria;
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
        public static CritResult Check(List<JoshaParity.SwingData> swings, float songLength, float njs, float noteJumpStartBeatOffset)
        {
            var issue = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;
            List<double> sps = new();

            if (songLength == 0)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Note Jump Speed",
                    Severity = Severity.Error,
                    CheckType = "NoteJumpSpeed",
                    Description = "NJS check error, SongLength is 0. Make sure to use an ogg file.",
                    ResultData = new(),
                });
                return CritResult.Fail;
            }

            for (int i = 0; i < songLength - 1; i++)
            {
                timescale.BPM.SetCurrentBPM(timescale.BPM.ToRealTime(i, true));
                var secInBeat = timescale.BPM.GetValue() / 60;
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

            if (njs <= 0)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Note Jump Speed",
                    Severity = Severity.Error,
                    CheckType = "NoteJumpSpeed",
                    Description = "Note Jump Speed cannot be lower or equal to 0.",
                    ResultData = new() { new("CurrentNoteJumpSpeed", njs.ToString()), new("MinimumNoteJumpSpeed", "0") }
                });
                issue = CritResult.Fail;
            }
            else
            {
                if (njs < NJS.min || njs > NJS.max)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Note Jump Speed",
                        Severity = Severity.Suggestion,
                        CheckType = "NoteJumpSpeed",
                        Description = "Note Jump Speed is outside of the recommended value.",
                        ResultData = new() { new("CurrentNoteJumpSpeed", njs.ToString()), new("RecommendedNoteJumpSpeed", NJS.min.ToString() + " - " + NJS.max.ToString()) }
                    });
                    issue = CritResult.Warning;
                }
                var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(njs, noteJumpStartBeatOffset, timescale.BPM.GetValue());
                var beatms = 60000 / timescale.BPM.GetValue();
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
                        ResultData = new() { new("CurrentReactionTime", reactionTime.ToString()), new("RecommendedReactionTime", RT.min.ToString() + " - " + RT.max.ToString()) }
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
                    ResultData = new()
                });
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Reaction Time",
                    Severity = Severity.Info,
                    CheckType = "ReactionTime",
                    Description = "Reaction Time is within the recommended value.",
                    ResultData = new()
                });
            }

            timescale.BPM.ResetCurrentBPM();

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
                    ResultData = new()
                });
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Reaction Time",
                    Severity = Severity.Passed,
                    CheckType = "ReactionTime",
                    Description = "No issue with RT detected.",
                    ResultData = new()
                });
            }

            return issue;
        }
    }
}

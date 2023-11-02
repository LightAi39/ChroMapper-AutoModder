using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.ChroMapper;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class NJS
    {
        // Warn the user if the current NJS and RT set doesn't match BeatLeader recommended value chart.
        public static CritSeverity Check(List<JoshaParity.SwingData> swings, float LoadedSongLength, float NoteJumpSpeed, float NoteJumpStartBeatOffset)
        {
            var issue = CritSeverity.Success;
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
                //ExtendOverallComment("R1A - NJS is currently " + diff.NoteJumpMovementSpeed); TODO: USE NEW METHOD
                issue = CritSeverity.Fail;
            }
            else
            {
                if (NoteJumpSpeed < NJS.min || NoteJumpSpeed > NJS.max)
                {
                    //ExtendOverallComment("R1A - Warning - Recommended NJS is " + NJS.min.ToString() + " - " + NJS.max.ToString()); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }
                var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(NoteJumpSpeed, NoteJumpStartBeatOffset, BeatPerMinute.BPM.GetValue());
                var beatms = 60000 / BeatPerMinute.BPM.GetValue();
                var reactionTime = beatms * halfJumpDuration;
                if (reactionTime < RT.min || reactionTime > RT.max)
                {
                    //ExtendOverallComment("R1A - Warning - Recommended RT is " + RT.min.ToString() + " - " + RT.max.ToString()); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }
            }

            if (issue == CritSeverity.Success)
            {
                //ExtendOverallComment("R1A - Recommended NJS is " + NJS.min.ToString() + " - " + NJS.max.ToString()); TODO: USE NEW METHOD
                //ExtendOverallComment("R1A - Recommended RT is " + RT.min.ToString() + " - " + RT.max.ToString()); TODO: USE NEW METHOD
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

using beatleader_parser.Timescale;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ColdEnd
    {
        // Check for object near or after the end of the audio duration
        public static CritResult Check(string characteristic, string difficulty, Timescale timescale, List<BeatmapGridObject> objects, List<Wall> walls, float songLength)
        {
            CritResult criteria = CritResult.Success;

            // Audio error
            if (songLength == 0)
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                    "Cold End", Severity.Error, "Duration", "Cold End check error, SongLength is 0. Make sure to use an ogg file");
                criteria = CritResult.Fail;
            }
            var limit = timescale.BPM.ToBeatTime(songLength - (float)Instance.ColdEndDuration, true);

            // There must be at least 2 seconds of time after the last interactable objects.
            foreach (var obj in objects)
            {
                if (obj.Beats > limit)
                {
                    CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Cold End", Severity.Error, "Duration", "There must be at least " + Instance.ColdEndDuration.ToString() + " seconds of time after the last interactable object",
                        new() { new("CurrentBeat", obj.Beats.ToString()), new("MaximumBeat", limit.ToString()) }, new() { obj });
                    criteria = CritResult.Fail;
                }
            }

            // Walls may not extend outside of the song's length (beat + duration <= final beat number of the song in the editor).
            // TODO: Not sure if first and last lane is supposed to be allowed or not.
            foreach (var w in walls)
            {
                if (w.Beats + w.DurationInBeats > limit && ((w.x + w.Width >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Cold End", Severity.Error, "Duration", "There must be at least " + Instance.ColdEndDuration.ToString() + " seconds of time after the last interactable object",
                        new() { new("CurrentBeat", (w.Beats + w.DurationInBeats).ToString()), new("MaximumBeat", limit.ToString()) }, new() { w });
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Cold End", Severity.Passed, "Duration", "There is at least " + Instance.ColdEndDuration.ToString() + " seconds of time after the last interactable object");
            }

            return criteria;
        }
    }
}

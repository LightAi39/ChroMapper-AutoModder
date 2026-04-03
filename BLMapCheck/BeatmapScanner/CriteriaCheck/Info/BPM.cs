using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class BPM
    {
        public static CritResult Check(float bpm)
        {
            // TODO: Add automatic BPM detection
            CheckResults.Instance.CreateInfoResult("BPM", Severity.Inconclusive, "SongInfo", 
                "The maps BPM must be set to the song BPM or a multiple of it. This cannot be automatically detected currently", new() { new("BPM", "Could not be found") });

            return CritResult.Warning;
        }
    }
}

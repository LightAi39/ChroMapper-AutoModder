using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class BPM
    {
        public static CritResult Check(float bpm)
        {
            // TODO: Add automatic BPM detection
            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "BPM",
                Severity = Severity.Inconclusive,
                CheckType = "SongInfo",
                Description = "The maps BPM must be set to the songs BPM or a multiple, but could not be autodetected.",
                ResultData = new() { new("BPM", "Could not be found") }
            });

            return CritResult.Warning;
        }
    }
}

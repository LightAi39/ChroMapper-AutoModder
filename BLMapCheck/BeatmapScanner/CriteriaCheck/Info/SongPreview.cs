using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal class SongPreview
    {
        public static CritResult Check(float PreviewStartTime, float PreviewDuration)
        {
            if (PreviewStartTime == 12 && PreviewDuration == 10)
            {
                CheckResults.Instance.CreateInfoResult("Song Preview", Severity.Suggestion, "SongInfo", "The song preview is using default values. Consider changing it",
                    new() { new("PreviewStartTime", PreviewStartTime.ToString()), new("PreviewDuration", PreviewDuration.ToString()) });
                return CritResult.Warning;
            }

            CheckResults.Instance.CreateInfoResult("Song Preview", Severity.Passed, "SongInfo", "The song preview has been set",
                    new() { new("PreviewStartTime", PreviewStartTime.ToString()), new("PreviewDuration", PreviewDuration.ToString()) });
            return CritResult.Success;
        }
    }
}

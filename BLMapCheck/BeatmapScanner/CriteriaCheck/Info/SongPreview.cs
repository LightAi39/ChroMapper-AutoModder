using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal class SongPreview
    {
        public static CritResult Check(CheckContext ctx, float PreviewStartTime, float PreviewDuration)
        {
            if (PreviewStartTime == 12 && PreviewDuration == 10)
            {
                ctx.Results.CreateInfoResult("Song Preview", Severity.Suggestion, "SongInfo", "The song preview is using default values. Consider changing it",
                    new() { new("PreviewStartTime", PreviewStartTime.ToString()), new("PreviewDuration", PreviewDuration.ToString()) });
                // Not part of the criteria, only a suggestion
                return CritResult.Success;
            }

            ctx.Results.CreateInfoResult("Song Preview", Severity.Passed, "SongInfo", "The song preview has been set",
                    new() { new("PreviewStartTime", PreviewStartTime.ToString()), new("PreviewDuration", PreviewDuration.ToString()) });
            return CritResult.Success;
        }
    }
}

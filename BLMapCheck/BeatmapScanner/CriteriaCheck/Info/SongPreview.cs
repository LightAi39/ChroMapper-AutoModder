using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal class SongPreview
    {
        public CritResult Check(float PreviewStartTime, float PreviewDuration)
        {
            if (PreviewStartTime == 12 && PreviewDuration == 10)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Song Preview",
                    Severity = Severity.Suggestion,
                    CheckType = "SongInfo",
                    Description = "The song preview is using default values. Consider changing it.",
                    ResultData = new() { new("PreviewStartTime", PreviewStartTime.ToString()), new("PreviewDuration", PreviewDuration.ToString()) }
                });
                return CritResult.Warning;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Song Preview",
                Severity = Severity.Passed,
                CheckType = "SongInfo",
                Description = "The song preview has been set.",
                ResultData = new() { new("PreviewStartTime", PreviewStartTime.ToString()), new("PreviewDuration", PreviewDuration.ToString()) }
            });
            return CritResult.Success;
        }
    }
}

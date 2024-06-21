using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class Offset
    {
        public static CritResult Check(float SongTimeOffset)
        {
            if (SongTimeOffset != 0)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Song Time Offset",
                    Severity = Severity.Error,
                    CheckType = "SongInfo",
                    Description = "The song time offset is not 0. This is a deprecated feature.",
                    ResultData = new() { new("SongTimeOffset", SongTimeOffset.ToString()) }
                });
                return CritResult.Fail;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Song Time Offset",
                Severity = Severity.Passed,
                CheckType = "SongInfo",
                Description = "The song time offset is 0.",
                ResultData = new() { new("SongTimeOffset", SongTimeOffset.ToString()) }
            });
            return CritResult.Success;
        }
    }
}

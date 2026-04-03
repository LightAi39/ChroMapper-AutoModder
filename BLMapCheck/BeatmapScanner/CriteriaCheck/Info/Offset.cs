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
                CheckResults.Instance.CreateInfoResult("Song Time Offset", Severity.Error, "SongInfo", "The song time offset is not 0. This is a deprecated feature", 
                    new() { new("SongTimeOffset", SongTimeOffset.ToString()) });
                return CritResult.Fail;
            }

            CheckResults.Instance.CreateInfoResult("Song Time Offset", Severity.Passed, "SongInfo", "The song time offset is 0",
                    new() { new("SongTimeOffset", SongTimeOffset.ToString()) });
            return CritResult.Success;
        }
    }
}

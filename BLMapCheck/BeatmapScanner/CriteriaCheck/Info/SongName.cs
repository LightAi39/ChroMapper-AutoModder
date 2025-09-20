using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SongName
    {
        public static CritResult Check(string SongName)
        {
            if (SongName.Count() == 0)
            {
                CheckResults.Instance.CreateInfoResult("Song Name", Severity.Error, "SongInfo", "The song name field is empty",
                    new() { new("SongNameLength", "0") });
                return CritResult.Fail;
            }

            CheckResults.Instance.CreateInfoResult("Song Name", Severity.Passed, "SongInfo", "The song name field is not empty",
                    new() { new("SongNameLength", SongName.Count().ToString()) });
            return CritResult.Success;
        }
    }
}

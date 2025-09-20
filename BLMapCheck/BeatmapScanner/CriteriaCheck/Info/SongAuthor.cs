using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SongAuthor
    {
        public static CritResult Check(string SongAuthorName)
        {
            if (SongAuthorName.Count() == 0)
            {
                CheckResults.Instance.CreateInfoResult("Song Author", Severity.Error, "SongInfo", "The song author field is empty",
                    new() { new("SongAuthorLength", "0") });
                return CritResult.Fail;
            }

            CheckResults.Instance.CreateInfoResult("Song Author", Severity.Passed, "SongInfo", "The song author field is not empty",
                    new() { new("SongAuthorLength", SongAuthorName.Count().ToString()) });
            return CritResult.Success;
        }
    }
}

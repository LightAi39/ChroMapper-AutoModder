using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SongAuthor
    {
        public static Severity Check(string SongAuthorName)
        {
            if (SongAuthorName.Count() == 0)
            {
                //CreateSongInfoComment("R7C - Song Author field is empty", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                return Severity.Fail;
            }
            return Severity.Success;
        }
    }
}

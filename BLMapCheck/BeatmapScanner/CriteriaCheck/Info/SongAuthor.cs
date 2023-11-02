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
                //CreateSongInfoComment("R7C - Song Author field is empty", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                return CritResult.Fail;
            }
            return CritResult.Success;
        }
    }
}

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
                // CreateSongInfoComment("R7A - Song Name field is empty", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                return CritResult.Fail;
            }

            return CritResult.Success;
        }
    }
}

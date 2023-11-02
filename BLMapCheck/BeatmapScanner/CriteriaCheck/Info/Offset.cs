using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class Offset
    {
        public static CritResult Check(float SongTimeOffset)
        {
            if (SongTimeOffset != 0)
            {
                //CreateSongInfoComment("R7C - Song Time Offset should be 0. This is a deprecated feature", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                return CritResult.Fail;
            }
            return CritResult.Success;
        }
    }
}

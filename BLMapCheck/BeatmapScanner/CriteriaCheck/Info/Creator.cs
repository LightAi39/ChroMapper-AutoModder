using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class Creator
    {
        public static CritSeverity Check(string LevelAuthorName)
        {
            if (LevelAuthorName.Count() == 0)
            {
                //CreateSongInfoComment("R7C - Creator field is empty", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                return CritSeverity.Fail;
            }
            if (LevelAuthorName.Count() > MaxChar)
            {
                //CreateSongInfoComment("R7C - Creator field is too long. Maybe use a group name instead?", CommentTypesEnum.Suggestion); TODO: USE NEW METHOD
                return CritSeverity.Warning;
            }
            return CritSeverity.Success;
        }
    }
}

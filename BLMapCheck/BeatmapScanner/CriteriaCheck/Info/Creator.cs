using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class Creator
    {
        public static CritResult Check(string LevelAuthorName)
        {
            if (LevelAuthorName.Count() == 0)
            {
                CheckResults.Instance.CreateInfoResult("Creator", Severity.Error, "SongInfo", "The creator field is empty", new() { new("CreatorLength", "0") });
                return CritResult.Fail;
            }
            if (LevelAuthorName.Count() > Instance.MaxChar)
            {
                CheckResults.Instance.CreateInfoResult("Creator", Severity.Warning, "SongInfo", "The creator field is very long. Consider using a group name", 
                    new() { new("CreatorLength", LevelAuthorName.Count().ToString()) });
                return CritResult.Warning;
            }

            CheckResults.Instance.CreateInfoResult("Creator", Severity.Passed, "SongInfo", "The creator field is not empty and is not too long", 
                new() { new("CreatorLength", LevelAuthorName.Count().ToString()) });
            return CritResult.Success;
        }
    }
}

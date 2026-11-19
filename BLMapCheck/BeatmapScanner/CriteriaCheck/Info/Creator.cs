using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class Creator
    {
        public static CritResult Check(CheckContext ctx, string LevelAuthorName)
        {
            if (LevelAuthorName.Count() == 0)
            {
                ctx.Results.CreateInfoResult("Creator", Severity.Error, "SongInfo", "The creator field is empty", new() { new("CreatorLength", "0") });
                return CritResult.Fail;
            }
            CritResult searchable = Classes.Helper.Helper.CheckSearchable(ctx, "Creator", LevelAuthorName);
            if (LevelAuthorName.Count() > ctx.Config.MaxChar)
            {
                ctx.Results.CreateInfoResult("Creator", Severity.Warning, "SongInfo", "The creator field is very long. Consider using a group name", 
                    new() { new("CreatorLength", LevelAuthorName.Count().ToString()) });
                return CritResult.Warning;
            }

            if (searchable == CritResult.Warning) return CritResult.Warning;

            ctx.Results.CreateInfoResult("Creator", Severity.Passed, "SongInfo", "The creator field is not empty and is not too long", 
                new() { new("CreatorLength", LevelAuthorName.Count().ToString()) });
            return CritResult.Success;
        }
    }
}

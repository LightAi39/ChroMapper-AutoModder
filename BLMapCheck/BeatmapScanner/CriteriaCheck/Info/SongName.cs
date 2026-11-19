using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SongName
    {
        public static CritResult Check(CheckContext ctx, string SongName)
        {
            if (SongName.Count() == 0)
            {
                ctx.Results.CreateInfoResult("Song Name", Severity.Error, "SongInfo", "The song name field is empty",
                    new() { new("SongNameLength", "0") });
                return CritResult.Fail;
            }

            if (Classes.Helper.Helper.CheckSearchable(ctx, "Song Name", SongName) == CritResult.Warning) return CritResult.Warning;

            ctx.Results.CreateInfoResult("Song Name", Severity.Passed, "SongInfo", "The song name field is not empty",
                    new() { new("SongNameLength", SongName.Count().ToString()) });
            return CritResult.Success;
        }
    }
}

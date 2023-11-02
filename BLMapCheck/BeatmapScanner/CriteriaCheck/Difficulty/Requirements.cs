using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Requirements
    {
        public static CritResult Check(List<string> Requirements)
        {
            var issue = CritResult.Success;

            if (Requirements.Any())
            {
                //CreateSongInfoComment("R1C - " + diff.BeatmapFilename + " has " + req + " requirement", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                issue = CritResult.Fail;
            }

            return issue;
        }
    }
}

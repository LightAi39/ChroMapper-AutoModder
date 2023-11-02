using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SubName
    {
        public static CritSeverity Check(string SongName, string Author)
        {
            var issue = CritSeverity.Success;
            if (SongName.Count() != 0)
            {
                if (SongName.Contains("remix") || SongName.Contains("ver.") || SongName.Contains("feat.") || SongName.Contains("ft.") || SongName.Contains("featuring") || SongName.Contains("cover"))
                {
                    // CreateSongInfoComment("R7B - Song Name - Tags should be in the Sub Name field", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }
            if (Author.Count() != 0)
            {
                if (Author.Contains("remix") || Author.Contains("ver.") || Author.Contains("feat.") || Author.Contains("ft.") || Author.Contains("featuring") || Author.Contains("cover"))
                {
                    //CreateSongInfoComment("R7B - Song Author - Tags should be in the Sub Name field", CommentTypesEnum.Issue); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }
            return issue;
        }
    }
}

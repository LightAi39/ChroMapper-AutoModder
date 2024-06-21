using BLMapCheck.Classes.Results;
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
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Song Author",
                    Severity = Severity.Error,
                    CheckType = "SongInfo",
                    Description = "The song author field is empty.",
                    ResultData = new() { new("SongAuthorLength", "0") }
                });
                return CritResult.Fail;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Song Author",
                Severity = Severity.Passed,
                CheckType = "SongInfo",
                Description = "The song author field is not empty.",
                ResultData = new() { new("SongAuthorLength", SongAuthorName.Count().ToString()) }
            });
            return CritResult.Success;
        }
    }
}

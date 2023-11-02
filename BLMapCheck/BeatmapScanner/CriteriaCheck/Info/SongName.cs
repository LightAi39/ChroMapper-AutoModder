using BLMapCheck.Classes.Results;
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
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Song Name",
                    Severity = Severity.Error,
                    CheckType = "SongInfo",
                    Description = "The song name field is empty.",
                    ResultData = new() { new("SongNameLength", "0") }
                });
                return CritResult.Fail;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Song Name",
                Severity = Severity.Passed,
                CheckType = "SongInfo",
                Description = "The song name field is not empty.",
                ResultData = new() { new("SongNameLength", SongName.Count().ToString()) }
            });
            return CritResult.Success;
        }
    }
}

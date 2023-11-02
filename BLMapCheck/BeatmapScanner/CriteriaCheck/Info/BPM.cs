using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class BPM
    {
        public static CritResult Check()
        {
            // TODO: Add automatic BPM detection
            //CreateSongInfoComment("R1A - The map's BPM must be set to one of the song's BPM or a multiple of the song's BPM", CommentTypesEnum.Unsure); TODO: USE NEW METHOD
            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "BPM",
                Severity = Severity.Inconclusive,
                CheckType = "Info",
                Description = "The map's BPM must be set to the song's BPM or a multiple, but could not be autodetected.",
                BeatmapObjects = null,
                ResultData = null
            });

            return CritResult.Success;
        }
    }
}

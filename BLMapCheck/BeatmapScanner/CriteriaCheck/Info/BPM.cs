using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class BPM
    {
        public static Severity Check()
        {
            // TODO: Add automatic BPM detection
            //CreateSongInfoComment("R1A - The map's BPM must be set to one of the song's BPM or a multiple of the song's BPM", CommentTypesEnum.Unsure); TODO: USE NEW METHOD
            return Severity.Success;
        }
    }
}

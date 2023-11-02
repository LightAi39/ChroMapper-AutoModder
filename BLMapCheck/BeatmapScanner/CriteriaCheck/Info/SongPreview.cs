using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal class SongPreview
    {
        public Severity Check(float PreviewStartTime, float PreviewDuration)
        {
            if (PreviewStartTime == 12 && PreviewDuration == 10)
            {
                //CreateSongInfoComment("R7C - Modify Default Song Preview", CommentTypesEnum.Suggestion); TODO: USE NEW METHOD
                return Severity.Warning;
            }
            return Severity.Success;
        }
    }
}

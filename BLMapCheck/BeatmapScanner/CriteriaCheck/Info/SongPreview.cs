using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal class SongPreview
    {
        public CritSeverity Check(float PreviewStartTime, float PreviewDuration)
        {
            if (PreviewStartTime == 12 && PreviewDuration == 10)
            {
                //CreateSongInfoComment("R7C - Modify Default Song Preview", CommentTypesEnum.Suggestion); TODO: USE NEW METHOD
                return CritSeverity.Warning;
            }
            return CritSeverity.Success;
        }
    }
}

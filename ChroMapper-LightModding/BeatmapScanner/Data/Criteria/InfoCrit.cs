namespace ChroMapper_LightModding.BeatmapScanner.Data.Criteria
{
    public class InfoCrit
    {
        public Severity SongName { get; set; } = Severity.Fail;
        public Severity SubName { get; set; } = Severity.Fail;
        public Severity SongAuthor { get; set; } = Severity.Fail;
        public Severity Creator { get; set; } = Severity.Fail;
        public Severity Offset { get; set; } = Severity.Fail;
        public Severity BPM { get; set; } = Severity.Fail;
        public Severity DifficultyOrdering { get; set; } = Severity.Fail;
        public Severity Preview { get; set; } = Severity.Fail;

        public enum Severity
        {
            Success = 0,
            Warning = 1,
            Fail = 2
        }
    }
}

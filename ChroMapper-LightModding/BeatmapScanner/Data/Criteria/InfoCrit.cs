namespace ChroMapper_LightModding.BeatmapScanner.Data.Criteria
{
    public enum Severity
    {
        Success = 0,
        Warning = 1,
        Fail = 2
    }

    public class InfoCrit
    {
        public Severity SongName { get; set; } = 0; // Done
        public Severity SubName { get; set; } = 0; // Done
        public Severity SongAuthor { get; set; } = 0; // Done
        public Severity Creator { get; set; } = 0; // Done
        public Severity Offset { get; set; } = 0; // Done
        public Severity BPM { get; set; } = 0; // ?
        public Severity DifficultyOrdering { get; set; } = 0; // Check the TODO in the method
        public Severity Requirement { get; set; } = 0; // Done
        // Subjective
        public Severity Preview { get; set; } = 0;  // Done
    }
}

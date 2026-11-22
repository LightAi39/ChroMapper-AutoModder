using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class GridDrift
    {
        // The map must have lined up the audio's BPM and the grid's BPM as best as possible, through audio file setup or BPM events.
        // The notes of the difficulties sit on the grid: if it drifts from the song (a slightly wrong BPM, or a tempo change without BPM
        // events), the time from the notes to the nearest sound moves away from zero in parts of the song. That time is measured in
        // windows of WindowSeconds, a warning when it stays over MaxOffset for MinWindows windows in a row (a minute).
        // Shorter stretches happen on ranked maps (a section whose sounds start softly): 6 of the 300 newest ranked maps have 40 seconds
        // over 12ms, 2 have a minute.
        private const double WindowSeconds = 20;
        private const int MinWindowNotes = 15;
        private const double MaxOffset = 0.012;
        private const int MinWindows = 3;

        public static CritResult Check(CheckContext ctx, string songFilename, List<DifficultySet> difficulties)
        {
            var path = ctx.FolderPath != null && !string.IsNullOrEmpty(songFilename) ? Path.Combine(ctx.FolderPath, songFilename) : null;
            if (path == null || !File.Exists(path)) return CritResult.Success;
            var onsets = BpmDetector.OnsetTimes(path);
            if (onsets.Count == 0) return CritResult.Success;

            // The difficulties share the grid, their notes are pooled
            var times = difficulties.Where(d => (d.Characteristic == "Standard" || d.Characteristic == "OneSaber") && d.Data.Notes.Count >= 20)
                .SelectMany(d => d.Data.Notes.Select(n => Math.Round(n.Seconds, 3))).Distinct().OrderBy(t => t).ToList();
            var deviations = NoteTiming.Deviations(onsets, times);
            if (deviations.Count < MinWindowNotes * 2) return CritResult.Success;

            var windows = deviations.GroupBy(d => Math.Floor(d.Time / WindowSeconds)).Where(g => g.Count() >= MinWindowNotes)
                .Select(g => (Start: g.Key * WindowSeconds, Median: Median(g.Select(d => d.Deviation)))).OrderBy(w => w.Start).ToList();
            if (windows.Count < 2) return CritResult.Success;

            // Runs of consecutive windows off by more than MaxOffset in the same direction
            var runs = new List<List<(double Start, double Median)>>();
            List<(double Start, double Median)> run = null;
            foreach (var w in windows)
            {
                var off = Math.Abs(w.Median) > MaxOffset;
                var continues = run != null && off && Math.Sign(run[0].Median) == Math.Sign(w.Median) && w.Start - run[run.Count - 1].Start <= WindowSeconds + 0.001;
                if (continues) run.Add(w);
                else if (off) runs.Add(run = new() { w });
                else run = null;
            }
            runs = runs.Where(r => r.Count >= MinWindows).ToList();

            var data = new List<Classes.Results.KeyValuePair> { new("OffsetsMs", string.Join(" ", windows.Select(w => Math.Round(w.Median * 1000)))) };
            if (runs.Count == 0)
            {
                ctx.Results.CreateInfoResult("Grid Drift", Severity.Passed, "SongInfo", "The grid stays lined up with the song", data);
                return CritResult.Success;
            }

            string Time(double seconds) => $"{(int)(seconds / 60)}:{(int)(seconds % 60):00}";
            var parts = runs.Select(r => $"from {Time(r[0].Start)} to {Time(r[r.Count - 1].Start + WindowSeconds)} the notes are about {Math.Round(Math.Abs(Median(r.Select(w => w.Median))) * 1000)}ms {(r[0].Median < 0 ? "late" : "early")}");
            ctx.Results.CreateInfoResult("Grid Drift", Severity.Warning, "SongInfo",
                $"The grid may not be lined up with the song: {string.Join(", ", parts)}. The BPM may be slightly off, or the tempo of the song changes there and needs BPM events", data);
            return CritResult.Warning;
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[sorted.Count / 2];
        }
    }
}

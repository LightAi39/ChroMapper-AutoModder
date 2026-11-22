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
    internal static class NoteTiming
    {
        // Each note must be mapped within 12ms of a chosen timing point. Notes can't be checked one by one (vocals, slow attacks and
        // intended rhythm don't start on a sharp sound), but over a whole difficulty, the median time from the notes to the nearest sound
        // shows a map that is early or late everywhere: a wrong offset or audio padding. On the 300 newest ranked maps, 98% of the
        // difficulties are within 10ms (the detector itself is about 1ms early).
        private const double MaxMedian = 0.012;
        // Notes further than this from any sound aren't counted
        private const double MatchWindow = 0.06;
        // Fewer notes near a sound than this and the median isn't reliable
        private const int MinMatched = 30;

        public static CritResult Check(CheckContext ctx, string songFilename, List<DifficultySet> difficulties)
        {
            var path = ctx.FolderPath != null && !string.IsNullOrEmpty(songFilename) ? Path.Combine(ctx.FolderPath, songFilename) : null;
            if (path == null || !File.Exists(path)) return CritResult.Success;
            var onsets = BpmDetector.OnsetTimes(path);
            if (onsets.Count == 0) return CritResult.Success;
            var criteria = CritResult.Success;

            foreach (var diff in difficulties.Where(d => (d.Characteristic == "Standard" || d.Characteristic == "OneSaber") && d.Data.Notes.Count >= 20))
            {
                var times = diff.Data.Notes.Select(n => Math.Round(n.Seconds, 3)).Distinct().OrderBy(t => t).ToList();
                var deviations = Deviations(onsets, times).Select(d => d.Deviation).ToList();
                if (deviations.Count < MinMatched) continue;
                deviations.Sort();
                var median = deviations[deviations.Count / 2];
                var spread = deviations.Select(d => Math.Abs(d - median)).OrderBy(d => d).ElementAt(deviations.Count / 2);
                var data = new List<Classes.Results.KeyValuePair> { new("Difficulty", diff.Characteristic + " " + diff.Difficulty), new("MedianMs", Math.Round(median * 1000, 1).ToString()),
                    new("SpreadMs", Math.Round(spread * 1000, 1).ToString()), new("MatchedNotes", $"{deviations.Count}/{times.Count}") };
                if (Math.Abs(median) <= MaxMedian)
                {
                    ctx.Results.CreateInfoResult("Note Timing", Severity.Passed, "SongInfo", $"{diff.Characteristic} {diff.Difficulty} notes are on time with the song on average", data);
                    continue;
                }
                // A sound before the note: the note is late
                var late = median < 0;
                ctx.Results.CreateInfoResult("Note Timing", Severity.Warning, "SongInfo",
                    $"{diff.Characteristic} {diff.Difficulty} notes are on average {Math.Round(Math.Abs(median) * 1000)}ms {(late ? "late" : "early")} compared to the sounds of the song. Check the timing, the offset and the silence at the start of the audio", data);
                criteria = CritResult.Warning;
            }
            return criteria;
        }

        // Time from each note to the nearest sound, positive when the sound comes after the note. Notes with no sound within MatchWindow
        // are left out
        internal static List<(double Time, double Deviation)> Deviations(List<double> onsets, List<double> times)
        {
            var deviations = new List<(double, double)>();
            foreach (var t in times)
            {
                var i = onsets.BinarySearch(t);
                if (i < 0) i = ~i;
                var best = double.MaxValue;
                if (i < onsets.Count) best = onsets[i] - t;
                if (i > 0 && Math.Abs(onsets[i - 1] - t) < Math.Abs(best)) best = onsets[i - 1] - t;
                if (Math.Abs(best) <= MatchWindow) deviations.Add((t, best));
            }
            return deviations;
        }
    }
}

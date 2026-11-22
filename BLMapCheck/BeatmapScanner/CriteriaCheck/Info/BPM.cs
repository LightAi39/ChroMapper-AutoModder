using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using System;
using System.IO;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class BPM
    {
        // Relative difference allowed between the map BPM and a multiple of a detected BPM
        private const double Tolerance = 0.005;
        // A detected BPM that is a whole number is precise (the detector only rounds within 0.05 BPM when the song fits it as well). A map
        // BPM close to it but not equal moves the grid away from the song over time, more than this over the mapped part is a warning
        private const double MaxDrift = 0.012;

        // The map's BPM must be set to one of the song's BPM or a multiple of it
        public static CritResult Check(CheckContext ctx, float bpm, string songFilename)
        {
            var path = ctx.FolderPath != null && !string.IsNullOrEmpty(songFilename) ? Path.Combine(ctx.FolderPath, songFilename) : null;
            if (!ctx.Config.DetectSongBpm || path == null || !File.Exists(path))
            {
                string reason = !ctx.Config.DetectSongBpm ? "BPM detection is disabled" : "the audio file could not be found";
                ctx.Results.CreateInfoResult("BPM", Severity.Inconclusive, "SongInfo",
                    $"The maps BPM must be set to the song BPM or a multiple of it. It was not verified, {reason}", new() { new("BPM", bpm.ToString()) });
                return CritResult.Warning;
            }

            var detected = BpmDetector.Detect(path);
            if (detected.Count == 0)
            {
                ctx.Results.CreateInfoResult("BPM", Severity.Inconclusive, "SongInfo",
                    "The maps BPM must be set to the song BPM or a multiple of it. The song BPM could not be detected", new() { new("BPM", bpm.ToString()) });
                return CritResult.Warning;
            }

            var values = string.Join(", ", detected.Select(d => Math.Round(d.Bpm, 2)));
            // The offsets are only in the data, the comment only shows the BPM. One line per detected BPM, then the first sound, which
            // explains why silence may need to be added even when the beat is already lined up (hot start)
            var tempo = string.Join("\n", detected.Select(d => $"- {Math.Round(d.Bpm, 2)} BPM{(IsMultiple(bpm, d.Bpm) ? " (matches the map)" : "")}: "
                    + $"first beat at {d.Offset:0.000}s, {Alignment(d, ctx.Config.HotStartDuration)}"))
                + $"\nThe first sound is at {detected[0].FirstSound:0.000}s, the changes keep at least {ctx.Config.HotStartDuration}s of silence before it";
            ctx.Results.CreateInfoResult("Detected BPM", Severity.Info, "SongInfo", $"Detected song BPM: {values}",
                new() { new("DetectedBPM", values), new("DetectedTempo", tempo), new("MapBPM", bpm.ToString()) });

            var match = detected.FirstOrDefault(d => IsMultiple(bpm, d.Bpm));
            // With BPM events, the grid isn't the map BPM alone
            var bpmEvents = ctx.Map.Difficulties.Any(d => d.Data.bpmEvents.Any(e => Math.Abs(e.Bpm - bpm) > 0.001));
            if (match != null && match.Bpm == Math.Round(match.Bpm) && !bpmEvents)
            {
                var exact = Exact(bpm, match.Bpm);
                var duration = ctx.Map.Difficulties.SelectMany(d => d.Data.Notes).Select(n => (double)n.Seconds).DefaultIfEmpty(0).Max();
                var drift = Math.Abs(bpm - exact) / bpm * duration;
                if (drift > MaxDrift)
                {
                    ctx.Results.CreateInfoResult("BPM", Severity.Warning, "SongInfo",
                        $"The map BPM {bpm} is close to the detected song BPM {Math.Round(match.Bpm, 2)} but not equal, the grid drifts about {Math.Round(drift * 1000)}ms from the song by the last note. Use {Math.Round(exact, 3)} BPM if the song is at {Math.Round(match.Bpm, 2)}",
                        new() { new("BPM", bpm.ToString()), new("DetectedBPM", values), new("DriftMs", Math.Round(drift * 1000).ToString()) });
                    return CritResult.Warning;
                }
            }
            if (match != null)
            {
                ctx.Results.CreateInfoResult("BPM", Severity.Passed, "SongInfo", $"The map BPM {bpm} matches the detected song BPM {Math.Round(match.Bpm, 2)}",
                    new() { new("BPM", bpm.ToString()), new("DetectedBPM", values) });
                return CritResult.Success;
            }

            ctx.Results.CreateInfoResult("BPM", Severity.Warning, "SongInfo",
                $"The map BPM {bpm} doesn't match the detected song BPM ({values}). It must be set to the song BPM or a multiple of it",
                new() { new("BPM", bpm.ToString()), new("DetectedBPM", values) });
            return CritResult.Warning;
        }

        // Silence to add (positive) or remove (negative) at the start of the song so a beat falls exactly on 0s,
        // keeping at least the hot start duration before the first sound. The smallest change that does both is used.
        private static string Alignment(BpmDetector.TempoResult tempo, double hotStart)
        {
            double beat = 60.0 / tempo.Bpm;
            double minimum = hotStart - tempo.FirstSound;
            double Shift(double k) => -tempo.Offset + k * beat;
            double k = Math.Max(Math.Ceiling((minimum + tempo.Offset) / beat), Math.Round(tempo.Offset / beat));
            int ms = (int)Math.Round(Shift(k) * 1000);
            // A few ms is within the detection's precision
            if (Math.Abs(ms) <= 2) return "the audio is already lined up with the grid";
            if (ms > 0) return $"add {ms}ms of silence at the start of the audio to line it up with the grid";
            return $"remove {-ms}ms from the start of the audio to line it up with the grid";
        }

        // The multiple (or fraction) of the detected BPM closest to the map BPM
        private static double Exact(double mapBpm, double detectedBpm)
        {
            var candidates = Enumerable.Range(1, 4).SelectMany(n => new[] { detectedBpm * n, detectedBpm / n });
            return candidates.OrderBy(c => Math.Abs(c - mapBpm)).First();
        }

        // The map BPM is a multiple of the detected BPM. Fractions are accepted too, the detection only looks
        // between 89 and 205 BPM, so a slow song can be detected at twice its BPM.
        private static bool IsMultiple(double mapBpm, double detectedBpm)
        {
            for (int n = 1; n <= 4; n++)
            {
                if (Math.Abs(mapBpm - detectedBpm * n) <= mapBpm * Tolerance) return true;
                if (Math.Abs(mapBpm * n - detectedBpm) <= detectedBpm * Tolerance) return true;
            }
            return false;
        }
    }
}

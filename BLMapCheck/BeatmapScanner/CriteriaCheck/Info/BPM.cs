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
            // The offsets are only in the data, the comment only shows the BPM
            // The first sound explains why silence may need to be added even when the beat is already lined up (hot start)
            var tempo = string.Join("; ", detected.Select(d => $"{Math.Round(d.Bpm, 2)} BPM, first beat at {d.Offset:0.000}s, {Alignment(d, ctx.Config.HotStartDuration)}"))
                + $" (first sound at {detected[0].FirstSound:0.000}s, keeping {ctx.Config.HotStartDuration}s before it)";
            ctx.Results.CreateInfoResult("Detected BPM", Severity.Info, "SongInfo", $"Detected song BPM: {values}",
                new() { new("DetectedBPM", values), new("DetectedTempo", tempo), new("MapBPM", bpm.ToString()) });

            var match = detected.FirstOrDefault(d => IsMultiple(bpm, d.Bpm));
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
            if (ms > 0) return $"add {ms}ms at the start";
            if (ms < 0) return $"remove {-ms}ms at the start";
            return "already lined up";
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

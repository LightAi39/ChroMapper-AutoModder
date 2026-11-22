using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.IO;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SongAudio
    {
        // The audio file must be of acceptable quality. Only hints: a low sample rate, or high frequencies that stop early (a low quality
        // source), are warnings for the ranking team to listen to. Of the 300 newest ranked songs, only one is below 44.1 kHz, and only that
        // one stops below 15 kHz; about 8% stop around 16 kHz. The bitrate is only shown: Vorbis sets it from the quality level, ranked
        // songs at 64 and 112 kbps keep their high frequencies.
        private const int MinSampleRate = 44100;
        private const double MinCutoffHz = 15000;

        public static CritResult Check(CheckContext ctx, string songFilename)
        {
            var path = ctx.FolderPath != null && !string.IsNullOrEmpty(songFilename) ? Path.Combine(ctx.FolderPath, songFilename) : null;
            var measures = path != null && File.Exists(path) ? AudioQuality.Measure(path) : null;
            if (measures == null)
            {
                ctx.Results.CreateInfoResult("Audio Quality", Severity.Inconclusive, "SongInfo", "The audio quality could not be measured, the audio file could not be read");
                return CritResult.Warning;
            }

            var data = new List<Classes.Results.KeyValuePair> { new("SampleRate", measures.SampleRate.ToString()), new("NominalKbps", measures.NominalKbps.ToString()),
                new("CutoffHz", Math.Round(measures.CutoffHz).ToString()) };
            var issues = new List<string>();
            if (measures.SampleRate < MinSampleRate) issues.Add($"the sample rate is {measures.SampleRate} Hz");
            if (measures.CutoffHz < MinCutoffHz) issues.Add($"the high frequencies stop at {Math.Round(measures.CutoffHz / 1000, 1)} kHz");

            if (issues.Count > 0)
            {
                ctx.Results.CreateInfoResult("Audio Quality", Severity.Warning, "SongInfo",
                    $"The audio may be of low quality, {string.Join(", ", issues)}. Listen to it and look for a better source", data);
                return CritResult.Warning;
            }

            ctx.Results.CreateInfoResult("Audio Quality", Severity.Passed, "SongInfo", "No audio quality issue detected", data);
            return CritResult.Success;
        }
    }
}

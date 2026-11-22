using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace BLMapCheck.Classes.Helper
{
    // Audio quality measures of the song file: what the header says, and where the high frequencies stop
    internal static class AudioQuality
    {
        public class Measures
        {
            public int SampleRate { get; set; }
            // Nominal bitrate from the Vorbis header in kbps, 0 if not set
            public int NominalKbps { get; set; }
            // Highest frequency in Hz still within CutoffDrop dB of the 1 to 4 kHz level, averaged over the song. A low pass well below
            // the sample rate's limit is left by a low quality source (a 128 kbps MP3 or a video rip stops around 16 kHz)
            public double CutoffHz { get; set; }
        }

        private const int FrameSize = 4096;
        private const double CutoffDrop = 60;
        private const double SilenceRms = 0.003;

        private static readonly ConcurrentDictionary<string, (DateTime Modified, Measures Result)> Cache = new();

        // Null if the file can't be read
        public static Measures Measure(string path)
        {
            var modified = File.GetLastWriteTimeUtc(path);
            if (Cache.TryGetValue(path, out var cached) && cached.Modified == modified) return cached.Result;

            Measures result = null;
            try
            {
                int kbps;
                using (var reader = new NVorbis.VorbisReader(path)) kbps = reader.NominalBitrate / 1000;
                if (BpmDetector.ReadSamples(path, out var samples, out int rate))
                {
                    result = new Measures { SampleRate = rate, NominalKbps = kbps, CutoffHz = Cutoff(Spectrum(samples), rate) };
                }
            }
            catch (Exception)
            {
                result = null;
            }

            Cache[path] = (modified, result);
            return result;
        }

        // Average power spectrum of the frames that aren't silent
        private static double[] Spectrum(float[] samples)
        {
            var power = new double[FrameSize / 2 + 1];
            var window = Enumerable.Range(0, FrameSize).Select(i => 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FrameSize - 1))).ToArray();
            double[] re = new double[FrameSize], im = new double[FrameSize];
            for (int start = 0; start + FrameSize <= samples.Length; start += FrameSize)
            {
                double sum = 0;
                for (int k = 0; k < FrameSize; k++) sum += samples[start + k] * samples[start + k];
                if (Math.Sqrt(sum / FrameSize) < SilenceRms) continue;
                for (int k = 0; k < FrameSize; k++) { re[k] = samples[start + k] * window[k]; im[k] = 0; }
                BpmDetector.Fft(re, im);
                for (int k = 0; k < power.Length; k++) power[k] += re[k] * re[k] + im[k] * im[k];
            }
            return power;
        }

        // Highest frequency still within CutoffDrop dB of the 1 to 4 kHz level, on a spectrum smoothed over about 200 Hz
        private static double Cutoff(double[] power, int rate)
        {
            double binHz = (double)rate / FrameSize;
            int half = Math.Max(1, (int)(100 / binHz));
            var db = new double[power.Length];
            for (int k = 0; k < power.Length; k++)
            {
                int a = Math.Max(0, k - half), b = Math.Min(power.Length - 1, k + half);
                double sum = 0;
                for (int j = a; j <= b; j++) sum += power[j];
                db[k] = 10 * Math.Log10(sum / (b - a + 1) + 1e-30);
            }
            int lo = (int)(1000 / binHz), hi = (int)(4000 / binHz);
            double reference = db.Skip(lo).Take(hi - lo).Average();
            for (int k = db.Length - 1; k > hi; k--)
            {
                if (db[k] >= reference - CutoffDrop) return k * binHz;
            }
            return hi * binHz;
        }
    }
}

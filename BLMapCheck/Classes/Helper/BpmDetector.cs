using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// This file is a port of ArrowVortex (https://github.com/uvcat7/ArrowVortex) tempo detection and of the
// aubio (https://github.com/aubio/aubio) onset detection it uses. Both are licensed under the GNU General
// Public License v3.0, and so is this file: you can redistribute it and/or modify it under the terms of the
// GPLv3 as published by the Free Software Foundation, either version 3 of the License, or (at your option)
// any later version. It is distributed WITHOUT ANY WARRANTY. See <https://www.gnu.org/licenses/gpl-3.0.html>.

namespace BLMapCheck.Classes.Helper
{
    // Song BPM detection, ported from ArrowVortex (src/Editor/FindTempo.cpp, FindOnsets.cpp).
    // Onsets come from aubio's "complex" onset detector, then every beat interval between 89 and 205 BPM
    // is scored by how well the onsets line up with it.
    internal static class BpmDetector
    {
        public class TempoResult
        {
            public double Bpm { get; set; }
            public double Fitness { get; set; }
            // Time of the first beat in seconds, between 0 and the length of one beat
            public double Offset { get; set; }
            // Time of the first onset in seconds, where the song stops being silent
            public double FirstSound { get; set; }
        }

        private struct Onset
        {
            public int Pos;
            public double Strength;
        }

        private const double MinimumBPM = 89.0;
        private const double MaximumBPM = 205.0;
        private const int IntervalDelta = 10;
        private const int IntervalDownsample = 3;

        // Detection takes a few seconds, keep the result of each audio file until it changes
        private static readonly ConcurrentDictionary<string, (DateTime Modified, List<TempoResult> Result)> Cache = new();

        // Up to 3 BPM candidates, best first. Empty if the file can't be read or has too few onsets.
        public static List<TempoResult> Detect(string audioPath)
        {
            var modified = File.GetLastWriteTimeUtc(audioPath);
            if (Cache.TryGetValue(audioPath, out var cached) && cached.Modified == modified) return cached.Result;

            var result = new List<TempoResult>();
            if (ReadSamples(audioPath, out var samples, out int samplerate))
            {
                var onsets = FindOnsets(samples, samplerate);
                // ArrowVortex only weights the first 100 onsets by their loudness, the others keep a strength of 1
                for (int i = 0; i < Math.Min(onsets.Count, 100); i++)
                {
                    int a = Math.Max(0, onsets[i].Pos - 100);
                    int b = Math.Min(samples.Length, onsets[i].Pos + 100);
                    double v = 0;
                    for (int j = a; j < b; j++) v += Math.Abs(samples[j]);
                    onsets[i] = new Onset { Pos = onsets[i].Pos, Strength = v / Math.Max(1, b - a) };
                }
                var onsetArray = onsets.ToArray();
                result = CalculateBPM(onsetArray, samplerate);
                CalculateOffset(samples, samplerate, onsetArray, result);
                double firstSound = onsetArray.Length > 0 ? onsetArray.Min(o => o.Pos) / (double)samplerate : 0;
                foreach (var t in result) t.FirstSound = firstSound;
            }

            Cache[audioPath] = (modified, result);
            return result;
        }

        // Mono samples, the average of all channels
        private static bool ReadSamples(string path, out float[] samples, out int samplerate)
        {
            samples = null;
            samplerate = 0;
            try
            {
                using var reader = new NVorbis.VorbisReader(path);
                int channels = reader.Channels;
                samplerate = reader.SampleRate;
                var mono = new List<float>((int)Math.Min(reader.TotalSamples, int.MaxValue));
                var buffer = new float[samplerate * channels];
                int read;
                while ((read = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i + channels <= read; i += channels)
                    {
                        float sum = 0;
                        for (int c = 0; c < channels; c++) sum += buffer[i + c];
                        mono.Add(sum / channels);
                    }
                }
                samples = mono.ToArray();
                return samples.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Onsets (aubio "complex" onset detection)

        private static List<Onset> FindOnsets(float[] samples, int samplerate)
        {
            const int hop = 256;
            const int window = hop * 4;
            const int bins = window / 2 + 1;
            const float threshold = 0.15f;
            const float silence = -70f;
            const float lambda = 1f;
            uint delay = (uint)(4.6 * hop);
            uint minioi = (uint)Math.Round(0.05 * samplerate);
            float whiteningDecay = (float)Math.Pow(0.001, (hop / (double)samplerate) / 250.0);
            const float whiteningFloor = 1e-4f;

            var onsets = new List<Onset>();

            // Phase vocoder
            var data = new float[window];
            var hann = new float[window];
            for (int i = 0; i < window; i++) hann[i] = (float)(0.5 * (1.0 - Math.Cos(2 * Math.PI * i / window)));
            var re = new double[window];
            var im = new double[window];
            var norm = new float[bins];
            var phas = new float[bins];

            // Spectral whitening and complex domain descriptor
            var peakValues = Enumerable.Repeat(whiteningFloor, bins).ToArray();
            var oldmag = new float[bins];
            var theta1 = new float[bins];
            var theta2 = new float[bins];

            // Peak picker: 1 frame before and 5 after the tested one
            const int winPost = 5, winPre = 1, pickLength = winPost + winPre + 1;
            var onsetKeep = new float[pickLength];
            var onsetProc = new float[pickLength];
            var scratch = new float[pickLength];
            var onsetPeek = new float[3];

            uint totalFrames = 0, lastOnset = 0;
            var input = new float[hop];

            for (int start = 0; start <= samples.Length - hop; start += hop)
            {
                Array.Copy(samples, start, input, 0, hop);

                // Slide the analysis window, then apply the Hann window. aubio also shifts the frame before the FFT,
                // that only adds a constant phase per bin which cancels out in the complex domain descriptor.
                Array.Copy(data, hop, data, 0, window - hop);
                Array.Copy(input, 0, data, window - hop, hop);
                for (int i = 0; i < window; i++)
                {
                    re[i] = data[i] * hann[i];
                    im[i] = 0;
                }
                Fft(re, im);
                for (int i = 0; i < bins; i++)
                {
                    norm[i] = (float)Math.Sqrt(re[i] * re[i] + im[i] * im[i]);
                    phas[i] = (float)Math.Atan2(im[i], re[i]);
                }
                phas[0] = re[0] < 0 ? (float)Math.PI : 0f;
                phas[bins - 1] = re[bins - 1] < 0 ? (float)Math.PI : 0f;

                // Adaptive spectral whitening
                for (int i = 0; i < bins; i++)
                {
                    float tmp = Math.Max(whiteningDecay * peakValues[i], whiteningFloor);
                    peakValues[i] = Math.Max(norm[i], tmp);
                    norm[i] /= peakValues[i];
                }
                // Logarithmic compression
                for (int i = 0; i < bins; i++) norm[i] = (float)Math.Log(lambda * norm[i] + 1);

                // Complex domain onset detection function
                float desc = 0;
                for (int j = 0; j < bins; j++)
                {
                    float predicted = 2f * theta1[j] - theta2[j];
                    desc += (float)Math.Sqrt(Math.Abs(oldmag[j] * oldmag[j] + norm[j] * norm[j] - 2 * oldmag[j] * norm[j] * Math.Cos(predicted - phas[j])));
                    theta2[j] = theta1[j];
                    theta1[j] = phas[j];
                    oldmag[j] = norm[j];
                }

                // Peak picking
                Array.Copy(onsetKeep, 1, onsetKeep, 0, pickLength - 1);
                onsetKeep[pickLength - 1] = desc;
                Array.Copy(onsetKeep, onsetProc, pickLength);
                FiltFilt(onsetProc, scratch);
                float mean = onsetProc.Average();
                Array.Copy(onsetProc, scratch, pickLength);
                Array.Sort(scratch);
                float median = scratch[(pickLength - 1) / 2];
                onsetPeek[0] = onsetPeek[1];
                onsetPeek[1] = onsetPeek[2];
                onsetPeek[2] = onsetProc[winPost] - median - mean * threshold;
                float isonset = 0;
                if (onsetPeek[1] > onsetPeek[0] && onsetPeek[1] > onsetPeek[2] && onsetPeek[1] > 0)
                {
                    float s0 = onsetPeek[0], s1 = onsetPeek[1], s2 = onsetPeek[2];
                    isonset = 1 + 0.5f * (s0 - s2) / (s0 - 2 * s1 + s2);
                }

                // Onset validation: not silent, not too close to the previous one
                bool silent = 10 * Math.Log10(input.Sum(x => (double)x * x) / hop) < silence;
                bool detected = false;
                if (isonset > 0)
                {
                    if (!silent)
                    {
                        uint newOnset = totalFrames + (uint)Math.Round(isonset * hop);
                        if (lastOnset + minioi < newOnset)
                        {
                            if (!(lastOnset > 0 && delay > newOnset))
                            {
                                lastOnset = Math.Max(delay, newOnset);
                                detected = true;
                            }
                        }
                    }
                }
                else if (totalFrames <= delay && !silent)
                {
                    // Start of the file
                    if (totalFrames == 0 || lastOnset + minioi < totalFrames)
                    {
                        lastOnset = totalFrames + delay;
                        detected = true;
                    }
                }
                totalFrames += hop;

                if (detected)
                {
                    long pos = (long)lastOnset - delay;
                    if (pos >= 0) onsets.Add(new Onset { Pos = (int)pos, Strength = 1.0 });
                }
            }

            return onsets;
        }

        // Low pass biquad applied forward then backward, the filter memory is reset between passes
        private static void FiltFilt(float[] values, float[] tmp)
        {
            Biquad(values);
            for (int j = 0; j < values.Length; j++) tmp[values.Length - j - 1] = values[j];
            Biquad(tmp);
            for (int j = 0; j < values.Length; j++) values[j] = tmp[values.Length - j - 1];
        }

        private static void Biquad(float[] values)
        {
            const double b0 = 0.15998789, b1 = 0.31997577, b2 = 0.15998789, a1 = 0.23484048, a2 = 0;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int j = 0; j < values.Length; j++)
            {
                double x0 = values[j];
                double y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                values[j] = (float)y0;
                x2 = x1; x1 = x0;
                y2 = y1; y1 = y0;
            }
        }

        // In-place radix-2 FFT, the length must be a power of 2
        private static void Fft(double[] re, double[] im)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    (re[i], re[j]) = (re[j], re[i]);
                    (im[i], im[j]) = (im[j], im[i]);
                }
            }
            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2 * Math.PI / len;
                double wr = Math.Cos(angle), wi = Math.Sin(angle);
                for (int i = 0; i < n; i += len)
                {
                    double cr = 1, ci = 0;
                    for (int k = 0; k < len / 2; k++)
                    {
                        int a = i + k, b = i + k + len / 2;
                        double tr = re[b] * cr - im[b] * ci;
                        double ti = re[b] * ci + im[b] * cr;
                        re[b] = re[a] - tr;
                        im[b] = im[a] - ti;
                        re[a] += tr;
                        im[a] += ti;
                        double ncr = cr * wr - ci * wi;
                        ci = cr * wi + ci * wr;
                        cr = ncr;
                    }
                }
            }
        }

        #endregion

        #region Tempo (ArrowVortex FindTempo)

        // Histogram of the onsets folded over one beat interval, and the window used to score a gap position
        private class GapData
        {
            public readonly Onset[] Onsets;
            public readonly int[] WrappedPos;
            public readonly double[] WrappedOnsets;
            public readonly double[] Window;
            public readonly int Downsample;

            public GapData(int bufferSize, int downsample, Onset[] onsets)
            {
                Onsets = onsets;
                Downsample = downsample;
                WrappedPos = new int[onsets.Length];
                WrappedOnsets = new double[bufferSize];
                int windowSize = 2048 >> downsample;
                Window = new double[windowSize];
                double t = 2 * Math.PI / (windowSize - 1);
                for (int i = 0; i < windowSize; i++) Window[i] = 0.54 - 0.46 * Math.Cos(i * t);
            }
        }

        // How many onsets are close to the gap position
        private static double GapConfidence(GapData gap, int gapPos, int interval)
        {
            int windowSize = gap.Window.Length;
            int half = windowSize / 2;
            double area = 0;
            int begin = gapPos - half;
            int end = gapPos + half;
            if (begin < 0)
            {
                int wrappedBegin = begin + interval;
                for (int i = wrappedBegin; i < interval; i++) area += gap.WrappedOnsets[i] * gap.Window[i - wrappedBegin];
                begin = 0;
            }
            if (end > interval)
            {
                int wrappedEnd = end - interval;
                int offset = windowSize - wrappedEnd;
                for (int i = 0; i < wrappedEnd; i++) area += gap.WrappedOnsets[i] * gap.Window[i + offset];
                end = interval;
            }
            for (int i = begin; i < end; i++) area += gap.WrappedOnsets[i] * gap.Window[i - begin];
            return area;
        }

        // Best gap confidence for an interval, the offbeat counts for half
        private static double BestGap(GapData gap, int interval)
        {
            double highest = 0;
            for (int i = 0; i < gap.Onsets.Length; i++)
            {
                int pos = gap.WrappedPos[i];
                double confidence = GapConfidence(gap, pos, interval);
                confidence += GapConfidence(gap, (pos + interval / 2) % interval, interval) * 0.5;
                if (confidence > highest) highest = confidence;
            }
            return highest;
        }

        private static double ConfidenceForInterval(GapData gap, int interval)
        {
            Array.Clear(gap.WrappedOnsets, 0, gap.WrappedOnsets.Length);
            int reduced = interval >> gap.Downsample;
            for (int i = 0; i < gap.Onsets.Length; i++)
            {
                int pos = (gap.Onsets[i].Pos % interval) >> gap.Downsample;
                gap.WrappedPos[i] = pos;
                gap.WrappedOnsets[pos] += gap.Onsets[i].Strength;
            }
            return BestGap(gap, reduced);
        }

        private static double ConfidenceForBPM(GapData gap, Func<double, double> trend, int samplerate, double bpm)
        {
            Array.Clear(gap.WrappedOnsets, 0, gap.WrappedOnsets.Length);
            double intervalf = samplerate * 60.0 / bpm;
            int interval = (int)(intervalf + 0.5);
            for (int i = 0; i < gap.Onsets.Length; i++)
            {
                int pos = (int)(gap.Onsets[i].Pos % intervalf);
                gap.WrappedPos[i] = pos;
                gap.WrappedOnsets[pos] += gap.Onsets[i].Strength;
            }
            return BestGap(gap, interval) - trend(intervalf);
        }

        private static List<TempoResult> CalculateBPM(Onset[] onsets, int samplerate)
        {
            var tempo = new List<TempoResult>();
            if (onsets.Length < 2) return tempo;

            int minInterval = (int)(samplerate * 60.0 / MaximumBPM + 0.5);
            int maxInterval = (int)(samplerate * 60.0 / MinimumBPM + 0.5);
            int numIntervals = maxInterval - minInterval;
            var fitness = new double[numIntervals];
            double IntervalToBPM(int i) => samplerate * 60.0 / (i + minInterval);

            // Every 10th interval first, each thread has its own buffers
            int numCoarse = (numIntervals + IntervalDelta - 1) / IntervalDelta;
            Parallel.For(0, numCoarse, () => new GapData(maxInterval + 2, IntervalDownsample, onsets), (i, _, gap) =>
            {
                int index = i * IntervalDelta;
                fitness[index] = Math.Max(0.001, ConfidenceForInterval(gap, minInterval + index));
                return gap;
            }, _ => { });

            // Longer intervals collect more onsets per gap, remove that trend with a 3rd order polynomial fit
            var coarseX = new List<double>();
            var coarseY = new List<double>();
            for (int i = 0; i < numIntervals; i += IntervalDelta)
            {
                coarseX.Add(minInterval + i);
                coarseY.Add(fitness[i]);
            }
            var trend = FitCubic(coarseX, coarseY);
            double maxFitness = 0.001;
            for (int i = 0; i < numIntervals; i += IntervalDelta)
            {
                fitness[i] -= trend(minInterval + i);
                maxFitness = Math.Max(maxFitness, fitness[i]);
            }

            // Refine around the best coarse intervals
            double fitnessThreshold = maxFitness * 0.4;
            var refine = new List<int>();
            for (int i = 0; i < numIntervals; i += IntervalDelta)
            {
                if (fitness[i] > fitnessThreshold) refine.Add(i);
            }
            var toFill = refine.SelectMany(i => Enumerable.Range(Math.Max(0, i - IntervalDelta), Math.Min(numIntervals, i + IntervalDelta) - Math.Max(0, i - IntervalDelta)))
                .Distinct().Where(i => fitness[i] == 0).ToArray();
            var filled = new double[toFill.Length];
            Parallel.For(0, toFill.Length, () => new GapData(maxInterval + 2, IntervalDownsample, onsets), (k, _, gap) =>
            {
                int interval = minInterval + toFill[k];
                filled[k] = Math.Max(ConfidenceForInterval(gap, interval) - trend(interval), 0.1);
                return gap;
            }, _ => { });
            for (int k = 0; k < toFill.Length; k++) fitness[toFill[k]] = filled[k];
            foreach (int i in refine)
            {
                int begin = Math.Max(0, i - IntervalDelta), end = Math.Min(numIntervals, i + IntervalDelta);
                int best = 0;
                double highest = 0;
                for (int j = begin; j < end; j++)
                {
                    if (fitness[j] > highest)
                    {
                        highest = fitness[j];
                        best = j;
                    }
                }
                tempo.Add(new TempoResult { Bpm = IntervalToBPM(best), Fitness = fitness[best] });
            }

            // More precise gap window from here
            var precise = new GapData(maxInterval + 2, 0, onsets);

            // Remove weaker duplicates and multiples, round BPM values close to integers
            tempo = tempo.OrderByDescending(t => t.Fitness).ToList();
            for (int i = 0; i < tempo.Count; i++)
            {
                double bpm = tempo[i].Bpm, doubled = bpm * 2, halved = bpm * 0.5;
                for (int j = tempo.Count - 1; j > i; j--)
                {
                    double v = tempo[j].Bpm;
                    if (Math.Min(Math.Min(Math.Abs(v - bpm), Math.Abs(v - doubled)), Math.Abs(v - halved)) < 0.1) tempo.RemoveAt(j);
                }
            }
            foreach (var t in tempo)
            {
                double rounded = Math.Round(t.Bpm);
                double diff = Math.Abs(t.Bpm - rounded);
                if (diff < 0.01) t.Bpm = rounded;
                else if (diff < 0.05)
                {
                    double old = ConfidenceForBPM(precise, trend, samplerate, t.Bpm);
                    double cur = ConfidenceForBPM(precise, trend, samplerate, rounded);
                    if (cur > old * 0.99) t.Bpm = rounded;
                }
            }

            // Very close first and second options get a second opinion
            if (tempo.Count >= 2 && tempo[0].Fitness / tempo[1].Fitness < 1.05)
            {
                foreach (var t in tempo) t.Fitness = ConfidenceForBPM(precise, trend, samplerate, t.Bpm);
                tempo = tempo.OrderByDescending(t => t.Fitness).ToList();
            }

            // ArrowVortex found the correct BPM in the top 3 in all of its 300 test cases
            return tempo.Take(3).ToList();
        }

        #region Offset (ArrowVortex CalculateOffset)

        // Picks the offset of each BPM candidate: the most supported onset position, or its offbeat if the waveform rises more there
        private static void CalculateOffset(float[] samples, int samplerate, Onset[] onsets, List<TempoResult> tempo)
        {
            if (tempo.Count == 0) return;
            double maxInterval = tempo.Max(t => samplerate * 60.0 / t.Bpm);
            var gap = new GapData((int)(maxInterval + 1.0) + 2, 1, onsets);
            var slopes = ComputeSlopes(samples, samplerate);
            foreach (var t in tempo)
            {
                t.Offset = AdjustForOffbeats(slopes, samplerate, BaseOffset(gap, samplerate, t.Bpm), t.Bpm);
            }
        }

        // Most promising offset for a BPM value, every onset counts the same here
        private static double BaseOffset(GapData gap, int samplerate, double bpm)
        {
            Array.Clear(gap.WrappedOnsets, 0, gap.WrappedOnsets.Length);
            double intervalf = samplerate * 60.0 / bpm;
            int interval = (int)(intervalf + 0.5);
            for (int i = 0; i < gap.Onsets.Length; i++)
            {
                int pos = (int)(gap.Onsets[i].Pos % intervalf);
                gap.WrappedPos[i] = pos;
                gap.WrappedOnsets[pos] += 1.0;
            }

            double highest = 0;
            int offsetPos = 0;
            for (int i = 0; i < gap.Onsets.Length; i++)
            {
                int pos = gap.WrappedPos[i];
                double confidence = GapConfidence(gap, pos, interval);
                confidence += GapConfidence(gap, (pos + interval / 2) % interval, interval) * 0.5;
                if (confidence > highest)
                {
                    highest = confidence;
                    offsetPos = pos;
                }
            }
            return offsetPos / (double)samplerate;
        }

        // How much the waveform rises around each sample, comparing the 50ms after with the 50ms before
        private static double[] ComputeSlopes(float[] samples, int samplerate)
        {
            var slopes = new double[samples.Length];
            int wh = samplerate / 20;
            if (samples.Length < wh * 2) return slopes;

            double sumL = 0, sumR = 0;
            for (int i = 0, j = wh; i < wh; i++, j++)
            {
                sumL += Math.Abs(samples[i]);
                sumR += Math.Abs(samples[j]);
            }
            double scalar = 1.0 / wh;
            for (int i = wh, end = samples.Length - wh; i < end; i++)
            {
                slopes[i] = Math.Max(0.0, (sumR - sumL) * scalar);
                double cur = Math.Abs(samples[i]);
                sumL -= Math.Abs(samples[i - wh]);
                sumL += cur;
                sumR -= cur;
                sumR += Math.Abs(samples[i + wh]);
            }
            return slopes;
        }

        // Keeps the offset or its offbeat, whichever has the most waveform rise on the beats
        private static double AdjustForOffbeats(double[] slopes, int samplerate, double offset, double bpm)
        {
            double secondsPerBeat = 60.0 / bpm;
            double offbeat = offset + secondsPerBeat * 0.5;
            if (offbeat > secondsPerBeat) offbeat -= secondsPerBeat;

            double end = slopes.Length;
            double interval = secondsPerBeat * samplerate;
            double posA = offset * samplerate, sumA = 0;
            double posB = offbeat * samplerate, sumB = 0;
            for (; posA < end && posB < end; posA += interval, posB += interval)
            {
                sumA += slopes[(int)posA];
                sumB += slopes[(int)posB];
            }
            return sumA >= sumB ? offset : offbeat;
        }

        #endregion

        // Least squares cubic fit. x is rescaled to [-1, 1] to keep the normal equations well conditioned.
        private static Func<double, double> FitCubic(List<double> xs, List<double> ys)
        {
            double center = (xs.Min() + xs.Max()) / 2;
            double scale = Math.Max(1e-9, (xs.Max() - xs.Min()) / 2);
            const int n = 4;
            var a = new double[n, n + 1];
            for (int k = 0; k < xs.Count; k++)
            {
                double t = (xs[k] - center) / scale;
                var powers = new double[] { 1, t, t * t, t * t * t };
                for (int r = 0; r < n; r++)
                {
                    for (int c = 0; c < n; c++) a[r, c] += powers[r] * powers[c];
                    a[r, n] += powers[r] * ys[k];
                }
            }
            // Gaussian elimination with partial pivoting
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < n; r++) if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r;
                for (int c = 0; c <= n; c++) (a[col, c], a[pivot, c]) = (a[pivot, c], a[col, c]);
                if (Math.Abs(a[col, col]) < 1e-12) continue;
                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double factor = a[r, col] / a[col, col];
                    for (int c = col; c <= n; c++) a[r, c] -= factor * a[col, c];
                }
            }
            var coefs = new double[n];
            for (int r = 0; r < n; r++) coefs[r] = Math.Abs(a[r, r]) < 1e-12 ? 0 : a[r, n] / a[r, r];
            return x =>
            {
                double t = (x - center) / scale;
                return coefs[0] + coefs[1] * t + coefs[2] * t * t + coefs[3] * t * t * t;
            };
        }

        #endregion
    }
}

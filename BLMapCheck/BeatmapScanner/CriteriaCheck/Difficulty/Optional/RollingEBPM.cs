using BLMapCheck.Classes.Results;
using BLMapCheck.Classes.Helper;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional
{
    internal static class RollingEBPM
    {
        public class EbpmData
        {
            public SwingInfo Swing { get; set; }
            public double Average { get; set; } = 0;
            public bool Flick { get; set; } = false;
        }

        // Check for flick while using average EBPM over time
        public static void Check(CheckContext ctx, List<SwingInfo> swings, List<Parser.Map.Difficulty.V3.Grid.Note> notes)
        {
            var windowSize = 4f; // beats
            Queue<SwingInfo> dataWindowLeft = new();
            Queue<SwingInfo> dataWindowRight = new();
            List<EbpmData> rollingAverageLeft = new();
            List<EbpmData> rollingAverageRight = new();
            List<EbpmData> ReverseRollingAverageLeft = new();
            List<EbpmData> ReverseRollingAverageRight = new();

            // Preprocess the data
            foreach (var swing in swings)
            {
                EbpmData data = new();
                var clean = true;
                if (!swing.RightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        // Only keep the swings that are part of the window size (in beat) in the queue
                        if (dataWindowLeft.Peek().StartBeat < swing.StartBeat - windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    // Get the average of the current queue
                    data.Average = dataWindowLeft.Select(d => d.EBPM).Average();
                    rollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().StartBeat < swing.StartBeat - windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.EBPM).Average();
                    rollingAverageRight.Add(data);
                }
            }

            dataWindowLeft.Clear();
            dataWindowRight.Clear();

            // Preprocess the data in reverse
            for (int i = swings.Count - 1; i >= 0; i--)
            {
                var swing = swings[i];
                EbpmData data = new();
                var clean = true;
                if (!swing.RightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        if (dataWindowLeft.Peek().StartBeat > swing.StartBeat + windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowLeft.Select(d => d.EBPM).Average();
                    ReverseRollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().StartBeat > swing.StartBeat + windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.EBPM).Average();
                    ReverseRollingAverageRight.Add(data);
                }
            }

            // If the double of the average is slower than the current swing EBPM, consider that swing a flick
            foreach (var data in rollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.EBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.EBPM) data.Flick = true;
            }
            foreach (var data in rollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.EBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.EBPM) data.Flick = true;
            }

            // Only keep the flick that are considered flick on both the rolling average and reverse rolling average
            // The reverse lists contain the same swings in the opposite order
            for (int i = 0; i < rollingAverageLeft.Count; i++)
            {
                rollingAverageLeft[i].Flick = rollingAverageLeft[i].Flick && ReverseRollingAverageLeft[rollingAverageLeft.Count - 1 - i].Flick;
            }
            for (int i = 0; i < rollingAverageRight.Count; i++)
            {
                rollingAverageRight[i].Flick = rollingAverageRight[i].Flick && ReverseRollingAverageRight[rollingAverageRight.Count - 1 - i].Flick;
            }
            List<EbpmData> merged = new(rollingAverageLeft);
            merged.AddRange(rollingAverageRight);
            merged = merged.OrderBy(x => x.Swing.Notes.FirstOrDefault().Beats).ToList();

            foreach (var data in merged)
            {
                if (data.Flick)
                {
                    // Find the note to convert it to BeatmapObject
                    var note = data.Swing.Notes.FirstOrDefault();
                    if (note == null) continue;
                    
                    var index = notes.FindIndex(n => n == note);
                    if (index < 0) continue;
                    
                    var cube = notes[index];
                    if (index < notes.Count - 2)
                    {
                        // Check if the next two notes have the same spacing than the next note and the current one as an extra check
                        if (notes[index + 1].Beats - cube.Beats != notes[index + 2].Beats - notes[index + 1].Beats)
                        {
                            ctx.Results.CreateDiffResult("Unexpected Speed", Severity.Data, "Speed", "High EBPM compared to rolling average",
                                new() { new("CurrentSwingEBPM", data.Swing.EBPM.ToString()), new("RollingAvgEBPM", data.Average.ToString()) }, new() { note });
                        }
                    }
                    else // No extra check on the last few notes
                    {
                        ctx.Results.CreateDiffResult("Unexpected Speed", Severity.Data, "Speed", "High EBPM compared to rolling average",
                            new() { new("CurrentSwingEBPM", data.Swing.EBPM.ToString()), new("RollingAvgEBPM", data.Average.ToString()) }, new() { note });
                    }
                }
            }
        }
    }
}

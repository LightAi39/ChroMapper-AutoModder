using BLMapCheck.Classes.Results;
using JoshaParity;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class RollingEBPM
    {
        public class EbpmData
        {
            public SwingData Swing { get; set; } = new();
            public float Average { get; set; } = 0f;
            public bool Flick { get; set; } = false;
        }

        // Check for flick while using average EBPM over time
        public static void Check(List<SwingData> swings, List<Parser.Map.Difficulty.V3.Grid.Note> notes)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Unexpected Speed";
            string checkType = "Speed";

            var windowSize = 4f; // beats
            Queue<SwingData> dataWindowLeft = new();
            Queue<SwingData> dataWindowRight = new();
            List<EbpmData> rollingAverageLeft = new();
            List<EbpmData> rollingAverageRight = new();
            List<EbpmData> ReverseRollingAverageLeft = new();
            List<EbpmData> ReverseRollingAverageRight = new();

            // Preprocess the data
            foreach (var swing in swings)
            {
                EbpmData data = new();
                var clean = true;
                if (!swing.rightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        // Only keep the swings that are part of the window size (in beat) in the queue
                        if (dataWindowLeft.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    // Get the average of the current queue
                    data.Average = dataWindowLeft.Select(d => d.swingEBPM).Average();
                    rollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.swingEBPM).Average();
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
                if (!swing.rightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        if (dataWindowLeft.Peek().swingStartBeat > swing.swingStartBeat + windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowLeft.Select(d => d.swingEBPM).Average();
                    ReverseRollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().swingStartBeat > swing.swingStartBeat + windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.swingEBPM).Average();
                    ReverseRollingAverageRight.Add(data);
                }
            }

            // If the double of the average is slower than the current swing EBPM, consider that swing a flick
            foreach (var data in rollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in rollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }

            // Only keep the flick that are considered flick on both the rolling average and reverse rolling average
            rollingAverageLeft.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageLeft.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);
            rollingAverageRight.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageRight.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);
            List<EbpmData> merged = new(rollingAverageLeft);
            merged.AddRange(rollingAverageRight);
            merged = merged.OrderBy(x => x.Swing.notes.FirstOrDefault().Beats).ToList();

            foreach (var data in merged)
            {
                if (data.Flick)
                {
                    // Find the note to convert it to BeatmapObject
                    var note = data.Swing.notes.FirstOrDefault();
                    var index = notes.FindIndex(n => n == note);
                    var cube = notes[index];
                    if (index < notes.Count - 3)
                    {
                        // Check if the next two notes have the same spacing than the next note and the current one as an extra check
                        if (notes[index + 1].Beats - cube.Beats != notes[index + 2].Beats - notes[index + 1].Beats)
                        {
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                                name, Severity.Info, checkType, "High EBPM compared to rolling average",
                                new() { new("CurrentSwingEBPM", data.Swing.swingEBPM.ToString()), new("RollingAvgEBPM", data.Average.ToString()) }, new() { note });
                        }
                    }
                    else // No extra check on the last few notes
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Info, checkType, "High EBPM compared to rolling average",
                            new() { new("CurrentSwingEBPM", data.Swing.swingEBPM.ToString()), new("RollingAvgEBPM", data.Average.ToString()) }, new() { note });
                    }
                }
            }
        }
    }
}

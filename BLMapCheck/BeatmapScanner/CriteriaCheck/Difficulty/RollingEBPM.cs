using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
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

        public static void Check(List<SwingData> swings, List<Parser.Map.Difficulty.V3.Grid.Note> notes)
        {
            if (Configs.Config.Instance.DisplayFlick)
            {
                var windowSize = 4f; // beats
                Queue<SwingData> dataWindowLeft = new();
                Queue<SwingData> dataWindowRight = new();
                List<EbpmData> rollingAverageLeft = new();
                List<EbpmData> rollingAverageRight = new();
                List<EbpmData> ReverseRollingAverageLeft = new();
                List<EbpmData> ReverseRollingAverageRight = new();

                foreach (var swing in swings)
                {
                    EbpmData data = new();
                    var clean = true;
                    if (!swing.rightHand)
                    {
                        dataWindowLeft.Enqueue(swing);
                        do
                        {
                            if (dataWindowLeft.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowLeft.Dequeue();
                            else clean = false;
                        } while (clean);
                        data.Swing = swing;
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
                rollingAverageLeft.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageLeft.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);
                rollingAverageRight.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageRight.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);

                foreach (var data in rollingAverageLeft)
                {
                    if (data.Flick)
                    {
                        var note = data.Swing.notes.FirstOrDefault();
                        var index = notes.FindIndex(c => c.Beats == note.b && c.Color == note.c && note.x == c.x && note.y == c.y);
                        var cube = notes[index];
                        if (index < notes.Count - 3)
                        {
                            if (notes[index + 1].Beats - cube.Beats != notes[index + 2].Beats - notes[index + 1].Beats)
                            {
                                CheckResults.Instance.AddResult(new CheckResult()
                                {
                                    Characteristic = CriteriaCheckManager.Characteristic,
                                    Difficulty = CriteriaCheckManager.Difficulty,
                                    Name = "Unexpected Speed",
                                    Severity = Severity.Info,
                                    CheckType = "Speed",
                                    Description = "High EBPM compared to rolling average.",
                                    ResultData = new() { new("UnexpectedSpeed", "Swing EBPM: " + data.Swing.swingEBPM.ToString() + " Rolling Average EBPM: " + data.Average.ToString()) },
                                    BeatmapObjects = new() { cube }
                                });
                            }
                        }
                        else
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Unexpected Speed",
                                Severity = Severity.Info,
                                CheckType = "Speed",
                                Description = "High EBPM compared to rolling average.",
                                ResultData = new() { new("UnexpectedSpeed", "Swing EBPM: " + data.Swing.swingEBPM.ToString() + " Rolling Average EBPM: " + data.Average.ToString()) },
                                BeatmapObjects = new() { cube }
                            });
                        }
                    }
                }

                foreach (var data in rollingAverageRight)
                {
                    if (data.Flick)
                    {
                        var note = data.Swing.notes.FirstOrDefault();
                        var index = notes.FindIndex(c => c.Beats == note.b && c.Color == note.c && note.x == c.x && note.y == c.y);
                        var cube = notes[index];
                        if (index < notes.Count - 3)
                        {
                            if (notes[index + 1].Beats - cube.Beats != notes[index + 2].Beats - notes[index + 1].Beats)
                            {
                                CheckResults.Instance.AddResult(new CheckResult()
                                {
                                    Characteristic = CriteriaCheckManager.Characteristic,
                                    Difficulty = CriteriaCheckManager.Difficulty,
                                    Name = "Unexpected Speed",
                                    Severity = Severity.Info,
                                    CheckType = "Speed",
                                    Description = "High EBPM compared to rolling average.",
                                    ResultData = new() { new("UnexpectedSpeed", "Swing EBPM: " + data.Swing.swingEBPM.ToString() + " Rolling Average EBPM: " + data.Average.ToString()) },
                                    BeatmapObjects = new() { cube }
                                });
                            }
                        }
                        else
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Unexpected Speed",
                                Severity = Severity.Info,
                                CheckType = "Speed",
                                Description = "High EBPM compared to rolling average.",
                                ResultData = new() { new("UnexpectedSpeed", "Swing EBPM: " + data.Swing.swingEBPM.ToString() + " Rolling Average EBPM: " + data.Average.ToString()) },
                                BeatmapObjects = new() { cube }
                            });
                        }
                    }
                }
            }
        }
    }
}

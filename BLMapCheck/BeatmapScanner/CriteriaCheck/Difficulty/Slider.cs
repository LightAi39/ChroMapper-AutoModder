using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using JoshaParity;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Slider
    {
        public static float AverageSliderDuration { get; set; } = -1;

        // Get the average sliders precision and warn if it's not applied to all sliders in the map.
        // Also check if sliders is above 45 degree (that could use some work)
        public static CritResult Check(List<Colornote> Notes)
        {
            var issue = CritResult.Success;
            var cube = BeatmapScanner.Cubes.Where(c => c.Slider && !c.Head);
            cube = cube.OrderBy(c => c.Time).ToList();

            AverageSliderDuration = (float)ScanMethod.Mode(cube.Select(c => c.Precision / (c.Spacing + 1))).FirstOrDefault();
            if (AverageSliderDuration == 0) AverageSliderDuration = 0.0625f;

            foreach (var c in cube)
            { 
                if (c.Slider && !c.Head)
                {
                    if (!(c.Precision <= ((c.Spacing + 1) * AverageSliderDuration) + 0.01 && c.Precision >= ((c.Spacing + 1) * AverageSliderDuration) - 0.01))
                    {
                        var note = Notes.Where(note => c.Time == note.b && c.Type == note.c && note.x == c.Line && note.y == c.Layer).FirstOrDefault();
                        // var reality = ScanMethod.RealToFraction(c.Precision, 0.01);
                        var expected = ScanMethod.RealToFraction(((c.Spacing + 1) * AverageSliderDuration), 0.01);
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = BSMapCheck.Characteristic,
                            Difficulty = BSMapCheck.Difficulty,
                            Name = "Slider Precision",
                            Severity = Severity.Warning,
                            CheckType = "Slider",
                            Description = "Sliders must have equal spacing between notes to keep consistent swing duration.",
                            ResultData = new() { new("SliderPrecision", "Expected " + expected.N.ToString() + "/" + expected.D.ToString()) },
                            BeatmapObjects = new() { note }
                        });
                        issue = CritResult.Warning;
                    }
                }
            }

            cube = BeatmapScanner.Cubes.Where(c => c.Slider);
            cube = cube.OrderBy(c => c.Time).ToList();
            var red = cube.Where(c => c.Type == 0).ToList();
            var blue = cube.Where(c => c.Type == 1).ToList();

            // TODO: This could probably be done way better but idk
            for (int i = 0; i < red.Count(); i++)
            {
                List<double> dir = new();
                if (red[i].Head)
                {
                    if (red[i].CutDirection != 8)
                    {
                        dir.Add(red[i].Direction);
                    }
                    else
                    {
                        dir.Add(ScanMethod.FindAngleViaPosition(red, i + 1, i));
                    }

                    do
                    {
                        i++;
                        if (red.Count() == i)
                        {
                            break;
                        }
                        if (red[i].Head || !red[i].Pattern)
                        {
                            break;
                        }

                        dir.Add(ScanMethod.FindAngleViaPosition(red, i, i - 1));
                    } while (!red[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(degree, dir[j], 45))
                        {
                            var n = red[i - dir.Count() + j];
                            var note = Notes.Where(note => n.Time == note.b && n.Type == note.c && note.x == n.Line && note.y == n.Layer).FirstOrDefault();
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = BSMapCheck.Characteristic,
                                Difficulty = BSMapCheck.Difficulty,
                                Name = "Slider Rotation",
                                Severity = Severity.Error,
                                CheckType = "Slider",
                                Description = "Multiple notes of the same color on the same swing must not differ by more than 45°.",
                                ResultData = new() { new("SliderRotation", "True") },
                                BeatmapObjects = new() { note }
                            });
                            issue = CritResult.Fail;
                        }
                    }

                    i--;
                }
            }

            for (int i = 0; i < blue.Count(); i++)
            {
                List<double> dir = new();

                if (blue[i].Head)
                {
                    if (blue[i].CutDirection != 8)
                    {
                        dir.Add(blue[i].Direction);
                    }
                    else
                    {
                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i + 1, i));
                    }

                    do
                    {
                        i++;
                        if (blue.Count() == i)
                        {
                            break;
                        }
                        if (blue[i].Head || !blue[i].Pattern)
                        {
                            break;
                        }

                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i, i - 1));
                    } while (!blue[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(degree, dir[j], 45))
                        {
                            var n = blue[i - dir.Count() + j];
                            var note = Notes.Where(note => n.Time == note.b && n.Type == note.c && note.x == n.Line && note.y == n.Layer).FirstOrDefault();
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = BSMapCheck.Characteristic,
                                Difficulty = BSMapCheck.Difficulty,
                                Name = "Slider Rotation",
                                Severity = Severity.Error,
                                CheckType = "Slider",
                                Description = "Multiple notes of the same color on the same swing must not differ by more than 45°.",
                                ResultData = new() { new("SliderRotation", "True") },
                                BeatmapObjects = new() { note }
                            });
                            issue = CritResult.Fail;
                        }
                    }

                    i--;
                }
            }

            return issue;
        }
    }
}

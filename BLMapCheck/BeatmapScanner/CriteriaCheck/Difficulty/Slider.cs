using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Slider
    {
        public static float AverageSliderDuration { get; set; } = -1;
        // Get the average sliders precision and warn if it's not applied to all sliders in the map.
        // Also check if sliders is above 45 degree (that could use some work)
        public static CritResult Check()
        {
            var issue = CritResult.Success;
            var sliders = NotesData.Where(c => c.Pattern && !c.Head && c.Precision != 0).ToList();

            var MinSliderPrecision = 0.0625f;
            AverageSliderDuration = (float)Mode(sliders.Select(c => c.Precision / (c.Spacing + 1))).FirstOrDefault();
            if (AverageSliderDuration == 0) AverageSliderDuration = MinSliderPrecision;
            // TODO: Add a target precision for the sliders, instead of an average

            for (int i = 0; i < sliders.Count(); i++)
            {
                NoteData note = sliders[i];
                if (note.Precision - 0.001 > (note.Spacing + 1) * MinSliderPrecision)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Slider Precision",
                        Severity = Severity.Error,
                        CheckType = "Slider",
                        Description = "Sliders duration must be fast enough to keep consistent swing speed.",
                        ResultData = new() { new("CurrentSliderPrecision", note.Precision.ToString()), new("MinimumSliderPrecision", MinSliderPrecision.ToString()) },
                        BeatmapObjects = new() { note.Note }
                    });
                    issue = CritResult.Fail;
                }

                if (!(note.Precision <= ((note.Spacing + 1) * AverageSliderDuration) + 0.01 && note.Precision >= ((note.Spacing + 1) * AverageSliderDuration) - 0.01))
                {
                    // var reality = ScanMethod.RealToFraction(c.Precision, 0.01);
                    var expected = RealToFraction(((note.Spacing + 1) * AverageSliderDuration), 0.01);
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Slider Precision",
                        Severity = Severity.Warning,
                        CheckType = "Slider",
                        Description = "Sliders must have equal spacing between notes to keep consistent swing duration.",
                        ResultData = new() { new("ExpectedSliderPrecision", expected.N.ToString() + "/" + expected.D.ToString()) },
                        BeatmapObjects = new() { note.Note }
                    });
                    issue = CritResult.Warning;
                }
            }

            // Check for reversed slider direction (doesn't work if it's dot notes)
            sliders = NotesData.Where(c => c.Pattern && c.Precision != 0).ToList();
            var red = sliders.Where(c => c.Note.Color == 0).ToList();
            var blue = sliders.Where(c => c.Note.Color == 1).ToList();
            for (int i = 0; i < red.Count; i++)
            {
                var note = red[i];
                if(!note.Head && i != 0)
                {
                    var note2 = red[i - 1];
                    if(note.Note.CutDirection != 8 && note2.Note.CutDirection != 8)
                    {
                        var angleOfAttack = DirectionToDegree[note2.Note.CutDirection];
                        // Simulate the position of the line based on the new angle found
                        var simulatedLineOfAttack = SimSwingPos(note2.Line, note2.Layer, angleOfAttack, 2);
                        // Check if the other note is close to the line
                        var Mismatch = BeforePointOnFiniteLine(new((float)note2.Line, (float)note2.Layer), new((float)simulatedLineOfAttack.x, (float)simulatedLineOfAttack.y), new((float)note.Line, (float)note.Layer));
                        if (Mismatch)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Reversed Slider",
                                Severity = Severity.Error,
                                CheckType = "Slider",
                                Description = "Slider beat doesn't match the direction",
                                ResultData = new(),
                                BeatmapObjects = new() { note2.Note }
                            });
                        }
                    }
                }
            }
            for (int i = 0; i < blue.Count; i++)
            {
                var note = blue[i];
                if (!note.Head && i != 0)
                {
                    var note2 = blue[i - 1];
                    if (note.Note.CutDirection != 8 && note2.Note.CutDirection != 8)
                    {
                        var angleOfAttack = DirectionToDegree[note2.Note.CutDirection];
                        // Simulate the position of the line based on the new angle found
                        var simulatedLineOfAttack = SimSwingPos(note2.Line, note2.Layer, angleOfAttack, 2);
                        // Check if the other note is close to the line
                        var InPath = NearestPointOnFiniteLine(new((float)note2.Line, (float)note2.Layer), new((float)simulatedLineOfAttack.x, (float)simulatedLineOfAttack.y), new((float)note.Line, (float)note.Layer));
                        if (!InPath)
                        {
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Reversed Slider",
                                Severity = Severity.Info,
                                CheckType = "Slider",
                                Description = "Slider beat doesn't match the direction",
                                ResultData = new(),
                                BeatmapObjects = new() { note2.Note }
                            });
                        }
                    }
                }
            }

            red = NotesData.Where(c => c.Note.Color == 0 && c.Pattern).ToList();
            blue = NotesData.Where(c => c.Note.Color == 1 && c.Pattern).ToList();

            // TODO: This could probably be done way better but idk
            for (int i = 0; i < red.Count() - 1; i++)
            {
                List<double> dir = new();
                if (red[i].Head)
                {
                    if (red[i].Note.CutDirection != 8)
                    {
                        dir.Add(DirectionToDegree[red[i].Note.CutDirection]);
                    }
                    else
                    {
                        dir.Add(ReverseCutDirection(FindAngleViaPosition(red, i + 1, i)));
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
                        dir.Add(FindAngleViaPosition(red, i, i - 1));
                    } while (!red[i].Head);
                    i--;
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!IsSameDirection(degree, dir[j]) && !IsSameDirection(degree, ReverseCutDirection(dir[j])))
                        {
                            var n = red[i - dir.Count() + j + 1];
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Slider Rotation",
                                Severity = Severity.Error,
                                CheckType = "Slider",
                                Description = "Multiple notes of the same color on the same swing must not differ by more than 45°.",
                                ResultData = new() { new("Type", "Exceeded slider rotation limit of 45°") },
                                BeatmapObjects = new() { n.Note }
                            });
                            issue = CritResult.Fail;
                        }
                    }
                }
            }

            for (int i = 0; i < blue.Count() - 1; i++)
            {
                List<double> dir = new();

                if (blue[i].Head)
                {
                    if (blue[i].Note.CutDirection != 8)
                    {
                        dir.Add(DirectionToDegree[blue[i].Note.CutDirection]);
                    }
                    else
                    {
                        dir.Add(ReverseCutDirection(FindAngleViaPosition(blue, i + 1, i)));
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

                        dir.Add(FindAngleViaPosition(blue, i, i - 1));
                    } while (!blue[i].Head);
                    i--;
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!IsSameDirection(degree, dir[j]) && !IsSameDirection(degree, ReverseCutDirection(dir[j])))
                        {
                            var n = blue[i - dir.Count() + j + 1];
                            CheckResults.Instance.AddResult(new CheckResult()
                            {
                                Characteristic = CriteriaCheckManager.Characteristic,
                                Difficulty = CriteriaCheckManager.Difficulty,
                                Name = "Slider Rotation",
                                Severity = Severity.Error,
                                CheckType = "Slider",
                                Description = "Multiple notes of the same color on the same swing must not differ by more than 45°.",
                                ResultData = new() { new("Type", "Exceeded slider rotation limit of 45°") },
                                BeatmapObjects = new() { n.Note }
                            });
                            issue = CritResult.Fail;
                        }
                    }
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Slider",
                    Severity = Severity.Passed,
                    CheckType = "Slider",
                    Description = "No issue with slider precision and rotation detected.",
                    ResultData = new()
                });
            }

            return issue;
        }
    }
}

using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Slider
    {
        // Get the average sliders precision and warn if it's not applied to all sliders in the map.
        // Also check if sliders is above 45 degree (that could use some work)
        public static CritResult Check()
        {
            var issue = CritResult.Success;
            var note = NotesData;
            var timescale = CriteriaCheckManager.timescale;

            
            if (note.Count > 2)
            {
                NoteData head = note[0];
                var counter = 1;
                for (int i = 0; i < note.Count() - 1; i++)
                {
                    counter++;
                    var n = note[i];
                    // Calc duration
                    timescale.BPM.SetCurrentBPM(n.Note.Beats);
                    var MaxSliderDuration = timescale.BPM.ToBeatTime(0.0375f);
                    // Cap at 1/8 (200BPM)
                    if (MaxSliderDuration > 0.125) MaxSliderDuration = 0.125f;
                    // Slider found
                    if ((note[i + 1].Head || !note[i + 1].Pattern) && n.Precision != 0)
                    {
                        var duration = n.Note.BpmTime - head.Note.BpmTime;
                        if (n.Spacing != 0 || counter > 2)
                        {
                            if (duration > MaxSliderDuration)
                            {
                                CheckResults.Instance.AddResult(new CheckResult()
                                {
                                    Characteristic = CriteriaCheckManager.Characteristic,
                                    Difficulty = CriteriaCheckManager.Difficulty,
                                    Name = "Slider Precision",
                                    Severity = Severity.Error,
                                    CheckType = "Slider",
                                    Description = "Sliders duration must be fast enough to keep consistent swing speed.",
                                    ResultData = new() { new("SliderPrecision", "Minimum: " + MaxSliderDuration + " Current: " + duration) },
                                    BeatmapObjects = new() { head.Note }
                                });
                                issue = CritResult.Fail;
                            }
                        }
                        else
                        {
                            if (duration > MaxSliderDuration / 2)
                            {
                                CheckResults.Instance.AddResult(new CheckResult()
                                {
                                    Characteristic = CriteriaCheckManager.Characteristic,
                                    Difficulty = CriteriaCheckManager.Difficulty,
                                    Name = "Slider Precision",
                                    Severity = Severity.Error,
                                    CheckType = "Slider",
                                    Description = "Sliders duration must be fast enough to keep consistent swing speed.",
                                    ResultData = new() { new("SliderPrecision", "Minimum: " + MaxSliderDuration / 2 + " Current: " + duration) },
                                    BeatmapObjects = new() { head.Note }
                                });
                                issue = CritResult.Fail;
                            }
                        }
                    }

                    if (n.Head)
                    {
                        head = n;
                        counter = 1;
                    }
                }
            }

            var red = NotesData.Where(c => c.Note.Color == 0 && c.Pattern).ToList();
            var blue = NotesData.Where(c => c.Note.Color == 1 && c.Pattern).ToList();

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
                                ResultData = new() { new("SliderRotation", "Error") },
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
                                ResultData = new() { new("SliderRotation", "Error") },
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
                    ResultData = new() { new("Slider", "Success") }
                });
            }

            timescale.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

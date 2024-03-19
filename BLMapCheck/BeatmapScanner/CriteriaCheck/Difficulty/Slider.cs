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
                    if(issue == CritResult.Success) issue = CritResult.Warning;
                }
            }

            // Check for reversed slider direction (doesn't work if it's dot notes)
            sliders = NotesData.Where(c => c.Pattern && c.Precision != 0).ToList();
            if (ReversedSliderCheck(sliders.Where(c => c.Note.Color == 0).ToList())) issue = CritResult.Fail;
            if (ReversedSliderCheck(sliders.Where(c => c.Note.Color == 1).ToList())) issue = CritResult.Fail;

            // TODO: This could probably be done way better but idk
            if (SliderAngleCheck(NotesData.Where(c => c.Note.Color == 0 && c.Pattern).ToList())) issue = CritResult.Fail;
            if (SliderAngleCheck(NotesData.Where(c => c.Note.Color == 1 && c.Pattern).ToList())) issue = CritResult.Fail;
            
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

        public static bool ReversedSliderCheck(List<NoteData> notes)
        {
            bool issue = false;

            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (!note.Head && i != 0)
                {
                    var note2 = notes[i - 1];
                    if (note.Note.CutDirection == 8 && note2.Note.CutDirection == 8)
                    {
                        continue;
                    }
                    // Based on one of the note direction (if it's not a dot note)
                    double angleOfAttack;
                    if (note2.Note.CutDirection != 8) angleOfAttack = DirectionToDegree[note2.Note.CutDirection];
                    else angleOfAttack = DirectionToDegree[note.Note.CutDirection];
                    // Simulate the position of the line based on the new angle found
                    var simulatedLineOfAttack = SimSwingPos(note2.Line, note2.Layer, angleOfAttack, 2);
                    // Check if the other note is before
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
                        issue = true;
                    }
                }
            }

            return issue;
        }

        public static bool SliderAngleCheck(List<NoteData> notes)
        {
            bool issue = false;

            for (int i = 0; i < notes.Count() - 1; i++)
            {
                List<double> dir = new();
                if (notes[i].Head)
                {
                    if (notes[i].Note.CutDirection != 8)
                    {
                        dir.Add(DirectionToDegree[notes[i].Note.CutDirection]);
                    }
                    else
                    {
                        dir.Add(ReverseCutDirection(FindAngleViaPosition(notes, i + 1, i)));
                    }
                    do
                    {
                        i++;
                        if (notes.Count() == i)
                        {
                            break;
                        }
                        if (notes[i].Head || !notes[i].Pattern)
                        {
                            break;
                        }
                        dir.Add(FindAngleViaPosition(notes, i, i - 1));
                    } while (!notes[i].Head);
                    i--;
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!IsSameDirection(degree, dir[j]) && !IsSameDirection(degree, ReverseCutDirection(dir[j])))
                        {
                            var n = notes[i - dir.Count() + j + 1];
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
                            issue = true;
                        }
                    }
                }
            }

            return issue;
        }
    }
}

using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
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
            var sliders = NotesData.Where(c => c.Pattern && !c.Head && c.Precision != 0).ToList();

            if (Config.Instance.AutomaticSliderPrecision)
            {
               SetAutoSliderPrecision();
            }

            for (int i = 0; i < sliders.Count(); i++)
            {
                NoteData note = sliders[i];
                if (note.Precision - 0.01 > (note.Spacing + 1) * Config.Instance.SliderPrecision)
                {
                    var expected = RealToFraction(((note.Spacing + 1) * Config.Instance.SliderPrecision), 0.05);
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Slider Precision",
                        Severity = Severity.Error,
                        CheckType = "Slider",
                        Description = "Sliders duration must be fast enough to keep consistent swing speed.",
                        ResultData = new() { new("ExpectedSliderPrecision", expected.N.ToString() + "/" + expected.D.ToString()) },
                        BeatmapObjects = new() { note.Note }
                    });
                    issue = CritResult.Fail;
                    continue;
                }

                if (!(note.Precision <= ((note.Spacing + 1) * Config.Instance.SliderPrecision) + 0.01 && note.Precision >= ((note.Spacing + 1) * Config.Instance.SliderPrecision) - 0.01))
                {
                    var expected = RealToFraction(((note.Spacing + 1) * Config.Instance.SliderPrecision), 0.05);
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

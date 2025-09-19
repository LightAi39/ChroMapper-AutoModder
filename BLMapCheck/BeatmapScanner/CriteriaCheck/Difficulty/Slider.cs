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
        // TODO: Add an algo that detect sliders that require rotation above 45 degree
        public static CritResult Check()
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Slider Precision";
            string checkType = "Slider";
            CritResult criteria = CritResult.Success;

            // Fetch non-head slider notes from preprocessed data
            var sliders = NotesData.Where(c => c.Pattern && !c.Head && c.Precision != 0).ToList();

            // Get average from preprocessed data, or use manual value
            if (Config.Instance.AutomaticSliderPrecision)
            {
               SetAutoSliderPrecision();
            }

            for (int i = 0; i < sliders.Count(); i++)
            {
                NoteData note = sliders[i];

                // The minimum effective slider precision is 1/16th (or 1/8th for a 2-note window,
                // as the missing note should be counted for effective precision.) relative to the mapping precision in that section.
                if (note.Precision - 0.01 > (note.Spacing + 1) * 0.0625)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "Slider cannot be slower than 1/16", new(), new() { note.Note });
                    criteria = CritResult.Fail;
                }

                // Slider Swing speed must be consistent per section of the map. This may be overruled with sufficient justification.
                if (note.Precision - 0.01 > (note.Spacing + 1) * Config.Instance.SliderPrecision)
                {
                    var expected = RealToFraction((note.Spacing + 1) * Config.Instance.SliderPrecision, 0.05);

                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Warning, checkType, "Slider Swing speed must be consistent per section of the map", 
                        new() { new("ExpectedSliderPrecision", expected.N.ToString() + "/" + expected.D.ToString()) }, new() { note.Note });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;

                    continue;
                }

                // Slider Swing speed must be consistent per section of the map. This may be overruled with sufficient justification.
                if (!(note.Precision <= ((note.Spacing + 1) * Config.Instance.SliderPrecision) + 0.01 && note.Precision >= ((note.Spacing + 1) * Config.Instance.SliderPrecision) - 0.01))
                {
                    var expected = RealToFraction((note.Spacing + 1) * Config.Instance.SliderPrecision, 0.05);

                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Warning, checkType, "Slider Swing speed must be consistent per section of the map",
                        new() { new("ExpectedSliderPrecision", expected.N.ToString() + "/" + expected.D.ToString()) }, new() { note.Note });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;

                    continue;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Passed, checkType, "No issue with slider precision detected");
            }

            return criteria;
        }
    }
}

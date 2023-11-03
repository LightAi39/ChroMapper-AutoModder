using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using System.ComponentModel;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelSize
    {
        // TODO: Check what would be a good value for MaxChar based on the numbers of difficulties in that characteristic (Count).
        public static CritResult Check(string DifficultyLabel, int Count = 1)
        {
            if (DifficultyLabel.Count() > Instance.MaxChar / Count)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Difficulty Label Size",
                    Severity = Severity.Error,
                    CheckType = "Label",
                    Description = "The difficulty label is too long.",
                    ResultData = new() { new("LabelSize", "Current is " + DifficultyLabel.Count().ToString() + " characters. Maximum " + Instance.MaxChar.ToString() + " characters." )}
                });
                return CritResult.Fail;
            }
            return CritResult.Success;
        }
    }
}

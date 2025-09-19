using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelSize
    {
        public static CritResult Check(string difficultyLabel, int count = 1)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Difficulty Label Size";
            string checkType = "Label";
            CritResult criteria = CritResult.Success;

            var maxLine = 1;
            if (count == 2) maxLine = 2;
            if (count >= 3) maxLine = 3;
                
            // Each diff remove around 5 letters
            var maxValue = (Instance.MaxChar + (5 - count) * 5) * maxLine;

            if (difficultyLabel != null)
            {
                if (difficultyLabel.Count() > maxValue)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "The difficulty label is too long",
                        new() { new("CurrentSize", difficultyLabel.Count().ToString() + " characters"), new("MaxSize", maxValue + " characters") });
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Passed, checkType, "The difficulty label size is valid",
                        new() { new("CurrentSize", difficultyLabel.Count().ToString() + " characters"), new("MaxSize", maxValue + " characters") });
            }

            return criteria;
        }
    }
}

using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelSize
    {
        public static CritResult Check(string difficultyLabel, int difficultyCount)
        {
            CritResult criteria = CritResult.Success;

            // Max line allowed is based on numbers of difficulty
            var maxLine = 1;
            if (difficultyCount == 2) maxLine = 2;
            if (difficultyCount >= 3) maxLine = 3;
                
            // Each diff remove around 5 letters
            var maxValue = (Instance.MaxChar + (5 - difficultyCount) * 5) * maxLine;

            if (difficultyLabel != null)
            {
                if (difficultyLabel.Count() > maxValue)
                {
                    CheckResults.Instance.CreateDiffResult("Difficulty Label Size", Severity.Error, "Label", "The difficulty label is too long",
                        new() { new("CurrentSize", difficultyLabel.Count().ToString() + " characters"), new("MaxSize", maxValue + " characters") });
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateDiffResult("Difficulty Label Size", Severity.Passed, "Label", "The difficulty label size is valid",
                        new() { new("CurrentSize", difficultyLabel?.Count().ToString() ?? "Default"), new("MaxSize", maxValue + " characters") });
            }

            return criteria;
        }
    }
}

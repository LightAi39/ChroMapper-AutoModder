using BLMapCheck.Classes.Results;
using System.Collections;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelSize
    {
        public static CritResult Check(string difficultyLabel, int count = 1)
        {
            var maxLine = 1;
            if (count == 2) maxLine = 2;
            if (count >= 3) maxLine = 3;
                
            // Each diff remove around 5 letters
            var maxValue = (Instance.MaxChar + (5 - count) * 5) * maxLine;

            if (difficultyLabel != null)
            {
                if (difficultyLabel.Count() > maxValue)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Difficulty Label Size",
                        Severity = Severity.Error,
                        CheckType = "Label",
                        Description = "The difficulty label is too long.",
                        ResultData = new() { new("CurrentSize", difficultyLabel.Count().ToString() + " characters"), new("MaxSize", maxValue + " characters") }
                    });
                    return CritResult.Fail;
                }

                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Difficulty Label Size",
                    Severity = Severity.Passed,
                    CheckType = "Label",
                    Description = "The difficulty label size is valid.",
                    ResultData = new() { new("CurrentSize", difficultyLabel.Count().ToString() + " characters"), new("MaxSize", maxValue + " characters") },
                });

                return CritResult.Success;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Characteristic = CriteriaCheckManager.Characteristic,
                Difficulty = CriteriaCheckManager.Difficulty,
                Name = "Difficulty Label Size",
                Severity = Severity.Passed,
                CheckType = "Label",
                Description = "The difficulty label size is valid.",
                ResultData = new() { new("CurrentSize", difficultyLabel?.Count().ToString() ?? "Default"), new("MaxSize", maxValue + " characters") },
            });

            return CritResult.Success;
        }
    }
}

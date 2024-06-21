using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelSize
    {
        // TODO: Check what would be a good value for MaxChar based on the numbers of difficulties in that characteristic (Count).
        public static CritResult Check(string difficultyLabel, int count = 1)
        {
            var maxValue = Instance.MaxChar * (6 - count);

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

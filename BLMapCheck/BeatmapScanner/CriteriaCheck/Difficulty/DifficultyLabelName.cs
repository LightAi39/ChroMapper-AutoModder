using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelName
    {
        // Compare current label name with a list of offensive words.
        public static CritResult Check(string difficultyLabel)
        {
            if(difficultyLabel != null)
            {
                ProfanityFilter.ProfanityFilter pf = new();
                var isProfanity = pf.ContainsProfanity(difficultyLabel);
                if (isProfanity)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Difficulty Label Name",
                        Severity = Severity.Error,
                        CheckType = "Label",
                        Description = "The label name cannot contain obscene content.",
                        ResultData = new()
                    });
                    return CritResult.Fail;
                }

                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Difficulty Label Name",
                    Severity = Severity.Passed,
                    CheckType = "Label",
                    Description = "The label name cannot contain obscene content.",
                    ResultData = new()
                });

                return CritResult.Success;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Characteristic = CriteriaCheckManager.Characteristic,
                Difficulty = CriteriaCheckManager.Difficulty,
                Name = "Difficulty Label Name",
                Severity = Severity.Passed,
                CheckType = "Label",
                Description = "The label name cannot contain obscene content.",
                ResultData = new()
            });

            return CritResult.Success;
        }
    }
}

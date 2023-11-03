using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelName
    {
        // Compare current label name with a list of offensive words.
        public static CritResult Check(string DifficultyLabel)
        {
            ProfanityFilter.ProfanityFilter pf = new();
            var isProfanity = pf.ContainsProfanity(DifficultyLabel);
            if (isProfanity)
            {
                //ExtendOverallComment("R7G - Difficulty name must not contain obscene content"); TODO: USE NEW METHOD
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Difficulty Label Name",
                    Severity = Severity.Error,
                    CheckType = "Label",
                    Description = "The label name cannot contain obscene content.",
                    ResultData = new() { new("Profanity", "True") }
                });
                return CritResult.Fail;
            }

            return CritResult.Success;
        }
    }
}

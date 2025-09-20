using BLMapCheck.Classes.Results;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelName
    {
        public static readonly ProfanityFilter.ProfanityFilter Profanity = new();

        // Compare current label name with a list of offensive words.
        public static CritResult Check(string characteristic, string difficulty, string difficultyLabel)
        {
            CritResult criteria = CritResult.Success;

            // Custom label
            if (difficultyLabel != null)
            {
                if (Profanity.ContainsProfanity(difficultyLabel))
                {
                    CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Difficulty Label Name", Severity.Error, "Label", "The label name cannot contain obscene content");
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateDiffResult(characteristic, difficulty,
                        "Difficulty Label Name", Severity.Passed, "Label", "The label name does not contain any obscene content");
            }

            return criteria;
        }
    }
}

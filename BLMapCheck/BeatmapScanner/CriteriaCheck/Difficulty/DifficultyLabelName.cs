using BLMapCheck.Classes.Results;
using System.Linq;
using System.Xml.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelName
    {
        // Compare current label name with a list of offensive words.
        public static CritResult Check(string difficultyLabel)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Difficulty Label Name";
            string checkType = "Label";
            string description = "The label name cannot contain obscene content";
            CritResult criteria = CritResult.Success;

            // Custom label
            if (difficultyLabel != null)
            {
                ProfanityFilter.ProfanityFilter pf = new();
                var isProfanity = pf.ContainsProfanity(difficultyLabel);
                if (isProfanity)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, description);
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Passed, checkType, description);
            }

            return criteria;
        }
    }
}

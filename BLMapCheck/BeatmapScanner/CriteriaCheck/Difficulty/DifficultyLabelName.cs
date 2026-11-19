using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelName
    {
        public static readonly ProfanityFilter.ProfanityFilter Profanity = new();

        // Compare current label name with a list of offensive words.
        public static CritResult Check(CheckContext ctx, string difficultyLabel)
        {
            CritResult criteria = CritResult.Success;

            // Custom label
            if (difficultyLabel != null)
            {
                var found = Profanity.FindProfanities(difficultyLabel);
                if (found.Count > 0)
                {
                    // The comment only shows the description, so it says what was detected
                    var words = string.Join(", ", found.Select(w => "\"" + w + "\""));
                    ctx.Results.CreateDiffResult("Difficulty Label Name", Severity.Warning, "Label", $"The label name cannot contain obscene content. Detected {words} in \"{difficultyLabel}\"",
                        new() { new("Label", difficultyLabel), new("Detected", string.Join(", ", found)) });
                    // The filter also matches inside words ("Classic"), so it needs a human to confirm
                    criteria = CritResult.Warning;
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Difficulty Label Name", Severity.Passed, "Label", "The label name does not contain any obscene content");
            }

            return criteria;
        }
    }
}

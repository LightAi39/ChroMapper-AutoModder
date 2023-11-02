using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelSize
    {
        // TODO: Check what would be a good value for MaxChar based on the numbers of difficulties in that characteristic (Count).
        public static Severity Check(string DifficultyLabel, int Count = 1)
        {
            if (DifficultyLabel.Count() > MaxChar / Count)
            {
                // ExtendOverallComment("R7E - " + diff.BeatmapFilename + " difficulty label is too long. Current is " + diff.CustomData["_difficultyLabel"].ToString().Count() + " characters. Maximum " + Plugin.configs.MaxChar.ToString() + " characters.");
                // TODO: USE NEW METHOD
                return Severity.Fail;
            }
            return Severity.Success;
        }
    }
}

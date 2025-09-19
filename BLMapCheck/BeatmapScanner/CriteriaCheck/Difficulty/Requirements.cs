using BLMapCheck.Classes.Results;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Requirements
    {
        public static CritResult Check(List<string> requirements)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Requirements";
            CritResult criteria = CritResult.Success;

            if (requirements != null && requirements.Any())
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, name, "Any map that is dependent on other mods or programs is not allowed", 
                    new() { new("Requirements", "Has " + string.Join(",", requirements.ToArray())) });
                criteria = CritResult.Fail;
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Passed, name, "Map doesn't have any mod requirement",
                    new() { new("Requirements", "None") });
            }

            return criteria;
        }
    }
}

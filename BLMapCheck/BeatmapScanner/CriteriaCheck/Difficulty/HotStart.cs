using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class HotStart
    {
        // Detect objects that are too early in the map
        public static CritResult Check(List<BeatmapGridObject> objects, List<Wall> walls)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Hot Start";
            string checkType = "Duration";
            CritResult criteria = CritResult.Success;
            var timescale = CriteriaCheckManager.timescale;

            var limit = timescale.BPM.ToBeatTime((float)Instance.HotStartDuration, true);

            foreach (var c in objects)
            {
                if (c.Beats < limit)
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "There must be at least " + Instance.HotStartDuration.ToString() + " seconds of time before any interactable objects",
                        new() { new("CurrentBeat", c.Beats.ToString()), new("MinimumBeat", limit.ToString()) }, new() { c });
                    criteria = CritResult.Fail;
                }
                else break;
            }

            // Need to take into consideration wall placement
            foreach (var w in walls)
            {
                if (w.Beats < limit && ((w.x + w.Width >= 2 && w.x < 2) || w.x == 1 || w.x == 2))
                {
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Error, checkType, "There must be at least " + Instance.HotStartDuration.ToString() + " seconds of time before any interactable objects",
                        new() { new("CurrentBeat", w.Beats.ToString()), new("MinimumBeat", limit.ToString()) }, new() { w });
                    criteria = CritResult.Fail;
                }
                else break;
            }

            if(criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        name, Severity.Passed, checkType, "There is at least " + Instance.HotStartDuration.ToString() + " seconds of time before any interactable objects");
            }

            return criteria;
        }
    }
}

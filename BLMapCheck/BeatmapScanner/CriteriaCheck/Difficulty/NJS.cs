using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class NJS
    {
        // NJS and VNJS criteria
        public static CritResult Check(float njs, float noteJumpStartBeatOffset, List<BeatmapGridObject> allObjects)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Note Jump Speed";
            string checkType = "NoteJumpSpeed";
            CritResult criteria = CritResult.Success;

            // The base NJS need to be a positive number
            if (njs <= 0)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "Note Jump Speed cannot be lower or equal to 0.", 
                    new() { new("CurrentNoteJumpSpeed", njs.ToString()), new("MinimumNoteJumpSpeed", "0") });
                criteria = CritResult.Fail;
            }

            // NJS must not reach below 1 at any point in the map
            List<BeatmapObject> objects = new();
            foreach (var obj in allObjects)
            {
                if (obj.njs < 1)
                {
                    objects.Add(obj);
                }
            }
            if (objects.Count > 0)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "NJS must not reach below 1 at any point in the map", new(), objects);
                criteria = CritResult.Fail;
            }

            // Values below 4 NJS may require justification
            objects = new();
            foreach (var obj in allObjects)
            {
                if (obj.njs < 4)
                {
                    objects.Add(obj);
                }
            }
            if (objects.Count > 0)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Warning, checkType, "Values below 4 NJS may require justification", new(), objects);
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            // The base NJS value must be between the minimum and maximum NJS used in interactable portions of the map
            List<float> allNjs = allObjects.Select(obj => obj.njs).ToList();
            if (njs < allNjs.Min() || njs > allNjs.Max())
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Error, checkType, "The base NJS value must be between the minimum and maximum NJS used in interactable portions of the map");
                criteria = CritResult.Fail;
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    name, Severity.Info, checkType, "No issue with NJS detected.");
            }

            return criteria;
        }
    }
}

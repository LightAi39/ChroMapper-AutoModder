using BLMapCheck.Classes.Results;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Configs.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Parity
    {
        // JoshaParity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public static CritResult Check(List<SwingData> swings, List<Parser.Map.Difficulty.V3.Grid.Note> notes)
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string checkType = "NoteJumpSpeed";
            CritResult criteria = CritResult.Success;

            foreach (var swing in swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    "Parity", Severity.Error, checkType, "Parity error", new() { new("ErrorType", "Reset") }, new(swing.notes) { });
                criteria = CritResult.Fail;
            }

            foreach (var swing in swings.Where(x => x.swingEBPM == float.PositiveInfinity).ToList())
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                    "Parity Mismatch", Severity.Error, checkType, "NJS must not reach below 1 at any point in the map", 
                    new() { new("ErrorType", "swingEBPM is equal PositiveInfinity") }, new(swing.notes) { });
                criteria = CritResult.Fail;
            }

            List<SwingData> rightHandSwings = swings.Where(x => x.rightHand).ToList();
            List<SwingData> leftHandSwings = swings.Where(x => !x.rightHand).ToList();

            for (int i = 0; i < rightHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    if (i == rightHandSwings.Count - 1 && rightHandSwings[i].notes.Last().CutDirection == 8) break;
                    float difference = rightHandSwings[i].startPos.rotation - rightHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Instance.ParityWarningAngle)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Parity Warning", Severity.Warning, checkType, "Parity degree difference", 
                            new() { new("WarningType", Math.Abs(difference) + " degree difference") }, new(rightHandSwings[i].notes) { });
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                    else if (Math.Abs(rightHandSwings[i].startPos.rotation) > 135 || Math.Abs(rightHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Instance.ParityInvertedWarning)
                        {
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Parity Inverted", Severity.Warning, checkType, "Parity playing inverted",
                                new() { new("WarningType", "Playing inverted") }, new(rightHandSwings[i].notes) { });
                        }
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                }
            }

            for (int i = 0; i < leftHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    if (i == leftHandSwings.Count - 1 && leftHandSwings[i].notes.Last().CutDirection == 8) break;
                    float difference = leftHandSwings[i].startPos.rotation - leftHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Instance.ParityWarningAngle)
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Parity Warning", Severity.Warning, checkType, "Parity degree difference",
                            new() { new("WarningType", Math.Abs(difference) + " degree difference") }, new(leftHandSwings[i].notes) { });
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                    else if (Math.Abs(leftHandSwings[i].startPos.rotation) > 135 || Math.Abs(leftHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Instance.ParityInvertedWarning)
                        {
                            CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            "Parity Inverted", Severity.Warning, checkType, "Parity playing inverted",
                                new() { new("WarningType", "Playing inverted") }, new(leftHandSwings[i].notes) { });
                        }
                        if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                    }
                }
            }

            if (Instance.ParityDebug)
            {
                foreach (var swing in swings)
                {
                    var swingWithoutNotes = swing;
                    Severity commentType = Severity.Info;
                    if (swing.resetType == ResetType.Rebound) commentType = Severity.Error;
                    if (Math.Abs(swing.endPos.rotation) > 135 || Math.Abs(swing.endPos.rotation) > 135) commentType = Severity.Inconclusive;

                    List<Classes.Results.KeyValuePair> resultData = new()
                    {
                        new("swingParity", swing.swingParity.ToString()),
                        new("resetType", swing.resetType.ToString()),
                        new("swingStartBeat", swing.swingStartBeat.ToString()),
                        new("swingEndBeat", swing.swingEndBeat.ToString()),
                        new("swingEBPM", swing.swingEBPM.ToString()),
                        new("notes", JsonConvert.SerializeObject(swing.notes, Formatting.Indented)),
                        new("startPos", JsonConvert.SerializeObject(swing.startPos, Formatting.Indented)),
                        new("endPos", JsonConvert.SerializeObject(swing.endPos, Formatting.Indented)),
                        new("rightHand", swing.rightHand.ToString())
                    };
                    
                    CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Parity Debug", commentType, checkType, "Parity Debug", resultData, new(swing.notes) { });
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                        "Parity", Severity.Passed, checkType, "No possible parity issue detected");
            }

            return criteria;
        }
    }
}

using JoshaParity;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Parity
    {
        // JoshaParity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public static CritResult Check(List<SwingData> Swings)
        {
            bool hadIssue = false;
            bool hadWarning = false;

            foreach (var swing in Swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                //CreateDiffCommentNotes("R2 - Parity Error", CommentTypesEnum.Issue, swing.notes); TODO: USE NEW METHOD
                hadIssue = true;
            }
            foreach (var swing in Swings.Where(x => x.swingEBPM == float.PositiveInfinity).ToList())
            {
                //CreateDiffCommentNotes("R2 - Parity Mismatch on same beat", CommentTypesEnum.Issue, swing.notes); TODO: USE NEW METHOD
                hadIssue = true;
            }

            List<SwingData> rightHandSwings = Swings.Where(x => x.rightHand).ToList();
            List<SwingData> leftHandSwings = Swings.Where(x => !x.rightHand).ToList();

            for (int i = 0; i < rightHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = rightHandSwings[i].startPos.rotation - rightHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= ParityWarningAngle)
                    {
                        //CreateDiffCommentNotes("Parity Warning - " + config.ParityWarningAngle + " degree difference", CommentTypesEnum.Unsure, rightHandSwings[i].notes); TODO: USE NEW METHOD
                        hadWarning = true;
                    }
                    else if (Math.Abs(rightHandSwings[i].startPos.rotation) > 135 || Math.Abs(rightHandSwings[i].endPos.rotation) > 135)
                    {
                        if (ParityInvertedWarning)
                        {
                            //CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, rightHandSwings[i].notes); TODO: USE NEW METHOD
                        }
                        hadWarning = true;
                    }
                }
            }

            for (int i = 0; i < leftHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = leftHandSwings[i].startPos.rotation - leftHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= ParityWarningAngle)
                    {
                        //CreateDiffCommentNotes("Parity Warning - " + config.ParityWarningAngle + " degree difference", CommentTypesEnum.Unsure, leftHandSwings[i].notes); TODO: USE NEW METHOD
                        hadWarning = true;
                    }
                    else if (Math.Abs(leftHandSwings[i].startPos.rotation) > 135 || Math.Abs(leftHandSwings[i].endPos.rotation) > 135)
                    {
                        if (ParityInvertedWarning)
                        {
                            //CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, leftHandSwings[i].notes); TODO: USE NEW METHOD
                        }
                        hadWarning = true;
                    }
                }
            }

            if (ParityDebug)
            {
                foreach (var swing in Swings)
                {
                    // TODO: USE NEW METHOD
                    /*
                    var swingWithoutNotes = swing;
                    swingWithoutNotes.notes = null;
                    string message = JsonConvert.SerializeObject(swingWithoutNotes);
                    CommentTypesEnum commentType = CommentTypesEnum.Info;
                    if (swing.resetType == ResetType.Rebound) commentType = CommentTypesEnum.Issue;
                    if (Math.Abs(swing.endPos.rotation) > 135 || Math.Abs(swing.endPos.rotation) > 135) commentType = CommentTypesEnum.Unsure;
                    CreateDiffCommentNotes(message, commentType, swing.notes);
                    */
                }
            }

            if (hadIssue)
            {
                return CritResult.Fail;
            }
            else if (hadWarning)
            {
                return CritResult.Warning;
            }
            else
            {
                return CritResult.Success;
            }
        }
    }
}

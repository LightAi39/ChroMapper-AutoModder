using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;
using static System.Net.Mime.MediaTypeNames;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Loloppe
    { 
        // Detect parallel notes
        public static CritResult Check(List<Note> notes)
        {
            var issue = CritResult.Success;

            if (DetectParallel(notes.Where(c => c.Color == 0).ToList())) issue = CritResult.Fail;
            if (DetectParallel(notes.Where(c => c.Color == 1).ToList())) issue = CritResult.Fail;

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Loloppe",
                    Severity = Severity.Passed,
                    CheckType = "Loloppe",
                    Description = "No parallel notes of the same color on the same swing detected.",
                    ResultData = new()
                });
            }

            return issue;
        }

        public static bool DetectParallel(List<Note> notes)
        {
            var issue = true;

            for (int i = 1; i < notes.Count; i++)
            {
                if (notes[i].CutDirection == 8 || notes[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (notes[i].Beats - notes[i - 1].Beats < 0.125)
                {
                    // Detect slanted window
                    /* Check if both notes have the same cutDirection.
                    If so, it could be a slanted window.
                    Get the line made by the two notes.
                    Slanted window happens if the angle change for notes to align is less than than 45. 
                    */
                    var direction = DirectionToDegree[notes[i].CutDirection];
                    if (notes[i].CutDirection == notes[i - 1].CutDirection)
                    {
                        var lineAngle = Mod(ConvertRadiansToDegrees(Math.Atan2(notes[i].y - notes[i - 1].y, notes[i].x - notes[i - 1].x)), 360);
                        var angle = Math.Abs(lineAngle - direction);
                        if (Math.Abs((angle + 180) % 360 - 180) < 45)
                        {
                            continue;
                        }
                    }
                    var sliderAngle = Mod(ConvertRadiansToDegrees(Math.Atan2(notes[i].y - notes[i - 1].y, notes[i].x - notes[i - 1].x)), 360);
                    if (Math.Abs(sliderAngle - direction) >= 90)
                    {
                        (notes[i - 1], notes[i]) = (notes[i], notes[i - 1]);
                    }
                    var sliderAngle2 = Mod(ConvertRadiansToDegrees(Math.Atan2(notes[i].y - notes[i - 1].y, notes[i].x - notes[i - 1].x)), 360);
                    if (Math.Abs(sliderAngle2 - direction) >= 45)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Loloppe",
                            Severity = Severity.Error,
                            CheckType = "Loloppe",
                            Description = "Multiple notes of the same color on the same swing must flow.",
                            ResultData = new() { new("Type", "Loloppe Note") },
                            BeatmapObjects = new() { notes[i], notes[i - 1] }
                        });
                        issue = true;
                    }
                }
            }

            return issue;
        }
    }
}

using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Loloppe
    { 
        // Detect parallel notes
        public static CritResult Check(List<Colornote> notes)
        {
            var issue = CritResult.Success;
            var red = notes.Where(c => c.c == 0).ToList();
            var blue = notes.Where(c => c.c == 1).ToList();
            for (int i = 1; i < red.Count; i++)
            {
                if (red[i].d == 8 || red[i - 1].d == 8)
                {
                    continue;
                }
                if (red[i].b - red[i - 1].b < 0.125)
                {
                    var direction = DirectionToDegree[red[i].d];
                    var sliderAngle = Mod(ConvertRadiansToDegrees(Math.Atan2(red[i].y - red[i - 1].y, red[i].x - red[i - 1].x)), 360);
                    if (Math.Abs(sliderAngle - direction) >= 90)
                    {
                        (red[i - 1], red[i]) = (red[i], red[i - 1]);
                    }
                    var sliderAngle2 = Mod(ConvertRadiansToDegrees(Math.Atan2(red[i].y - red[i - 1].y, red[i].x - red[i - 1].x)), 360);
                    if (Math.Abs(sliderAngle2 - direction) >= 45 && Math.Abs(sliderAngle2 - direction) <= 90)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Loloppe",
                            Severity = Severity.Error,
                            CheckType = "Loloppe",
                            Description = "Multiple notes of the same color on the same swing must not be parallel.",
                            ResultData = new() { new("Loloppe", "Error") },
                            BeatmapObjects = new() { red[i], red[i - 1] }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }
            for (int i = 1; i < blue.Count; i++)
            {
                if (blue[i].d == 8 || blue[i - 1].d == 8)
                {
                    continue;
                }
                if (blue[i].b - blue[i - 1].b < 0.125)
                {
                    var direction = DirectionToDegree[blue[i].d];
                    var sliderAngle = Mod(ConvertRadiansToDegrees(Math.Atan2(blue[i].y - blue[i - 1].y, blue[i].x - blue[i - 1].x)), 360);
                    if (Math.Abs(sliderAngle - direction) >= 90)
                    {
                        (blue[i - 1], blue[i]) = (blue[i], blue[i - 1]);
                    }
                    var sliderAngle2 = Mod(ConvertRadiansToDegrees(Math.Atan2(blue[i].y - blue[i - 1].y, blue[i].x - blue[i - 1].x)), 360);
                    if (Math.Abs(sliderAngle2 - direction) >= 45 && Math.Abs(sliderAngle2 - direction) <= 90)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Loloppe",
                            Severity = Severity.Error,
                            CheckType = "Loloppe",
                            Description = "Multiple notes of the same color on the same swing must not be parallel.",
                            ResultData = new() { new("Loloppe", "Error") },
                            BeatmapObjects = new() { blue[i], blue[i - 1] }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }

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
                    ResultData = new() { new("Loloppe", "Success") }
                });
            }

            return issue;
        }
    }
}

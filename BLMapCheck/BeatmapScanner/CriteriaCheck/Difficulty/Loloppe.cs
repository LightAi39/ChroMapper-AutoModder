using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Loloppe
    { 
        // Detect parallel notes
        public static CritResult Check(List<Colornote> Notes)
        {
            var issue = CritResult.Success;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var red = cubes.Where(c => c.Type == 0).ToList();
            var blue = cubes.Where(c => c.Type == 1).ToList();
            for (int i = 1; i < red.Count; i++)
            {
                if (red[i].CutDirection == 8 || red[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (red[i].Time - red[i - 1].Time < 0.125)
                {
                    var sliderAngle = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle - red[i].Direction) >= 90)
                    {
                        (red[i - 1], red[i]) = (red[i], red[i - 1]);
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - red[i].Direction) >= 45 && Math.Abs(sliderAngle2 - red[i].Direction) <= 90)
                    {
                        var note = Notes.Where(n => n.b == red[i - 1].Time && n.d == red[i - 1].Direction && n.x == red[i - 1].Line && n.y == red[i - 1].Layer && n.c == red[i - 1].Type).FirstOrDefault();
                        var note2 = Notes.Where(n => n.b == red[i].Time && n.d == red[i].Direction && n.x == red[i].Line && n.y == red[i].Layer && n.c == red[i].Type).FirstOrDefault();
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Loloppe",
                            Severity = Severity.Error,
                            CheckType = "Loloppe",
                            Description = "Multiple notes of the same color on the same swing must not be parallel.",
                            ResultData = new() { new("Loloppe", "Error") },
                            BeatmapObjects = new() { note, note2 }
                        });
                        issue = CritResult.Fail;
                    }
                }
            }
            for (int i = 1; i < blue.Count; i++)
            {
                if (blue[i].CutDirection == 8 || blue[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (blue[i].Time - blue[i - 1].Time < 0.125)
                {
                    var sliderAngle = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle - blue[i].Direction) >= 90)
                    {
                        (blue[i - 1], blue[i]) = (blue[i], blue[i - 1]);
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - blue[i].Direction) >= 45 && Math.Abs(sliderAngle2 - blue[i].Direction) <= 90)
                    {
                        var note = Notes.Where(n => n.b == blue[i - 1].Time && n.d == blue[i - 1].Direction && n.x == blue[i - 1].Line && n.y == blue[i - 1].Layer && n.c == blue[i - 1].Type).FirstOrDefault();
                        var note2 = Notes.Where(n => n.b == blue[i].Time && n.d == blue[i].Direction && n.x == blue[i].Line && n.y == blue[i].Layer && n.c == blue[i].Type).FirstOrDefault();
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "Loloppe",
                            Severity = Severity.Error,
                            CheckType = "Loloppe",
                            Description = "Multiple notes of the same color on the same swing must not be parallel.",
                            ResultData = new() { new("Loloppe", "Error") },
                            BeatmapObjects = new() { note, note2 }
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

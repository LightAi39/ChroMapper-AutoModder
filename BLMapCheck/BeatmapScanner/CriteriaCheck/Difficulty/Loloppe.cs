using System;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Loloppe
    { 
        // Detect parallel notes
        public static CritSeverity Check()
        {
            var issue = CritSeverity.Success;
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
                        var temp = red[i];
                        red[i] = red[i - 1];
                        red[i - 1] = temp;
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - red[i].Direction) >= 45 && Math.Abs(sliderAngle2 - red[i].Direction) <= 90)
                    {
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, red[i - 1]); TODO: USE NEW METHOD 
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, red[i]); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
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
                        var temp = blue[i];
                        blue[i] = blue[i - 1];
                        blue[i - 1] = temp;
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - blue[i].Direction) >= 45 && Math.Abs(sliderAngle2 - blue[i].Direction) <= 90)
                    {
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, blue[i - 1]); TODO: USE NEW METHOD 
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, blue[i]); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
            }

            return issue;
        }
    }
}

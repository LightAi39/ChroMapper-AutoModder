using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Slider
    {
        public static float averageSliderDuration { get; set; } = 0.0625f;

        // Get the average sliders precision and warn if it's not applied to all sliders in the map.
        // Also check if sliders is above 45 degree (that could use some work)
        public static CritResult Check()
        {
            var issue = CritResult.Success;
            var cube = BeatmapScanner.Cubes.Where(c => c.Slider && !c.Head);
            cube = cube.OrderBy(c => c.Time).ToList();

            averageSliderDuration = (float)ScanMethod.Mode(cube.Select(c => c.Precision / (c.Spacing + 1))).FirstOrDefault();
            if (averageSliderDuration == 0) averageSliderDuration = 0.0625f;

            foreach (var c in cube)
            {
                if (c.Slider && !c.Head)
                {
                    if (!(c.Precision <= ((c.Spacing + 1) * averageSliderDuration) + 0.01 && c.Precision >= ((c.Spacing + 1) * averageSliderDuration) - 0.01))
                    {
                        // var reality = ScanMethod.RealToFraction(c.Precision, 0.01);
                        var expected = ScanMethod.RealToFraction(((c.Spacing + 1) * averageSliderDuration), 0.01);
                        //CreateDiffCommentNote("R2A - Expected " + expected.N.ToString() + "/" + expected.D.ToString(), CommentTypesEnum.Unsure, c); TODO: USE NEW METHOD
                        issue = CritResult.Warning;
                    }
                }
            }

            cube = BeatmapScanner.Cubes.Where(c => c.Slider);
            cube = cube.OrderBy(c => c.Time).ToList();
            var red = cube.Where(c => c.Type == 0).ToList();
            var blue = cube.Where(c => c.Type == 1).ToList();

            // TODO: This could probably be done way better but idk
            for (int i = 0; i < red.Count(); i++)
            {
                List<double> dir = new();
                if (red[i].Head)
                {
                    if (red[i].CutDirection != 8)
                    {
                        dir.Add(red[i].Direction);
                    }
                    else
                    {
                        dir.Add(ScanMethod.FindAngleViaPosition(red, i + 1, i));
                    }

                    do
                    {
                        i++;
                        if (red.Count() == i)
                        {
                            break;
                        }
                        if (red[i].Head || !red[i].Pattern)
                        {
                            break;
                        }

                        dir.Add(ScanMethod.FindAngleViaPosition(red, i, i - 1));
                    } while (!red[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(degree, dir[j], 45))
                        {
                            //CreateDiffCommentNote("R3F - Slider over 45°", CommentTypesEnum.Issue, red[i - dir.Count() + j]); TODO: USE NEW METHOD
                            issue = CritResult.Fail;
                        }
                    }

                    i--;
                }
            }

            for (int i = 0; i < blue.Count(); i++)
            {
                List<double> dir = new();

                if (blue[i].Head)
                {
                    if (blue[i].CutDirection != 8)
                    {
                        dir.Add(blue[i].Direction);
                    }
                    else
                    {
                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i + 1, i));
                    }

                    do
                    {
                        i++;
                        if (blue.Count() == i)
                        {
                            break;
                        }
                        if (blue[i].Head || !blue[i].Pattern)
                        {
                            break;
                        }

                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i, i - 1));
                    } while (!blue[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(degree, dir[j], 45))
                        {
                            //CreateDiffCommentNote("R3F - Slider over 45°", CommentTypesEnum.Issue, blue[i - dir.Count() + j]); TODO: USE NEW METHOD
                            issue = CritResult.Fail;
                        }
                    }

                    i--;
                }
            }

            return issue;
        }
    }
}

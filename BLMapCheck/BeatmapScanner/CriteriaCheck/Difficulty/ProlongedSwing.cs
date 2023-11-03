using BLMapCheck.BeatmapScanner.Data;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class ProlongedSwing
    {
        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public static CritResult Check()
        {
            if(Slider.averageSliderDuration == -1)
            {
                Slider.Check();
            }

            var issue = false;
            var unsure = false;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.b).ToList();
            var slider = false;
            if (cubes.Exists(x => x.Slider))
            {
                slider = true;
            }
            foreach (var ch in chains)
            {
                if (ch.tb - ch.b >= Slider.averageSliderDuration * 4.2)
                {
                    if (slider)
                    {
                        //CreateDiffCommentLink("R2D - Duration is too high", CommentTypesEnum.Issue, ch); TODO: USE NEW METHOD
                        issue = true;
                    }
                    else if (ch.tb - ch.b > 0.125)
                    {
                        //CreateDiffCommentLink("R2D - Duration might be too high", CommentTypesEnum.Unsure, ch); TODO: USE NEW METHOD
                        unsure = true;
                    }
                }
                else if (ch.tb - ch.b >= Slider.averageSliderDuration * 3.15)
                {
                    if (slider)
                    {
                        //CreateDiffCommentLink("Y2A - Recommend shorter chain", CommentTypesEnum.Suggestion, ch); TODO: USE NEW METHOD
                        unsure = true;
                    }
                    else if (ch.tb - ch.b > 0.125)
                    {
                        //CreateDiffCommentLink("Y2A - Duration might be too high", CommentTypesEnum.Unsure, ch); TODO: USE NEW METHOD
                        unsure = true;
                    }
                }
                if (!cubes.Exists(c => c.Time == ch.b && c.Type == ch.c && c.Line == ch.x && c.Layer == ch.y))
                {
                    // Link spam maybe idk
                    //CreateDiffCommentLink("R2D - No head note", CommentTypesEnum.Issue, ch); TODO: USE NEW METHOD
                    issue = true;
                }
            }
            // Dot spam and pauls maybe
            var leftCube = cubes.Where(d => d.Type == 0).ToList();
            var rightCube = cubes.Where(d => d.Type == 1).ToList();
            Cube previous = null;
            foreach (var left in leftCube)
            {
                if (previous != null)
                {
                    if (((left.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(left.Direction, previous.Direction, 67.5)) || (left.Time - previous.Time <= 0.142857)) && left.Time != previous.Time && left.Line == previous.Line && left.Layer == previous.Layer)
                    {
                        if (left.CutDirection == 8)
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Unsure, left); TODO: USE NEW METHOD
                            unsure = true;
                        }
                        else
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Issue, left); TODO: USE NEW METHOD
                            issue = true;
                        }
                    }
                }

                previous = left;
            }

            previous = null;
            foreach (var right in rightCube)
            {
                if (previous != null)
                {
                    if (((right.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(right.Direction, previous.Direction, 67.5)) || (right.Time - previous.Time <= 0.142857)) && right.Time != previous.Time && right.Line == previous.Line && right.Layer == previous.Layer)
                    {
                        if (right.CutDirection == 8)
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Unsure, right); TODO: USE NEW METHOD
                            unsure = true;
                        }
                        else
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Issue, right); TODO: USE NEW METHOD
                            issue = true;
                        }
                    }
                }

                previous = right;
            }

            if (issue)
            {
                return CritResult.Fail;
            }
            else if (unsure)
            {
                return CritResult.Warning;
            }

            return CritResult.Success;
        }
    }
}

using BLMapCheck.BeatmapScanner.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Chain
    {
        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public static CritResult Check()
        {
            var issue = CritResult.Success;
            var links = BeatmapScanner.Chains.OrderBy(c => c.b).ToList();
            var notes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();

            if (notes.Count >= 16)
            {
                var link = links.Where(l => l.b <= notes[15].Time).ToList();
                foreach (var l in link)
                {
                    //CreateDiffCommentLink("R2D - Cannot be part of the first 16 notes", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
            }
            else if (links.Any())
            {
                var link = links.Where(l => l.b >= notes.Last().Time).Take(16 - notes.Count).ToList();
                foreach (var l in link)
                {
                    //CreateDiffCommentLink("R2D - Cannot be part of the first 16 notes", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
            }

            // TODO: Make this mess better idk
            foreach (var l in links)
            {
                var chain = l;
                var x = Math.Abs(l.tx - l.x) * chain.s;
                var y = Math.Abs(l.ty - l.y) * chain.s;
                var distance = Math.Sqrt(x * x + y * y);
                var value = distance / (chain.sc - 1);
                // Difference between expected and current distance, multiplied by current squish to know maximum value
                double max;
                if (l.ty == l.y) max = Math.Round(ChainLinkVsAir / value * chain.s, 2);
                else max = Math.Round(ChainLinkVsAir * 1.1 / value * chain.s, 2);
                if (chain.s - 0.01 > max)
                {
                    //CreateDiffCommentLink("R2D - Link spacing issue. Maximum squish for placement: " + max, CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                var newX = l.x + (l.tx - l.x) * chain.s;
                var newY = l.y + (l.ty - l.y) * chain.s;
                if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                {
                    //CreateDiffCommentLink("R2D - Lead too far", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                if (l.tb < l.b)
                {
                    //CreateDiffCommentLink("R2D - Reverse Direction", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
                var note = notes.Find(x => x.Time >= l.tb && x.Type == l.c);
                if (note != null)
                {
                    if (l.tb + (l.tb - l.b) > note.Time)
                    {
                        //CreateDiffCommentLink("R2D - Duration between tail and next note is too short", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                        issue = CritResult.Fail;
                    }
                }

                var temp = new Cube(notes.First())
                {
                    Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[l.d], 360),
                    Line = l.x,
                    Layer = l.y
                };
                var temp2 = new Cube(notes.First())
                {
                    Line = l.tx,
                    Layer = l.ty
                };
                var temp3 = new List<Cube>
                {
                    temp,
                    temp2
                };
                if (!ScanMethod.IsSameDirection(ScanMethod.ReverseCutDirection(ScanMethod.FindAngleViaPosition(temp3, 0, 1)), temp.Direction, MaxChainRotation))
                {
                    //CreateDiffCommentLink("R2D - Over 45°", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritResult.Fail;
                }
            }

            return issue;
        }
    }
}

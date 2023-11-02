using BLMapCheck.BeatmapScanner.MapCheck;
using System;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class FusedObject
    {
        // Detect if objects are too close. Configurable margin (in ms)
        // TODO: There's probably a way better way to do this, can someone clean this mess
        public static CritSeverity Check(float NoteJumpSpeed)
        {
            var issue = CritSeverity.Success;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.b).ToList();
            var bombs = BeatmapScanner.Bombs.OrderBy(b => b.b).ToList();
            var walls = BeatmapScanner.Walls.OrderBy(w => w.b).ToList();

            foreach (var w in walls)
            {
                foreach (var c in cubes)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(c.Time);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c.Time - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (c.Time >= w.b - max && c.Time <= w.b + w.d + max && c.Line <= w.x + w.w - 1 && c.Line >= w.x && c.Layer < w.y + w.h && c.Layer >= w.y - 1)
                    {
                        //CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
                foreach (var b in bombs)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(b.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (b.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (b.b >= w.b - max && b.b <= w.b + w.d + max && b.x <= w.x + w.w - 1 && b.x >= w.x && b.y < w.y + w.h && b.y >= w.y - 1)
                    {
                        //CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    BeatPerMinute.BPM.SetCurrentBPM(c.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c.b - (w.b + w.d) >= max)
                    {
                        break;
                    }
                    if (c.b >= w.b - max && c.b <= w.b + w.d + max && c.tx <= w.x + w.w - 1 && c.tx >= w.x && c.ty < w.y + w.h && c.ty >= w.y - 1)
                    {
                        //CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
            }

            for (int i = 0; i < cubes.Count; i++)
            {
                var c = cubes[i];
                for (int j = i + 1; j < cubes.Count; j++)
                {
                    var c2 = cubes[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.Time);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.Time - c.Time >= max)
                    {
                        break;
                    }
                    if (c.Time >= c2.Time - max && c.Time <= c2.Time + max && c.Line == c2.Line && c.Layer == c2.Layer)
                    {
                        //CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                        //CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
                for (int j = 0; j < bombs.Count; j++)
                {
                    var b = bombs[j];
                    BeatPerMinute.BPM.SetCurrentBPM(b.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (b.b - c.Time >= max)
                    {
                        break;
                    }
                    if (c.Time >= b.b - max && c.Time <= b.b + max && c.Line == b.x && c.Layer == b.y)
                    {
                        //CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                        //CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - c.Time >= max)
                    {
                        break;
                    }
                    if (c.Time >= c2.b - max && c.Time <= c2.b + max && c.Line == c2.tx && c.Layer == c2.ty)
                    {
                        //CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD
                        //CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
            }

            for (int i = 0; i < bombs.Count; i++)
            {
                var b = bombs[i];
                for (int j = i + 1; j < bombs.Count; j++)
                {
                    var b2 = bombs[j];
                    BeatPerMinute.BPM.SetCurrentBPM(b2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (b2.b - b.b >= max)
                    {
                        break;
                    }
                    if (b.b >= b2.b - max && b.b <= b2.b + max && b.x == b2.x && b.y == b2.y)
                    {
                        //CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                        //CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b2); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - b.b >= max)
                    {
                        break;
                    }
                    if (b.b >= c2.b - max && b.b <= c2.b + max && b.x == c2.tx && b.y == c2.ty)
                    {
                        //CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD 
                        //CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
            }

            for (int i = 0; i < chains.Count; i++)
            {
                var c = chains[i];
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    BeatPerMinute.BPM.SetCurrentBPM(c2.b);
                    var max = Math.Round(BeatPerMinute.BPM.ToBeatTime(1) / NoteJumpSpeed * FusedDistance, 3);
                    if (c2.b - c.b >= max)
                    {
                        break;
                    }
                    if (c.b >= c2.b - max && c.b <= c2.b + max && c.tx == c2.tx && c.ty == c2.ty)
                    {
                        //CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c); TODO: USE NEW METHOD 
                        //CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2); TODO: USE NEW METHOD 
                        issue = CritSeverity.Fail;
                    }
                }
            }

            BeatPerMinute.BPM.ResetCurrentBPM();
            return issue;
        }
    }
}

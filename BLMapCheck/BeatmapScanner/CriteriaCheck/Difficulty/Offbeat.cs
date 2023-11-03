using System;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Offbeat
    {
        public static readonly float[] AllowedSnap = { 0, 0.125f, 0.167f, 0.25f, 0.333f, 0.375f, 0.5f, 0.625f, 0.667f, 0.75f, 0.833f, 0.875f };

        public static void Check()
        {
            var swings = BeatmapScanner.Datas.OrderBy(c => c.Time).ToList();
            if (swings.Any())
            {
                foreach (var swing in swings)
                {
                    var precision = (float)Math.Round(swing.Start.Time % 1, 3);
                    if (!AllowedSnap.Contains(precision))
                    {
                        var reality = ScanMethod.RealToFraction(precision, 0.01);
                        // TODO: USE NEW METHOD
                        //CreateDiffCommentNote(reality.N.ToString() + "/" + reality.D.ToString(), CommentTypesEnum.Info, swing.Start);
                    }
                }
            }
        }

    }
}

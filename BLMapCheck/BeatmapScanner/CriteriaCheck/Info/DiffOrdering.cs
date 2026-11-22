using BLMapCheck.Classes.Results;
using Parser.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class DiffOrdering
    {
        // The difficulties must be ordered based on increasing star difficulty, with an allowed leniency of up to 0.5 stars.
        private const double Leniency = 0.5;

        // Stars are BeatLeader's star rating from the acc AI (CheckContext.GetStars). If the AI can't run, the pass rating is used,
        // as an estimate: the result is then a warning.
        // Checked per characteristic. If no characteristic is sent, every characteristic is checked
        public static CritResult Check(CheckContext ctx, List<DifficultySet> difficulties, string characteristic = "")
        {
            CritResult criteria = CritResult.Success;

            var characteristics = characteristic == ""
                ? difficulties.Select(x => x.Characteristic).Distinct().ToList()
                : new List<string>() { characteristic };

            foreach (var chara in characteristics)
            {
                var ordered = new List<double>();
                var estimated = false;

                foreach (var difficulty in difficulties.Where(x => x.Characteristic == chara && x.Data.Notes.Count >= 20).OrderBy(x => x.BeatMap._difficultyRank))
                {
                    var stars = ctx.GetStars(difficulty);
                    if (stars != null)
                    {
                        ordered.Add(Math.Round(stars.Value, 2));
                        continue;
                    }
                    var data = ctx.GetRating(difficulty);
                    if (data == null) continue;
                    ordered.Add(Math.Round(data.PassRating, 2));
                    estimated = true;
                }

                if (ordered.Count < 2) continue;

                // A difficulty may be rated lower than any easier difficulty, as long as it's within the leniency
                var highest = double.MinValue;
                var valid = true;
                foreach (var rating in ordered)
                {
                    if (rating < highest - Leniency) valid = false;
                    highest = Math.Max(highest, rating);
                }

                if (valid)
                {
                    ctx.Results.CreateInfoResult("Difficulty Ordering", Severity.Passed, "SongInfo", "Difficulty ordering is correct",
                        new() { new("Characteristic", chara), new("CurrentOrder", string.Join(",", ordered)) });
                }
                else
                {
                    ctx.Results.CreateInfoResult("Difficulty Ordering", estimated ? Severity.Warning : Severity.Error, "SongInfo",
                        estimated ? "Difficulty ordering may be wrong, estimated with the pass rating" : "Difficulty ordering is wrong",
                        new() { new("Characteristic", chara), new("CurrentOrder", string.Join(",", ordered)), new("ExpectedOrder", string.Join(",", ordered.OrderBy(x => x))) });
                    if (estimated) { if (CritResult.Warning > criteria) criteria = CritResult.Warning; }
                    else criteria = CritResult.Fail;
                }
            }

            return criteria;
        }
    }
}

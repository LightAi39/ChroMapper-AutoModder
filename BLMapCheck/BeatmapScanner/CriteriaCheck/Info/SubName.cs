using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SubName
    {
        public static CritResult Check(CheckContext ctx, string songName, string Author, string subName)
        {
            CritResult criteria = Classes.Helper.Helper.CheckSearchable(ctx, "Song Sub Name", subName);

            if (songName.Count() != 0)
            {
                var containedSubstrings = CheckForSubstrings(songName);

                if (containedSubstrings.Any())
                {
                    ctx.Results.CreateInfoResult("Song Name", Severity.Error, "SongInfo", "Tags should only be in the Sub Name field",
                    new() { new("SongName", songName), new("Tags", string.Join(", ", containedSubstrings)) });
                    criteria = CritResult.Fail;
                }
            }
            if (Author.Count() != 0)
            {
                var containedSubstrings = CheckForSubstrings(Author);
                if (containedSubstrings.Any())
                {
                    ctx.Results.CreateInfoResult("Song Author", Severity.Error, "SongInfo", "Tags should only be in the Sub Name field",
                    new() { new("SongAuthor", Author), new("Tags", string.Join(", ", containedSubstrings)) });
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateInfoResult("Song SubName", Severity.Passed, "SongInfo", "Tags were not found in the Song Name or Song Author field",
                   new() { new("SubName", "Success") });
            }

            return criteria;
        }

        private static readonly List<(string Name, Regex Pattern)> Tags = new[] { "remix", "ver.", "feat.", "ft.", "featuring", "cover" }
            .Select(tag => (tag, new Regex(@"\b" + Regex.Escape(tag) + (char.IsLetter(tag.Last()) ? @"\b" : ""), RegexOptions.IgnoreCase)))
            .ToList();

        private static List<string> CheckForSubstrings(string input)
        {
            // Match whole words only, so words like "Discover" or "Forever." are not detected as tags
            var containedSubstrings = Tags
                .Where(tag => tag.Pattern.IsMatch(input)).Select(tag => tag.Name).ToList();

            return containedSubstrings;
        }
    }

    
}

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

            if (CheckOrder(ctx, subName, ref criteria) && criteria == CritResult.Success) criteria = CritResult.Warning;

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateInfoResult("Song SubName", Severity.Passed, "SongInfo", "Tags were not found in the Song Name or Song Author field",
                   new() { new("SubName", "Success") });
            }

            return criteria;
        }

        private static readonly List<(string Name, Regex Pattern)> Tags = new[] { "remix", "ver.", "feat.", "ft.", "featuring", "cover", "covered by", "edit", "nightcore", "sped up" }
            .Select(tag => (tag, new Regex(@"\b" + Regex.Escape(tag) + (char.IsLetter(tag.Last()) ? @"\b" : ""), RegexOptions.IgnoreCase)))
            .ToList();

        private static List<string> CheckForSubstrings(string input)
        {
            // Match whole words only, so words like "Discover" or "Forever." are not detected as tags
            var containedSubstrings = Tags
                .Where(tag => tag.Pattern.IsMatch(input)).Select(tag => tag.Name).ToList();

            return containedSubstrings;
        }

        // Tags should follow a logical order: all tags without parentheses come before those with, and remixes and covers come before
        // speed or duration modifications, like "feat. Nanahira (Camellia Remix) (Short Edit)". Returns true for a warning only
        private static bool CheckOrder(CheckContext ctx, string subName, ref CritResult criteria)
        {
            if (string.IsNullOrWhiteSpace(subName)) return false;

            // Parenthesized groups, and the text between them
            var parts = Regex.Split(subName, @"(\([^()]*\)|\[[^\[\]]*\])").Where(p => p.Trim() != "").ToList();
            var bracketed = parts.Select(p => p.StartsWith("(") || p.StartsWith("[")).ToList();

            var firstGroup = bracketed.IndexOf(true);
            if (firstGroup >= 0)
            {
                var late = parts.Skip(firstGroup).Where((p, i) => !bracketed[firstGroup + i] && CheckForSubstrings(p).Any()).ToList();
                if (late.Any())
                {
                    ctx.Results.CreateInfoResult("Song SubName", Severity.Error, "SongInfo", "All tags without parentheses should come before those with",
                        new() { new("SubName", subName), new("Tags", string.Join(", ", late.Select(p => p.Trim()))) });
                    criteria = CritResult.Fail;
                }
            }

            var groups = parts.Where((p, i) => bracketed[i]).ToList();
            var remix = groups.FindLastIndex(g => Fundamental.IsMatch(g));
            var modification = groups.FindIndex(g => Modification.IsMatch(g) && !Fundamental.IsMatch(g));
            if (modification >= 0 && modification < remix)
            {
                ctx.Results.CreateInfoResult("Song SubName", Severity.Warning, "SongInfo", "Tags should follow a logical order, remixes and covers before speed or duration modifications",
                    new() { new("SubName", subName) });
                return true;
            }
            return false;
        }

        private static readonly Regex Fundamental = new(@"\b(remix|mix|bootleg|cover|covered by)\b", RegexOptions.IgnoreCase);
        private static readonly Regex Modification = new(@"\b(edit|short|long|extended|sped up|nightcore|ver\.|version)", RegexOptions.IgnoreCase);
    }

    
}

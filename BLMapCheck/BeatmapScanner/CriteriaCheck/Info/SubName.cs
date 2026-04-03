using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SubName
    {
        public static CritResult Check(string songName, string Author)
        {
            CritResult criteria = CritResult.Success;

            if (songName.Count() != 0)
            {
                var containedSubstrings = CheckForSubstrings(songName);

                if (containedSubstrings.Any())
                {
                    CheckResults.Instance.CreateInfoResult("Song Name", Severity.Error, "SongInfo", "Tags should only be in the Sub Name field",
                    new() { new("SongName", songName), new("Tags", string.Join(", ", containedSubstrings)) });
                    criteria = CritResult.Fail;
                }
            }
            if (Author.Count() != 0)
            {
                var containedSubstrings = CheckForSubstrings(Author);
                if (containedSubstrings.Any())
                {
                    CheckResults.Instance.CreateInfoResult("Song Author", Severity.Error, "SongInfo", "Tags should only be in the Sub Name field",
                    new() { new("SongAuthor", Author), new("Tags", string.Join(", ", containedSubstrings)) });
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                CheckResults.Instance.CreateInfoResult("Song SubName", Severity.Passed, "SongInfo", "Tags were not found in the Song Name or Song Author field",
                   new() { new("SubName", "Success") });
            }

            return criteria;
        }

        private static List<string> CheckForSubstrings(string input)
        {
            string[] substringsToCheck = { "remix", "ver.", "feat.", "ft.", "featuring", "cover" };
            var containedSubstrings = substringsToCheck
                .Where(substring => input.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            return containedSubstrings;
        }
    }

    
}

using BLMapCheck.Classes.Results;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class SubName
    {
        public static CritResult Check(string songName, string Author)
        {
            var issue = CritResult.Success;
            if (songName.Count() != 0)
            {
                var containedSubstrings = CheckForSubstrings(songName);

                if (containedSubstrings.Any())
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Name = "Song Name",
                        Severity = Severity.Error,
                        CheckType = "SongInfo",
                        Description = "Tags should only be in the Sub Name field.",
                        ResultData = new()
                        {
                            new("SongName", songName),
                            new("Tags", string.Join(", ", containedSubstrings))
                        }
                    });
                    issue = CritResult.Fail;
                }
            }
            if (Author.Count() != 0)
            {
                var containedSubstrings = CheckForSubstrings(Author);
                if (containedSubstrings.Any())
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Name = "Song Author",
                        Severity = Severity.Error,
                        CheckType = "SongInfo",
                        Description = "Tags should only be in the Sub Name field.",
                        ResultData = new()
                        {
                            new("SongAuthor", Author),
                            new("Tags", string.Join(", ", containedSubstrings))
                        }
                    });
                    issue = CritResult.Fail;
                }
            }
            return issue;
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

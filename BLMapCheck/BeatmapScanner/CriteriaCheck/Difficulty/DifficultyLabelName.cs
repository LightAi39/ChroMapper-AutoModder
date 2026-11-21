using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class DifficultyLabelName
    {
        public static readonly ProfanityFilter.ProfanityFilter Profanity = new();

        // The map cannot contain obscene content in its difficulty names, or in the map itself through bookmarks.
        // Compare them with a list of offensive words.
        public static CritResult Check(CheckContext ctx, string difficultyLabel, string filename, List<Bookmark> bookmarks)
        {
            CritResult criteria = CritResult.Success;

            // Custom label
            if (difficultyLabel != null)
            {
                var found = Profanity.FindProfanities(difficultyLabel);
                if (found.Count > 0)
                {
                    // The comment only shows the description, so it says what was detected
                    var words = string.Join(", ", found.Select(w => "\"" + w + "\""));
                    ctx.Results.CreateDiffResult("Difficulty Label Name", Severity.Warning, "Label", $"The label name cannot contain obscene content. Detected {words} in \"{difficultyLabel}\"",
                        new() { new("Label", difficultyLabel), new("Detected", string.Join(", ", found)) });
                    // The filter also matches inside words ("Classic"), so it needs a human to confirm
                    criteria = CritResult.Warning;
                }
            }

            // Bookmarks. ChroMapper can't put a comment on a bookmark, the comment says where it is
            foreach (var (beat, name) in BookmarkNames(ctx, filename, bookmarks))
            {
                var found = Profanity.FindProfanities(name);
                if (found.Count == 0) continue;
                var words = string.Join(", ", found.Select(w => "\"" + w + "\""));
                ctx.Results.CreateDiffResult("Bookmark Name", Severity.Warning, "Label", $"Bookmarks cannot contain obscene content. Detected {words} in the bookmark \"{name}\" at beat {Math.Round(beat, 3)}",
                    new() { new("Bookmark", name), new("Beat", Math.Round(beat, 3).ToString()), new("Detected", string.Join(", ", found)) });
                criteria = CritResult.Warning;
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Difficulty Label Name", Severity.Passed, "Label", "The label name and bookmarks do not contain any obscene content");
            }

            return criteria;
        }

        // Bookmarks of the difficulty file (v2 _customData._bookmarks, v3 and v4 customData.bookmarks).
        // The parser only reads v3 bookmarks, they are used when the map wasn't loaded from a folder
        private static List<(float Beat, string Name)> BookmarkNames(CheckContext ctx, string filename, List<Bookmark> bookmarks)
        {
            var path = ctx.FolderPath != null && !string.IsNullOrEmpty(filename) ? Path.Combine(ctx.FolderPath, filename) : null;
            if (path == null || !File.Exists(path))
            {
                return (bookmarks ?? new()).Where(b => !string.IsNullOrWhiteSpace(b.n)).Select(b => (b.Beats, b.n)).ToList();
            }

            var names = new List<(float Beat, string Name)>();
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return names;
                foreach (var (customKey, bookmarksKey, beatKey, nameKey) in new[] { ("_customData", "_bookmarks", "_time", "_name"), ("customData", "bookmarks", "b", "n") })
                {
                    if (!root.TryGetProperty(customKey, out var custom) || custom.ValueKind != JsonValueKind.Object) continue;
                    if (!custom.TryGetProperty(bookmarksKey, out var array) || array.ValueKind != JsonValueKind.Array) continue;
                    foreach (var bookmark in array.EnumerateArray())
                    {
                        if (bookmark.ValueKind != JsonValueKind.Object) continue;
                        if (!bookmark.TryGetProperty(nameKey, out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString())) continue;
                        float beat = bookmark.TryGetProperty(beatKey, out var b) && b.ValueKind == JsonValueKind.Number ? b.GetSingle() : 0;
                        names.Add((beat, name.GetString()));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
            }
            return names;
        }
    }
}

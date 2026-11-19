using BLMapCheck.Classes.Results;
using Parser.Map;
using Parser.Map.V4;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class InfoValues
    {
        // Colors that every color scheme must define, the white colors are optional
        private static readonly string[] RequiredColors = { "saberAColor", "saberBColor", "environmentColor0", "environmentColor1", "obstaclesColor", "environmentColor0Boost", "environmentColor1Boost" };

        // Deprecated or invalid values in the info file
        public static CritResult Check(CheckContext ctx, Parser.Map.Info info)
        {
            CritResult criteria = CritResult.Success;

            // Shuffle must be unused. Editors set the shuffle period to 0.5 by default, so 0.5 also counts as unused
            if (info._shuffle != 0 || (info._shufflePeriod != 0 && info._shufflePeriod != 0.5f))
            {
                ctx.Results.CreateInfoResult("Shuffle", Severity.Error, "SongInfo", "Shuffle and Shuffle Period must be unused or set to 0. This is a deprecated feature",
                    new() { new("Shuffle", info._shuffle.ToString()), new("ShufflePeriod", info._shufflePeriod.ToString()) });
                criteria = CritResult.Fail;
            }

            // The audio file must be in the .ogg format (distributed as .egg is allowed)
            var extension = System.IO.Path.GetExtension(info._songFilename ?? "").ToLowerInvariant();
            if (extension != ".ogg" && extension != ".egg")
            {
                ctx.Results.CreateInfoResult("Audio File", Severity.Error, "SongInfo", "The audio file must be in the .ogg format (.egg is allowed)",
                    new() { new("SongFilename", info._songFilename ?? "") });
                criteria = CritResult.Fail;
            }

            // All defined color schemes must provide values for all colors
            var schemes = info._colorSchemes ?? new object[0];
            for (int i = 0; i < schemes.Length; i++)
            {
                var (name, missing) = GetMissingColors(schemes[i]);
                if (missing.Count > 0)
                {
                    ctx.Results.CreateInfoResult("Color Scheme", Severity.Error, "SongInfo", $"Color scheme {(string.IsNullOrEmpty(name) ? (i + 1).ToString() : name)} must provide values for all colors",
                        new() { new("MissingColors", string.Join(", ", missing)) });
                    criteria = CritResult.Fail;
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateInfoResult("Info Values", Severity.Passed, "SongInfo", "The audio file is an ogg, shuffle is unused and color schemes are complete");
            }

            return criteria;
        }

        // Info v2 schemes are raw JSON, info v4 schemes are converted by the parser
        private static (string name, List<string> missing) GetMissingColors(object scheme)
        {
            if (scheme is ColorScheme v4)
            {
                var colors = new Dictionary<string, string>
                {
                    { "saberAColor", v4.saberAColor }, { "saberBColor", v4.saberBColor },
                    { "environmentColor0", v4.environmentColor0 }, { "environmentColor1", v4.environmentColor1 },
                    { "obstaclesColor", v4.obstaclesColor },
                    { "environmentColor0Boost", v4.environmentColor0Boost }, { "environmentColor1Boost", v4.environmentColor1Boost }
                };
                return (v4.colorSchemeName, colors.Where(c => string.IsNullOrWhiteSpace(c.Value)).Select(c => c.Key).ToList());
            }

            if (scheme is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                if (!element.TryGetProperty("colorScheme", out var data) || data.ValueKind != JsonValueKind.Object)
                {
                    return (null, RequiredColors.ToList());
                }
                string name = data.TryGetProperty("colorSchemeId", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
                return (name, RequiredColors.Where(c => !data.TryGetProperty(c, out var color) || !IsColor(color)).ToList());
            }

            return (null, RequiredColors.ToList());
        }

        private static bool IsColor(JsonElement color)
        {
            if (color.ValueKind != JsonValueKind.Object) return false;
            foreach (var channel in new[] { "r", "g", "b", "a" })
            {
                if (!color.TryGetProperty(channel, out var value) || value.ValueKind != JsonValueKind.Number) return false;
            }
            return true;
        }
    }
}

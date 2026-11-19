using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    // Optional: objects that use Mapping Extensions or Noodle Extensions, no mod can be required to play a map
    internal static class ModdedObjects
    {
        // Object custom data used by Noodle Extensions (v2 and v3 names). Chroma keys like color are allowed.
        private static readonly HashSet<string> NoodleKeys = new()
        {
            "_position", "_rotation", "_localRotation", "_scale", "_noteJumpMovementSpeed", "_noteJumpStartBeatOffset", "_fake", "_interactable",
            "_cutDirection", "_flip", "_disableNoteGravity", "_disableNoteLook", "_track", "_animation",
            "coordinates", "tailCoordinates", "worldRotation", "localRotation", "size", "noteJumpMovementSpeed", "noteJumpStartBeatOffset",
            "uninteractable", "disableNoteGravity", "disableNoteLook", "track", "animation", "link", "flip",
            "disableBadCutDirection", "disableBadCutSpeed", "disableBadCutSaberType"
        };

        // Difficulty custom data used by Noodle Extensions (custom events, fake objects, point definitions)
        private static readonly HashSet<string> NoodleDifficultyKeys = new()
        {
            "_customEvents", "_pointDefinitions", "customEvents", "pointDefinitions", "fakeColorNotes", "fakeBombNotes", "fakeObstacles", "fakeBurstSliders"
        };

        // Object arrays of the v2 and v3 difficulty formats
        private static readonly string[] ObjectArrays = { "_notes", "_obstacles", "_sliders", "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" };

        // Above this many flagged objects, a single summary is reported instead of one result per object
        private const int MaxObjectResults = 20;

        private static bool InGrid(int x, int y) => x >= 0 && x <= 3 && y >= 0 && y <= 2;

        // Difficulties that require Noodle or Mapping Extensions, or place many notes with them
        public static bool IsModdedDifficulty(List<string> requirements, List<Note> notes)
        {
            if (requirements != null && requirements.Any(r => r.Equals("Noodle Extensions", StringComparison.OrdinalIgnoreCase) || r.Equals("Mapping Extensions", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            int moddedNotes = notes.Count(n => n.customData?.coordinates != null || !InGrid(n.x, n.y) || !IsDirection(n.CutDirection));
            return notes.Count > 0 && moddedNotes > notes.Count / 10;
        }
        private static bool IsDirection(int direction) => direction >= 0 && direction <= 8;
        // Mapping Extensions precision values are offset by 1000
        private static bool IsPrecision(int value) => Math.Abs(value) >= 1000;

        public static CritResult Check(CheckContext ctx, string filename, List<Note> notes, List<Bomb> bombs, List<Chain> chains, List<Arc> arcs, List<Wall> walls)
        {
            CritResult criteria = CritResult.Success;
            if (!ctx.Config.DetectModdedObjects) return criteria;

            // Mapping Extensions: positions outside the grid and precision angles or walls
            var mapping = new List<BeatmapGridObject>();
            mapping.AddRange(notes.Where(n => !InGrid(n.x, n.y) || !IsDirection(n.CutDirection)));
            mapping.AddRange(bombs.Where(b => !InGrid(b.x, b.y)));
            mapping.AddRange(chains.Where(c => !InGrid(c.x, c.y) || !InGrid(c.tx, c.ty) || !IsDirection(c.CutDirection)));
            mapping.AddRange(arcs.Where(a => !InGrid(a.x, a.y) || !InGrid(a.tx, a.ty) || !IsDirection(a.CutDirection) || !IsDirection(a.TailCutDirection)));
            mapping.AddRange(walls.Where(w => IsPrecision(w.x) || IsPrecision(w.y) || IsPrecision(w.Width) || IsPrecision(w.Height)));
            var mappingResults = new List<(BeatmapObject Object, List<Classes.Results.KeyValuePair> Data)>();
            foreach (var obj in mapping.OrderBy(o => o.Beats))
            {
                var data = new List<Classes.Results.KeyValuePair>() { new("Position", obj.x + "," + obj.y) };
                if (obj is Note note && !IsDirection(note.CutDirection)) data.Add(new("CutDirection", note.CutDirection.ToString()));
                mappingResults.Add((Anchor(obj), data));
            }
            if (Report(ctx, "Mapping Extensions", "Objects cannot use Mapping Extensions (position, angle or size outside of the vanilla values)", mappingResults)) criteria = CritResult.Fail;

            // Noodle Extensions: the parser only keeps positions, the raw difficulty file has everything else
            var path = ctx.FolderPath != null && !string.IsNullOrEmpty(filename) ? Path.Combine(ctx.FolderPath, filename) : null;
            if (path != null && File.Exists(path))
            {
                if (CheckNoodleFile(ctx, path, notes, bombs, chains, walls)) criteria = CritResult.Fail;
            }
            else
            {
                var objects = new List<BeatmapGridObject>();
                objects.AddRange(notes);
                objects.AddRange(bombs);
                objects.AddRange(chains);
                objects.AddRange(arcs);
                var noodleResults = objects.Where(o => o.customData?.coordinates != null || o.customData?.tailCoordinates != null).OrderBy(o => o.Beats)
                    .Select(o => (Anchor(o), new List<Classes.Results.KeyValuePair>() { new("Keys", "coordinates") })).ToList();
                if (Report(ctx, "Noodle Extensions", "Objects cannot use Noodle Extensions", noodleResults)) criteria = CritResult.Fail;
            }

            return criteria;
        }

        private static bool CheckNoodleFile(CheckContext ctx, string path, List<Note> notes, List<Bomb> bombs, List<Chain> chains, List<Wall> walls)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
                return false;
            }

            bool found = false;
            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return false;

                // Difficulty wide features
                var difficultyKeys = Keys(CustomData(root)).Where(NoodleDifficultyKeys.Contains).ToList();
                if (difficultyKeys.Count > 0)
                {
                    ctx.Results.CreateDiffResult("Noodle Extensions", Severity.Error, "Requirements", "The difficulty cannot use Noodle Extensions",
                        new() { new("Keys", string.Join(", ", difficultyKeys)) });
                    found = true;
                }

                // Objects
                var objectResults = new List<(BeatmapObject Object, List<Classes.Results.KeyValuePair> Data)>();
                foreach (var name in ObjectArrays)
                {
                    if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array) continue;
                    foreach (var element in array.EnumerateArray())
                    {
                        if (element.ValueKind != JsonValueKind.Object) continue;
                        var keys = Keys(CustomData(element)).Where(NoodleKeys.Contains).ToList();
                        if (keys.Count == 0) continue;

                        float beat = Number(element, "b", "_time");
                        int x = (int)Number(element, "x", "_lineIndex");
                        int y = (int)Number(element, "y", "_lineLayer");
                        objectResults.Add((Find(name, element, beat, x, y, notes, bombs, chains, walls), new() { new("Keys", string.Join(", ", keys)) }));
                    }
                }
                if (Report(ctx, "Noodle Extensions", "Objects cannot use Noodle Extensions", objectResults.OrderBy(r => r.Object.Beats).ToList())) found = true;
            }
            return found;
        }

        // One result per object, or a single summary when there are too many (thousands of comments make ChroMapper lag)
        private static bool Report(CheckContext ctx, string name, string description, List<(BeatmapObject Object, List<Classes.Results.KeyValuePair> Data)> results)
        {
            if (results.Count == 0) return false;
            if (results.Count <= MaxObjectResults)
            {
                foreach (var (obj, data) in results)
                {
                    ctx.Results.CreateDiffResult(name, Severity.Error, "Requirements", description, data, new() { obj });
                }
                return true;
            }

            var keys = results.SelectMany(r => r.Data.Where(d => d.Key == "Keys").SelectMany(d => d.Value.Split(new[] { ", " }, StringSplitOptions.None))).Distinct().ToList();
            var summary = new List<Classes.Results.KeyValuePair>() { new("Count", results.Count.ToString()) };
            if (keys.Count > 0) summary.Add(new("Keys", string.Join(", ", keys)));
            ctx.Results.CreateDiffResult(name, Severity.Error, "Requirements", $"{description}. {results.Count} objects found, starting at beat {Math.Round(results[0].Object.Beats, 3)}", summary);
            return true;
        }

        // Parsed object the raw element comes from, a placeholder note at its position otherwise
        private static BeatmapObject Find(string array, JsonElement element, float beat, int x, int y, List<Note> notes, List<Bomb> bombs, List<Chain> chains, List<Wall> walls)
        {
            bool Same(BeatmapGridObject o) => Math.Abs(o.Beats - beat) < 0.001 && o.x == x && o.y == y;
            bool isBomb = array == "bombNotes" || (array == "_notes" && (int)Number(element, "_type", "_type") == 3);
            BeatmapGridObject match = array switch
            {
                "_obstacles" or "obstacles" => walls.FirstOrDefault(Same),
                "burstSliders" => chains.FirstOrDefault(Same),
                _ when isBomb => bombs.FirstOrDefault(Same),
                "_notes" or "colorNotes" => notes.FirstOrDefault(Same),
                _ => null
            };
            return match ?? new Note { Beats = beat, x = x, y = y };
        }

        // The plugin can't place comments on arcs, use a placeholder note at the arc head instead
        private static BeatmapObject Anchor(BeatmapGridObject obj)
        {
            return obj is Arc arc ? new Note { Beats = arc.Beats, x = arc.x, y = arc.y, Color = arc.Color } : obj;
        }

        private static JsonElement? CustomData(JsonElement element)
        {
            if (element.TryGetProperty("customData", out var data) && data.ValueKind == JsonValueKind.Object) return data;
            if (element.TryGetProperty("_customData", out data) && data.ValueKind == JsonValueKind.Object) return data;
            return null;
        }

        private static IEnumerable<string> Keys(JsonElement? data)
        {
            return data == null ? Enumerable.Empty<string>() : data.Value.EnumerateObject().Select(p => p.Name);
        }

        private static float Number(JsonElement element, string name, string legacyName)
        {
            if ((element.TryGetProperty(name, out var value) || element.TryGetProperty(legacyName, out value)) && value.ValueKind == JsonValueKind.Number)
            {
                return value.GetSingle();
            }
            return 0;
        }
    }
}

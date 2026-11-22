using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Event;
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
        // Object custom data used by Noodle Extensions (v2 and v3 names). Chroma keys like color are allowed. Tracks are shared with Chroma
        // (color animations), an object's track is only Noodle when a custom event animates it with NoodleTrackProperties
        private static readonly HashSet<string> NoodleKeys = new()
        {
            "_position", "_rotation", "_localRotation", "_scale", "_noteJumpMovementSpeed", "_noteJumpStartBeatOffset", "_fake", "_interactable",
            "_cutDirection", "_flip", "_disableNoteGravity", "_disableNoteLook", "_animation",
            "coordinates", "tailCoordinates", "worldRotation", "localRotation", "size", "noteJumpMovementSpeed", "noteJumpStartBeatOffset",
            "uninteractable", "disableNoteGravity", "disableNoteLook", "animation", "link", "flip",
            "disableBadCutDirection", "disableBadCutSpeed", "disableBadCutSaberType"
        };
        private static readonly HashSet<string> NoodleTrackProperties = new()
        {
            "_position", "_localPosition", "_definitePosition", "_rotation", "_localRotation", "_scale", "_dissolve", "_dissolveArrow", "_time", "_interactable",
            "position", "localPosition", "offsetPosition", "definitePosition", "offsetWorldRotation", "rotation", "localRotation", "scale", "dissolve",
            "dissolveArrow", "time", "interactable", "uninteractable"
        };

        // Difficulty custom data used only by Noodle Extensions: fake objects. Custom events and point definitions are shared with Chroma
        // (AnimateTrack on environment tracks), only the event types below are Noodle's. Objects moved by AnimateTrack have a track,
        // which is flagged on the object
        private static readonly HashSet<string> NoodleDifficultyKeys = new() { "fakeColorNotes", "fakeBombNotes", "fakeObstacles", "fakeBurstSliders" };
        private static readonly HashSet<string> NoodleEventTypes = new() { "AssignPathAnimation", "AssignTrackParent", "AssignPlayerToTrack" };

        // Object arrays of the v2 and v3 difficulty formats
        private static readonly string[] ObjectArrays = { "_notes", "_obstacles", "_sliders", "colorNotes", "bombNotes", "obstacles", "sliders", "burstSliders" };

        // Above this many flagged objects, a single summary is reported instead of one result per object
        private const int MaxObjectResults = 20;

        // Rotation event angles of the v2 format without Mapping Extensions
        private static readonly HashSet<float> VanillaRotations = new() { -60, -45, -30, -15, 15, 30, 45, 60 };

        private static bool InGrid(int x, int y) => x >= 0 && x <= 3 && y >= 0 && y <= 2;
        // Chain links may extend one lane outside the grid (R6C), and ranked arcs do too, but not above or below it
        private static bool InWideGrid(int x, int y) => x >= -1 && x <= 4 && y >= 0 && y <= 2;

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

        public static CritResult Check(CheckContext ctx, string filename, List<Note> notes, List<Bomb> bombs, List<Chain> chains, List<Arc> arcs, List<Wall> walls,
            List<RotationEvent> rotations, string sourceVersion)
        {
            CritResult criteria = CritResult.Success;
            if (!ctx.Config.DetectModdedObjects) return criteria;

            // Mapping Extensions: positions outside the grid and precision angles or walls
            var mapping = new List<BeatmapGridObject>();
            mapping.AddRange(notes.Where(n => !InGrid(n.x, n.y) || !IsDirection(n.CutDirection)));
            mapping.AddRange(bombs.Where(b => !InGrid(b.x, b.y)));
            mapping.AddRange(chains.Where(c => !InGrid(c.x, c.y) || !InWideGrid(c.tx, c.ty) || !IsDirection(c.CutDirection)));
            mapping.AddRange(arcs.Where(a => !InWideGrid(a.x, a.y) || !InWideGrid(a.tx, a.ty) || !IsDirection(a.CutDirection) || !IsDirection(a.TailCutDirection)));
            mapping.AddRange(walls.Where(w => IsPrecision(w.x) || IsPrecision(w.y) || IsPrecision(w.Width) || IsPrecision(w.Height)));
            var mappingResults = new List<(BeatmapObject Object, List<Classes.Results.KeyValuePair> Data)>();
            foreach (var obj in mapping.OrderBy(o => o.Beats))
            {
                var data = new List<Classes.Results.KeyValuePair>() { new("Position", obj.x + "," + obj.y) };
                if (obj is Note note && !IsDirection(note.CutDirection)) data.Add(new("CutDirection", note.CutDirection.ToString()));
                mappingResults.Add((Anchor(obj), data));
            }
            if (Report(ctx, "Mapping Extensions", "Objects cannot use Mapping Extensions (position, angle or size outside of the vanilla values)", mappingResults)) criteria = CritResult.Fail;

            // Mapping Extensions rotations: in v2, rotation values 1000 to 1720 are exact angles (the parser converts them to degrees), only
            // the vanilla angles exist without it. V3 rotation events take any angle
            if (sourceVersion != null && sourceVersion.StartsWith("2") && rotations != null)
            {
                var rotationResults = rotations.Where(r => !VanillaRotations.Contains(r.Rotation)).OrderBy(r => r.Beats)
                    .Select(r => ((BeatmapObject)new Note { Beats = r.Beats, x = 1, y = 0 }, new List<Classes.Results.KeyValuePair>() { new("Rotation", r.Rotation.ToString()) })).ToList();
                if (Report(ctx, "Mapping Extensions", "Rotation events cannot use Mapping Extensions (an exact angle instead of 15, 30, 45 or 60 degrees)", rotationResults)) criteria = CritResult.Fail;
            }

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
                objects.AddRange(walls);
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
                var difficultyData = CustomData(root);
                var difficultyKeys = Keys(difficultyData).Where(NoodleDifficultyKeys.Contains).ToList();
                difficultyKeys.AddRange(EventTypes(difficultyData).Where(NoodleEventTypes.Contains).Distinct());
                if (difficultyKeys.Count > 0)
                {
                    ctx.Results.CreateDiffResult("Noodle Extensions", Severity.Error, "Requirements", "The difficulty cannot use Noodle Extensions",
                        new() { new("Keys", string.Join(", ", difficultyKeys)) });
                    found = true;
                }

                // Tracks of the objects animated with Noodle properties
                var noodleTracks = new HashSet<string>();
                foreach (var name in new[] { "customEvents", "_customEvents" })
                {
                    if (difficultyData == null || !difficultyData.Value.TryGetProperty(name, out var events) || events.ValueKind != JsonValueKind.Array) continue;
                    foreach (var e in events.EnumerateArray())
                    {
                        if (e.ValueKind != JsonValueKind.Object) continue;
                        if ((!e.TryGetProperty("d", out var data) && !e.TryGetProperty("_data", out data)) || data.ValueKind != JsonValueKind.Object) continue;
                        if (!data.EnumerateObject().Any(p => NoodleTrackProperties.Contains(p.Name))) continue;
                        foreach (var track in Tracks(data)) noodleTracks.Add(track);
                    }
                }

                // Objects
                var objectResults = new List<(BeatmapObject Object, List<Classes.Results.KeyValuePair> Data)>();
                foreach (var name in ObjectArrays)
                {
                    if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array) continue;
                    foreach (var element in array.EnumerateArray())
                    {
                        if (element.ValueKind != JsonValueKind.Object) continue;
                        var custom = CustomData(element);
                        var keys = Keys(custom).Where(NoodleKeys.Contains).ToList();
                        if (custom != null && Tracks(custom.Value).Any(noodleTracks.Contains)) keys.Add("track");
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

        // Track names of an object or event (track or _track, a name or a list of names)
        private static IEnumerable<string> Tracks(JsonElement data)
        {
            if (!data.TryGetProperty("track", out var track) && !data.TryGetProperty("_track", out track)) return Enumerable.Empty<string>();
            if (track.ValueKind == JsonValueKind.String) return new[] { track.GetString() };
            if (track.ValueKind == JsonValueKind.Array) return track.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()).ToList();
            return Enumerable.Empty<string>();
        }

        // Types of the custom events (v2 _customEvents with _type, v3 customEvents with t)
        private static IEnumerable<string> EventTypes(JsonElement? data)
        {
            if (data == null) return Enumerable.Empty<string>();
            var types = new List<string>();
            foreach (var name in new[] { "customEvents", "_customEvents" })
            {
                if (!data.Value.TryGetProperty(name, out var events) || events.ValueKind != JsonValueKind.Array) continue;
                foreach (var e in events.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object) continue;
                    if ((e.TryGetProperty("t", out var type) || e.TryGetProperty("_type", out type)) && type.ValueKind == JsonValueKind.String) types.Add(type.GetString());
                }
            }
            return types;
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

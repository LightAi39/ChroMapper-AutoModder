using beatleader_parser.Timescale;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Info;
using Parser.Map.Difficulty.V3.Event;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Debug = UnityEngine.Debug;

namespace ChroMapper_LightModding.Helpers
{
    // Saves the beat of every object of the loaded difficulty, so the timing fixes done afterward can be listed and applied to the other difficulties
    internal class SnapshotHelper
    {
        public enum SnapshotKind { Note, Bomb, Wall, Arc, Chain }

        public class SnapshotObject
        {
            public SnapshotKind Kind;
            public int Color;
            public int PosX;
            public int PosY;
            public int Direction;
            public float Beat;
            public float? EndBeat; // wall end, arc and chain tail, null for notes and bombs
        }

        public class TimingChange
        {
            public SnapshotKind Kind;
            public int Color;
            public float? OriginalBeat; // null when the object was added
            public float? NewBeat; // null when the object was removed
            public float? OriginalEndBeat;
            public float? NewEndBeat;
        }

        public class DifficultyTimingCheck
        {
            public string Name;
            public List<TimingChange> Changes = new();
            public string Error;
        }

        // Objects that moved by more than this are a removal and an addition
        public const float MaxMoveBeats = 0.25f;
        // Under this the beat is considered unchanged
        private const float SameBeatTolerance = 0.001f;
        // Same hand notes closer than this are the same swing (sliders, windows, stacks), from the BLMapCheck swing detection
        private const float SameSwingSeconds = 0.07f;

        private readonly Plugin plugin;

        public List<SnapshotObject> Snapshot { get; private set; }
        public string SnapshotCharacteristic { get; private set; }
        public string SnapshotDifficulty { get; private set; }
        public List<TimingChange> Changes { get; private set; }
        public List<DifficultyTimingCheck> OtherDifficultyChecks { get; private set; }

        public SnapshotHelper(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public bool IsSnapshotOfCurrentDifficulty()
        {
            var current = plugin.BeatSaberSongContainer.MapDifficultyInfo;
            return Snapshot != null && current.Characteristic == SnapshotCharacteristic && current.Difficulty == SnapshotDifficulty;
        }

        public int TakeSnapshot()
        {
            var current = plugin.BeatSaberSongContainer.MapDifficultyInfo;
            Snapshot = GetCurrentObjects();
            SnapshotCharacteristic = current.Characteristic;
            SnapshotDifficulty = current.Difficulty;
            Changes = null;
            OtherDifficultyChecks = null;
            return Snapshot.Count;
        }

        public List<TimingChange> CompareToSnapshot()
        {
            Changes = Compare(Snapshot, GetCurrentObjects());
            return Changes;
        }

        private List<SnapshotObject> GetCurrentObjects()
        {
            return ToSnapshotObjects(plugin.NoteGridContainer.MapObjects, plugin.ObstacleGridContainer.MapObjects, plugin.ArcGridContainer.MapObjects, plugin.ChainGridContainer.MapObjects);
        }

        private static List<SnapshotObject> ToSnapshotObjects(IEnumerable<BaseNote> notes, IEnumerable<BaseObstacle> walls, IEnumerable<BaseArc> arcs, IEnumerable<BaseChain> chains)
        {
            List<SnapshotObject> objects = new();
            foreach (var note in notes)
            {
                objects.Add(new()
                {
                    Kind = note.Type == (int)NoteType.Bomb ? SnapshotKind.Bomb : SnapshotKind.Note,
                    Color = note.Color,
                    PosX = note.PosX,
                    PosY = note.PosY,
                    Direction = note.CutDirection,
                    Beat = note.JsonTime
                });
            }
            foreach (var wall in walls)
            {
                objects.Add(new() { Kind = SnapshotKind.Wall, PosX = wall.PosX, PosY = wall.PosY, Direction = wall.Width, Beat = wall.JsonTime, EndBeat = wall.JsonTime + wall.Duration });
            }
            foreach (var arc in arcs)
            {
                objects.Add(new() { Kind = SnapshotKind.Arc, Color = arc.Color, PosX = arc.PosX, PosY = arc.PosY, Direction = arc.CutDirection, Beat = arc.JsonTime, EndBeat = arc.TailJsonTime });
            }
            foreach (var chain in chains)
            {
                objects.Add(new() { Kind = SnapshotKind.Chain, Color = chain.Color, PosX = chain.PosX, PosY = chain.PosY, Direction = chain.CutDirection, Beat = chain.JsonTime, EndBeat = chain.TailJsonTime });
            }
            return objects;
        }

        // Pairs each original object with a current object of the same kind and color within MaxMoveBeats.
        // The closest pairs are taken first, and a pair that also kept its position is preferred, so untouched objects keep their match.
        // A matched pair is a change when its start or its end moved.
        private static List<TimingChange> Compare(List<SnapshotObject> original, List<SnapshotObject> current)
        {
            List<TimingChange> changes = new();
            var currentGroups = current.GroupBy(o => (o.Kind, o.Color)).ToDictionary(g => g.Key, g => g.OrderBy(o => o.Beat).ToList());

            foreach (var group in original.GroupBy(o => (o.Kind, o.Color)))
            {
                var originals = group.OrderBy(o => o.Beat).ToList();
                var currents = currentGroups.TryGetValue(group.Key, out var list) ? list : new();
                currentGroups.Remove(group.Key);

                List<(int o, int c, float cost, float endCost)> pairs = new();
                int start = 0;
                for (int i = 0; i < originals.Count; i++)
                {
                    var orig = originals[i];
                    while (start < currents.Count && currents[start].Beat < orig.Beat - MaxMoveBeats - SameBeatTolerance) start++;
                    for (int j = start; j < currents.Count && currents[j].Beat <= orig.Beat + MaxMoveBeats + SameBeatTolerance; j++)
                    {
                        var cur = currents[j];
                        bool samePlace = cur.PosX == orig.PosX && cur.PosY == orig.PosY && cur.Direction == orig.Direction;
                        pairs.Add((i, j, Math.Abs(cur.Beat - orig.Beat) + (samePlace ? 0 : 1), EndDistance(orig, cur)));
                    }
                }

                var originalMatched = new bool[originals.Count];
                var currentMatched = new bool[currents.Count];
                foreach (var (o, c, _, _) in pairs.OrderBy(p => p.cost).ThenBy(p => p.endCost))
                {
                    if (originalMatched[o] || currentMatched[c]) continue;
                    originalMatched[o] = currentMatched[c] = true;
                    var orig = originals[o];
                    var cur = currents[c];
                    if (!SameBeat(orig.Beat, cur.Beat) || !SameBeat(orig.EndBeat, cur.EndBeat))
                    {
                        changes.Add(new() { Kind = group.Key.Kind, Color = group.Key.Color, OriginalBeat = orig.Beat, NewBeat = cur.Beat, OriginalEndBeat = orig.EndBeat, NewEndBeat = cur.EndBeat });
                    }
                }

                for (int i = 0; i < originals.Count; i++)
                {
                    if (!originalMatched[i]) changes.Add(new() { Kind = group.Key.Kind, Color = group.Key.Color, OriginalBeat = originals[i].Beat, OriginalEndBeat = originals[i].EndBeat });
                }
                for (int j = 0; j < currents.Count; j++)
                {
                    if (!currentMatched[j]) changes.Add(new() { Kind = group.Key.Kind, Color = group.Key.Color, NewBeat = currents[j].Beat, NewEndBeat = currents[j].EndBeat });
                }
            }

            // Kinds and colors that only exist in the current map
            foreach (var group in currentGroups)
            {
                foreach (var cur in group.Value) changes.Add(new() { Kind = group.Key.Kind, Color = group.Key.Color, NewBeat = cur.Beat, NewEndBeat = cur.EndBeat });
            }

            return changes;
        }

        private static bool SameBeat(float? a, float? b)
        {
            if (a == null || b == null) return a == null && b == null;
            return Math.Abs(a.Value - b.Value) <= SameBeatTolerance;
        }

        private static float EndDistance(SnapshotObject a, SnapshotObject b)
        {
            if (a.EndBeat == null || b.EndBeat == null) return 0;
            return Math.Abs(a.EndBeat.Value - b.EndBeat.Value);
        }

        // Uses the loaded difficulty as the reference timing for every other difficulty.
        // For notes only the first note of a swing is compared, so a single note can match a slider or a window.
        // Bombs, walls, arcs and chains are compared to the reference objects of the same kind and color, their end included.
        // An object is only fixed when the reference has one within MaxMoveBeats, the rest is considered as mapped to something else in the song.
        // Nothing is ever added or removed.
        public List<DifficultyTimingCheck> CheckOtherDifficulties()
        {
            var info = plugin.BeatSaberSongContainer.Info;
            var currentInfo = plugin.BeatSaberSongContainer.MapDifficultyInfo;

            List<BaseBpmEvent> currentBpmEvents = plugin.BPMChangeGridContainer.MapObjects.ToList();
            if (currentBpmEvents.Count == 0) // same as the timeline markers, bpm changes can be missing on initial load
            {
                currentBpmEvents = BeatSaberSongUtils.GetMapFromInfoFiles(info, currentInfo).BpmEvents;
            }
            var reference = GetSwingStarts(plugin.NoteGridContainer.MapObjects, CreateTimescale(currentBpmEvents));
            var referenceObjects = GetCurrentObjects()
                .Where(o => o.Kind != SnapshotKind.Note)
                .GroupBy(o => (o.Kind, o.Color))
                .ToDictionary(g => g.Key, g => g.OrderBy(o => o.Beat).ToList());

            // Objects that the timing fix moved, a difficulty still at the original timing gets the same fix
            Dictionary<float, float> moved = new();
            Dictionary<(SnapshotKind, int), Dictionary<float, float>> movedObjects = new();
            if (IsSnapshotOfCurrentDifficulty() && Changes != null)
            {
                foreach (var change in Changes.Where(c => c.OriginalBeat != null && c.NewBeat != null))
                {
                    var target = moved;
                    if (change.Kind != SnapshotKind.Note && !movedObjects.TryGetValue((change.Kind, change.Color), out target))
                    {
                        target = new();
                        movedObjects.Add((change.Kind, change.Color), target);
                    }
                    if (!target.ContainsKey(change.OriginalBeat.Value)) target.Add(change.OriginalBeat.Value, change.NewBeat.Value);
                }
            }

            List<DifficultyTimingCheck> checks = new();
            foreach (var set in info.DifficultySets)
            {
                if (set.Characteristic == "Lightshow") continue;
                foreach (var difficulty in set.Difficulties)
                {
                    if (set.Characteristic == currentInfo.Characteristic && difficulty.Difficulty == currentInfo.Difficulty) continue;

                    DifficultyTimingCheck check = new() { Name = $"{difficulty.Difficulty} {set.Characteristic}" };
                    checks.Add(check);
                    try
                    {
                        BaseDifficulty map = BeatSaberSongUtils.GetMapFromInfoFiles(info, difficulty);
                        foreach (float beat in GetSwingStarts(map.Notes, CreateTimescale(map.BpmEvents)).Distinct())
                        {
                            float? newBeat = FindReferenceBeat(beat, reference, moved);
                            if (newBeat != null) check.Changes.Add(new() { Kind = SnapshotKind.Note, OriginalBeat = beat, NewBeat = newBeat });
                        }
                        foreach (var obj in ToSnapshotObjects(map.Notes, map.Obstacles, map.Arcs, map.Chains).Where(o => o.Kind != SnapshotKind.Note))
                        {
                            if (!referenceObjects.TryGetValue((obj.Kind, obj.Color), out var references)) continue;
                            movedObjects.TryGetValue((obj.Kind, obj.Color), out var movedOfKind);
                            var change = FindReferenceObject(obj, references, movedOfKind ?? new());
                            if (change != null) check.Changes.Add(change);
                        }
                    }
                    catch (Exception e)
                    {
                        check.Error = e.Message;
                        Debug.LogError($"Automodder: failed to check the timings of {check.Name}: {e}");
                    }
                }
            }

            OtherDifficultyChecks = checks;
            return checks;
        }

        // Null when the beat is already on the reference timing, or when the reference has nothing close enough
        private static float? FindReferenceBeat(float beat, List<float> reference, Dictionary<float, float> moved)
        {
            int index = reference.BinarySearch(beat);
            if (index < 0) index = ~index;

            float? closest = null;
            for (int i = Math.Max(0, index - 1); i <= Math.Min(reference.Count - 1, index); i++)
            {
                if (closest == null || Math.Abs(reference[i] - beat) < Math.Abs(closest.Value - beat)) closest = reference[i];
            }
            if (closest != null && Math.Abs(closest.Value - beat) <= SameBeatTolerance) return null;

            var fix = moved.Where(m => Math.Abs(m.Key - beat) <= SameBeatTolerance).Select(m => (float?)m.Value).FirstOrDefault();
            if (fix != null) return fix;

            if (closest != null && Math.Abs(closest.Value - beat) <= MaxMoveBeats + SameBeatTolerance) return closest;
            return null;
        }

        // Null when the object already matches the reference, or when the reference has nothing close enough.
        // The start follows the same rules as the swings, then the end follows the reference object at that start, however far it is.
        private static TimingChange FindReferenceObject(SnapshotObject obj, List<SnapshotObject> references, Dictionary<float, float> moved)
        {
            float? newBeat = FindReferenceBeat(obj.Beat, references.Select(r => r.Beat).ToList(), moved);
            if (newBeat == null && !references.Any(r => SameBeat(r.Beat, obj.Beat))) return null;
            float start = newBeat ?? obj.Beat;

            float? newEndBeat = null;
            if (obj.EndBeat != null)
            {
                // A reference at the same place is preferred, then the one with the closest end (stacked walls, arcs from the same note)
                float? end = references
                    .Where(r => SameBeat(r.Beat, start) && r.EndBeat != null)
                    .OrderBy(r => r.PosX == obj.PosX && r.PosY == obj.PosY && r.Direction == obj.Direction ? 0 : 1)
                    .ThenBy(r => Math.Abs(r.EndBeat.Value - obj.EndBeat.Value))
                    .FirstOrDefault()?.EndBeat;
                if (end != null && !SameBeat(end, obj.EndBeat)) newEndBeat = end;
            }

            if (newBeat == null && newEndBeat == null) return null;
            return new()
            {
                Kind = obj.Kind,
                Color = obj.Color,
                OriginalBeat = obj.Beat,
                NewBeat = start,
                OriginalEndBeat = obj.EndBeat,
                NewEndBeat = newEndBeat ?? obj.EndBeat
            };
        }

        // Sorted beats of the first note of each swing, both hands together
        private static List<float> GetSwingStarts(IEnumerable<BaseNote> notes, Timescale timescale)
        {
            List<float> starts = new();
            foreach (var hand in notes.Where(n => n.Type == (int)NoteType.Red || n.Type == (int)NoteType.Blue).GroupBy(n => n.Type))
            {
                float? previousTime = null;
                foreach (var note in hand.OrderBy(n => n.JsonTime))
                {
                    float time = timescale.ToRealTime(note.JsonTime);
                    if (previousTime == null || time - previousTime.Value > SameSwingSeconds) starts.Add(note.JsonTime);
                    previousTime = time;
                }
            }
            starts.Sort();
            return starts;
        }

        private static Timescale CreateTimescale(IEnumerable<BaseBpmEvent> bpmEvents)
        {
            List<BpmEvent> bpmChanges = bpmEvents.Select(b => new BpmEvent { Beats = b.JsonTime, Bpm = b.Bpm }).ToList();
            return Timescale.Create(BeatSaberSongContainer.Instance.Info.BeatsPerMinute, bpmChanges, BeatSaberSongContainer.Instance.Info.SongTimeOffset);
        }

        public string ExportText()
        {
            StringBuilder text = new();
            if (Changes != null)
            {
                text.AppendLine($"{SnapshotDifficulty} {SnapshotCharacteristic}");
                AppendChanges(text, Changes);
            }
            if (OtherDifficultyChecks != null)
            {
                foreach (var check in OtherDifficultyChecks)
                {
                    if (text.Length > 0) text.AppendLine();
                    text.AppendLine(check.Name);
                    if (check.Error != null) text.AppendLine($"Could not be checked: {check.Error}");
                    else AppendChanges(text, check.Changes);
                }
            }
            return text.ToString().TrimEnd();
        }

        private static void AppendChanges(StringBuilder text, List<TimingChange> changes)
        {
            if (changes.Count == 0)
            {
                text.AppendLine("No timing change");
                return;
            }

            text.AppendLine("Original beat - New beat");
            // Objects on the same beat moved the same way (stacks, arcs and chains on a note) only need one line
            var lines = changes
                .OrderBy(c => c.OriginalBeat ?? c.NewBeat)
                .ThenBy(c => c.OriginalBeat == null)
                .Select(c =>
                {
                    string kind = c.Kind.ToString().ToLower();
                    if (c.NewBeat == null) return $"{FormatBeat(c.OriginalBeat.Value)} - Remove {kind}";
                    if (c.OriginalBeat == null) return $"{FormatBeat(c.NewBeat.Value)} - Add {kind}";
                    string line = $"{FormatBeat(c.OriginalBeat.Value)} - {FormatBeat(c.NewBeat.Value)}";
                    if (!SameBeat(c.OriginalEndBeat, c.NewEndBeat))
                    {
                        string end = c.Kind == SnapshotKind.Wall ? "end" : "tail";
                        line += $" ({kind} {end} {FormatBeat(c.OriginalEndBeat.Value)} - {FormatBeat(c.NewEndBeat.Value)})";
                    }
                    return line;
                })
                .Distinct();
            foreach (var line in lines) text.AppendLine(line);
        }

        public static string FormatBeat(float beat) => Math.Round(beat, 3).ToString(CultureInfo.InvariantCulture);
    }
}

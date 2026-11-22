using BLMapCheck.Classes.Results;
using beatleader_parser.VNJS;
using BLMapCheck.Classes.ChroMapper;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using Parser.Map.Difficulty.V3.Event;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class NJS
    {
        // NJS and VNJS criteria
        // Sine, Cubic, Quart, Quint and Expo (4 to 18) are unsupported
        private static bool IsValidEasing(int easing) => easing == -1 || (easing >= 0 && easing <= 3) || (easing >= 19 && easing <= 30) || (easing >= 100 && easing <= 102);

        private const double SampleStep = 0.01;

        // The jump duration doesn't change with the NJS, so an object is (time before hit) * (current NJS) away from the player.
        // Objects that would normally be far away but come within hittable range because of a lower NJS are returned.
        private static List<BeatmapGridObject> ObjectsPulledInRange(CheckContext ctx, float baseNjs, float offset, List<BeatmapGridObject> objects, List<NjsEvent> njsEvents)
        {
            var result = new List<BeatmapGridObject>();
            if (njsEvents == null || njsEvents.Count == 0 || objects.Count == 0) return result;

            var vnjs = new VNJS(baseNjs, njsEvents);
            var bpm = ctx.Map.Info._beatsPerMinute;
            double halfJump = SpawnParameterHelper.CalculateHalfJumpDuration(baseNjs, offset, bpm) * 60 / bpm;
            double range = ctx.Config.VNJSHittableRange;

            // NJS sampled over the whole map
            double last = objects.Max(o => o.Seconds);
            var samples = new float[(int)(last / SampleStep) + 2];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = vnjs.NjsAtSeconds((float)(i * SampleStep));
            }

            foreach (var obj in objects)
            {
                for (double t = System.Math.Max(0, obj.Seconds - halfJump); t < obj.Seconds; t += SampleStep)
                {
                    double timeLeft = obj.Seconds - t;
                    double distance = timeLeft * samples[(int)(t / SampleStep)];
                    if (distance <= range && timeLeft * obj.njs >= ctx.Config.VNJSFarDistance)
                    {
                        result.Add(obj);
                        break;
                    }
                }
            }
            return result.OrderBy(o => o.Beats).ToList();
        }

        // The NJS every SampleStep seconds from the start of the map until after the last NJS event or object, with the NJS event
        // controlling it (the next event while easing towards it, or the last one)
        private static List<(float Njs, NjsEvent Event)> NjsCurve(float baseNjs, List<NjsEvent> njsEvents, List<BeatmapGridObject> objects)
        {
            var curve = new List<(float Njs, NjsEvent Event)>();
            if (njsEvents == null || njsEvents.Count == 0)
            {
                curve.Add((baseNjs, null));
                return curve;
            }
            var vnjs = new VNJS(baseNjs, njsEvents);
            var events = njsEvents.OrderBy(e => e.Seconds).ToList();
            double end = System.Math.Max(events[events.Count - 1].Seconds, objects.Count > 0 ? objects.Max(o => o.Seconds) : 0) + 1;
            for (double t = 0; t <= end; t += SampleStep)
            {
                var next = events.FirstOrDefault(e => e.Seconds > t && e.Easing != -1);
                var last = events.LastOrDefault(e => e.Seconds <= t);
                var controlling = next != null && (last == null || events.IndexOf(next) == events.IndexOf(last) + 1) ? next : last;
                curve.Add((vnjs.NjsAtSeconds((float)t), controlling));
            }
            return curve;
        }

        public static CritResult Check(CheckContext ctx, float njs, float noteJumpStartBeatOffset, List<BeatmapGridObject> allObjects, List<NjsEvent> njsEvents)
        {
            CritResult criteria = CritResult.Success;

            // NJS events may not use invalid or unsupported easings
            var badEasing = (njsEvents ?? new()).Where(e => !IsValidEasing(e.Easing)).ToList();
            if (badEasing.Count > 0)
            {
                ctx.Results.CreateDiffResult("NJS Easing", Severity.Error, "NoteJumpSpeed", "NJS events may not use invalid or unsupported easings (Sine, Cubic, Quart, Quint, Expo)",
                    new() { new("Easings", string.Join(", ", badEasing.Select(e => e.Easing).Distinct())) }, new(badEasing));
                criteria = CritResult.Fail;
            }

            // The base NJS need to be a positive number
            if (njs <= 0)
            {
                ctx.Results.CreateDiffResult("Note Jump Speed", Severity.Error, "NoteJumpSpeed", "Note Jump Speed cannot be lower or equal to 0.", 
                    new() { new("CurrentNoteJumpSpeed", njs.ToString()), new("MinimumNoteJumpSpeed", "0") });
                criteria = CritResult.Fail;
            }

            // NJS must not reach below 1 at any point in the map, values below 4 NJS may require justification.
            // The NJS curve is sampled over the whole map, not only where objects are
            var curve = NjsCurve(njs, njsEvents, allObjects);
            foreach (var (limit, severity, message) in new[] {
                (1.0, Severity.Error, "NJS must not reach below 1 at any point in the map"),
                (4.0, Severity.Warning, "Values below 4 NJS may require justification") })
            {
                var below = curve.Where(c => c.Njs < limit).ToList();
                if (below.Count == 0) continue;
                // The objects below the limit, and the NJS events making it go below
                List<BeatmapObject> objects = allObjects.Where(o => o.njs < limit).Cast<BeatmapObject>()
                    .Concat(below.Where(c => c.Event != null).Select(c => (BeatmapObject)c.Event)).Distinct().OrderBy(o => o.Beats).ToList();
                ctx.Results.CreateDiffResult("Note Jump Speed", severity, "NoteJumpSpeed", message,
                    new() { new("MinimumNJS", System.Math.Round(below.Min(c => c.Njs), 2).ToString()) }, objects);
                if (severity == Severity.Error) criteria = CritResult.Fail;
                else if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            // The base NJS value must be between the minimum and maximum NJS used in interactable portions of the map
            List<float> allNjs = allObjects.Select(obj => obj.njs).ToList();
            if (njs < allNjs.Min() || njs > allNjs.Max())
            {
                ctx.Results.CreateDiffResult("Note Jump Speed", Severity.Error, "NoteJumpSpeed", "The base NJS value must be between the minimum and maximum NJS used in interactable portions of the map");
                criteria = CritResult.Fail;
            }

            // The base NJS value should match what is most used within the map
            var mostUsed = allObjects.Where(o => !(o is Wall)).GroupBy(o => System.Math.Round(o.njs, 2)).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
            if (mostUsed != null && System.Math.Abs(njs - mostUsed.Value) > 0.005)
            {
                ctx.Results.CreateDiffResult("Note Jump Speed", Severity.Warning, "NoteJumpSpeed", "The base NJS value should match the NJS most used within the map",
                    new() { new("BaseNJS", njs.ToString()), new("MostUsedNJS", mostUsed.Value.ToString()) });
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
            }

            // NJS changes cannot place future objects within hittable range
            var pulled = ObjectsPulledInRange(ctx, njs, noteJumpStartBeatOffset, allObjects.Where(o => !(o is Wall)).ToList(), njsEvents);
            foreach (var obj in pulled)
            {
                ctx.Results.CreateDiffResult("NJS Hittable Range", Severity.Error, "NoteJumpSpeed", "NJS changes cannot bring objects that are far away into hittable range (1.5m)", new(), new() { obj });
                criteria = CritResult.Fail;
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Note Jump Speed", Severity.Passed, "NoteJumpSpeed", "No issue with NJS detected.");
            }

            return criteria;
        }
    }
}

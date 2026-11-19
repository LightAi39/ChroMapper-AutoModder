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

            // NJS must not reach below 1 at any point in the map
            List<BeatmapObject> objects = new();
            foreach (var obj in allObjects)
            {
                if (obj.njs < 1)
                {
                    objects.Add(obj);
                }
            }
            if (objects.Count > 0)
            {
                ctx.Results.CreateDiffResult("Note Jump Speed", Severity.Error, "NoteJumpSpeed", "NJS must not reach below 1 at any point in the map", new(), objects);
                criteria = CritResult.Fail;
            }

            // Values below 4 NJS may require justification
            objects = new();
            foreach (var obj in allObjects)
            {
                if (obj.njs < 4)
                {
                    objects.Add(obj);
                }
            }
            if (objects.Count > 0)
            {
                ctx.Results.CreateDiffResult("Note Jump Speed", Severity.Warning, "NoteJumpSpeed", "Values below 4 NJS may require justification", new(), objects);
                if (CritResult.Warning > criteria) criteria = CritResult.Warning;
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

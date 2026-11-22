using beatleader_parser.Timescale;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Event;
using Parser.Map.Difficulty.V3.Event.V3;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class Lights
    {
        // Light events that light up the environment (5 is the color boost)
        internal static readonly HashSet<int> LightTypes = new() { 0, 1, 2, 3, 4, 6, 7, 10, 11 };

        // Fetch the average event per beat, and compare it to a configurable value
        public static CritResult Check(CheckContext ctx, float songLength, List<Light> events, List<Lightcoloreventboxgroup> v3events)
        {
            CritResult criteria = CritResult.Success;
            Timescale timescale = ctx.Timescale;

            if (songLength == 0)
            {
                ctx.Results.CreateDiffResult("Light", Severity.Error, "Light", "Light check error, SongLength is 0. Make sure to use an ogg file");
                return CritResult.Fail;
            }

            // Calculate the length of the song in beats
            var end = timescale.BPM.ToBeatTime(songLength, true);

            // Check if there's lights in the map
            if ((!events.Any() || !events.Exists(e => LightTypes.Contains(e.Type))) && !v3events.Any())
            {
                ctx.Results.CreateDiffResult("Light", Severity.Error, "Light", "The map must have sufficient lighting throughout");
                return CritResult.Fail;
            }
            else
            {
                // Calculate the average amount of light per beat
                // Basic light events, plus every color event of every box of the V3 light groups
                var lights = events.Count(e => LightTypes.Contains(e.Type));
                var v3lights = v3events.Sum(g => (g.EventBoxGroup ?? new()).Sum(b => b.e?.Count ?? 0));
                var average = (lights + v3lights) / end;
                if (average < ctx.Config.AverageLightPerBeat)
                {
                    ctx.Results.CreateDiffResult("Average Light", Severity.Error, "Light", "The map must have sufficient lighting throughout", 
                        new() { new("CurrentAvgLightPerBeat", average.ToString()), new("RequiredAvgLightPerBeat", ctx.Config.AverageLightPerBeat.ToString()) });
                    criteria = CritResult.Fail;
                }

                if (criteria == CritResult.Success)
                {
                    ctx.Results.CreateDiffResult("Light", Severity.Passed, "Light", "Map has enough light per beat in average",
                        new() { new("CurrentAvgLightPerBeat", average.ToString()), new("RequiredAvgLightPerBeat", ctx.Config.AverageLightPerBeat.ToString()) });
                }
            }

            return criteria;
        }
    }
}

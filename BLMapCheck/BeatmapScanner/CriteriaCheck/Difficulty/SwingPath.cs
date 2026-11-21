using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SwingPath
    {
        // The pre-swing of a note may not be blocked by a note or chain of the opposite color
        public static CritResult Check(CheckContext ctx, List<BeatmapGridObject> beatmapGridObjects, List<Note> notes, List<Chain> chains, List<SwingInfo> swings)
        {
            CritResult criteria = CritResult.Success;

            if (beatmapGridObjects.Any())
            {
                // The pre-swing of a note may not be blocked by a note, chain head or link of the opposite color.
                // The pre-swing is the cell behind the note, only objects at about the same time block it
                var swingDirection = SwingInfo.DirectionByNote(swings);
                // Only the notes starting a swing have a pre-swing, the others are reached from the previous note of the swing
                var swingStarts = new HashSet<Note>(swings.SelectMany(s => s.Notes.Where(n => n.Beats - s.Notes[0].Beats < 0.001)));
                var links = chains.SelectMany(c => FusedObject.GetLinks(c).Select(l => (Chain: c, l.X, l.Y, l.Beats))).ToList();
                foreach (var note in notes)
                {
                    if ((note.Color != 0 && note.Color != 1) || !swingStarts.Contains(note)) continue;
                    var degree = CutDegree(note, swingDirection);
                    if (degree == null) continue;
                    var (dx, dy) = GridStep(degree.Value);
                    var path = new HashSet<(int, int)> { (note.x - dx, note.y - dy) };
                    var window = ctx.SecondsToBeats(0.01, note.Beats);

                    BeatmapGridObject blocker = InRange(notes, note.Beats - window, note.Beats + window)
                        .FirstOrDefault(n => n.Color == 1 - note.Color && path.Contains((n.x, n.y)));
                    blocker ??= links.FirstOrDefault(l => l.Chain.Color == 1 - note.Color && Math.Abs(l.Beats - note.Beats) <= window && path.Contains((l.X, l.Y))).Chain;
                    if (blocker != null)
                    {
                        ctx.Results.CreateDiffResult("Swing Path", Severity.Error, "Swing", "The pre-swing of a note may not be blocked by a note or chain of the opposite color", new(), new() { note, blocker });
                        criteria = CritResult.Fail;
                    }
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Swing Path", Severity.Passed, "Swing", "No issue with swing path detected");
            }

            return criteria;
        }
    }
}

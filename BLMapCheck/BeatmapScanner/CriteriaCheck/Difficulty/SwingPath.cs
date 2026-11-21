using beatleader_parser.Timescale;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;
using SwingType = BLMapCheck.BeatmapScanner.MapCheck.SwingType;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SwingPath
    {
        private static readonly double[,] angle = { { 45, 1 }, { 15, 2 } };
        private static readonly double[,] angle2 = { { 45, 1 }, { 15, 1.5 } };

        // Check if a note block the swing path of another note of a different color
        public static CritResult Check(CheckContext ctx, List<BeatmapGridObject> beatmapGridObjects, List<Note> notes, List<Chain> chains, List<SwingInfo> swings)
        {
            CritResult criteria = CritResult.Success;
            Timescale timescale = ctx.Timescale;

            if (beatmapGridObjects.Any())
            {
                // List to store found notes that are possible swing path issue
                List<Note> found = new();
                var lastTime = 0d;

                // TODO: Add comments to this, or come up with a better algo
                for (int i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var current = beatmapGridObjects[i];
                    if (current is not Note || timescale.BPM.ToRealTime(current.Beats) < lastTime + 0.01)
                    {
                        continue;
                    }
                    for (int j = i + 1; j < beatmapGridObjects.Count; j++)
                    {
                        var curr = (Note)current;
                        var compareTo = beatmapGridObjects[j];
                        if (timescale.BPM.ToRealTime(compareTo.Beats) > timescale.BPM.ToRealTime(current.Beats) + 0.01)
                        {
                            break;
                        }
                        if(compareTo is not Note)
                        {
                            continue;
                        }
                        var comp = (Note)compareTo;
                        if (curr.Color == comp.Color)
                        {
                            continue;
                        }
                        var IsDiagonal = false;
                        var dX = Math.Abs(curr.x - comp.x);
                        var dY = Math.Abs(curr.y - comp.y);
                        if (dX == dY)
                        {
                            IsDiagonal = true;
                        }
                        var d = Math.Sqrt(Math.Pow(curr.x - comp.x, 2) + Math.Pow(curr.y - comp.y, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {
                            if (curr.CutDirection == comp.CutDirection && SwingType.Diagonal.Contains(curr.CutDirection))
                            {
                                found.Add(curr);
                                lastTime = timescale.BPM.ToRealTime(curr.Beats);
                                continue;
                            }
                        }
                        if (d >= 1.001 && d < 1.5 && IsDiagonal)
                        {
                            var pos = (curr.x, curr.y);
                            var target = (comp.x, comp.y);
                            var index = 1;
                            var rev = Reverse.Get(curr.CutDirection);
                            var count = found.Count;
                            if (curr.CutDirection != 8)
                            {
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(curr, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        found.Add(curr);
                                        lastTime = timescale.BPM.ToRealTime(curr.Beats);
                                        break;
                                    }
                                }
                                if(count != found.Count)
                                {
                                    continue;
                                }
                            }
                            if (comp.CutDirection != 8)
                            {
                                target = (curr.x, curr.y);
                                pos = (comp.x, comp.y);
                                index = 1;
                                rev = Reverse.Get(comp.CutDirection);
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(comp, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        found.Add(curr);
                                        lastTime = timescale.BPM.ToRealTime(curr.Beats);
                                        break;
                                    }
                                }
                                if (count != found.Count)
                                {
                                    continue;
                                }
                            }
                        }
                        if (((curr.y == comp.y || curr.x == comp.x) && Swing.IsIntersect(curr, comp, angle, 2)) ||
                                (IsDiagonal && Swing.IsIntersect(curr, comp, angle2, 2)))
                        {
                            found.Add(curr);
                            lastTime = timescale.BPM.ToRealTime(curr.Beats);
                        }
                    }
                }
                foreach (var item in found)
                {
                    ctx.Results.CreateDiffResult("Swing Path", Severity.Error, "Swing", "Swing path issue", new(), new() { item });
                    criteria = CritResult.Fail;
                }

                // The pre-swing of a note may not be blocked by a note, chain head or link of the opposite color.
                // The pre-swing is the cell behind the note, only objects at about the same time block it
                var swingDirection = SwingInfo.DirectionByNote(swings);
                // Only the notes starting a swing have a pre-swing, the others are reached from the previous note of the swing
                var swingStarts = new HashSet<Note>(swings.SelectMany(s => s.Notes.Where(n => n.Beats - s.Notes[0].Beats < 0.001)));
                var links = chains.SelectMany(c => FusedObject.GetLinks(c).Select(l => (Chain: c, l.X, l.Y, l.Beats))).ToList();
                foreach (var note in notes)
                {
                    if ((note.Color != 0 && note.Color != 1) || found.Contains(note) || !swingStarts.Contains(note)) continue;
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

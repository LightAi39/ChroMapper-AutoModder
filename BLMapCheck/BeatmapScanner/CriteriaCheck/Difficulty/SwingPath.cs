using beatleader_parser.Timescale;
using BLMapCheck.BeatmapScanner.MapCheck;
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
        public static CritResult Check(CheckContext ctx, List<BeatmapGridObject> beatmapGridObjects, List<Note> notes)
        {
            CritResult criteria = CritResult.Success;
            Timescale timescale = ctx.Timescale;

            if (beatmapGridObjects.Any())
            {
                // No dot support for now
                if (ctx.Config.DisplayBadcut)
                {
                    // Find the double and group them together
                    var doubleNotes = notes.Where(n => n.Color == 0 || n.Color == 1).GroupBy(n => n.Beats)
                        .Where(g => g.Any(n => n.Color == 0) && g.Any(n => n.Color == 1))
                        .Select(g => g.ToList()).ToList();

                    foreach (var group in doubleNotes)
                    {
                        for (int i = 0; i < group.Count; i++)
                        {
                            var note = group[i];
                            for (int j = 0; j < group.Count; j++)
                            {
                                if (i == j) continue;
                                var note2 = group[j];
                                if (note.Beats != note2.Beats) break; // Not a double anymore
                                if (note.Color == note2.Color) continue; // Same color
                                // Fetch previous note, simulate swing
                                var previous = notes.Where(c => c.Beats < note2.Beats && c.Color == note2.Color).LastOrDefault();
                                if (previous != null)
                                {
                                    // This is calculating the angle between the previous note + extra swing and the next note
                                    if (note2.CutDirection != 8 && previous.CutDirection != 8)
                                    {
                                        var angleOfAttack = DirectionToDegree[note2.CutDirection];
                                        var prevDirection = ReverseCutDirection(DirectionToDegree[previous.CutDirection]);
                                        var di = Math.Abs(prevDirection - angleOfAttack);
                                        di = Math.Min(di, 360 - di) / 4;
                                        if (angleOfAttack < prevDirection)
                                        {
                                            if (prevDirection - angleOfAttack < 180)
                                            {
                                                angleOfAttack += di;
                                            }
                                            else
                                            {
                                                angleOfAttack -= di;
                                            }
                                        }
                                        else
                                        {
                                            if (angleOfAttack - prevDirection < 180)
                                            {
                                                angleOfAttack -= di;
                                            }
                                            else
                                            {
                                                angleOfAttack += di;
                                            }
                                        }
                                        // Simulate the position of the line based on the new angle found
                                        var simulatedLineOfAttack = SimSwingPos(note2.x, note2.y, ReverseCutDirection(angleOfAttack), 2);
                                        // Check if the other note is close to the line
                                        var InPath = NearestPointOnFiniteLine(new(note2.x, note2.y), new((float)simulatedLineOfAttack.x, (float)simulatedLineOfAttack.y), new(note.x, note.y));
                                        if (InPath)
                                        {
                                            var obj = beatmapGridObjects.Where(c => c.Beats == note.Beats && note.x == c.x && note.y == c.y).FirstOrDefault();

                                            ctx.Results.CreateDiffResult("Swing Path", Severity.Data, "Swing", "Possible swing path issue", new(), new() { obj });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

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
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Swing Path", Severity.Passed, "Swing", "No issue with swing path detected");
            }

            return criteria;
        }
    }
}

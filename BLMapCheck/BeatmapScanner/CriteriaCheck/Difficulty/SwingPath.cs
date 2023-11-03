using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using BLMapCheck.Classes.Unity;
using JoshaParity;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using SwingData = JoshaParity.SwingData;
using SwingType = BLMapCheck.BeatmapScanner.MapCheck.SwingType;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class SwingPath
    {
        public static bool NearestPointOnFiniteLine(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 direction = B - A;
            Vector2 pointAP = P - A;

            float t = Vector2.Dot(pointAP, direction) / Vector2.Dot(direction, direction);
            if (t < 0)
            {
                // Before A
            }
            else if (t > 1)
            {
                // After B
                Vector2 closestPoint = B;
                float distance = Vector2.Distance(P, closestPoint);
                if (distance < 0.4) return true;
            }
            else
            {
                // In between
                Vector2 closestPoint = A + direction * t;
                float distance = Vector2.Distance(P, closestPoint);
                if (distance < 0.4) return true;
            }
            return false;
        }

        // Check if a note block the swing path of another note of a different color
        public static CritResult Check(List<BeatmapGridObject> beatmapGridObjects, List<SwingData> Swings)
        {
            var issue = CritResult.Success;

            if (beatmapGridObjects.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                List<List<Cube>> doubleNotes = new();
                foreach (var note in cubes) // Find the double and group them together
                {
                    var n = cubes.Where(n => n.Time == note.Time && n != note && ((n.Type == 0 && note.Type == 1) || (n.Type == 1 && note.Type == 0))).FirstOrDefault();
                    if (n != null)
                    {
                        if (doubleNotes.Count == 0)
                        {
                            doubleNotes.Add(new());
                        }
                        else if (!doubleNotes.Last().Exists(x => x.Time == n.Time))
                        {
                            doubleNotes.Add(new());
                            doubleNotes[doubleNotes.Count - 1] = new();
                        }
                        if (!doubleNotes.Last().Contains(note)) doubleNotes.Last().Add(note);
                        if (!doubleNotes.Last().Contains(n)) doubleNotes.Last().Add(n);
                    }
                }

                // No dot support for now
                foreach (var group in doubleNotes)
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        var note = group[i];
                        for (int j = 0; j < group.Count; j++)
                        {
                            if (i == j) continue;
                            var note2 = group[j];
                            if (note.Time != note2.Time) break; // Not a double anymore
                            if (note.Type == note2.Type) continue; // Same color
                                                                   // Fetch previous note, simulate swing
                            var previous = cubes.Where(c => c.Time < note2.Time && c.Type == note2.Type).LastOrDefault();
                            if (previous != null)
                            {
                                var angleOfAttack = note2.Direction;
                                var prevDirection = ScanMethod.ReverseCutDirection(previous.Direction);
                                // This is calculating the angle between the previous note + extra swing and the next note
                                if (note2.CutDirection != 8 && previous.CutDirection != 8)
                                {
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
                                    var simulatedLineOfAttack = ScanMethod.SimulateSwingPos(note2.Line, note2.Layer, ScanMethod.ReverseCutDirection(angleOfAttack), 2);
                                    // Check if the other note is close to the line
                                    var InPath = NearestPointOnFiniteLine(new(note2.Line, note2.Layer), new((float)simulatedLineOfAttack.x, (float)simulatedLineOfAttack.y), new(note.Line, note.Layer));
                                    if (InPath)
                                    {
                                        var obj = beatmapGridObjects.Where(c => c.b == note.Time && note.Line == c.x && note.Layer == c.y).FirstOrDefault();
                                        CheckResults.Instance.AddResult(new CheckResult()
                                        {
                                            Characteristic = CriteriaCheckManager.Characteristic,
                                            Difficulty = CriteriaCheckManager.Difficulty,
                                            Name = "Swing Path",
                                            Severity = Severity.Info,
                                            CheckType = "Swing",
                                            Description = "Possible swing path issue.",
                                            ResultData = new() { new("SwingPath", "Possible swing path issue") },
                                            BeatmapObjects = new() { obj }
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                List<Colornote> arr = new();
                List<Colornote> arr2 = new();
                var lastTime = 0d;

                for (int i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var current = beatmapGridObjects[i];
                    if (current is not Colornote || (current.b / BeatPerMinute.BPM.GetValue() * 60) < lastTime + 0.01)
                    {
                        continue;
                    }
                    for (int j = i + 1; j < beatmapGridObjects.Count; j++)
                    {
                        var curr = (Colornote)current;
                        var compareTo = beatmapGridObjects[j];
                        if ((compareTo.b / BeatPerMinute.BPM.GetValue() * 60) > (current.b / BeatPerMinute.BPM.GetValue() * 60) + 0.01)
                        {
                            break;
                        }
                        if(compareTo is not Colornote)
                        {
                            continue;
                        }
                        var comp = (Colornote)compareTo;
                        if (curr.c == comp.c)
                        {
                            continue;
                        }
                        double[,] angle = { { 45, 1 }, { 15, 2 } };
                        double[,] angle2 = { { 45, 1 }, { 15, 1.5 } };
                        var IsDiagonal = false;
                        var dX = Math.Abs(curr.x - comp.x);
                        var dY = Math.Abs(curr.y - comp.y);
                        if (dX == dY)
                        {
                            IsDiagonal = true;
                        }
                        var a = Swings.Where(x => x.notes.Any(y => y.b == curr.b && y.c == curr.c && y.d == curr.d && y.x == curr.x && y.y == curr.y)).FirstOrDefault();
                        var b = Swings.Where(x => x.notes.Any(y => y.b == comp.b && y.c == comp.c && y.d == comp.d && y.x == comp.x && y.y == comp.y)).FirstOrDefault();
                        var d = Math.Sqrt(Math.Pow(curr.x - comp.x, 2) + Math.Pow(curr.y - comp.y, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {
                            if (curr.d == comp.d && SwingType.Diagonal.Contains(curr.d))
                            {
                                arr.Add(curr);
                                lastTime = (curr.b / BeatPerMinute.BPM.GetValue() * 60);
                                continue;
                            }
                        }
                        if (IsDiagonal)
                        {
                            var pos = (curr.x, curr.y);
                            var target = (comp.x, comp.y);
                            var index = 1;
                            var rev = Reverse.Get(curr.d);
                            if (curr.d != 8)
                            {
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(curr, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        arr2.Add(curr);
                                        lastTime = (curr.b / BeatPerMinute.BPM.GetValue() * 60);
                                        continue;
                                    }
                                }
                            }
                            if (comp.d != 8)
                            {
                                target = (curr.x, curr.y);
                                pos = (comp.x, comp.y);
                                index = 1;
                                rev = Reverse.Get(comp.d);
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(comp, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        arr2.Add(curr);
                                        lastTime = (curr.b / BeatPerMinute.BPM.GetValue() * 60);
                                        continue;
                                    }
                                }
                            }
                        }
                        if (((curr.y == comp.y || curr.x == comp.x) && Swing.IsIntersect(curr, comp, angle, 2)) ||
                                (IsDiagonal && Swing.IsIntersect(curr, comp, angle2, 2)))
                        {
                            arr.Add(curr);
                            lastTime = (curr.b / BeatPerMinute.BPM.GetValue() * 60);
                        }
                    }
                }
                foreach (var item in arr)
                {
                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = CriteriaCheckManager.Characteristic,
                        Difficulty = CriteriaCheckManager.Difficulty,
                        Name = "Swing Path",
                        Severity = Severity.Error,
                        CheckType = "Swing",
                        Description = "Swing path issue.",
                        ResultData = new() { new("SwingPath", "Error") },
                        BeatmapObjects = new() { item }
                    });
                    issue = CritResult.Fail;
                }
            }

            if (issue == CritResult.Success)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Characteristic = CriteriaCheckManager.Characteristic,
                    Difficulty = CriteriaCheckManager.Difficulty,
                    Name = "Swing Path",
                    Severity = Severity.Passed,
                    CheckType = "Swing",
                    Description = "No issue with swing path detected.",
                    ResultData = new() { new("SwingPath", "Success") }
                });
            }

            return issue;
        }
    }
}

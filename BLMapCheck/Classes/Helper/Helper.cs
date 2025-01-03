using BLMapCheck.Classes.Unity;
using BLMapCheck.Configs;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.Helper
{
    internal class Helper
    {
        public static double[] DirectionToDegree = { 90, 270, 180, 0, 135, 45, 225, 315 };
        public static double[] ChainDirToDegree = { 180, 0, -90, 90, 135, -135, -45, 45 };

        public class NoteData
        {
            public Note Note { get; set; }
            public double Direction = 0;
            public double Line = 0;
            public double Layer = 0;
            public bool Head = false;
            public bool Pattern = false;
            public double Precision = 0;
            public double Spacing = 0;

            public NoteData()
            {

            }

            public NoteData(Note note)
            {
                Note = note;
                Line = note.x;
                Layer = note.y;
            }
        }

        public static List<NoteData> NotesData = new();

        public static Note FindNote(List<Note> notes, JoshaParity.Note note)
        {
            return notes.FirstOrDefault(n => n.Beats == note.b && n.x == note.x && n.y == note.y && n.CutDirection == note.d && n.Color == note.c && n.AngleOffset == note.a);
        }

        public static void CreateNoteData(List<Note> notes, List<JoshaParity.SwingData> swingData)
        {
            NotesData = new();
            var red = swingData.Where(s => !s.rightHand).ToList();
            var blue = swingData.Where(s => s.rightHand).ToList();
            HandleSwings(notes, red);
            HandleSwings(notes, blue);
        }

        public static void HandleSwings(List<Note> notes, List<JoshaParity.SwingData> swings)
        {
            var newSwing = false;

            for (int i = 0; i < swings.Count; i++)
            {
                newSwing = true;
                var swing = swings[i];

                if ((int)swing.swingType >= 1 && (int)swing.swingType <= 3) // Slider, stack or window
                {
                    // There's a bug with arrow-less swings in JoshaParity, notes need to be re-ordered
                    swing.notes = swing.notes.OrderBy(x => x.b).ToList();

                    for (int j = 1; j < swing.notes.Count; j++)
                    {
                        var prev = swing.notes[j - 1];
                        var note = swing.notes[j];
                        var n = FindNote(notes, note);
                        if (n != null)
                        {
                            var data = new NoteData()
                            {
                                Note = n,
                                Pattern = true,
                                Precision = note.b - prev.b,
                                Spacing = Math.Max(Math.Max(Math.Abs(note.x - prev.x), Math.Abs(note.y - prev.y)) - 1, 0),
                                Line = note.x,
                                Layer = note.y
                            };
                            if (newSwing)
                            {
                                var no = FindNote(notes, prev);
                                if (no == null) break; // Couldn't find head note, ignore that swing.
                                NotesData.Add(new(no));
                                NotesData.Last().Head = true;
                                NotesData.Last().Pattern = true;
                                NotesData.Last().Precision = data.Precision;
                                NotesData.Last().Spacing = data.Spacing;
                                NotesData.Last().Note = FindNote(notes, prev);
                                NotesData.Last().Line = prev.x;
                                NotesData.Last().Layer = prev.y;
                                newSwing = false;
                            }
                            NotesData.Add(data);
                        }
                    }
                }
                else // Everything else
                {
                    foreach (var note in swing.notes)
                    {
                        var n = FindNote(notes, note);
                        if (n != null) NotesData.Add(new(n));
                    }
                }
            }
        }

        public static void SetAutoSliderPrecision()
        {
            var averageSliderDuration = NotesData.GroupBy(c => c.Precision / (c.Spacing + 1))
            .OrderByDescending(g => g.Count())
            .Last()
            .Key;
            if (averageSliderDuration != 0) Config.Instance.SliderPrecision = averageSliderDuration;
            else Config.Instance.SliderPrecision = 0.0625;
        }

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

        // https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
        public static bool DoLinesIntersect(Chain chain1, Chain chain2, double tolerance = 0.001)
        {
            double x1 = chain1.x, y1 = chain1.y;
            double x2 = chain1.tx, y2 = chain1.ty;
            double x3 = chain2.x, y3 = chain2.y;
            double x4 = chain2.tx, y4 = chain2.ty;
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance && Math.Abs(x1 - x3) < tolerance)
            {
                return false;
            }
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance && Math.Abs(y1 - y3) < tolerance)
            {
                return false;
            }
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance)
            {
                return false;
            }
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance)
            {
                return false;
            }
            double x, y;
            if (Math.Abs(x1 - x2) < tolerance)
            {
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;
                x = x1;
                y = c2 + m2 * x1;
            }
            else if (Math.Abs(x3 - x4) < tolerance)
            {
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;
                x = x3;
                y = c1 + m1 * x3;
            }
            else
            {
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

                if (!(Math.Abs(-m1 * x + y - c1) < tolerance
                    && Math.Abs(-m2 * x + y - c2) < tolerance))
                {
                    return false;
                }
            }
            if (IsInsideLine(chain1, x, y) &&
                IsInsideLine(chain2, x, y))
            {
                return true;
            }
            return false;
        }

        private static bool IsInsideLine(Chain line, double x, double y)
        {
            return (x >= line.x && x <= line.tx
                        || x >= line.tx && x <= line.x)
                   && (y >= line.y && y <= line.ty
                        || y >= line.ty && y <= line.y);
        }

        public static bool IsPointBetween(BeatmapGridObject target, Chain chain)
        {
            bool isXBetween = (target.x >= Math.Min(chain.x, chain.tx) && target.x <= Math.Max(chain.x, chain.tx));
            bool isYBetween = (target.y >= Math.Min(chain.y, chain.ty) && target.y <= Math.Max(chain.y, chain.ty));

            return isXBetween && isYBetween;
        }

        public static double Mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public static double ReverseCutDirection(double direction)
        {
            if (direction >= 180)
            {
                return direction - 180;
            }
            else
            {
                return direction + 180;
            }
        }

        public static bool IsSameDirection(double before, double after, double degree = 67.5)
        {
            before = Mod(before, 360);
            after = Mod(after, 360);

            if (Math.Abs(before - after) <= 180)
            {
                if (Math.Abs(before - after) < degree)
                {
                    return true;
                }
            }
            else
            {
                if (360 - Math.Abs(before - after) < degree)
                {
                    return true;
                }
            }

            return false;
        }

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = degrees * (Math.PI / 180f);
            return radians;
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = radians * (180f / Math.PI);
            return degrees;
        }

        public static double FindAngleViaPosition(List<NoteData> data, int next, int prev)
        {
            // Use Math.Atan2 to calculate the angle in radians
            double angle = Math.Atan2(data[prev].Layer - data[next].Layer, data[prev].Line - data[next].Line);
            angle = Mod(ConvertRadiansToDegrees(angle), 360);
            return angle;
        }

        public static (double x, double y) SimSwingPos(double x, double y, double direction, double dis = 5)
        {
            return (x + dis * Math.Cos(ConvertDegreesToRadians(direction)), y + dis * Math.Sin(ConvertDegreesToRadians(direction)));
        }

        public static Fraction RealToFraction(double value, double accuracy)
        {
            if (accuracy <= 0.0 || accuracy >= 1.0)
            {
                throw new ArgumentOutOfRangeException("accuracy", "Must be > 0 and < 1.");
            }

            int sign = Math.Sign(value);

            if (sign == -1)
            {
                value = Math.Abs(value);
            }

            // Accuracy is the maximum relative error; convert to absolute maxError
            double maxError = sign == 0 ? accuracy : value * accuracy;

            int n = (int)Math.Floor(value);
            value -= n;

            if (value < maxError)
            {
                return new Fraction(sign * n, 1);
            }

            if (1 - maxError < value)
            {
                return new Fraction(sign * (n + 1), 1);
            }

            // The lower fraction is 0/1
            int lower_n = 0;
            int lower_d = 1;

            // The upper fraction is 1/1
            int upper_n = 1;
            int upper_d = 1;

            while (true)
            {
                // The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
                int middle_n = lower_n + upper_n;
                int middle_d = lower_d + upper_d;

                if (middle_d * (value + maxError) < middle_n)
                {
                    // real + error < middle : middle is our new upper
                    upper_n = middle_n;
                    upper_d = middle_d;
                }
                else if (middle_n < (value - maxError) * middle_d)
                {
                    // middle < real - error : middle is our new lower
                    lower_n = middle_n;
                    lower_d = middle_d;
                }
                else
                {
                    // Middle is our best fraction
                    return new Fraction((n * middle_d + middle_n) * sign, middle_d);
                }
            }
        }
        public struct Fraction
        {
            public Fraction(int n, int d)
            {
                N = n;
                D = d;
            }

            public int N { get; set; }
            public int D { get; set; }
        }

        public static List<Vector3> FindChainLinksPosition(int n, Chain chain)
        {
            if (n == 0) n = 1;
            List<Vector3> list = new();
            Vector3 linkSegment;
            var head = new Vector2(chain.x, chain.y);
            var tail = new Vector2(chain.tx, chain.ty);
            var dir = (Math.PI * 2) / 360 * ChainDirToDegree[chain.Direction];
            var headDirection = new Vector2((float)Math.Sin(dir), (float)-Math.Cos(dir));
            var multiplier = (head - tail).magnitude / 2;
            var next = head + new Vector2((multiplier * headDirection.x), multiplier * headDirection.y);

            for (int j = 0; j < chain.Segment; j++)
            {
                float squish = 1;
                if (chain.Squish != 0) squish = chain.Squish;
                var interval = (float)j / n * squish;
                var path = tail - head + new Vector2(1.5f, 0);
                if (Math.Abs(Vector2.SignedAngle(new Vector2(0f, -1f), path) - ChainDirToDegree[chain.Direction]) < 0.01f)
                {
                    var pos = Vector3.LerpUnclamped(new Vector3(head.x, head.y, 0), new Vector3(tail.x, tail.y, 0), interval);
                    linkSegment = new Vector3(pos.x, pos.y, 0);
                }
                else
                {
                    var pos = ((float)Math.Pow(1 - interval, 2) * head) + (2 * (1 - interval) * interval * next) +
                                     ((float)Math.Pow(interval, 2) * tail);
                    linkSegment = new Vector3(pos.x, pos.y, 0);
                }

                list.Add(linkSegment);
            }
            return list;
        }
    }
}

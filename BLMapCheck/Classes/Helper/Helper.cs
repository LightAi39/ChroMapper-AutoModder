using BLMapCheck.Classes.Unity;
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

        public static void CreateNoteData(List<Note> notes)
        {
            NotesData = new();

            var red = notes.Where(n => n.Color == 0).ToList();
            var blue = notes.Where(n => n.Color == 1).ToList();

            if (red.Count > 2)
            {
                NotesData.Add(new(red[0]));
                for (int i = 1; i < red.Count; i++)
                {
                    if (red[i].Beats - red[i - 1].Beats <= 0.125)
                    {
                        var data = new NoteData()
                        {
                            Note = red[i],
                            Pattern = true,
                            Precision = red[i].Beats - red[i - 1].Beats,
                            Spacing = Math.Max(Math.Max(Math.Abs(red[i].x - red[i - 1].x), Math.Abs(red[i].y - red[i - 1].y)) - 1, 0),
                            Line = red[i].x,
                            Layer = red[i].y
                        };
                        if (!NotesData.Last().Pattern)
                        {
                            NotesData.Last().Head = true;
                            NotesData.Last().Pattern = true;
                            NotesData.Last().Note = red[i - 1];
                        }
                        NotesData.Add(data);
                    }
                    else
                    {
                        NotesData.Add(new(red[i]));
                    }
                }
            }

            if (blue.Count > 2)
            {
                NotesData.Add(new(blue[0]));
                for (int i = 1; i < blue.Count; i++)
                {
                    
                    if (blue[i].Beats - blue[i - 1].Beats <= 0.125)
                    {
                        var data = new NoteData()
                        {
                            Note = blue[i],
                            Pattern = true,
                            Precision = blue[i].Beats - blue[i - 1].Beats,
                            Spacing = Math.Max(Math.Max(Math.Abs(blue[i].x - blue[i - 1].x), Math.Abs(blue[i].y - blue[i - 1].y)) - 1, 0),
                            Line = blue[i].x,
                            Layer = blue[i].y
                        };
                        if (!NotesData.Last().Pattern)
                        {
                            NotesData.Last().Head = true;
                            NotesData.Last().Pattern = true;
                            NotesData.Last().Note = blue[i - 1];
                        }
                        NotesData.Add(data);
                    }
                    else
                    {
                        NotesData.Add(new(blue[i]));
                    }
                }
            }
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

        public static IEnumerable<T> Mode<T>(IEnumerable<T> input)
        {
            var dict = input.ToLookup(x => x);
            if (dict.Count == 0)
                return Enumerable.Empty<T>();
            var maxCount = dict.Max(x => x.Count());
            return dict.Where(x => x.Count() == maxCount).Select(x => x.Key);
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

        public static List<Vector3> Interpolate(int n, Chain chain)
        {
            List<Vector3> list = new();
            Vector3 linkSegment;

            var p0 = new Vector2(chain.x, chain.y);
            var p2 = new Vector2(chain.tx, chain.ty);

            var zRads = (Math.PI * 2) / 360 * Directionalize(chain.Direction).z;
            var headDirection = new Vector2((float)Math.Sin(zRads), (float)-Math.Cos(zRads));

            var interMult = (p0 - p2).magnitude / 2;
            var interPoint = p0 + new Vector2((interMult * headDirection.x), interMult * headDirection.y);

            for (int j = 0; j < chain.Segment; j++)
            {
                // This is how the game displays squish
                var gameSquish = (chain.Squish < 0.001f) ? 1f : chain.Squish;

                var t = (float)j / n;
                var tSquish = t * gameSquish;

                
                var p1 = interPoint;

                var lerpZPos = 0;

                var path = p2 - p0 + new Vector2(1.5f, 0);
                var pathAngle = Vector2.SignedAngle(new Vector2(0f, -1f), path);
                var cutAngle = Directionalize(chain.Direction).z;

                var headPointsToTail = Math.Abs(pathAngle - cutAngle) < 0.01f;

                if (headPointsToTail)
                {
                    var lerpPos = Vector3.LerpUnclamped(new Vector3(p0.x, p0.y, 0), new Vector3(p2.x, p2.y, 0), tSquish);
                    linkSegment = new Vector3(lerpPos.x, lerpPos.y, 0);
                }
                else
                {
                    // Quadratic bezier curve
                    // B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2, 0 < t < 1
                    var bezierLerp = ((float)Math.Pow(1 - tSquish, 2) * p0) + (2 * (1 - tSquish) * tSquish * p1) +
                                     ((float)Math.Pow(tSquish, 2) * p2);
                    linkSegment = new Vector3(bezierLerp.x, bezierLerp.y, lerpZPos);
                }

                list.Add(linkSegment);
            }
            return list;
        }

        internal static Vector3 Directionalize(int cutDirection)
        {
            var directionEuler = new Vector3(0, 0, 0);
            switch (cutDirection)
            {
                case 0:
                    directionEuler += new Vector3(0, 0, 180);
                    break;
                case 1:
                    directionEuler += new Vector3(0, 0, 0);
                    break;
                case 2:
                    directionEuler += new Vector3(0, 0, -90);
                    break;
                case 3:
                    directionEuler += new Vector3(0, 0, 90);
                    break;
                case 4:
                    directionEuler += new Vector3(0, 0, 135);
                    break;
                case 5:
                    directionEuler += new Vector3(0, 0, -135);
                    break;
                case 6:
                    directionEuler += new Vector3(0, 0, -45);
                    break;
                case 7:
                    directionEuler += new Vector3(0, 0, 45);
                    break;
            }

            return directionEuler;
        }
    }
}

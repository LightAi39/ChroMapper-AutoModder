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
                    swing.notes = swing.notes.OrderBy(x => x.Beats).ToList();

                    for (int j = 1; j < swing.notes.Count; j++)
                    {
                        var prev = swing.notes[j - 1];
                        var note = swing.notes[j];
                        var data = new NoteData()
                        {
                            Note = note,
                            Pattern = true,
                            Precision = note.Beats - prev.Beats,
                            Spacing = Math.Max(Math.Max(Math.Abs(note.x - prev.x), Math.Abs(note.y - prev.y)) - 1, 0),
                            Line = note.x,
                            Layer = note.y
                        };
                        if (newSwing)
                        {
                            NotesData.Add(new(note));
                            NotesData.Last().Head = true;
                            NotesData.Last().Pattern = true;
                            NotesData.Last().Precision = data.Precision;
                            NotesData.Last().Spacing = data.Spacing;
                            NotesData.Last().Note = prev;
                            NotesData.Last().Line = prev.x;
                            NotesData.Last().Layer = prev.y;
                            newSwing = false;
                        }
                        NotesData.Add(data);
                    }
                }
                else // Everything else
                {
                    foreach (var note in swing.notes)
                    {
                        NotesData.Add(new(note));
                    }
                }
            }
        }

        // 1/16, 1/20, 1/24, 1/25, 1/30, 1/32, 1/40, 1/48, 1/50, 1/60, 1/64
        public static double[] ExpectedDenominator = { 0.0625, 0.05, 0.04166666666, 0.04, 0.03333333333, 0.03125, 0.025, 0.02083333333, 0.02, 0.01666666666, 0.015625 };

        public static void SetAutoSliderPrecision()
        {
            double? averageSliderDuration = NotesData.Select(c => c.Precision / (c.Spacing + 1))?
            .Where(p => p != 0)?
            .GroupBy(p => p)?
            .OrderByDescending(g => g.Count())?
            .FirstOrDefault()?
            .Key;

            if (averageSliderDuration != null)
            {
                double closestNumber = ExpectedDenominator[0];
                double minDifference = Math.Abs((double)(averageSliderDuration - closestNumber));

                for (int i = 1; i < ExpectedDenominator.Length; i++)
                {
                    double currentNumber = ExpectedDenominator[i];
                    double currentDifference = Math.Abs((double)averageSliderDuration - currentNumber);

                    if (currentDifference < minDifference)
                    {
                        minDifference = currentDifference;
                        closestNumber = currentNumber;
                    }
                }

                Config.Instance.SliderPrecision = closestNumber;
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

        public static (int num, int den) DoubleToFraction(double value, int maxDen = 64)
        {
            int bestNum = 1;
            int bestDen = 1;
            double bestError = Math.Abs(value - 1.0);

            for (int den = 1; den <= maxDen; den++)
            {
                int num = (int)Math.Round(value * den);
                double error = Math.Abs(value - (double)num / den);

                if (error < bestError)
                {
                    bestError = error;
                    bestNum = num;
                    bestDen = den;

                    // Perfect match?
                    if (error == 0)
                        break;
                }
            }

            return (bestNum, bestDen);
        }

        public static Vector2 PointOnQuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            return ((float)Math.Pow(1 - t, 2) * p0) + (2 * (1 - t) * t * p1) + ((float)Math.Pow(t, 2) * p2);
        }

        public static float AngleOnQuadBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            Vector2 derivative = (2 * (1 - t) * (p1 - p0)) + (2 * t * (p2 - p1));
            return (float)Helper.ConvertRadiansToDegrees(Math.Atan2(derivative.x, -derivative.y));
        }

        // Code mostly taken from ArcViewer
        public static List<Vector3> FindChainLinksPosition(Chain c)
        {
            List<Vector3> list = new();
            //These are the start and end points of the bezier curve
            Vector2 startPos = new(c.x, c.y);
            Vector2 endPos = new(c.tx, c.ty);
            //The midpoint of the curve is 1/2 the distance between the start points, in the direction the chain faces
            float directDistance = Vector2.Distance(startPos, endPos);
            if (c.CutDirection >= DirectionToDegree.Length) return new();
            double cutDegree = DirectionToDegree[c.CutDirection];
            Vector2 DirectionVector = new Vector2((float)Math.Cos(Helper.ConvertDegreesToRadians(cutDegree)), (float)Math.Sin(Helper.ConvertDegreesToRadians(cutDegree)));
            Vector2 midOffset = DirectionVector * directDistance / 2f;
            Vector2 midPoint = startPos + midOffset;
            float duration = c.TailInBeats - c.Beats;
            Vector3 linkSegment;
            //Start at 1 because head note counts as a "segment"
            for (int i = 1; i < c.SliceCount; i++)
            {
                float timeProgress = (float)i / (c.SliceCount - 1);
                //Calculate beat based on time progress
                float beat = c.Beats + (duration * timeProgress);
                //Calculate position based on the chain's bezier curve
                float t = timeProgress * c.Squish;
                Vector2 linkPos = PointOnQuadBezier(startPos, midPoint, endPos, t);
                float linkAngle = AngleOnQuadBezier(startPos, midPoint, endPos, t);
                linkSegment = new Vector3(linkPos.x, linkPos.y, 0);
                list.Add(linkSegment);
            }

            return list;
        }
    }
}

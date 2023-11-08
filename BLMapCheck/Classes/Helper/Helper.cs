using beatleader_parser;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.Helper
{
    internal class Helper
    {
        public static double[] DirectionToDegree = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

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
                        var data = new NoteData
                        {
                            Note = red[i],
                            Pattern = true,
                            Precision = red[i].Beats - red[i - 1].Beats,
                            Spacing = Math.Max(Math.Max(Math.Abs(red[i].x - red[i - 1].x), Math.Abs(red[i].y - red[i - 1].y)) - 1, 0)
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
                        var data = new NoteData
                        {
                            Note = blue[i],
                            Pattern = true,
                            Precision = blue[i].Beats - blue[i - 1].Beats,
                            Spacing = Math.Max(Math.Max(Math.Abs(blue[i].x - blue[i - 1].x), Math.Abs(blue[i].y - blue[i - 1].y)) - 1, 0)
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

        public static double FindAngleViaPosition(List<NoteData> data, int index, int h)
        {
            (double x, double y) previousPosition = (data[h].Line, data[h].Layer);
            (double x, double y) currentPosition = (data[index].Line, data[index].Layer);

            var currentAngle = ReverseCutDirection(Mod(ConvertRadiansToDegrees(Math.Atan2(previousPosition.y - currentPosition.y, previousPosition.x - currentPosition.x)), 360));
            return currentAngle;
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
    }
}

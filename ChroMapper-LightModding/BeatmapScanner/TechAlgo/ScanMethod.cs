using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using ChroMapper_LightModding.BeatmapScanner.Data;

namespace ChroMapper_LightModding.BeatmapScanner
{
    internal class ScanMethod
    {
        public static int[] DirectionToDegree = { 90, 270, 180, 0, 135, 45, 225, 315, 270 };

        public static List<Vector2> CalcCurvePoints(List<Vector2> controlPoints, double interval)
        {
            int N = controlPoints.Count() - 1;
            if (N > 16)
            {
                controlPoints.RemoveRange(16, controlPoints.Count - 16);
            }

            List<Vector2> p = new();

            for (double t = 0.0; t <= 1.0; t += interval)
            {
                Vector2 point = new();
                for (int i = 0; i < controlPoints.Count; ++i)
                {
                    float bn = Bernstein(N, i, t);
                    point.x += bn * controlPoints[i].x;
                    point.y += bn * controlPoints[i].y;
                }
                p.Add(point);
            }

            return p;
        }

        public static float Bernstein(int n, int i, double t)
        {
            double t_i = Math.Pow(t, i);
            double t_n_minus_i = Math.Pow((1 - t), (n - i));

            double basis = Binomial(n, i) * t_i * t_n_minus_i;
            return (float)basis;
        }

        public static double Binomial(int n, int i)
        {
            double ni;
            double a1 = Factorial[n];
            double a2 = Factorial[i];
            double a3 = Factorial[n - i];
            ni = a1 / (a2 * a3);
            return ni;
        }

        public static readonly double[] Factorial = new double[]
        {
                1.0d,
                1.0d,
                2.0d,
                6.0d,
                24.0d,
                120.0d,
                720.0d,
                5040.0d,
                40320.0d,
                362880.0d,
                3628800.0d,
                39916800.0d,
                479001600.0d,
                6227020800.0d,
                87178291200.0d,
                1307674368000.0d,
                20922789888000.0d,
        };

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = degrees * (Math.PI / 180f);
            return (radians);
        }

        public static double ConvertRadiansToDegrees(double radians)
        {
            double degrees = radians * (180f / Math.PI);
            return (degrees);
        }

        public static ((double x, double y) entry, (double x, double y) exit) CalculateBaseEntryExit((double x, double y) position, double angle)
        {
            (double, double) entry = (position.x * 0.333333 - Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667,
                position.y * 0.333333 - Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667);

            (double, double) exit = (position.x * 0.333333 + Math.Cos(ConvertDegreesToRadians(angle)) * 0.166667f + 0.166667,
                position.y * 0.333333 + Math.Sin(ConvertDegreesToRadians(angle)) * 0.166667 + 0.166667);

            return (entry, exit);
        }

        public static double SwingAngleStrainCalc(List<SwingData> swingData, bool leftOrRight)
        {
            var strainAmount = 0d;

            for (int i = 0; i < swingData.Count(); i++)
            {
                if (swingData[i].Forehand)
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - swingData[i].Angle) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }

        public static double BezierAngleStrainCalc(List<double> angleData, bool forehand, bool leftOrRight)
        {
            var strainAmount = 0d;

            for (int i = 0; i < angleData.Count(); i++)
            {
                if (forehand)
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - angleData[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - angleData[i]) - 180)) / 180, 2);
                    }
                }
                else
                {
                    if (leftOrRight)
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(247.5 - 180 - angleData[i]) - 180)) / 180, 2);
                    }
                    else
                    {
                        strainAmount += 2 * Math.Pow((180 - Math.Abs(Math.Abs(292.5 - 180 - angleData[i]) - 180)) / 180, 2);
                    }
                }
            }

            return strainAmount;
        }

        public static double Mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static (double x, double y) SimulateSwingPos(double x, double y, double direction)
        {
            return (x + 5 * Math.Cos(ConvertDegreesToRadians(direction)), y + 5 * Math.Sin(ConvertDegreesToRadians(direction)));
        }

        public static void HandlePattern(List<Cube> cubes)
        {
            var length = 0;
            for (int n = 0; n < cubes.Count - 2; n++)
            {
                if (length > 0)
                {
                    length--;
                    continue;
                }
                if (cubes[n].Time == cubes[n + 1].Time)
                {
                    length = cubes.Where(c => c.Time == cubes[n].Time).Count() - 1;
                    var arrow = cubes.Where(c => c.CutDirection != 8 && c.Time == cubes[n].Time);
                    double direction = 0;
                    if (arrow.Count() == 0)
                    {
                        var foundArrow = cubes.Where(c => c.CutDirection != 8 && c.Time > cubes[n].Time).ToList();
                        if (foundArrow.Count() > 0)
                        {
                            direction = ReverseCutDirection(Mod(DirectionToDegree[foundArrow[0].CutDirection] + foundArrow[0].AngleOffset, 360));
                            for (int i = cubes.IndexOf(foundArrow[0]) - 1; i > n; i--)
                            {
                                if (cubes[i + 1].Time - cubes[i].Time >= 0.25)
                                {
                                    direction = ReverseCutDirection(direction);
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        direction = ReverseCutDirection(Mod(DirectionToDegree[arrow.Last().CutDirection] + arrow.Last().AngleOffset, 360));
                    }
                    (double x, double y) pos;
                    if (n > 0)
                    {
                        pos = SimulateSwingPos(cubes[n - 1].Line, cubes[n - 1].Layer, direction);
                    }
                    else
                    {
                        pos = SimulateSwingPos(cubes[0].Line, cubes[0].Layer, direction);
                    }
                    List<double> distance = new();
                    for (int i = n; i < n + length + 1; i++)
                    {
                        distance.Add(Math.Sqrt(Math.Pow(pos.y - cubes[i].Layer, 2) + Math.Pow(pos.x - cubes[i].Line, 2)));
                    }
                    for (int i = 0; i < distance.Count; i++)
                    {
                        for (int j = n; j < n + length; j++)
                        {
                            if (distance[j - n + 1] < distance[j - n])
                            {
                                Swap(cubes, j, j + 1);
                                Swap(distance, j - n + 1, j - n);
                            }
                        }
                    }
                }
            }
        }

        public static string DegreeToName(double direction)
        {
            return direction switch
            {
                double d when (d > 67.5 && d <= 112.5) => "UP",
                double d when (d > 247.5 && d <= 292.5) => "DOWN",
                double d when (d > 157.5 && d <= 202.5) => "LEFT",
                double d when ((d <= 22.5 && d >= 0) || (d > 337.5 && d < 360)) => "RIGHT",
                double d when (d > 112.5 && d <= 157.5) => "UP-LEFT",
                double d when (d > 22.5 && d <= 67.5) => "UP-RIGHT",
                double d when (d > 202.5 && d <= 247.5) => "DOWN-LEFT",
                double d when (d > 292.5 && d <= 337.5) => "DOWN-RIGHT",
                _ => "ERROR",
            };
        }

        public static double FindAngleViaPosition(List<Cube> cubes, int index, int h, double guideAngle, bool pattern)
        {
            (double x, double y) previousPosition = SimulateSwingPos(cubes[h].Line, cubes[h].Layer, guideAngle);
            (double x, double y) = (cubes[index].Line, cubes[index].Layer);

            if (pattern)
            {
                previousPosition = (cubes[h].Line, cubes[h].Layer);
            }

            var currentAngle = ReverseCutDirection(Mod(ConvertRadiansToDegrees(Math.Atan2(previousPosition.y - y, previousPosition.x - x)), 360));

            currentAngle = Math.Round(currentAngle / 45) * 45;

            if (pattern && !IsSameDirection(guideAngle, currentAngle, 67.5))
            {
                currentAngle = ReverseCutDirection(currentAngle);
            }
            else if (!pattern && IsSameDirection(guideAngle, currentAngle, 67.5))
            {
                currentAngle = ReverseCutDirection(currentAngle);
            }

            return currentAngle;
        }

        public static double FindAngleViaPosition(List<Cube> cubes, int index, int h)
        {
            (double x, double y) previousPosition = (cubes[h].Line, cubes[h].Layer);
            (double x, double y) = (cubes[index].Line, cubes[index].Layer);

            var currentAngle = ReverseCutDirection(Mod(ConvertRadiansToDegrees(Math.Atan2(previousPosition.y - y, previousPosition.x - x)), 360));

            return currentAngle;
        }

        public static bool IsSameDirection(double before, double after, double degree)
        {
            Mod(before, 360);
            Mod(after, 360);

            if (Math.Abs(before - after) <= 180)
            {
                if (Math.Abs(before - after) <= degree)
                {
                    return true;
                }
            }
            else if (Math.Abs(before - after) > 180)
            {
                if (360 - Math.Abs(before - after) <= degree)
                {
                    return true;
                }
            }

            return false;
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

        public static bool IsSlider(Cube prev, Cube next, double direction, bool dot)
        {
            if (dot && prev.Line == next.Line && prev.Layer == next.Layer)
            {
                return true;
            }

            switch (direction)
            {
                case double d when (d > 67.5 && d <= 112.5):
                    if (prev.Layer < next.Layer)
                    {
                        return true;
                    }
                    break;
                case double d when (d > 247.5 && d <= 292.5):
                    if (prev.Layer > next.Layer)
                    {
                        return true;
                    }
                    break;
                case double d when (d > 157.5 && d <= 202.5):
                    if (prev.Line > next.Line)
                    {
                        return true;
                    }
                    break;
                case double d when ((d <= 22.5 && d >= 0) || (d > 337.5 && d < 360)):
                    if (prev.Line < next.Line)
                    {
                        return true;
                    }
                    break;
                case double d when (d > 112.5 && d <= 157.5):
                    if (prev.Layer < next.Layer)
                    {
                        return true;
                    }
                    if (prev.Line > next.Line)
                    {
                        return true;
                    }
                    break;
                case double d when (d > 22.5 && d <= 67.5):
                    if (prev.Layer < next.Layer)
                    {
                        return true;
                    }
                    if (prev.Line < next.Line)
                    {
                        return true;
                    }
                    break;
                case double d when (d > 202.5 && d <= 247.5):
                    if (prev.Layer > next.Layer)
                    {
                        return true;
                    }
                    if (prev.Line > next.Line)
                    {
                        return true;
                    }
                    break;
                case double d when (d > 292.5 && d <= 337.5):
                    if (prev.Layer > next.Layer)
                    {
                        return true;
                    }
                    if (prev.Line < next.Line)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        public static bool IsInLinearPath(Cube previous, Cube current, Cube next)
        {
            var prev = CalculateBaseEntryExit((previous.Line, previous.Layer), previous.Direction);
            var curr = CalculateBaseEntryExit((current.Line, current.Layer), current.Direction);
            var nxt = CalculateBaseEntryExit((next.Line, next.Layer), next.Direction);

            var dxc = nxt.entry.x - prev.entry.x;
            var dyc = nxt.entry.y - prev.entry.y;

            var dxl = curr.exit.x - prev.entry.x;
            var dyl = curr.exit.y - prev.entry.y;

            var cross = dxc * dyl - dyc * dxl;
            if (cross != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void CalculateLinear(List<Cube> cubes)
        {
            Cube pre = cubes[1];
            Cube pre2 = cubes[0];

            cubes[0].Linear = true;
            cubes[1].Linear = true;

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (!cubes[i].Pattern || cubes[i].Head)
                {
                    if (IsInLinearPath(pre2, pre, cubes[i]))
                    {
                        cubes[i].Linear = true;
                    }

                    pre2 = pre;
                    pre = cubes[i];
                }
            }
        }

        public static float GetEBPM(List<Cube> cubes, float bpm)
        {
            #region Prep

            var previous = 0f;
            var effectiveBPM = 1000f;
            var peakBPM = 1000f;

            #endregion

            #region Algorithm

            for (int i = 1; i < cubes.Count(); i++)
            {
                if (cubes[i].Pattern && !cubes[i].Head)
                {
                    continue;
                }

                var duration = cubes[i].Time - cubes[i - 1].Time;

                if (IsSameDirection(cubes[i - 1].Direction, cubes[i].Direction, 67.5))
                {
                    duration /= 2;
                }

                if (duration > 0)
                {
                    if (previous >= duration - 0.01 && previous <= duration + 0.01 && duration < effectiveBPM)
                    {
                        effectiveBPM = duration;
                    }

                    if (duration < peakBPM)
                    {
                        peakBPM = duration;
                    }

                    previous = duration;
                }
            }

            #endregion

            if (effectiveBPM == 1000)
            {
                effectiveBPM = peakBPM;
            }

            effectiveBPM = 0.5f / effectiveBPM * bpm;

            return effectiveBPM;
        }

        public static IEnumerable<T> Mode<T>(IEnumerable<T> input)
        {
            var dict = input.ToLookup(x => x);
            if (dict.Count == 0)
                return Enumerable.Empty<T>();
            var maxCount = dict.Max(x => x.Count());
            return dict.Where(x => x.Count() == maxCount).Select(x => x.Key);
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

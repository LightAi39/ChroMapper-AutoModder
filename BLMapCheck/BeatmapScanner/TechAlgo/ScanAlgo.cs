using System.Collections.Generic;
using System.Linq;
using System;
using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.Classes.ChroMapper;
using BLMapCheck.Classes.Unity;

namespace BLMapCheck.BeatmapScanner
{
    internal class ScanAlgo
    {
        #region Main

        public static (double diff, double tech, List<SwingData> data) UseLackWizAlgorithm(List<Cube> red, List<Cube> blue, double bpm, List<BaseNote> bombs)
        {
            double leftDiff = 0;
            double rightDiff = 0;
            double tech = 0;
            List<SwingData> redSwingData;
            List<SwingData> blueSwingData;
            List<List<SwingData>> redPatternData = new();
            List<List<SwingData>> bluePatternData = new();
            List<SwingData> data = new();

            if (red.Count() > 2)
            {
                FlowDetector(red, false);
                redSwingData = SwingProcesser(red);
                if (redSwingData != null)
                {
                    redPatternData = PatternSplitter(redSwingData);
                }
                if (redSwingData != null && redPatternData != null)
                {
                    redSwingData = ParityPredictor(redPatternData, false);
                }
                if (redSwingData != null)
                {
                    SwingCurveCalc(redSwingData, false);
                    redSwingData = CalcSwingDiff(redSwingData, bpm);
                    leftDiff = DiffToPass(redSwingData, 8);
                    leftDiff += DiffToPass(redSwingData, 16);
                    leftDiff += DiffToPass(redSwingData, 32);
                    leftDiff += DiffToPass(redSwingData, 48);
                    leftDiff += DiffToPass(redSwingData, 96);
                    leftDiff /= 5;
                }
                data.AddRange(redSwingData);
            }

            if (blue.Count() > 2)
            {
                FlowDetector(blue, true);
                blueSwingData = SwingProcesser(blue);
                if (blueSwingData != null)
                {
                    bluePatternData = PatternSplitter(blueSwingData);
                }
                if (blueSwingData != null && bluePatternData != null)
                {
                    blueSwingData = ParityPredictor(bluePatternData, true);
                }
                if (blueSwingData != null)
                {
                    SwingCurveCalc(blueSwingData, true);
                    blueSwingData = CalcSwingDiff(blueSwingData, bpm);
                    rightDiff = DiffToPass(blueSwingData, 8);
                    rightDiff += DiffToPass(blueSwingData, 16);
                    rightDiff += DiffToPass(blueSwingData, 32);
                    rightDiff += DiffToPass(blueSwingData, 48);
                    rightDiff += DiffToPass(blueSwingData, 96);
                    rightDiff /= 5;
                }
                data.AddRange(blueSwingData);
            }

            if (data.Count() > 2)
            {
                var test = data.Select(c => c.AngleStrain + c.PathStrain).ToList();
                test.Sort();
                tech = test.Skip((int)(data.Count() * 0.25)).Average();
            }

            var balanced_pass = Math.Max(leftDiff, rightDiff);
            var balanced_tech = tech * (-1 * Math.Pow(1.4, -balanced_pass) + 1) * 10;

            return (balanced_pass, balanced_tech, data);
        }

        #endregion

        #region FlowDetector
        public static void FlowDetector(List<Cube> cubes, bool leftOrRight)
        {
            if (cubes.Count() < 2)
            {
                return;
            }

            double testValue = 45;

            if (leftOrRight)
            {
                testValue = -45;
            }

            cubes.OrderBy(c => c.Time);
            ScanMethod.HandlePattern(cubes);

            if (cubes[0].CutDirection == 8)
            {
                if (cubes[1].CutDirection != 8 && cubes[1].Time - cubes[0].Time <= 0.1429)
                {
                    cubes[0].Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[cubes[1].CutDirection] + cubes[1].AngleOffset, 360);
                }
                else
                {
                    var c = cubes.Where(ca => ca.CutDirection != 8).FirstOrDefault();
                    if (c != null)
                    {
                        cubes[0].Direction = ScanMethod.DirectionToDegree[c.CutDirection] + c.AngleOffset;
                        for (int i = cubes.IndexOf(c); i > 0; i--)
                        {
                            if (cubes[i].Time - cubes[i - 1].Time >= 0.25)
                            {
                                cubes[0].Direction = ScanMethod.ReverseCutDirection(cubes[0].Direction);
                            }
                        }
                    }
                    else
                    {
                        if (cubes[0].Layer >= 2)
                        {
                            cubes[0].Direction = 90;
                        }
                        else
                        {
                            cubes[0].Direction = 270;
                        }
                    }
                }
            }
            else
            {
                cubes[0].Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[cubes[0].CutDirection] + cubes[0].AngleOffset, 360);
            }

            if (cubes[1].CutDirection == 8)
            {
                if ((cubes[1].Time - cubes[0].Time < 0.25 && ScanMethod.IsSlider(cubes[0], cubes[1], cubes[0].Direction, true)) || cubes[1].Time - cubes[0].Time <= 0.1429)
                {
                    cubes[1].Direction = ScanMethod.FindAngleViaPosition(cubes, 1, 0, cubes[0].Direction, true);
                    if (cubes[0].CutDirection == 8)
                    {
                        cubes[0].Direction = cubes[1].Direction;
                    }
                    if (cubes[0].Time != cubes[1].Time)
                    {
                        cubes[0].Slider = true;
                        cubes[1].Slider = true;
                        cubes[1].Precision = cubes[1].Time - cubes[0].Time;
                        cubes[1].Spacing = Math.Max(Math.Max(Math.Abs(cubes[1].Line - cubes[0].Line), Math.Abs(cubes[1].Layer - cubes[0].Layer)) - 1, 0);
                    }
                    cubes[1].Pattern = true;
                    cubes[0].Pattern = true;
                    cubes[0].Head = true;
                }
                else
                {
                    cubes[1].Direction = ScanMethod.FindAngleViaPosition(cubes, 1, 0, cubes[0].Direction, false);
                }
            }
            else
            {
                cubes[1].Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[cubes[1].CutDirection] + cubes[1].AngleOffset, 360);
                if (((cubes[1].Time - cubes[0].Time < 0.25 && ScanMethod.IsSlider(cubes[0], cubes[1], cubes[0].Direction, true)) || cubes[1].Time - cubes[0].Time <= 0.1429)
                    && ScanMethod.IsSameDirection(cubes[0].Direction, cubes[1].Direction, 67.5))
                {
                    if (cubes[0].Time != cubes[1].Time)
                    {
                        cubes[0].Slider = true;
                        cubes[1].Slider = true;
                        cubes[1].Precision = cubes[1].Time - cubes[0].Time;
                        cubes[1].Spacing = Math.Max(Math.Max(Math.Abs(cubes[1].Line - cubes[0].Line), Math.Abs(cubes[1].Layer - cubes[0].Layer)) - 1, 0);
                    }
                    cubes[0].Head = true;
                    cubes[0].Pattern = true;
                    cubes[1].Pattern = true;
                }
            }

            for (int i = 2; i < cubes.Count() - 1; i++)
            {
                if (cubes[i].CutDirection == 8)
                {
                    // Pattern
                    if ((cubes[i].Time - cubes[i - 1].Time < 0.25 && ScanMethod.IsSlider(cubes[i - 1], cubes[i], cubes[i - 1].Direction, true))
                        || cubes[i].Time - cubes[i - 1].Time <= 0.1429)
                    {
                        cubes[i].Direction = ScanMethod.FindAngleViaPosition(cubes, i, i - 1, cubes[i - 1].Direction, true);
                        if (cubes[i - 1].CutDirection == 8)
                        {
                            cubes[i - 1].Direction = cubes[i].Direction;
                        }
                        cubes[i].Pattern = true;
                        if (cubes[i].Time != cubes[i - 1].Time)
                        {
                            cubes[i].Slider = true;
                            cubes[i - 1].Slider = true;
                            cubes[i].Precision = cubes[i].Time - cubes[i - 1].Time;
                            cubes[i].Spacing = Math.Max(Math.Max(Math.Abs(cubes[i].Line - cubes[i - 1].Line), Math.Abs(cubes[i].Layer - cubes[i - 1].Layer)) - 1, 0);
                        }
                        if (!cubes[i - 1].Pattern)
                        {
                            cubes[i - 1].Pattern = true;
                            cubes[i - 1].Head = true;
                        }
                        continue;
                    }
                    else // Probably not a pattern
                    {
                        cubes[i].Direction = ScanMethod.FindAngleViaPosition(cubes, i, i - 1, cubes[i - 1].Direction, false);
                    }

                    // Check if the flow work
                    if (!ScanMethod.IsSameDirection(cubes[i - 1].Direction, cubes[i].Direction, 67.5))
                    {
                        if (cubes[i + 1].CutDirection != 8)
                        {
                            // If the next note is an arrow, we want to check that too
                            var nextDir = ScanMethod.Mod(ScanMethod.DirectionToDegree[cubes[i + 1].CutDirection] + cubes[i + 1].AngleOffset, 360);
                            if (ScanMethod.IsSameDirection(cubes[i].Direction, nextDir, 67.5))
                            {
                                // Attempt a different angle
                                if (!ScanMethod.IsSameDirection(cubes[i].Direction + testValue, nextDir, 67.5))
                                {
                                    cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction + testValue, 360);
                                    continue; // Work
                                }
                                else if (!ScanMethod.IsSameDirection(cubes[i].Direction - testValue, nextDir, 67.5))
                                {
                                    cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction - testValue, 360);
                                    continue; // Work
                                }
                            }
                        }
                        continue;
                    }
                    if (!ScanMethod.IsSameDirection(cubes[i - 1].Direction, cubes[i].Direction + testValue, 67.5))
                    {
                        cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction + testValue, 360);
                        continue; // Work
                    }
                    else if (!ScanMethod.IsSameDirection(cubes[i - 1].Direction, cubes[i].Direction - testValue, 67.5))
                    {
                        cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction - testValue, 360);
                        continue; // Work
                    }

                    // Maybe the note before is wrong?
                    if (cubes[i - 1].CutDirection == 8 && !ScanMethod.IsSameDirection(cubes[i - 2].Direction, cubes[i - 1].Direction + testValue, 67.5))
                    {
                        var lastDir = ScanMethod.Mod(cubes[i - 1].Direction + testValue, 360);
                        if (!ScanMethod.IsSameDirection(lastDir, cubes[i].Direction + testValue * 2, 67.5))
                        {
                            cubes[i - 1].Direction = ScanMethod.Mod(cubes[i - 1].Direction + testValue, 360);
                            cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction + testValue * 2, 360);
                            continue; // Work
                        }
                    }
                    else if (cubes[i - 1].CutDirection == 8 && !ScanMethod.IsSameDirection(cubes[i - 2].Direction, cubes[i - 1].Direction - testValue, 67.5))
                    {
                        var lastDir = ScanMethod.Mod(cubes[i - 1].Direction - testValue, 360);
                        if (!ScanMethod.IsSameDirection(lastDir, cubes[i].Direction - testValue * 2, 67.5))
                        {
                            cubes[i - 1].Direction = ScanMethod.Mod(cubes[i - 1].Direction - testValue, 360);
                            cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction - testValue * 2, 360);
                            continue; // Work
                        }
                    }
                }
                else
                {
                    cubes[i].Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[cubes[i].CutDirection] + cubes[i].AngleOffset, 360);
                    if (((cubes[i].Time - cubes[i - 1].Time < 0.25 && ScanMethod.IsSlider(cubes[i - 1], cubes[i], cubes[i - 1].Direction, false))
                        || cubes[i].Time - cubes[i - 1].Time <= 0.1429)
                    && ScanMethod.IsSameDirection(cubes[i].Direction, cubes[i - 1].Direction, 67.5))
                    {
                        cubes[i].Pattern = true;
                        if (cubes[i].Time != cubes[i - 1].Time)
                        {
                            cubes[i].Slider = true;
                            cubes[i - 1].Slider = true;
                            cubes[i].Precision = cubes[i].Time - cubes[i - 1].Time;
                            cubes[i].Spacing = Math.Max(Math.Max(Math.Abs(cubes[i].Line - cubes[i - 1].Line), Math.Abs(cubes[i].Layer - cubes[i - 1].Layer)) - 1, 0);
                        }
                        if (!cubes[i - 1].Pattern)
                        {
                            cubes[i - 1].Pattern = true;
                            cubes[i - 1].Head = true;
                        }
                    }
                    continue;
                }
            }

            for (int i = 2; i < cubes.Count() - 2; i++)
            {
                if (cubes[i].CutDirection == 8 && cubes[i].Time - cubes[i - 1].Time >= 0.125) // If a dot note only flow from one way
                {
                    if ((ScanMethod.IsSameDirection(cubes[i].Direction, cubes[i - 1].Direction, 67.5) && !ScanMethod.IsSameDirection(cubes[i].Direction, cubes[i + 1].Direction, 67.5))
                        || (!ScanMethod.IsSameDirection(cubes[i].Direction, cubes[i - 1].Direction, 67.5) && ScanMethod.IsSameDirection(cubes[i].Direction, cubes[i + 1].Direction, 67.5)))
                    {
                        if (!ScanMethod.IsSameDirection(cubes[i].Direction + testValue, cubes[i - 1].Direction, 67.5) && !ScanMethod.IsSameDirection(cubes[i].Direction + testValue, cubes[i + 1].Direction, 67.5))
                        {
                            cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction + testValue, 360);
                        }
                        else if (!ScanMethod.IsSameDirection(cubes[i].Direction - testValue, cubes[i - 1].Direction, 67.5) && !ScanMethod.IsSameDirection(cubes[i].Direction - testValue, cubes[i + 1].Direction, 67.5))
                        {
                            cubes[i].Direction = ScanMethod.Mod(cubes[i].Direction - testValue, 360);
                        }
                    }
                }
            }

            if (cubes.Last().CutDirection == 8)
            {
                if ((cubes.Last().Time - cubes[cubes.Count() - 2].Time < 0.25 && ScanMethod.IsSlider(cubes[cubes.Count() - 2], cubes.Last(), cubes[cubes.Count() - 2].Direction, true))
                    || cubes.Last().Time - cubes[cubes.Count() - 2].Time <= 0.1429)
                {
                    cubes.Last().Direction = ScanMethod.FindAngleViaPosition(cubes, cubes.Count - 1, cubes.Count - 2, cubes[cubes.Count() - 2].Direction, true);
                    if (cubes[cubes.Count() - 2].CutDirection == 8)
                    {
                        cubes[cubes.Count() - 2].Direction = cubes.Last().Direction;
                    }
                    cubes.Last().Pattern = true;
                    if (cubes.Last().Time != cubes[cubes.Count() - 2].Time)
                    {
                        cubes.Last().Slider = true;
                        cubes[cubes.Count() - 2].Slider = true;
                        cubes.Last().Precision = cubes.Last().Time - cubes[cubes.Count() - 2].Time;
                        cubes.Last().Spacing = Math.Max(Math.Max(Math.Abs(cubes.Last().Line - cubes[cubes.Count() - 2].Line), Math.Abs(cubes.Last().Layer - cubes[cubes.Count() - 2].Layer)) - 1, 0);
                    }
                    if (!cubes[cubes.Count() - 2].Pattern)
                    {
                        cubes[cubes.Count() - 2].Pattern = true;
                        cubes[cubes.Count() - 2].Head = true;
                    }
                }
                else
                {
                    cubes.Last().Direction = ScanMethod.FindAngleViaPosition(cubes, cubes.Count - 1, cubes.Count - 2, cubes[cubes.Count() - 2].Direction, false);
                }
            }
            else
            {
                cubes.Last().Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[cubes.Last().CutDirection] + cubes.Last().AngleOffset, 360);
                if (((cubes.Last().Time - cubes[cubes.Count() - 2].Time < 0.25 && ScanMethod.IsSlider(cubes[cubes.Count() - 2], cubes.Last(), cubes[cubes.Count() - 2].Direction, false))
                    || cubes.Last().Time - cubes[cubes.Count() - 2].Time <= 0.1429) && ScanMethod.IsSameDirection(cubes[cubes.Count() - 2].Direction, cubes.Last().Direction, 67.5))
                {
                    cubes.Last().Pattern = true;
                    if (cubes.Last().Time != cubes[cubes.Count() - 2].Time)
                    {
                        cubes.Last().Slider = true;
                        cubes[cubes.Count() - 2].Slider = true;
                        cubes.Last().Precision = cubes.Last().Time - cubes[cubes.Count() - 2].Time;
                        cubes.Last().Spacing = Math.Max(Math.Max(Math.Abs(cubes.Last().Line - cubes[cubes.Count() - 2].Line), Math.Abs(cubes.Last().Layer - cubes[cubes.Count() - 2].Layer)) - 1, 0);
                    }
                    if (!cubes[cubes.Count() - 2].Pattern)
                    {
                        cubes[cubes.Count() - 2].Pattern = true;
                        cubes[cubes.Count() - 2].Head = true;
                    }
                }
                else
                {
                    cubes.Last().Pattern = false;
                    cubes.Last().Slider = false;
                }
            }
        }

        #endregion

        #region SwingProcesser

        public static List<SwingData> SwingProcesser(List<Cube> cubes)
        {
            var swingData = new List<SwingData>();

            if (cubes.Count() == 0)
            {
                return swingData;
            }

            swingData.Add(new SwingData(cubes[0].Time, cubes[0].Direction, cubes[0]));
            (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = ScanMethod.CalculateBaseEntryExit((cubes[0].Line, cubes[0].Layer), cubes[0].Direction);

            for (int i = 1; i < cubes.Count - 1; i++)
            {
                var previousAngle = swingData.Last().Angle;

                var currentBeat = cubes[i].Time;
                var currentAngle = cubes[i].Direction;
                (double x, double y) currentPosition = (cubes[i].Line, cubes[i].Layer);

                if (!cubes[i].Pattern || cubes[i].Head)
                {
                    swingData.Add(new SwingData(currentBeat, currentAngle, cubes[i]));
                    (swingData.Last().EntryPosition, swingData.Last().ExitPosition) = ScanMethod.CalculateBaseEntryExit(currentPosition, currentAngle);
                }
                else
                {
                    for (int f = i; f > 0; f--)
                    {
                        if (cubes[f].Head)
                        {
                            currentAngle = ScanMethod.FindAngleViaPosition(cubes, i, f, previousAngle, true);
                            break;
                        }
                    }
                    if (!ScanMethod.IsSameDirection(currentAngle, previousAngle, 67.5))
                    {
                        currentAngle = ScanMethod.ReverseCutDirection(currentAngle);
                    }
                    swingData.Last().Angle = currentAngle;
                    var xtest = (swingData.Last().EntryPosition.x - (currentPosition.x * 0.333333 - Math.Cos(ScanMethod.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667)) * Math.Cos(ScanMethod.ConvertDegreesToRadians(currentAngle));
                    var ytest = (swingData.Last().EntryPosition.y - (currentPosition.y * 0.333333 - Math.Sin(ScanMethod.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667)) * Math.Sin(ScanMethod.ConvertDegreesToRadians(currentAngle));
                    if (xtest <= 0.001 && ytest >= 0.001)
                    {
                        swingData.Last().EntryPosition = (currentPosition.x * 0.333333 - Math.Cos(ScanMethod.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667, currentPosition.y * 0.333333 - Math.Sin(ScanMethod.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667);
                    }
                    else
                    {
                        swingData.Last().ExitPosition = (currentPosition.x * 0.333333 + Math.Cos(ScanMethod.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667, currentPosition.y * 0.333333 + Math.Sin(ScanMethod.ConvertDegreesToRadians(currentAngle)) * 0.166667 + 0.166667);
                    }
                }
            }

            return swingData;
        }

        #endregion

        #region PatternSplitter

        public static List<List<SwingData>> PatternSplitter(List<SwingData> swingData)
        {
            if (swingData.Count() < 2)
            {
                return null;
            }

            for (int i = 0; i < swingData.Count(); i++)
            {
                if (i > 0 && i + 1 < swingData.Count())
                {
                    swingData[i].SwingFrequency = 2 / (swingData[i + 1].Time - swingData[i - 1].Time);
                }
                else
                {
                    swingData[i].SwingFrequency = 0;
                }
            }

            var patternFound = false;
            var SFList = swingData.Select(s => s.SwingFrequency);
            var SFMargin = SFList.Average() / 32;
            List<List<SwingData>> patternList = new();
            List<SwingData> tempPList = new();

            for (int i = 0; i < swingData.Count(); i++)
            {
                if (i > 0)
                {
                    if (1 / (swingData[i].Time - swingData[i - 1].Time) - swingData[i].SwingFrequency <= SFMargin)
                    {
                        if (!patternFound)
                        {
                            patternFound = true;
                            tempPList.Remove(tempPList.Last());
                            if (tempPList.Count() > 0)
                            {
                                patternList.Add(tempPList);
                            }
                            tempPList = new List<SwingData>()
                            {
                                swingData[i - 1]
                            };
                        }
                        tempPList.Add(swingData[i]);
                    }
                    else
                    {
                        if (tempPList.Count() > 0 && patternFound)
                        {
                            tempPList.Add(swingData[i]);
                            patternList.Add(tempPList);
                            tempPList = new List<SwingData>();
                        }
                        else
                        {
                            patternFound = false;
                            tempPList.Add(swingData[i]);
                        }
                    }
                }
                else
                {
                    tempPList.Add(swingData[0]);
                }
            }

            if (tempPList.Count > 0 && patternList.Count() == 0)
            {
                patternList.Add(tempPList);
            }

            return patternList;
        }

        #endregion

        #region ParityPredictor

        public static List<SwingData> DeepCopy(List<SwingData> data)
        {
            List<SwingData> cloneList = new();
            foreach (var d in data)
            {
                SwingData clone = new()
                {
                    EntryPosition = d.EntryPosition,
                    ExitPosition = d.ExitPosition,
                    Time = d.Time,
                    Angle = d.Angle,
                    SwingFrequency = d.SwingFrequency,
                    SwingDiff = d.SwingDiff,
                    Forehand = d.Forehand,
                    Reset = d.Reset,
                    PathStrain = d.PathStrain,
                    AngleStrain = d.AngleStrain,
                    AnglePathStrain = d.AnglePathStrain,
                    PreviousDistance = d.PreviousDistance,
                    PositionComplexity = d.PositionComplexity,
                    CurveComplexity = d.CurveComplexity,
                    Start = d.Start
                };

                cloneList.Add(clone);
            }
            return cloneList;
        }

        public static List<SwingData> ParityPredictor(List<List<SwingData>> patternData, bool leftOrRight)
        {
            if (patternData.Count() < 1)
            {
                return null;
            }

            var newPatternData = new List<SwingData>();

            for (int p = 0; p < patternData.Count(); p++)
            {
                var testData1 = patternData[p];
                var testData2 = DeepCopy(patternData[p]);

                for (int i = 0; i < testData1.Count(); i++)
                {
                    if (i > 0)
                    {
                        if (ScanMethod.IsSameDirection(testData1[i - 1].Angle, testData1[i].Angle, 67.5))
                        {
                            testData1[i].Reset = true;
                            testData1[i].Forehand = testData1[i - 1].Forehand;
                        }
                        else
                        {
                            testData1[i].Reset = false;
                            testData1[i].Forehand = !testData1[i - 1].Forehand;
                        }
                    }
                    else
                    {
                        testData1[0].Reset = false;
                        testData1[0].Forehand = true;
                    }
                }
                for (int i = 0; i < testData2.Count(); i++)
                {
                    if (i > 0)
                    {
                        if (ScanMethod.IsSameDirection(testData2[i - 1].Angle, testData2[i].Angle, 67.5))
                        {
                            testData2[i].Reset = true;
                            testData2[i].Forehand = testData2[i - 1].Forehand;
                        }
                        else
                        {
                            testData2[i].Reset = false;
                            testData2[i].Forehand = !testData2[i - 1].Forehand;
                        }
                    }
                    else
                    {
                        testData2[0].Reset = false;
                        testData2[0].Forehand = false;
                    }
                }

                var forehandTest = ScanMethod.SwingAngleStrainCalc(testData1, leftOrRight);
                var backhandTest = ScanMethod.SwingAngleStrainCalc(testData2, leftOrRight);
                if (forehandTest <= backhandTest)
                {
                    newPatternData.AddRange(testData1);
                }
                else if (forehandTest > backhandTest)
                {
                    newPatternData.AddRange(testData2);
                }
            }
            for (int i = 0; i < newPatternData.Count(); i++)
            {
                newPatternData[i].AngleStrain = ScanMethod.SwingAngleStrainCalc(new List<SwingData> { newPatternData[i] }, leftOrRight) * 2;
            }

            return newPatternData;
        }

        #endregion

        #region SwingCurveCalc

        public static void SwingCurveCalc(List<SwingData> swingData, bool leftOrRight)
        {
            if (swingData.Count() < 2)
            {
                return;
            }

            double pathLookback;
            (double x, double y) simHandCurPos;
            (double x, double y) simHandPrePos;
            double curveComplexity;
            double pathAngleStrain;
            double positionComplexity;

            swingData[0].PathStrain = 0;
            swingData[0].PositionComplexity = 0;
            swingData[0].PreviousDistance = 0;
            swingData[0].CurveComplexity = 0;
            swingData[0].AnglePathStrain = 0;

            for (int i = 1; i < swingData.Count(); i++)
            {
                Vector2 point0 = new((float)swingData[i - 1].ExitPosition.x, (float)swingData[i - 1].ExitPosition.y);
                Vector2 point1;
                point1.x = (float)(point0.x + 1 * Math.Cos(ScanMethod.ConvertDegreesToRadians(swingData[i - 1].Angle)));
                point1.y = (float)(point0.y + 1 * Math.Sin(ScanMethod.ConvertDegreesToRadians(swingData[i - 1].Angle)));
                Vector2 point3 = new((float)swingData[i].EntryPosition.x, (float)swingData[i].EntryPosition.y);
                Vector2 point2;
                point2.x = (float)(point3.x - 1 * Math.Cos(ScanMethod.ConvertDegreesToRadians(swingData[i].Angle)));
                point2.y = (float)(point3.y - 1 * Math.Sin(ScanMethod.ConvertDegreesToRadians(swingData[i].Angle)));

                List<Vector2> points = new()
                {
                    point0,
                    point1,
                    point2,
                    point3
                };

                var point = ScanMethod.CalcCurvePoints(points, 0.04);

                positionComplexity = 0;
                List<double> angleChangeList = new();
                List<double> angleList = new();
                double distance = 0;
                for (int f = 1; f < point.Count(); f++)
                {
                    angleList.Add(ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(point[f].y - point[f - 1].y, point[f].x - point[f - 1].x)), 360));
                    distance += Math.Sqrt(Math.Pow(point[f].y - point[f - 1].y, 2) + Math.Pow(point[f].x - point[f - 1].x, 2));
                    if (f > 1)
                    {
                        angleChangeList.Add(180 - Math.Abs(Math.Abs(angleList.Last() - angleList[angleList.Count() - 2]) - 180));
                    }
                }
                distance -= 0.75;
                if (i > 1)
                {
                    simHandCurPos = swingData[i].EntryPosition;
                    if (!swingData[i].Reset && !swingData[i - 1].Reset)
                    {
                        simHandPrePos = swingData[i - 2].EntryPosition;
                    }
                    else if (!swingData[i].Reset && swingData[i - 1].Reset)
                    {
                        simHandPrePos = swingData[i - 1].EntryPosition;

                    }
                    else if (swingData[i].Reset)
                    {
                        simHandPrePos = swingData[i - 1].EntryPosition;
                    }
                    else
                    {
                        simHandPrePos = simHandCurPos;
                    }
                    positionComplexity = Math.Pow(Math.Sqrt(Math.Pow(simHandCurPos.y - simHandPrePos.y, 2) + Math.Pow(simHandCurPos.x - simHandPrePos.x, 2)), 2);
                    if (positionComplexity > 10)
                    {
                        positionComplexity = 10;
                    }
                }

                var lengthOfList = angleChangeList.Count() * 0.6;
                double first;
                double last;

                if (swingData[i].Reset)
                {
                    pathLookback = 0.9;
                    first = 0.5;
                    last = 1;
                }
                else
                {
                    pathLookback = 0.5;
                    first = 0.2;
                    last = 0.8;
                }
                var pathLookbackIndex = (int)(angleList.Count() * pathLookback);
                var firstIndex = (int)(angleChangeList.Count() * first) - 1;
                var lastIndex = (int)(angleChangeList.Count() * last) - 1;

                curveComplexity = Math.Abs((lengthOfList * angleChangeList.Take(lastIndex).Skip(firstIndex).Average() - 180) / 180);
                pathAngleStrain = ScanMethod.BezierAngleStrainCalc(angleList.Skip(pathLookbackIndex).ToList(), swingData[i].Forehand, leftOrRight) / angleList.Count() * 2;

                swingData[i].PositionComplexity = positionComplexity;
                swingData[i].PreviousDistance = distance;
                swingData[i].CurveComplexity = curveComplexity;
                swingData[i].AnglePathStrain = pathAngleStrain;
                swingData[i].PathStrain = curveComplexity + pathAngleStrain + positionComplexity;
            }
        }

        #endregion

        #region DiffToPass


        public static List<SwingData> CalcSwingDiff(List<SwingData> swingData, double bpm)
        {
            if (swingData.Count() == 0)
            {
                return swingData;
            }
            var bps = bpm / 60;
            var data = new List<SData>();
            swingData[0].SwingDiff = 0;
            for (int i = 1; i < swingData.Count(); i++)
            {
                var distanceDiff = swingData[i].PreviousDistance / (swingData[i].PreviousDistance + 3) + 1;
                data.Add(new SData(swingData[i].SwingFrequency * distanceDiff * bps));
                if (swingData[i].Reset)
                {
                    data.Last().SwingSpeed *= 2;
                }
                var xHitDist = swingData[i].EntryPosition.x - swingData[i].ExitPosition.x;
                var yHitDist = swingData[i].EntryPosition.y - swingData[i].ExitPosition.y;
                data.Last().HitDistance = Math.Sqrt(Math.Pow(xHitDist, 2) + Math.Pow(yHitDist, 2));
                data.Last().HitDiff = data.Last().HitDistance / (data.Last().HitDistance + 2) + 1;
                data.Last().Stress = (swingData[i].AngleStrain + swingData[i].PathStrain) * data.Last().HitDiff;
                swingData[i].SwingDiff = data.Last().SwingSpeed * (-1 * Math.Pow(1.4, -data.Last().SwingSpeed) + 1) * (data.Last().Stress / (data.Last().Stress + 2) + 1);
            }

            return swingData;
        }


        public static double DiffToPass(List<SwingData> swingData, int WINDOW)
        {
            if (swingData.Count() < 2)
            {
                return 0;
            }

            var qDiff = new Queue<double>();
            var difficultyIndex = new List<double>();

            for (int i = 1; i < swingData.Count(); i++)
            {
                if (i > WINDOW)
                {
                    qDiff.Dequeue();
                }
                qDiff.Enqueue(swingData[i].SwingDiff);
                List<double> tempList = qDiff.ToList();
                tempList.Sort();
                tempList.Reverse();
                if (i >= WINDOW)
                {
                    var windowDiff = tempList.Average() * 0.8;
                    difficultyIndex.Add(windowDiff);
                }
            }

            if (difficultyIndex.Count > 0)
            {
                return difficultyIndex.Max();
            }
            else
            {
                return 0;
            }
        }

        #endregion
    }
}

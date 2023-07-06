using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;

namespace ChroMapper_LightModding.BeatmapScanner.MapCheck
{
    // Based on https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/analyzers/swing/swing.ts
    internal class Swing
    {
        public double time { get; set; }
        public double duration { get; set; }
        public double minSpeed { get; set; }
        public double maxSpeed { get; set; }
        public double ebpm { get; set; }
        public double ebpmSwing { get; set; }
        public List<BaseNote> data { get; set; }

        public Swing(double time, double duration, List<BaseNote> data, double ebpm, double ebpmSwing, double maxSpeed, double minSpeed)
        {
            this.time = time;
            this.duration = duration;
            this.data = data;
            this.ebpm = ebpm;
            this.ebpmSwing = ebpmSwing;
            this.maxSpeed = maxSpeed;
            this.minSpeed = minSpeed;
        }

        public static List<Swing> Generate(List<BaseNote> nc, double bpm)
        {
            var sc = new List<Swing>();
            var ebpm = 0d;
            var ebpmSwing = 0d;
            var minSpeed = 0d;
            var maxSpeed = 0d;
            var firstNote = new List<List<BaseNote>>
            {
                new List<BaseNote>(),
                new List<BaseNote>()
            };
            var lastNote = new List<List<BaseNote>>
            {
                new List<BaseNote>(),
                new List<BaseNote>()
            };
            var swingNoteArray = new List<List<BaseNote>>
            {
                new List<BaseNote>(),
                new List<BaseNote>()
            };

            foreach (var n in nc) {
                if (n.Type != 0 && n.Type != 1)
                {
                    continue;
                }
                minSpeed = 0;
                maxSpeed = double.MaxValue;
                if (lastNote[n.Type].Count > 0)
                {
                    if (Next(n, lastNote[n.Type][0], bpm, swingNoteArray[n.Type]))
                    {
                        minSpeed = CalcMinSliderSpeed(swingNoteArray[n.Type], bpm);
                        maxSpeed = CalcMaxSliderSpeed(swingNoteArray[n.Type], bpm);
                        if (!(minSpeed > 0 && maxSpeed != double.PositiveInfinity))
                        {
                            minSpeed = 0;
                            maxSpeed = 0;
                        }
                        ebpmSwing = CalcEBPMBetweenObject(n, firstNote[n.Type][0], bpm);
                        ebpm = CalcEBPMBetweenObject(n, lastNote[n.Type][0], bpm);
                        sc.Add(new(firstNote[n.Type][0].JsonTime, lastNote[n.Type][0].JsonTime - firstNote[n.Type][0].JsonTime, swingNoteArray[n.Type], ebpm, ebpmSwing, maxSpeed, minSpeed));
                        firstNote[n.Type][0] = n;
                        swingNoteArray[n.Type].Clear();
                    }
                }
                else
                {
                    firstNote[n.Type][0] = n;
                }
                lastNote[n.Type][0] = n;
                swingNoteArray[n.Type].Add(n);
            }

            for (var color = 0; color < 2; color++)
            {
                if (lastNote[color].Count > 0)
                {
                    minSpeed = CalcMinSliderSpeed(swingNoteArray[color], bpm);
                    maxSpeed = CalcMaxSliderSpeed(swingNoteArray[color], bpm);
                    if (!(minSpeed > 0 && maxSpeed != double.PositiveInfinity))
                    {
                        minSpeed = 0;
                        maxSpeed = 0;
                    }
                    sc.Add(new(firstNote[color][0].JsonTime, lastNote[color][0].JsonTime - firstNote[color][0].JsonTime, swingNoteArray[color], ebpm, ebpmSwing, maxSpeed, minSpeed));
                }
            }

            return sc;
        }

        public static bool CheckDirection(BaseNote n1, BaseNote n2, double angleTol, bool equal)
        {
            if (n1 == null || n2 == null)
            {
                return false;
            }
            if (n1.CutDirection == NoteDirection.ANY)
            {
                return false;
            }
            var nA1 = n1.CutDirection;
            if (n2.CutDirection == NoteDirection.ANY)
            {
                return false;
            }
            var nA2 = n2.CutDirection;
            return equal
                ? ShortRotDistance(nA1, nA2, 360) <= angleTol
                : ShortRotDistance(nA1, nA2, 360) >= angleTol;
        }

        public static double ShortRotDistance(double a, double b, double m)
        {
            return Math.Min(ScanMethod.Mod(a - b, m), ScanMethod.Mod(b - a, m));
        }

        public static bool Next(BaseNote currNote, BaseNote prevNote, double bpm, List<BaseNote> context = null)
        {
            if (currNote.Type == 3 || prevNote.Type == 3)
            {
                return false;
            }
            if (context != null)
            {
                if (context.Count > 0 &&
                (prevNote.JsonTime / bpm * 60) + 0.005 < (currNote.JsonTime / bpm * 60) &&
                currNote.CutDirection != NoteDirection.ANY)
                {
                    foreach (var n in context) {
                        if (n.Type == 0 || n.Type == 1)
                        {
                            if (n.CutDirection != NoteDirection.ANY &&
                                CheckDirection(currNote, n, 90, false))
                            {
                                return true;
                            }
                        }
                    }
                }
                if (context.Count > 0)
                {
                    foreach (var other in context)
                    {
                        if (other.Type == 0 || other.Type == 1)
                        {
                            var distance = Math.Sqrt(Math.Pow(other.PosX - currNote.PosX, 2) + Math.Pow(other.PosY - currNote.PosY, 2));
                            if(distance <= 0.5)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            var dist = Math.Sqrt(Math.Pow(prevNote.PosX - currNote.PosX, 2) + Math.Pow(prevNote.PosY - currNote.PosY, 2));
            return (dist > 1.8 && (currNote.JsonTime / bpm * 60) - (prevNote.JsonTime / bpm * 60) > 0.08) || ((currNote.JsonTime / bpm * 60) - (prevNote.JsonTime / bpm * 60) > 0.07);
        }

        public static double CalcEBPMBetweenObject(BaseNote currObj, BaseNote prevObj, double bpm)
        {
            return bpm / ((currObj.JsonTime / bpm * 60) - (prevObj.JsonTime / bpm * 60) / 60 * bpm) * 2;
        }

        public static double CalcMinSliderSpeed(List<BaseNote> notes, double bpm)
        {
            var hasStraight = false;
            var hasDiagonal = false;
            var curvedSpeed = 0d;
            List<double> speedList = new();

            for (int i = 1; i < notes.Count; i++)
            {
                var dist = Math.Sqrt(Math.Pow(notes[i - 1].PosX - notes[i].PosX, 2) + Math.Pow(notes[i - 1].PosY - notes[i].PosY, 2));
                if ((notes[i].PosY == notes[i - 1].PosY || notes[i].PosX == notes[i - 1].PosX) && !hasStraight)
                {
                    hasStraight = true;
                    curvedSpeed = (notes[i].JsonTime - notes[i - 1].JsonTime) / dist;
                }
                var dX = Math.Abs(notes[i].PosX - notes[i - 1].PosX);
                var dY = Math.Abs(notes[i].PosY - notes[i - 1].PosY);
                if(dX == dY)
                {
                    hasDiagonal = true;
                }
                if(dist > 1.8 && !hasStraight && !hasDiagonal)
                {
                    hasDiagonal = true;
                }
                speedList.Add((notes[i].JsonTime - notes[i - 1].JsonTime) / dist);
            }
            var speed = speedList.Max() / bpm * 60;

            if (hasStraight && hasDiagonal)
            {
                return (curvedSpeed / bpm * 60);
            }
            return speed;
        }

        public static double CalcMaxSliderSpeed(List<BaseNote> notes, double bpm)
        {
            var hasStraight = false;
            var hasDiagonal = false;
            var curvedSpeed = double.MaxValue;
            List<double> speedList = new();

            for (int i = 1; i < notes.Count; i++)
            {
                var dist = Math.Sqrt(Math.Pow(notes[i - 1].PosX - notes[i].PosX, 2) + Math.Pow(notes[i - 1].PosY - notes[i].PosY, 2));
                if ((notes[i].PosY == notes[i - 1].PosY || notes[i].PosX == notes[i - 1].PosX) && !hasStraight)
                {
                    hasStraight = true;
                    curvedSpeed = (notes[i].JsonTime - notes[i - 1].JsonTime) / dist;
                }
                var dX = Math.Abs(notes[i].PosX - notes[i - 1].PosX);
                var dY = Math.Abs(notes[i].PosY - notes[i - 1].PosY);
                if (dX == dY)
                {
                    hasDiagonal = true;
                }
                if (dist > 1.8 && !hasStraight && !hasDiagonal)
                {
                    hasDiagonal = true;
                }
                speedList.Add((notes[i].JsonTime - notes[i - 1].JsonTime) / dist);
            }
            var speed = speedList.Min() / bpm * 60;

            if (hasStraight && hasDiagonal)
            {
                return (curvedSpeed / bpm * 60);
            }
            return speed;
        }

        public static int[] NoteDirectionAngle = { 90, 270, 180, 0, 115, 45, 225, 335, 0 };

        public static bool IsIntersect(BaseNote currNote, BaseNote compareTo, double[,] angleDistances, int index, double a1 = -1, double a2 = -1)
        {
            (var nX1, var nY1) = (currNote.PosX, currNote.PosY);
            (var nX2, var nY2) = (compareTo.PosX, compareTo.PosY);
            var resultN1 = false;
            if (currNote.CutDirection != 8 && currNote.Type != 3)
            { 
                double nA1 = NoteDirectionAngle[currNote.CutDirection];
                if (a1 != -1)
                {
                    nA1 = a1;
                }
                var a = (ScanMethod.ConvertRadiansToDegrees(Math.Atan2(nY1 - nY2, nX1 - nX2)) + 360) % 360;
                for (int i = 0; i < index; i++)
                {
                    var aS = (nA1 + 360 - angleDistances[i, 0]) % 360;
                    var aE = (nA1 + 360 + angleDistances[i, 0]) % 360;
                    resultN1 = (angleDistances[i, 1] >= Math.Sqrt(Math.Pow(nX1 - nX2, 2) + Math.Pow(nY1 - nY2, 2)) &&
                    ((aS < aE && aS <= a && a <= aE) || (aS >= aE && (a <= aE || a >= aS)))) ||
                    resultN1;
                    if (resultN1)
                    {
                        break;
                    }
                }
            }
            var resultN2 = false;
            if (compareTo.CutDirection != 8 && compareTo.Type != 3)
            {
                double nA2 = NoteDirectionAngle[compareTo.CutDirection];
                if (a2 != -1)
                {
                    nA2 = a2;
                }
                var a = (ScanMethod.ConvertRadiansToDegrees(Math.Atan2(nY2 - nY1, nX2 - nX1)) + 360) % 360;
                for (int i = 0; i < index; i++)
                {
                    var aS = (nA2 + 360 - angleDistances[i, 0]) % 360;
                    var aE = (nA2 + 360 + angleDistances[i, 0]) % 360;

                    resultN2 = (angleDistances[i, 1] >= Math.Sqrt(Math.Pow(nX1 - nX2, 2) + Math.Pow(nY1 - nY2, 2)) &&
                    ((aS < aE && aS <= a && a <= aE) || (aS >= aE && (a <= aE || a >= aS)))) ||
                    resultN2;
                    if (resultN2)
                    {
                        break;
                    }
                }
            }
            if(resultN1 || resultN2)
            {
                return true;
            }

            return false;
        }

        public static bool IsDouble(BaseNote note, List<BaseNote> nc, int index)
        {
            for(int i = index; i < nc.Count; i++)
            {
                if (nc[i].Type != 0 && nc[i].Type != 1)
                {
                    continue;
                }
                if (nc[i].JsonTime < note.JsonTime + 0.01 && nc[i].Type != note.Type)
                {
                    return true;
                }
                if (nc[i].JsonTime > note.JsonTime + 0.01)
                {
                    return false;
                }
            }
            return false;
        }
    }

    internal static class SwingType
    {
        public static List<int> Up = new() { 0, 4, 5 };
        public static List<int> Down = new() { 1, 6, 7 };
        public static List<int> Left = new() { 2, 4, 6 };
        public static List<int> Right = new() { 3, 5, 7 };
        public static List<int> Up_Left = new() { 0, 2, 4 };
        public static List<int> Up_Right = new() { 0, 3, 5 };
        public static List<int> Down_Left = new() { 1, 2, 6 };
        public static List<int> Down_Right = new() { 1, 3, 7 };
        public static List<int> Vertical = new() { 0, 1, 4, 5, 6, 7 };
        public static List<int> Horizontal = new() { 2, 3, 4, 5, 6, 7 };
        public static List<int> Diagonal = new() { 4, 5, 6, 7 };
    }

    internal static class NoteDirectionSpace
    {
        public static int[] UP = { 0, 1 };
        public static int[] DOWN = { 0, -1 };
        public static int[] LEFT = { -1, 0 };
        public static int[] RIGHT =  { 1, 0 };
        public static int[] UP_LEFT =  { -1, 1};
        public static int[] UP_RIGHT =  { 1, 1 };
        public static int[] DOWN_LEFT = { -1, -1 };
        public static int[] DOWN_RIGHT =  { 1, - 1 };
        public static int[] ANY = { 0, 0 };

        public static int[] Get(int cutDir)
        {
            switch(cutDir)
            {
                case 0: return UP;
                case 1: return DOWN;
                case 2: return LEFT;
                case 3: return RIGHT;
                case 4: return UP_LEFT;
                case 5: return UP_RIGHT;
                case 6: return DOWN_LEFT;
                case 7: return DOWN_RIGHT;
                default: return ANY;
            }
        }
    }

    internal static class AdjacentHandClap
    {
        public static int[] UP = { 1, 6, 7 };
        public static int[] DOWN = { 0, 4, 5 };
        public static int[] LEFT = { 3, 5, 7 };
        public static int[] RIGHT = { 2, 4, 6 };
        public static int[] UP_LEFT = { 1, 3, 7 };
        public static int[] UP_RIGHT = { 1, 2, 6 };
        public static int[] DOWN_LEFT = { 0, 3, 5 };
        public static int[] DOWN_RIGHT = { 0, 2, 4 };
        public static int[] ANY = { 0, 0 };

        public static int[] Get(int cutDir)
        {
            switch (cutDir)
            {
                case 0: return UP;
                case 1: return DOWN;
                case 2: return LEFT;
                case 3: return RIGHT;
                case 4: return UP_LEFT;
                case 5: return UP_RIGHT;
                case 6: return DOWN_LEFT;
                case 7: return DOWN_RIGHT;
                default: return ANY;
            }
        }
    }

    internal static class Reverse
    {
        public static int UP = 1;
        public static int DOWN = 0;
        public static int LEFT = 3;
        public static int RIGHT = 2;
        public static int UP_LEFT = 7;
        public static int UP_RIGHT = 6;
        public static int DOWN_LEFT = 5;
        public static int DOWN_RIGHT = 4;
        public static int ANY = 0;

        public static int Get(int cutDir)
        {
            switch (cutDir)
            {
                case 0: return UP;
                case 1: return DOWN;
                case 2: return LEFT;
                case 3: return RIGHT;
                case 4: return UP_LEFT;
                case 5: return UP_RIGHT;
                case 6: return DOWN_LEFT;
                case 7: return DOWN_RIGHT;
                default: return ANY;
            }
        }
    }
}


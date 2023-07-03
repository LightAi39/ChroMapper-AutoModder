using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChroMapper_LightModding.BeatmapScanner.MapCheck
{
    // Based on https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/analyzers/parity/parity.ts
    internal class Parity
    {
        public int color { get; set; }
        public int rotation { get; set; }
        public (double x, double y) position { get; set; }
        public int warningThreshold { get; set; }
        public int errorThreshold { get; set; }
        public int allowedRotation { get; set; }
        public ParityState? state { get; set; }
        public static readonly int[,] CONSTRAINT_ROTATION = { { -155, 195 }, { -195, 155 } };

        public Parity(List<BaseNote> notes, int type, int warningThreshold, int errorThreshold, int allowedRotation, ParityState? parity) 
        {
            color = type;
            this.warningThreshold = warningThreshold;
            this.errorThreshold = errorThreshold;
            this.allowedRotation = allowedRotation;
            if (parity != null && parity != ParityState.Neutral)
            {
                state = parity;
                rotation = 0;
            }
            else
            {
                state = PredictStartState(notes, type);
                rotation = PredictStartRotation(notes, type);
            }
            position = PredictStartPosition(notes, type);
        }

        public ParityStatus Check(List<BaseNote> noteContext, List<BaseNote> bombContext)
        {
            if (state == ParityState.Neutral)
            {
                return ParityStatus.None;
            }
            if(noteContext.Count == 0)
            {
                return ParityStatus.None;
            }
            var startTime = noteContext.Last().JsonTime;
            var noteType = color;
            var currentState = state;
            var currentRotation = rotation;
            
            bombContext.ForEach((bomb) => {
                if (bomb.JsonTime - 0.001 > startTime)
                {
                    return;
                }
                else if (bomb.PosY == 0)
                {
                    if(noteType == 0)
                    {
                        if (bomb.PosX == 1)
                        {
                            currentState = ParityState.Backhand;
                            currentRotation = 0;
                        }
                    }
                    else if (noteType == 1)
                    {
                        if (bomb.PosX == 2)
                        {
                            currentState = ParityState.Backhand;
                            currentRotation = 0;
                        }
                    }
                }
                if (bomb.PosY == 2)
                {
                    if (noteType == 0)
                    {
                        if (bomb.PosX == 1)
                        {
                            currentState = ParityState.Forehand;
                            currentRotation = 0;
                        }
                    }
                    else if (noteType == 1)
                    {
                        if (bomb.PosX == 2)
                        {
                            currentState = ParityState.Forehand;
                            currentRotation = 0;
                        }
                    }
                }
            });

            BaseNote prevNote = null;
            var expectedDirection = NoteDirection.ANY;
            foreach (var note in noteContext) {
                if (note.Type != 0 && note.Type != 1)
                {
                    continue;
                }
                if (note.CutDirection != NoteDirection.ANY)
                {
                    expectedDirection = note.CutDirection;
                }
                if(prevNote != null)
                {
                    if (expectedDirection == NoteDirection.ANY)
                    {
                        expectedDirection = PredictDirection(note, prevNote);
                    }
                }
                prevNote = note;
            }
            if (expectedDirection == NoteDirection.ANY)
            {
                return ParityStatus.None;
            }

            int parityRotation = 0;
            if (noteType == 0)
            {
                var state = ParitySwitch(currentState);
                if(state == ParityState.Forehand)
                {
                    parityRotation = NoteParityRotation.RedForehand[expectedDirection];
                }
                else if(state == ParityState.Backhand)
                {
                    parityRotation = NoteParityRotation.RedBackhand[expectedDirection];
                }
            }
            else if (noteType == 1)
            {
                var state = ParitySwitch(currentState);
                if (state == ParityState.Forehand)
                {
                    parityRotation = NoteParityRotation.BlueForehand[expectedDirection];
                }
                else if (state == ParityState.Backhand)
                {
                    parityRotation = NoteParityRotation.BlueBackhand[expectedDirection];
                }
            }
            if ((currentRotation > parityRotation ? currentRotation - parityRotation : parityRotation - currentRotation) > 180)
            {
                return ParityStatus.Error;
            }
            if (parityRotation < CONSTRAINT_ROTATION[noteType, 0] + errorThreshold || parityRotation > CONSTRAINT_ROTATION[noteType, 1] - errorThreshold)
            {
                return ParityStatus.Error;
            }
            if (parityRotation < CONSTRAINT_ROTATION[noteType, 0] + warningThreshold || parityRotation > CONSTRAINT_ROTATION[noteType, 1] - warningThreshold)
            {
                return ParityStatus.Warning;
            }
            if ((currentRotation > parityRotation ? currentRotation - parityRotation : parityRotation - currentRotation) > allowedRotation)
            {
                return ParityStatus.Warning;
            }

            return ParityStatus.None;
        }

        public void Next(List<BaseNote> noteContext, List<BaseNote> bombContext)
        {
            if (Check(noteContext, bombContext) != ParityStatus.Error)
            {
                switch (state)
                {
                    case ParityState.Forehand:
                        {
                            state = ParityState.Backhand;
                            break;
                        }
                    case ParityState.Backhand:
                        {
                            state = ParityState.Forehand;
                            break;
                        }
                    case ParityState.Neutral:
                        {
                            for (var i = 0; i < noteContext.Count; i++)
                            {
                                if (noteContext[i].Type != 0 || noteContext[i].Type != 1)
                                {
                                    continue;
                                }
                                var note = noteContext[i];
                                if(note.Type == 0)
                                {
                                    if (NoteInitParity.RedForehand.Contains(note.CutDirection))
                                    {
                                        state = ParityState.Backhand;
                                        break;
                                    }
                                    if (NoteInitParity.RedBackhand.Contains(note.CutDirection))
                                    {
                                        state = ParityState.Forehand;
                                        break;
                                    }
                                }
                                else if (note.Type == 1)
                                {
                                    if (NoteInitParity.BlueForehand.Contains(note.CutDirection))
                                    {
                                        state = ParityState.Backhand;
                                        break;
                                    }
                                    if (NoteInitParity.BlueBackhand.Contains(note.CutDirection))
                                    {
                                        state = ParityState.Forehand;
                                        break;
                                    }
                                }

                                if (state == ParityState.Neutral && note.CutDirection == NoteDirection.ANY)
                                {
                                    if (note.PosY == 0)
                                    {
                                        state = ParityState.Backhand;
                                    }
                                    if (note.PosY > 0)
                                    {
                                        state = ParityState.Forehand;
                                    }
                                }
                            }
                            break;
                        }
                }
            }
            if (state == ParityState.Neutral)
            {
                return;
            }

            var startTime = noteContext.First().JsonTime;
            var noteType = color;

            bombContext.ForEach(bomb => {
                if (bomb.PosY == 0)
                {
                    if (noteType == 0)
                    {
                        if (bomb.PosX == 1)
                        {
                            state = ParityState.Backhand;
                            rotation = 0;
                        }
                    }
                    else if (noteType == 1)
                    {
                        if (bomb.PosX == 2)
                        {
                            state = ParityState.Backhand;
                            rotation = 0;
                        }
                    }
                }
                if (bomb.PosY == 2)
                {
                    if (noteType == 0)
                    {
                        if (bomb.PosX == 2)
                        {
                            state = ParityState.Forehand;
                            rotation = 0;
                        }
                    }
                    else if (noteType == 1)
                    {
                        if (bomb.PosX == 2)
                        {
                            state = ParityState.Forehand;
                            rotation = 0;
                        }
                    }
                }
            });

            BaseNote prevNote = null;
            var expectedDirection = NoteDirection.ANY;
            foreach (var note in noteContext) {
                if (note.Type != 0 && note.Type != 1)
                {
                    continue;
                }
                if (note.CutDirection != NoteDirection.ANY)
                {
                    expectedDirection = note.CutDirection;
                }
                if(prevNote != null)
                {
                    if (expectedDirection == NoteDirection.ANY)
                    {
                        expectedDirection = PredictDirection(note, prevNote);
                    }
                }
                prevNote = note;
            }
            if (expectedDirection != NoteDirection.ANY)
            {
                if(color == 0)
                {
                    if(state == ParityState.Forehand)
                    {
                        rotation = NoteParityRotation.RedForehand[expectedDirection];
                    }
                    else if (state == ParityState.Backhand)
                    {
                        rotation = NoteParityRotation.RedBackhand[expectedDirection];
                    }
                }
                else if (color == 1)
                {
                    if (state == ParityState.Forehand)
                    {
                        rotation = NoteParityRotation.BlueForehand[expectedDirection];
                    }
                    else if (state == ParityState.Backhand)
                    {
                        rotation = NoteParityRotation.BlueBackhand[expectedDirection];
                    }
                }
            }
        }

        public ParityState ParitySwitch(ParityState? currentState)
        {
            if(currentState == ParityState.Forehand)
            {
                return ParityState.Backhand;
            }
            else if(currentState == ParityState.Backhand)
            {
                return ParityState.Forehand;
            }

            return ParityState.Neutral;
        }

        public (double, double) PredictStartPosition(List<BaseNote> nc, int type)
        {
            if (type == 0)
            {
                return (-0.5, 1);
            }
            else if (type == 1)
            {
                return (0.5, 1);
            }

            return (0, 1);
        }

        public int PredictStartRotation(List<BaseNote> nc, int color)
        {
            var rotation = 0;
            for (var i = 0; i < nc.Count; i++)
            {
                if (nc[i].Type != 0 || nc[i].Type != 1)
                {
                    continue;
                }
                var note = nc[i];
                if (note.Type != color)
                {
                    continue;
                }
                if (note.Type == color)
                {
                    var startTime = note.JsonTime;
                    for (var j = i; j < nc.Count; j++)
                    {
                        if (nc[j].JsonTime > note.JsonTime + 0.001 && startTime < note.JsonTime + 0.001)
                        {
                            break;
                        }
                        note = nc[j];
                        if (note.CutDirection != NoteDirection.ANY)
                        {
                            if (note.Type == 0)
                            {
                                return NoteInitRotation.Red[note.CutDirection];
                            }
                            else if (note.Type == 1)
                            {
                                return NoteInitRotation.Blue[note.CutDirection];
                            }
                        }
                        if (note.CutDirection == NoteDirection.ANY)
                        {
                            if (note.PosY == 0)
                            {
                                if (note.PosX == 0)
                                {
                                    if (note.Type == 0)
                                    {
                                        rotation = NoteInitRotation.Red[6];
                                    }
                                    else if (note.Type == 1)
                                    {
                                        rotation = NoteInitRotation.Blue[6];
                                    }
                                }
                                if (note.PosX == 3)
                                {
                                    if (note.Type == 0)
                                    {
                                        rotation = NoteInitRotation.Red[7];
                                    }
                                    else if (note.Type == 1)
                                    {
                                        rotation = NoteInitRotation.Blue[7];
                                    }
 
                                }
                            }
                            if (note.PosY == 1)
                            {
                                if (note.PosX == 0)
                                {
                                    if (note.Type == 0)
                                    {
                                        rotation = NoteInitRotation.Red[2];
                                    }
                                    else if (note.Type == 1)
                                    {
                                        rotation = NoteInitRotation.Blue[2];
                                    }
                                }
                                if (note.PosX == 3)
                                {
                                    if (note.Type == 0)
                                    {
                                        rotation = NoteInitRotation.Red[3];
                                    }
                                    else if (note.Type == 1)
                                    {
                                        rotation = NoteInitRotation.Blue[3];
                                    }

                                }
                            }
                            if (note.PosY == 2)
                            {
                                if (note.PosX == 0)
                                {
                                    if (note.Type == 0)
                                    {
                                        rotation = NoteInitRotation.Red[4];
                                    }
                                    else if (note.Type == 1)
                                    {
                                        rotation = NoteInitRotation.Blue[4];
                                    }
                                }
                                if (note.PosX == 3)
                                {
                                    if (note.Type == 0)
                                    {
                                        rotation = NoteInitRotation.Red[5];
                                    }
                                    else if (note.Type == 1)
                                    {
                                        rotation = NoteInitRotation.Blue[5];
                                    }

                                }
                            }
                        }
                    }
                    break;
                }
            }
            return rotation;
        }

        public ParityState PredictStartState(List<BaseNote> nc, int type)
        {
            var startParity = ParityState.Neutral;
            for (var i = 0;  i < nc.Count; i++)
            {
                if (nc[i].Type == 3)
                {
                    if (nc[i].PosY == 0)
                    {
                        if(type == 0)
                        {
                            if (nc[i].PosX == 1)
                            {
                                startParity = ParityState.Backhand;
                            }
                        }
                        else if(type == 1)
                        {
                            if (nc[i].PosX == 2)
                            {
                                startParity = ParityState.Backhand;
                            }
                        }
                    }
                    if (nc[i].PosY == 2)
                    {
                        if (type == 0)
                        {
                            if (nc[i].PosX == 1)
                            {
                                startParity = ParityState.Forehand;
                            }
                        }
                        else if (type == 1)
                        {
                            if (nc[i].PosX == 2)
                            {
                                startParity = ParityState.Forehand;
                            }
                        }
                    }
                }
                if (nc[i].Type != 0 && nc[i].Type != 1)
                {
                    continue;
                }
                var note = nc[i];
                if (note.Type == Math.Abs(type - 1))
                {
                    continue;
                }
                if (note.Type == type)
                {
                    if (startParity != ParityState.Neutral)
                    {
                        break;
                    }
                    var startTime = note.JsonTime;
                    for (var j = i; j < nc.Count; j++)
                    {
                        if (nc[j].JsonTime > note.JsonTime + 0.001 && startTime < note.JsonTime + 0.001)
                        {
                            break;
                        }
                        note = nc[j];
                        if(note.Type == 0)
                        {
                            if (NoteInitParity.RedForehand.Contains(note.CutDirection))
                            {
                                return ParityState.Backhand;
                            }
                            if (NoteInitParity.RedBackhand.Contains(note.CutDirection))
                            {
                                return ParityState.Forehand;
                            }
                        }
                        else if (note.Type == 1)
                        {
                            if (NoteInitParity.BlueForehand.Contains(note.CutDirection))
                            {
                                return ParityState.Backhand;
                            }
                            if (NoteInitParity.BlueBackhand.Contains(note.CutDirection))
                            {
                                return ParityState.Forehand;
                            }
                        }
                        if (startParity == ParityState.Neutral && note.CutDirection == NoteDirection.ANY)
                        {
                            if (note.PosY == 0)
                            {
                                startParity = ParityState.Backhand;
                            }
                            if (note.PosY > 0)
                            {
                                startParity = ParityState.Forehand;
                            }
                        }
                    }
                    break;
                }
            }
            return startParity;
        }

        public static int PredictDirection(BaseNote currNote, BaseNote prevNote)
        {
            if (IsEnd(currNote, prevNote, NoteDirection.ANY))
            {
                return currNote.CutDirection == NoteDirection.ANY ? prevNote.CutDirection : currNote.CutDirection;
            }
            if (currNote.CutDirection != NoteDirection.ANY)
            {
                return currNote.CutDirection;
            }
            if (currNote.JsonTime > prevNote.JsonTime)
            {
                // if end note on right side
                if (currNote.PosX > prevNote.PosX)
                {
                    if (currNote.PosY == prevNote.PosY)
                    {
                        return NoteDirection.RIGHT;
                    }
                }
                // if end note on left side
                if (currNote.PosX < prevNote.PosX)
                {
                    if (currNote.PosY == prevNote.PosY)
                    {
                        return NoteDirection.LEFT;
                    }
                }
                // if end note is above
                if (currNote.PosY > prevNote.PosY)
                {
                    if (currNote.PosX == prevNote.PosX)
                    {
                        return NoteDirection.UP;
                    }
                    if (currNote.PosX > prevNote.PosX)
                    {
                        return NoteDirection.UP_RIGHT;
                    }
                    if (currNote.PosX < prevNote.PosX)
                    {
                        return NoteDirection.UP_LEFT;
                    }
                }
                // if end note is below
                if (currNote.PosY < prevNote.PosY)
                {
                    if (currNote.PosX == prevNote.PosX)
                    {
                        return NoteDirection.DOWN;
                    }
                    if (currNote.PosX > prevNote.PosX)
                    {
                        return NoteDirection.DOWN_RIGHT;
                    }
                    if (currNote.PosX < prevNote.PosX)
                    {
                        return NoteDirection.DOWN_LEFT;
                    }
                }
            }
            return NoteDirection.ANY;
        }

        public static bool IsEnd(BaseNote currNote, BaseNote prevNote, int cd)
        {
            if(currNote.CutDirection == NoteDirection.ANY && prevNote.CutDirection == NoteDirection.ANY && cd != NoteDirection.ANY)
            {
                if(currNote.PosX > prevNote.PosX)
                {
                    if(cd == NoteDirection.UP_RIGHT || cd == NoteDirection.RIGHT || cd == NoteDirection.DOWN_RIGHT)
                    {
                        return true;
                    }
                }
                if(currNote.PosX < prevNote.PosX)
                {
                    if (cd == NoteDirection.DOWN_LEFT || cd == NoteDirection.LEFT || cd == NoteDirection.UP_LEFT)
                    {
                        return true;
                    }
                }
                if(currNote.PosY > prevNote.PosY)
                {
                    if(cd == NoteDirection.UP_LEFT || cd == NoteDirection.UP || cd == NoteDirection.UP_RIGHT)
                    {
                        return true;
                    }
                }
                if (currNote.PosY < prevNote.PosY)
                {
                    if (cd == NoteDirection.DOWN_LEFT || cd == NoteDirection.DOWN || cd == NoteDirection.DOWN_RIGHT)
                    {
                        return true;
                    }
                }
            }
            if(currNote.PosX > prevNote.PosX)
            {
                if(currNote.CutDirection == NoteDirection.UP_RIGHT || currNote.CutDirection == NoteDirection.RIGHT || currNote.CutDirection == NoteDirection.DOWN_RIGHT)
                {
                    return true;
                }
                if ((prevNote.CutDirection == NoteDirection.UP_RIGHT || prevNote.CutDirection == NoteDirection.RIGHT || prevNote.CutDirection == NoteDirection.DOWN_RIGHT) && currNote.CutDirection == NoteDirection.ANY)
                {
                    return true;
                }
            }
            if (currNote.PosX < prevNote.PosX)
            {
                if (currNote.CutDirection == NoteDirection.DOWN_LEFT || currNote.CutDirection == NoteDirection.LEFT || currNote.CutDirection == NoteDirection.UP_LEFT)
                {
                    return true;
                }
                if ((prevNote.CutDirection == NoteDirection.DOWN_LEFT || prevNote.CutDirection == NoteDirection.LEFT || prevNote.CutDirection == NoteDirection.UP_LEFT) && currNote.CutDirection == NoteDirection.ANY)
                {
                    return true;
                }
            }
            if (currNote.PosY > prevNote.PosY)
            {
                if (currNote.CutDirection == NoteDirection.UP_LEFT || currNote.CutDirection == NoteDirection.UP || currNote.CutDirection == NoteDirection.UP_RIGHT)
                {
                    return true;
                }
                if ((prevNote.CutDirection == NoteDirection.UP_LEFT || prevNote.CutDirection == NoteDirection.UP || prevNote.CutDirection == NoteDirection.UP_RIGHT) && currNote.CutDirection == NoteDirection.ANY)
                {
                    return true;
                }
            }
            if (currNote.PosY < prevNote.PosY)
            {
                if (currNote.CutDirection == NoteDirection.DOWN_LEFT || currNote.CutDirection == NoteDirection.DOWN || currNote.CutDirection == NoteDirection.DOWN_RIGHT)
                {
                    return true;
                }
                if ((prevNote.CutDirection == NoteDirection.DOWN_LEFT || prevNote.CutDirection == NoteDirection.DOWN || prevNote.CutDirection == NoteDirection.DOWN_RIGHT) && currNote.CutDirection == NoteDirection.ANY)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class NoteInitParity
    {
        public static List<int> RedForehand { get; set; } = new() { 1, 3, 6, 7};
        public static List<int> RedBackhand { get; set; } = new() { 0, 2, 4, 5 };
        public static List<int> BlueForehand { get; set; } = new() { 1, 2, 6, 7 };
        public static List<int> BlueBackhand{ get; set; } = new() { 0, 3, 4, 5 };
    }
    internal static class NoteInitRotation
    {
        public static List<int> Red { get; set; } = new() { 0, 0, 90, 90, 45, -45, 45, -45 };
        public static List<int> Blue { get; set; } = new() { 0, 0, -90, -90, -45, 45, -45, 45 };
    }

    internal static class NoteParityRotation
    {
        public static List<int> RedForehand { get; set; } = new() { 180, 0, -90, 90, -135, 135, -45, 45 };
        public static List<int> RedBackhand { get; set; } = new() { 0, 180, 90, -90, 45, -45, 135, -135 };
        public static List<int> BlueForehand{ get; set; } = new() { -180, 0, -90, 90, -135, 135, -45, 45 };
        public static List<int> BlueBackhand { get; set; } = new() { 0, -180, 90, -90, 45, -45, 135, -135 };
    }

    internal enum ParityState
    {
        Forehand,
        Backhand,
        Neutral
    }

    internal enum ParityStatus
    {
        Error,
        Warning,
        None
    }

    internal static class NoteDirection
    {
        public static int UP { get; set; } = 0;
        public static int DOWN { get; set; } = 1;
        public static int LEFT { get; set; } = 2;
        public static int RIGHT { get; set; } = 3;
        public static int UP_LEFT { get; set; } = 4;
        public static int UP_RIGHT { get; set; } = 5;
        public static int DOWN_LEFT { get; set; } = 6;
        public static int DOWN_RIGHT { get; set; } = 7;
        public static int ANY { get; set; } = 8;

        public static (int x, int y) Move((int x, int y) position, int direction)
        {
            switch(direction)
            {
                case 0: return (position.x, position.y + 1);
                case 1: return (position.x, position.y - 1);
                case 2: return (position.x - 1, position.y);
                case 3: return (position.x + 1, position.y);
                case 4: return (position.x - 1, position.y + 1);
                case 5: return (position.x + 1, position.y + 1);
                case 6: return (position.x - 1, position.y - 1);
                case 7: return (position.x + 1, position.y - 1);
                default: return (position.x, position.y);
            }
        }
    }
}

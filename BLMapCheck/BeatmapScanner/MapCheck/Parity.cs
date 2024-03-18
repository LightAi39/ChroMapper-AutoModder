using Parser.Map.Difficulty.V3.Grid;

namespace BLMapCheck.BeatmapScanner.MapCheck
{
    internal class Parity
    {
        public static bool IsEnd(Note currNote, Note prevNote, int cd)
        {
            if(currNote.CutDirection == NoteDirection.ANY && prevNote.CutDirection == NoteDirection.ANY && cd != NoteDirection.ANY)
            {
                if(currNote.x > prevNote.x)
                {
                    if(cd == NoteDirection.UP_RIGHT || cd == NoteDirection.RIGHT || cd == NoteDirection.DOWN_RIGHT)
                    {
                        return true;
                    }
                }
                if(currNote.x < prevNote.x)
                {
                    if (cd == NoteDirection.DOWN_LEFT || cd == NoteDirection.LEFT || cd == NoteDirection.UP_LEFT)
                    {
                        return true;
                    }
                }
                if(currNote.y > prevNote.y)
                {
                    if(cd == NoteDirection.UP_LEFT || cd == NoteDirection.UP || cd == NoteDirection.UP_RIGHT)
                    {
                        return true;
                    }
                }
                if (currNote.y < prevNote.y)
                {
                    if (cd == NoteDirection.DOWN_LEFT || cd == NoteDirection.DOWN || cd == NoteDirection.DOWN_RIGHT)
                    {
                        return true;
                    }
                }
            }
            if(currNote.x > prevNote.x)
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
            if (currNote.x < prevNote.x)
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
            if (currNote.y > prevNote.y)
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
            if (currNote.y < prevNote.y)
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

        public static (int x, int y) Move(Note note, int amount = 1)
        {
            (int x, int y) position = (note.x, note.y);
            int direction = note.CutDirection;
            switch(direction)
            {
                case 0: return (position.x, position.y + (1 * amount));
                case 1: return (position.x, position.y - (1 * amount));
                case 2: return (position.x - (1 * amount), position.y);
                case 3: return (position.x + (1 * amount), position.y);
                case 4: return (position.x - (1 * amount), position.y + (1 * amount));
                case 5: return (position.x + (1 * amount), position.y + (1 * amount));
                case 6: return (position.x - (1 * amount), position.y - (1 * amount));
                case 7: return (position.x + (1 * amount), position.y - (1 * amount));
                default: return (position.x, position.y);
            }
        }

        public static bool IsLimit((int x, int y) position, int direction)
        {
            switch (direction)
            {
                case 0: if (position.y == 2) return true; break;
                case 1: if (position.y == 0) return true; break;
                case 2: if (position.x == 0) return true; break;
                case 3: if (position.x == 3) return true; break;
                case 4:
                    if (position.y == 2) return true;
                    if (position.x == 0) return true;
                    break;
                case 5:
                    if (position.y == 2) return true;
                    if (position.x == 3) return true;
                    break;
                case 6:
                    if (position.y == 0) return true;
                    if (position.x == 0) return true;
                    break;
                case 7:
                    if (position.y == 0) return true;
                    if (position.x == 3) return true;
                    break;
                default: return false;
            }

            return false;
        }
    }
}

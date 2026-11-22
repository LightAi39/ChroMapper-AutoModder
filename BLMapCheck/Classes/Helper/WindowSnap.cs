using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.Classes.Helper
{
    // Window snapping, as the game does it (ObjectsInTimeRowProcessor): when a color has exactly two notes on the same beat, with the
    // same direction or a dot, both turn to the line between them, unless that's more than 40 degrees from the arrow. Two dots always
    // turn, a dot with a diagonal arrow turns 45 degrees further. The turn replaces the map's angle offset.
    internal static class WindowSnap
    {
        private const double MaxTurn = 40;
        // Turned angle of each note, in degrees, set per difficulty by Apply
        private static readonly ConditionalWeakTable<Note, StrongBox<double>> Turned = new();

        // Angle the game turned the note to, null if it isn't turned
        public static double? Angle(Note note) => Turned.TryGetValue(note, out var angle) ? angle.Value : null;

        // Finds the turned notes of a difficulty, call it before the checks
        public static void Apply(IEnumerable<Note> notes)
        {
            foreach (var row in notes.Where(n => n.Color == 0 || n.Color == 1).GroupBy(n => (n.Color, Math.Round(n.Seconds * 1000))))
            {
                foreach (var note in row) Turned.Remove(note);
                if (row.Count() != 2) continue;
                Note first = row.First(), second = row.Last();
                bool dot1 = first.CutDirection == 8, dot2 = second.CutDirection == 8;
                if (first.CutDirection != second.CutDirection && !dot1 && !dot2) continue;
                if (!dot1 && !IsArrow(first) || !dot2 && !IsArrow(second)) continue;
                // The arrow, if any, decides
                Note arrow = dot1 ? second : first, other = dot1 ? first : second;
                var bothDots = dot1 && dot2;
                var baseAngle = bothDots ? 90 : DirectionToDegree[arrow.CutDirection];
                var line = ConvertRadiansToDegrees(Math.Atan2(other.y - arrow.y, other.x - arrow.x));
                // Turn to the closest end of the line
                var turn = Mod(line - baseAngle + 180, 360) - 180;
                if (Math.Abs(turn) > 90) turn = Mod(turn + 360, 360) - 180;
                if (!bothDots && Math.Abs(turn) > MaxTurn) continue;
                // Dots turn from the down note rotation
                Turned.Add(arrow, new StrongBox<double>(Mod((bothDots ? 270 : baseAngle) + turn, 360)));
                var diagonal = !bothDots && arrow.CutDirection >= 4 && arrow.CutDirection <= 7;
                Turned.Add(other, new StrongBox<double>(Mod((other.CutDirection == 8 ? 270 : baseAngle) + turn + (other.CutDirection == 8 && diagonal ? 45 : 0), 360)));
            }
        }

        private static bool IsArrow(Note note) => note.CutDirection >= 0 && note.CutDirection < DirectionToDegree.Length;
    }
}

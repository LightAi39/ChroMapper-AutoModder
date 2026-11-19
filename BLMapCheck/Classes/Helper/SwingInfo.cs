using Analyzer.BeatmapScanner.Data;
using beatleader_analyzer.BeatmapScanner.Data;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.Helper
{
    // A swing as seen by the Analyzer, with the extra data the checks need
    internal class SwingInfo
    {
        public List<Note> Notes { get; private set; } = new();
        public bool RightHand { get; private set; }
        public bool Forehand { get; private set; }
        // Direction of the swing in degrees, from 0 to 360 (0 = right, 90 = up)
        public double Direction { get; private set; }
        // Keeps the parity of the previous swing of that hand
        public bool IsReset { get; private set; }
        // Reset that is explained by a bomb since the previous swing of that hand
        public bool IsBombReset { get; private set; }
        public bool IsRebound => IsReset && !IsBombReset;
        public float StartBeat { get; private set; }
        public float EndBeat { get; private set; }
        public float StartSeconds { get; private set; }
        public float EndSeconds { get; private set; }
        // Rotation of the wrist from neutral (0 = down for forehand, up for backhand), from -180 to 180
        public double StartRotation { get; private set; }
        public double EndRotation { get; private set; }
        // Speed compared to the previous swing of that hand, 0 for the first swing
        public double EBPM { get; private set; }
        public bool IsMultiNote => Notes.Count > 1;

        // Swings of both hands, ordered by time
        public static List<SwingInfo> FromRatings(Ratings ratings, List<Bomb> bombs)
        {
            var bombTimes = bombs.Select(b => b.Seconds).OrderBy(t => t).ToList();
            List<SwingInfo> swings = new();

            foreach (var hand in ratings.SwingData.Where(s => s.Cubes.Count > 0).GroupBy(s => s.Cubes[0].Type))
            {
                SwingInfo previous = null;
                foreach (var data in hand.OrderBy(s => s.Cubes[0].Seconds))
                {
                    var cubes = data.Cubes.OrderBy(c => c.Seconds).ToList();
                    var swing = new SwingInfo()
                    {
                        Notes = cubes.Select(c => c.Note).ToList(),
                        RightHand = cubes[0].Type == 1,
                        Forehand = data.Forehand,
                        Direction = Helper.Mod(data.Direction, 360),
                        IsReset = cubes[0].ParityErrors,
                        StartBeat = cubes[0].BpmTime,
                        EndBeat = cubes[cubes.Count - 1].BpmTime,
                        StartSeconds = cubes[0].Seconds,
                        EndSeconds = cubes[cubes.Count - 1].Seconds
                    };
                    swing.StartRotation = Rotation(cubes[0].Direction >= 0 ? cubes[0].Direction : data.Direction, swing.RightHand, swing.Forehand);
                    swing.EndRotation = Rotation(cubes[cubes.Count - 1].Direction >= 0 ? cubes[cubes.Count - 1].Direction : data.Direction, swing.RightHand, swing.Forehand);

                    if (previous != null)
                    {
                        swing.EBPM = 60 / (2 * (swing.StartSeconds - previous.EndSeconds));
                        // A reset takes twice the movement
                        if (previous.IsReset) swing.EBPM *= 2;
                        if (swing.IsReset)
                        {
                            swing.IsBombReset = data.BombAvoidance || bombTimes.Exists(t => t > previous.EndSeconds && t <= swing.StartSeconds);
                        }
                    }

                    swings.Add(swing);
                    previous = swing;
                }
            }

            return swings.OrderBy(s => s.StartSeconds).ThenBy(s => s.RightHand).ToList();
        }

        // Direction is in degrees (90 = up, 0 = right). Neutral is down for forehand and up for backhand.
        // Positive values turn the wrist outward, mirrored for the left hand.
        private static double Rotation(double direction, bool rightHand, bool forehand)
        {
            var rotation = Helper.Mod(direction - (forehand ? 270 : 90) + 180, 360) - 180;
            return rightHand ? rotation : -rotation;
        }
    }
}

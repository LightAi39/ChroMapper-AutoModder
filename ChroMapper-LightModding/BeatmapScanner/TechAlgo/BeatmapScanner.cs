using System.Collections.Generic;
using System.Linq;
using System;
using Beatmap.Base;
using ChroMapper_LightModding.BeatmapScanner.Data;

namespace ChroMapper_LightModding.BeatmapScanner
{
    internal class BeatmapScanner
    {
        static public List<Cube> Cubes = new();
        static public List<BaseNote> Bombs = new();
        static public List<BaseObstacle> Walls = new();
        static public List<SwingData> Datas = new();

        #region Analyzer

        public static (double diff, double tech, double ebpm, double slider, double reset, double bomb, int crouch, double linear) Analyzer(List<BaseNote> notes, List<BaseNote> bombs, List<BaseObstacle> obstacles, float bpm)
        {
            #region Prep

            var pass = 0d;
            var tech = 0d;
            var ebpm = 0d;
            var reset = 0d;
            var bomb = 0d;
            var slider = 0d;
            var crouch = 0;
            var linear = 0d;

            List<Cube> cube = new();
            List<SwingData> data = new();

            foreach (var note in notes)
            {
                cube.Add(new Cube(note));
            }

            cube.OrderBy(c => c.Time);
            var red = cube.Where(c => c.Type == 0).OrderBy(c => c.Time).ToList();
            var blue = cube.Where(c => c.Type == 1).OrderBy(c => c.Time).ToList();

            #endregion

            #region Algorithm

            var tempRed = red;
            var tempBlue = blue;

            float end;
            if (tempRed.Count > 0 && tempBlue.Count > 0)
            {
                end = Math.Max(tempRed.Last().Time, tempBlue.Last().Time);
            }
            else if (tempRed.Count > 0)
            {
                end = tempRed.Last().Time;
            }
            else
            {
                end = tempBlue.Last().Time;
            }

            var temp = end;
            if (tempRed.Count() > 0)
            {
                var length = tempRed.Count();
                while (tempRed.Count() < 50)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var note = new Cube(tempRed[i]);
                        note.Time += temp;
                        tempRed.Add(note);
                    }
                    temp = tempRed.Last().Time + 16;
                }
            }
            if (tempBlue.Count() > 0)
            {
                var length = tempBlue.Count();
                while (tempBlue.Count() < 50)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var note = new Cube(tempBlue[i]);
                        note.Time += temp;
                        tempBlue.Add(note);
                    }
                    temp = tempBlue.Last().Time + 16;
                }
            }

            (pass, tech, data) = ScanAlgo.UseLackWizAlgorithm(tempRed, tempBlue, bpm, bombs);

            if (red.Count() > 0)
            {
                ebpm = ScanMethod.GetEBPM(red, bpm);
                ScanMethod.CalculateLinear(red);
            }

            if (blue.Count() > 0)
            {
                ebpm = Math.Max(ScanMethod.GetEBPM(blue, bpm), ebpm);
                ScanMethod.CalculateLinear(blue);
            }

            #endregion

            #region Calculator

            slider = Math.Round((double)cube.Where(c => c.Slider && c.Head).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            linear = Math.Round((double)cube.Where(c => c.Linear && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            reset = Math.Round((double)data.Where(c => c.Reset).Count() / data.Count() * 100, 2);
            //bomb = Math.Round((double)data.Where(c => c.Reset && c.Bomb && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);

            // Find group of walls and list them together
            List<List<BaseObstacle>> wallsGroup = new()
            {
                new List<BaseObstacle>()
            };

            for (int i = 0; i < obstacles.Count(); i++)
            {
                wallsGroup.Last().Add(obstacles[i]);

                for (int j = i; j < obstacles.Count() - 1; j++)
                {
                    if (obstacles[j + 1].JsonTime >= obstacles[j].JsonTime && obstacles[j + 1].JsonTime <= obstacles[j].JsonTime + obstacles[j].Duration)
                    {
                        wallsGroup.Last().Add(obstacles[j + 1]);
                    }
                    else
                    {
                        i = j;
                        wallsGroup.Add(new List<BaseObstacle>());
                        break;
                    }
                }
            }

            // Find how many time the player has to crouch
            List<int> wallsFound = new();
            int count;

            foreach (var group in wallsGroup)
            {
                float found = 0f;
                count = 0;

                for (int j = 0; j < group.Count(); j++)
                {
                    var wall = group[j];

                    if (found != 0f && wall.JsonTime - found < 1.5) // Skip too close
                    {
                        continue;
                    }
                    else
                    {
                        found = 0f;
                    }

                    // Individual
                    if (wall.PosY >= 2 && wall.Width >= 3)
                    {
                        count++;
                        found = wall.JsonTime + wall.Duration;
                    }
                    else if (wall.PosY >= 2 && wall.Width >= 2 && wall.PosX == 1)
                    {
                        count++;
                        found = wall.JsonTime + wall.Duration;
                    }
                    else if (group.Count() > 1) // Multiple
                    {
                        for (int k = j + 1; k < group.Count(); k++)
                        {
                            if (k == j + 100) // So it doesn't take forever on some maps :(
                            {
                                break;
                            }

                            var other = group[k];

                            if ((wall.PosY >= 2 || other.PosY >= 2) && wall.Width >= 2 && wall.PosX == 0 && other.PosX == 2)
                            {
                                count++;
                                found = wall.JsonTime + wall.Duration;
                                break;
                            }
                            else if ((wall.PosY >= 2 || other.PosY >= 2) && other.Width >= 2 && wall.PosX == 2 && other.PosX == 0)
                            {
                                count++;
                                found = wall.JsonTime + wall.Duration;
                                break;
                            }
                            else if ((wall.PosY >= 2 || other.PosY >= 2) && wall.PosX == 1 && other.PosX == 2)
                            {
                                count++;
                                found = wall.JsonTime + wall.Duration;
                                break;
                            }
                        }
                    }
                }

                crouch += count;
            }

            #endregion

            Cubes = cube;
            Walls = obstacles;
            Bombs = bombs;
            Datas = data;
            return (Math.Round(pass, 3), Math.Round(tech, 3), Math.Round(ebpm, 3), Math.Round(slider, 3), Math.Round(reset, 3), Math.Round(bomb, 3), crouch, Math.Round(linear, 3));
        }

        #endregion
    }
}

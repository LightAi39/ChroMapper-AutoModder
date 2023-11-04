using System.Collections.Generic;
using System.Linq;
using System;
using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.Classes.MapVersion.Difficulty;

namespace BLMapCheck.BeatmapScanner
{
    internal class BeatmapScanner
    {
        static public List<Cube> Cubes = new();
        static public List<Bombnote> Bombs = new();
        static public List<Obstacle> Walls = new();
        static public List<Burstslider> Chains = new();
        static public List<SwingData> Datas = new();

        #region Analyzer

        public static (double diff, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) Analyzer(List<Colornote> notes, List<Burstslider> chains, List<Bombnote> bombs, List<Obstacle> obstacles, float bpm)
        {
            #region Prep

            var pass = 0d;
            var tech = 0d;
            var ebpm = 0d;
            var reset = 0d;
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

            (pass, tech, data) = ScanAlgo.UseLackWizAlgorithm(red, blue, bpm, bombs);

            if (red.Count() > 0)
            {
                // ebpm = ScanMethod.GetEBPM(red, bpm);
                ScanMethod.CalculateLinear(red);
            }

            if (blue.Count() > 0)
            {
                // ebpm = Math.Max(ScanMethod.GetEBPM(blue, bpm), ebpm);
                ScanMethod.CalculateLinear(blue);
            }

            cube = new(red);
            cube.AddRange(blue);
            cube = cube.OrderBy(c => c.Time).ToList();

            #endregion

            #region Calculator

            slider = Math.Round((double)cube.Where(c => c.Slider && c.Head).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            linear = Math.Round((double)cube.Where(c => c.Linear && (c.Head || !c.Pattern)).Count() / cube.Where(c => c.Head || !c.Pattern).Count() * 100, 2);
            reset = Math.Round((double)data.Where(c => c.Reset).Count() / data.Count() * 100, 2);

            // Find group of walls and list them together
            List<List<Obstacle>> wallsGroup = new()
            {
                new List<Obstacle>()
            };

            for (int i = 0; i < obstacles.Count(); i++)
            {
                wallsGroup.Last().Add(obstacles[i]);

                for (int j = i; j < obstacles.Count() - 1; j++)
                {
                    if (obstacles[j + 1].b >= obstacles[j].b && obstacles[j + 1].b <= obstacles[j].b + obstacles[j].d)
                    {
                        wallsGroup.Last().Add(obstacles[j + 1]);
                    }
                    else
                    {
                        i = j;
                        wallsGroup.Add(new List<Obstacle>());
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

                    if (found != 0f && wall.b - found < 1.5) // Skip too close
                    {
                        continue;
                    }
                    else
                    {
                        found = 0f;
                    }

                    // Individual
                    if (wall.y >= 2 && wall.w >= 3)
                    {
                        count++;
                        found = wall.b + wall.d;
                    }
                    else if (wall.y >= 2 && wall.w >= 2 && wall.x == 1)
                    {
                        count++;
                        found = wall.b + wall.d;
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

                            if ((wall.y >= 2 || other.y >= 2) && wall.w >= 2 && wall.x == 0 && other.x == 2)
                            {
                                count++;
                                found = wall.b + wall.d;
                                break;
                            }
                            else if ((wall.y >= 2 || other.y >= 2) && other.w >= 2 && wall.x == 2 && other.x == 0)
                            {
                                count++;
                                found = wall.b + wall.d;
                                break;
                            }
                            else if ((wall.y >= 2 || other.y >= 2) && wall.x == 1 && other.x == 2)
                            {
                                count++;
                                found = wall.b + wall.d;
                                break;
                            }
                        }
                    }
                }

                crouch += count;
            }

            #endregion

            Cubes = cube;
            Chains = chains;
            Walls = obstacles;
            Bombs = bombs;
            Datas = data;
            return (Math.Round(pass, 3), Math.Round(tech, 3), Math.Round(ebpm, 3), Math.Round(slider, 3), Math.Round(reset, 3), crouch, Math.Round(linear, 3), -1, "-1");
        }

        #endregion
    }
}

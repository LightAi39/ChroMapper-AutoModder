using beatleader_analyzer;
using beatleader_parser;
using BLMapCheck.BeatmapScanner.CriteriaCheck;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck
{
    public class BLMapChecker
    {
        // this should be the entry point for the program
        private bool mapLoaded = false;
        public static readonly Parse parser = new();
        public static readonly Analyze analyzer = new();
        public static BeatmapV3 map;

        public BLMapChecker(Config config = null)
        {
            if (config != null)
            {
                Config.Instance = config;
            }
        }

        public void LoadMap(string folderPath)
        {
            mapLoaded = false;

            map = parser.TryLoadPath(folderPath);
            if (map != null)
            {
                mapLoaded = true;
            }
        }

        public void LoadMap(BeatmapV3 beatmap)
        {
            mapLoaded = false;

            if(beatmap != null)
            {
                map = beatmap;
                mapLoaded = true;
            }
        }

        public void LoadMap(List<(string filename, string json)> jsonStrings, float songLength)
        {
            mapLoaded = false;

            map = parser.TryLoadString(jsonStrings, songLength).FirstOrDefault();
            if(map != null)
            {
                mapLoaded = true;
            }
        }

        public CheckResults CheckAllCriteria()
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            CriteriaCheckManager manager = new();
            manager.CheckAllCriteria();

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }

        public CheckResults CheckSongInfo()
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            CriteriaCheckManager manager = new();
            manager.CheckSongInfo();

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }

        public CheckResults ImportMod(string characteristic, string difficulty, int difficultyRank, List<string> mod)
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            GenericMod.Import(characteristic, difficulty, difficultyRank, mod);

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }

        public CheckResults CompareTimings(string characteristic, string difficulty, int difficultyRank, string targetChar, string targetDiff)
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            DifficultyTimings.Compare(characteristic, difficulty, difficultyRank, targetChar, targetDiff);

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }

        public CheckResults CheckSingleDifficulty(string characteristic, string difficulty, int difficultyRank)
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            CriteriaCheckManager manager = new();
            manager.CheckSingleDifficulty(characteristic, difficulty, difficultyRank);

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }

        public CheckResults CheckDifficultyStatistics(string characteristic, string difficulty, int difficultyRank)
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            CriteriaCheckManager manager = new();
            manager.CheckDifficultyStatistics(characteristic, difficulty, difficultyRank);

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }
    }
}

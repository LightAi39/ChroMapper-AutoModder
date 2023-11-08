using beatleader_analyzer;
using beatleader_parser;
using beatleader_parser.Timescale;
using BLMapCheck.BeatmapScanner.CriteriaCheck;
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

        public CheckResults CheckSingleDifficulty(string characteristic, string difficulty)
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            CriteriaCheckManager manager = new();
            manager.CheckSingleDifficulty(characteristic, difficulty);

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }

        public CheckResults CheckDifficultyStatistics(string characteristic, string difficulty)
        {
            CheckResults.Reset();
            if (!mapLoaded)
            {
                throw new Exception("Map not loaded");
            }

            CriteriaCheckManager manager = new();
            manager.CheckDifficultyStatistics(characteristic, difficulty);

            if (CheckResults.Instance.CheckFinished)
            {
                return CheckResults.Instance;
            }
            throw new Exception("Check was not finished correctly");
        }
    }
}

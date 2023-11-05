using beatleader_parser;
using BLMapCheck.BeatmapScanner.CriteriaCheck;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser;
using Parser.Map;
using System;
using System.Collections.Generic;


namespace BLMapCheck
{
    public class BLMapChecker
    {
        // this should be the entry point for the program
        private bool mapLoaded = false;

        public BLMapChecker(Config config = null)
        {
            if (config != null)
            {
                Config.Instance = config;
            }
        }

        public void LoadMap(string folderPath)
        {
            if (Parse.TryLoadPath(folderPath))
            {
                mapLoaded = true;
            }
        }

        public void LoadMap(BeatmapV3 Beatmap, float songLength)
        {
            BeatmapV3.Reset();
            BeatmapV3.Instance.Info = Beatmap.Info;
            BeatmapV3.Instance.Difficulties = Beatmap.Difficulties;
            BeatmapV3.Instance.SongLength = songLength;

            mapLoaded = true;
        }

        public void LoadMap(List<(string filename, string json)> jsonStrings, float songLength)
        {
            if(Parse.TryLoadString(jsonStrings, songLength))
            {
                BeatmapV3.Instance.SongLength = songLength;
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
    }
}

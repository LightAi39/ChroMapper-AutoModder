using BLMapCheck.BeatmapScanner.CriteriaCheck;
using BLMapCheck.Classes.MapVersion;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.MapVersion.Info;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public void LoadMap(string folderPath, float songLength)
        {
            BeatmapV3.Reset();
            BeatmapV3.Instance.Info = JsonConvert.DeserializeObject<InfoV3>(File.ReadAllText($"{folderPath}/Info.dat"));
            if (BeatmapV3.Instance.Info != null)
            {
                List<(string path, string difficulty, string characteristic)> difficultyFiles = new();

                foreach (var characteristic in BeatmapV3.Instance.Info._difficultyBeatmapSets)
                {
                    string characteristicName = characteristic._beatmapCharacteristicName;

                    foreach (var difficultyBeatmap in characteristic._difficultyBeatmaps)
                    {
                        string difficultyName = difficultyBeatmap._difficulty;
                        difficultyFiles.Add(new($"{difficultyBeatmap._beatmapFilename}", difficultyName, characteristicName));
                    }
                }

                foreach (var difficulty in difficultyFiles)
                {
                    if (File.ReadAllText($"{folderPath}/{difficulty.path}").Contains("_version\":\"2"))
                    {
                        DifficultyV2 v2 = JsonConvert.DeserializeObject<DifficultyV2>(File.ReadAllText($"{folderPath}/{difficulty.path}"));
                        BeatmapV3.Instance.Difficulties.Add(new(difficulty.difficulty, difficulty.characteristic, DifficultyV3.V2toV3(v2)));
                        // This might not be necessary, but I'm adding a BPM event at beat 0 just in case.
                        BeatmapV3.Instance.Difficulties.Last().Data.bpmEvents.Add(new(BeatmapV3.Instance.Info._beatsPerMinute));
                    }
                    else
                    {
                        BeatmapV3.Instance.Difficulties.Add(new(difficulty.difficulty, difficulty.characteristic, JsonConvert.DeserializeObject<DifficultyV3>(File.ReadAllText($"{folderPath}/{difficulty.path}"))));
                    }
                }

                BeatmapV3.Instance.SongLength = songLength;
            }

            mapLoaded = true;
        }

        public void LoadMap(BeatmapV3 beatmapV3, float songLength)
        {
            BeatmapV3.Reset();
            BeatmapV3.Instance.Info = beatmapV3.Info;
            BeatmapV3.Instance.Difficulties = beatmapV3.Difficulties;
            BeatmapV3.Instance.SongLength = songLength;

            mapLoaded = true;
        }

        public void LoadMap(List<(string filename, string json)> jsonStrings, float songLength)
        {
            BeatmapV3.Reset();

            BeatmapV3.Instance.Info = JsonConvert.DeserializeObject<InfoV3>(jsonStrings.Where(x => x.filename == "Info.json").FirstOrDefault().json);

            foreach (var characteristic in BeatmapV3.Instance.Info._difficultyBeatmapSets)
            {
                string characteristicName = characteristic._beatmapCharacteristicName;

                foreach (var difficultyBeatmap in characteristic._difficultyBeatmaps)
                {
                    string difficultyName = difficultyBeatmap._difficulty;
                    string json = jsonStrings.Where(x => x.filename == $"{difficultyName + characteristicName}.dat").FirstOrDefault().json;
                    BeatmapV3.Instance.Difficulties.Add(new(difficultyName, characteristicName, JsonConvert.DeserializeObject<DifficultyV3>(json)));
                }
            }

            BeatmapV3.Instance.SongLength = songLength;

            mapLoaded = true;
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

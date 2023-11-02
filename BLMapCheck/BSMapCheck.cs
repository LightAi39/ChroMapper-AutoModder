﻿using BLMapCheck.Classes.MapVersion;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.MapVersion.Info;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck
{
    public class BSMapCheck
    {
        // this should be the entry point for the program


        public void LoadMap(string folderPath)
        {
            BeatmapV3.Instance.Info = JsonConvert.DeserializeObject<InfoV3>(File.ReadAllText($"{folderPath}/Info.dat"));

            List<(string path, string difficulty, string characteristic)> difficultyFiles = new();

            foreach (var characteristic in BeatmapV3.Instance.Info._difficultyBeatmapSets)
            {
                string characteristicName = characteristic._beatmapCharacteristicName;

                foreach (var difficultyBeatmap in characteristic._difficultyBeatmaps)
                {
                    string difficultyName = difficultyBeatmap._difficulty;
                    difficultyFiles.Add(new($"{difficultyName + characteristicName}.dat", difficultyName, characteristicName));
                }
            }

            foreach (var difficulty in difficultyFiles)
            {
                BeatmapV3.Instance.Difficulties.Add(new(difficulty.difficulty, difficulty.characteristic, JsonConvert.DeserializeObject<DifficultyV3>(File.ReadAllText($"{folderPath}/{difficulty.path}"))));
            }
        }

        public void LoadMap(BeatmapV3 beatmapV3)
        {
            BeatmapV3.Instance.Info = beatmapV3.Info;
            BeatmapV3.Instance.Difficulties = beatmapV3.Difficulties;
        }
    }
}

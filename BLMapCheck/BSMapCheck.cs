using BLMapCheck.Classes.MapVersion;
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

            List<string> difficultyFiles = new();

            foreach (var characteristic in BeatmapV3.Instance.Info._difficultyBeatmapSets)
            {
                string characteristicName = characteristic._beatmapCharacteristicName;

                foreach (var difficultyBeatmap in characteristic._difficultyBeatmaps)
                {
                    string difficultyName = difficultyBeatmap._difficulty;
                    difficultyFiles.Add($"{difficultyName + characteristicName}.dat");
                }
            }

            foreach (string difficultyFile in difficultyFiles)
            {
                BeatmapV3.Instance.Difficulties.Add(JsonConvert.DeserializeObject<DifficultyV3>(File.ReadAllText($"{folderPath}/{difficultyFile}")));
            }
        }

        public void LoadMap(BeatmapV3 beatmapV3)
        {
            BeatmapV3.Instance.Info = beatmapV3.Info;
            BeatmapV3.Instance.Difficulties = beatmapV3.Difficulties;
        }
    }
}

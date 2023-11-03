using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.MapVersion.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.MapVersion
{
    public class BeatmapV3
    {
        private static BeatmapV3 _instance;

        private BeatmapV3() { }

        public static BeatmapV3 Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BeatmapV3();
                }
                return _instance;
            }
        }

        public InfoV3 Info { get; set; }
        public List<DifficultySet> Difficulties { get; set; } = new();

        public float SongLength { get; set; }

        public static BeatmapV3 Reset()
        {
            _instance = new();
            return _instance;
        }
    }

    public class DifficultySet
    {
        public string Difficulty { get; set; }
        public string Characteristic { get; set; }
        public DifficultyV3 Data { get; set; }

        public DifficultySet(string difficulty, string characteristic, DifficultyV3 data)
        {
            Difficulty = difficulty;
            Characteristic = characteristic;
            Data = data;
        }
    }
}

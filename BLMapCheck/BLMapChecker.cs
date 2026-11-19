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
    // Entry point of the library. Each instance holds its own map and config, and every check
    // returns a new CheckResults. An instance shouldn't be used by multiple threads at the same time,
    // use one instance per thread instead.
    public class BLMapChecker
    {
        private readonly Parse parser = new();
        private readonly Analyze analyzer = new();

        public BeatmapV3 Map { get; private set; }
        public Config Config { get; set; }
        // Folder of the loaded map, used for the files that aren't parsed (cover image)
        public string FolderPath { get; private set; }

        public BLMapChecker(Config config = null)
        {
            Config = config ?? new Config();
        }

        public void LoadMap(string folderPath)
        {
            Map = parser.TryLoadPath(folderPath);
            FolderPath = folderPath;
        }

        public void LoadMap(BeatmapV3 beatmap)
        {
            Map = beatmap;
            FolderPath = null;
        }

        public void LoadMap(List<(string filename, string json)> jsonStrings, float songLength)
        {
            Map = parser.TryLoadString(jsonStrings, songLength);
            FolderPath = null;
        }

        // Reload a single difficulty from disk, without reloading the rest of the map
        public void ReloadDifficulty(string folderPath, string characteristic, string difficulty)
        {
            var current = GetLoadedMap().Difficulties.FirstOrDefault(x => x.Characteristic == characteristic && x.Difficulty == difficulty);
            var reloaded = parser.TryLoadPath(folderPath, characteristic, difficulty)?.Difficulties.FirstOrDefault()?.Data;
            if (current != null && reloaded != null)
            {
                current.Data = reloaded;
            }
        }

        public CheckResults CheckAllCriteria()
        {
            return Run(manager => manager.CheckAllCriteria());
        }

        public CheckResults CheckSongInfo()
        {
            return Run(manager => manager.CheckSongInfo());
        }

        public CheckResults ImportMod(string characteristic, string difficulty, int difficultyRank, List<string> mod)
        {
            return Run(manager => GenericMod.Import(manager.Context, characteristic, difficulty, difficultyRank, mod));
        }

        public CheckResults CompareTimings(string characteristic, string difficulty, int difficultyRank, string targetChar, string targetDiff)
        {
            return Run(manager => DifficultyTimings.Compare(manager.Context, characteristic, difficulty, difficultyRank, targetChar, targetDiff));
        }

        public CheckResults CheckSingleDifficulty(string characteristic, string difficulty, int difficultyRank)
        {
            return Run(manager => manager.CheckSingleDifficulty(characteristic, difficulty, difficultyRank));
        }

        public CheckResults CheckDifficultyStatistics(string characteristic, string difficulty, int difficultyRank)
        {
            return Run(manager => manager.CheckDifficultyStatistics(characteristic, difficulty, difficultyRank));
        }

        private BeatmapV3 GetLoadedMap()
        {
            return Map ?? throw new Exception("Map not loaded");
        }

        private CheckResults Run(Action<CriteriaCheckManager> check)
        {
            CheckContext context = new(GetLoadedMap(), Config, analyzer) { FolderPath = FolderPath };
            check(new CriteriaCheckManager(context));
            context.Results.CheckFinished = true;
            return context.Results;
        }
    }
}

using BLMapCheck.BeatmapScanner.Data.Criteria;
using Parser.Map.Difficulty.V3.Base;
using System.Collections.Generic;

namespace BLMapCheck.Classes.Results
{
    public class CheckResults
    {
        internal CheckResults() { }

        public InfoCrit InfoCriteriaResult { get; set; }
        public List<DifficultyCriteriaResults> DifficultyCriteriaResults { get; set; } = new();
        public List<CheckResult> Results { get; set; } = new();

        public bool CheckFinished { get; set; } = false;

        // Difficulty that new difficulty results are attached to, set through CheckContext.SetDifficulty
        internal string CurrentCharacteristic;
        internal string CurrentDifficulty;
        internal int CurrentDifficultyRank;

        internal void CreateDiffResult(string name, Severity severity,
            string checkType, string description, List<KeyValuePair> resultData = null, List<BeatmapObject> beatmapObjects = null)
        {
            if (resultData == null) resultData = new();
            if (beatmapObjects == null) beatmapObjects = new();

            AddResult(new CheckResult()
            {
                Characteristic = CurrentCharacteristic,
                difficultyRank = CurrentDifficultyRank,
                Difficulty = CurrentDifficulty,
                Name = name,
                Severity = severity,
                CheckType = checkType,
                Description = description,
                ResultData = resultData,
                BeatmapObjects = beatmapObjects
            });
        }

        internal void CreateInfoResult(string name, Severity severity,
            string checkType, string description, List<KeyValuePair> resultData = null, List<BeatmapObject> beatmapObjects = null)
        {
            if (resultData == null) resultData = new();
            if (beatmapObjects == null) beatmapObjects = new();

            AddResult(new CheckResult()
            {
                Name = name,
                Severity = severity,
                CheckType = checkType,
                Description = description,
                ResultData = resultData,
                BeatmapObjects = beatmapObjects
            });
        }

        public void AddResult(CheckResult result)
        {
            Results.Add(result);
        }

        public void RemoveResult(CheckResult result)
        {
            Results.Remove(result);
        }

    }

    public class DifficultyCriteriaResults
    {
        public string Difficulty { get; set; }
        public string Characteristic { get; set; }
        public DiffCrit Crit { get; set; }

        public DifficultyCriteriaResults(string difficulty, string characteristic, DiffCrit crit)
        {
            Difficulty = difficulty;
            Characteristic = characteristic;
            Crit = crit;
        }
    }
}

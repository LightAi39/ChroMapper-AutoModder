using BLMapCheck.BeatmapScanner.Data.Criteria;
using Parser.Map.Difficulty.V3.Base;
using System.Collections.Generic;

namespace BLMapCheck.Classes.Results
{
    public class CheckResults
    {
        private static CheckResults _instance;

        private CheckResults() { }

        public static CheckResults Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CheckResults();
                }
                return _instance;
            }
        }

        public InfoCrit InfoCriteriaResult { get; set; }
        public List<DifficultyCriteriaResults> DifficultyCriteriaResults { get; set; } = new();
        public List<CheckResult> Results { get; set; } = new();

        public bool CheckFinished { get; set; } = false;

        public void CreateAndAddResult(string characteristic, string difficulty, string name, Severity severity,
            string checkType, string description, List<KeyValuePair> resultData = null, List<BeatmapObject> beatmapObjects = null)
        {
            if (resultData == null) resultData = new();
            if (beatmapObjects == null) beatmapObjects = new();

            Instance.AddResult(new CheckResult()
            {
                Characteristic = characteristic,
                Difficulty = difficulty,
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

        public static CheckResults Reset()
        {
            _instance = new();
            return _instance;
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

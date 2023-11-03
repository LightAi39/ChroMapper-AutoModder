using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.Classes.MapVersion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

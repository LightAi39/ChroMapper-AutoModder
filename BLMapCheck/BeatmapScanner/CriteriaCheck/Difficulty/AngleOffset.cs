using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class AngleOffset
    {
        public static void Check(List<Note> notes)
        {
            if (Configs.Config.Instance.DisplayAngleOffset)
            {
                if (notes.Any())
                {
                    var n = notes.Where(o => o.AngleOffset != 0).ToList();
                    foreach (Note note in n)
                    {
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = CriteriaCheckManager.Characteristic,
                            Difficulty = CriteriaCheckManager.Difficulty,
                            Name = "AngleOffset Note",
                            Severity = Severity.Info,
                            CheckType = "AngleOffset",
                            Description = "AngleOffset",
                            ResultData = new() { new("AngleOffset", note.AngleOffset.ToString()) },
                            BeatmapObjects = new() { note }
                        });
                    }
                }
            }
        }
    }
}

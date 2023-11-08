using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Info;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map;
using Parser.Map.Difficulty.V3.Base;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Lights = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Lights;
using Parity = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Parity;
using Slider = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Slider;
using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser.Timescale;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck
{
    public class CriteriaCheckManager
    {
        public static string Difficulty { get; set; }
        public static string Characteristic { get; set; }
        public static readonly List<string> DiffOrder = new(){ "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };
        public static Timescale timescale { get; set; }

        public void CheckAllCriteria(string characteristic, string difficulty, bool onlyStat)
        {
            BLMapChecker.map.Difficulties.OrderBy(x => DiffOrder.IndexOf(x.Difficulty));
            CheckResults.Instance.InfoCriteriaResult = AutoInfoCheck();

            if(characteristic != "" && difficulty != "")
            {
                CheckResults.Instance.DifficultyCriteriaResults.Add(new(difficulty, characteristic, AutoDiffCheck(characteristic, difficulty, onlyStat)));
            }

            CheckResults.Instance.CheckFinished = true;
            // Debug.Log(JsonConvert.SerializeObject(CheckResults.Instance, Formatting.Indented));
        }

        public InfoCrit AutoInfoCheck()
        {
            InfoCrit infoCrit = new()
            {
                SongName = SongName.Check(BLMapChecker.map.Info._songName),
                SubName = SubName.Check(BLMapChecker.map.Info._songName, BLMapChecker.map.Info._songAuthorName),
                SongAuthor = SongAuthor.Check(BLMapChecker.map.Info._songAuthorName),
                Creator = Creator.Check(BLMapChecker.map.Info._levelAuthorName),
                Offset = Offset.Check(BLMapChecker.map.Info._songTimeOffset),
                BPM = BPM.Check(BLMapChecker.map.Info._beatsPerMinute),
                DifficultyOrdering = DiffOrdering.Check(BLMapChecker.map.Difficulties, BLMapChecker.map.Info._beatsPerMinute),
                Preview = SongPreview.Check(BLMapChecker.map.Info._previewStartTime, BLMapChecker.map.Info._previewDuration)
            };

            return infoCrit;
        }

        public DiffCrit AutoDiffCheck(string characteristic, string difficulty, bool onlyStat)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;

            // Debug.Log("Current diff: " + Difficulty + Characteristic);

            Parser.Map.Difficulty.V3.Base.DifficultyV3 diff = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

            timescale = Timescale.Create(BLMapChecker.map.Info._beatsPerMinute, diff.bpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), BLMapChecker.map.Info._songTimeOffset);

            //Debug.Log(JsonConvert.SerializeObject(diff, Formatting.Indented));

            DiffAnalysis diffAnalysis;
            List<SwingData> swings;

            if (Enum.TryParse(difficulty, true, out BeatmapDifficultyRank difficultyRank))
            {
                string infoDat = JsonConvert.SerializeObject(BLMapChecker.map.Info);
                string diffDat = JsonConvert.SerializeObject(diff);

                diffAnalysis = new(infoDat, diffDat, difficultyRank);
                
                swings = diffAnalysis.GetSwingData();
            }
            else
            {
                throw new Exception("Difficulty could not be parsed to BeatmapDifficultyRank");
            }

            List<Ratings> BeatmapScannerData = new();

            if (diff.Notes.Any())
            {
                BeatmapScannerData = BLMapChecker.analyzer.GetRating(diff, characteristic, difficulty, BLMapChecker.map.Info._beatsPerMinute);
                Helper.CreateNoteData(diff.Notes);
            } else
            {
                return new(); // temporary since it also load lightshow diff, etc.
            }

            DiffCrit diffCrit = new();

            if(!onlyStat) // Skip all the criteria check
            {
                List<BeatmapGridObject> allNoteObjects = new();
                allNoteObjects.AddRange(diff.Notes);
                allNoteObjects.AddRange(diff.Bombs);
                // allNoteObjects.AddRange(diff.Chains);

                _Difficultybeatmaps difficultyBeatmap = BLMapChecker.map.Info._difficultyBeatmapSets.Where(x => x._beatmapCharacteristicName == Characteristic).FirstOrDefault()._difficultyBeatmaps.Where(x => x._difficulty == Difficulty).FirstOrDefault();

                // Debug.Log(JsonConvert.SerializeObject(difficultyBeatmap, Formatting.Indented));

                diffCrit.HotStart = HotStart.Check(allNoteObjects, diff.Walls);
                diffCrit.ColdEnd = ColdEnd.Check(allNoteObjects, diff.Walls, (float)BLMapChecker.map.SongLength);
                diffCrit.MinSongDuration = SongDuration.Check(diff.Notes);
                diffCrit.Slider = Slider.Check();
                diffCrit.DifficultyLabelSize = DifficultyLabelSize.Check(difficultyBeatmap._customData?._difficultyLabel);
                diffCrit.DifficultyName = DifficultyLabelName.Check(difficultyBeatmap._customData?._difficultyLabel);
                diffCrit.Requirement = Requirements.Check(difficultyBeatmap._customData?._requirements);
                diffCrit.NJS = NJS.Check(swings, (float)BLMapChecker.map.SongLength, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset);
                diffCrit.FusedObject = FusedObject.Check(diff.Notes, diff.Bombs, diff.Walls, diff.Chains, difficultyBeatmap._noteJumpMovementSpeed);
                diffCrit.Outside = Outside.Check((float)BLMapChecker.map.SongLength, diff.Notes, diff.Chains, diff.Bombs, diff.Walls);
                diffCrit.Light = Lights.Check((float)BLMapChecker.map.SongLength, diff.Lights, diff.lightColorEventBoxGroups, diff.Bombs);
                diffCrit.Wall = CriteriaCheck.Difficulty.Obstacle.Check(diff.Notes, diff.Walls, diff.Bombs);
                diffCrit.Chain = Chains.Check(diff.Chains, diff.Notes);
                diffCrit.Parity = Parity.Check(swings, diff.Notes);
                diffCrit.VisionBlock = VisionBlock.Check(allNoteObjects, BeatmapScannerData[0].Pass, BeatmapScannerData[0].Tech);
                diffCrit.ProlongedSwing = ProlongedSwing.Check(diff.Notes, diff.Chains);
                diffCrit.Loloppe = Loloppe.Check(diff.Notes);
                diffCrit.SwingPath = SwingPath.Check(allNoteObjects, swings, diff.Notes);
                diffCrit.Hitbox = Hitbox.HitboxCheck(diff.Notes, difficultyBeatmap._noteJumpMovementSpeed);
                diffCrit.HandClap = Handclap.Check(diff.Notes);

                Offbeat.Check(diff.Notes);
                RollingEBPM.Check(swings, diff.Notes);

            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Statistical Data",
                Difficulty = Difficulty,
                Characteristic = Characteristic,
                Severity = Severity.Info,
                CheckType = "Statistics",
                Description = "BeatmapScanner result data",
                ResultData = new()
                {
                    new("Pass", Math.Round(BeatmapScannerData[0].Pass, 2).ToString()),
                    new("Tech", Math.Round(BeatmapScannerData[0].Tech, 2).ToString()),
                    new("EBPM", diffAnalysis.GetAverageEBPM().ToString()),
                    new("Slider", Math.Round(BeatmapScannerData[0].Pattern, 2).ToString()), // TODO: rename to pattern instead
                    new("BombReset","0"), // TODO: remove or fix
                    new("Reset", diffAnalysis.GetResetCount().ToString()),
                    new("Crouch", "0"), // TODO: remove or fix
                    new("Linear", Math.Round(BeatmapScannerData[0].Linear, 2).ToString()),
                    new("SPS", diffAnalysis.GetSPS().ToString()),
                    new("Handness", diffAnalysis.GetHandedness().ToString())
                }
            });

            Characteristic = characteristic;
            Difficulty = difficulty;

            return diffCrit;
        }
    }
}

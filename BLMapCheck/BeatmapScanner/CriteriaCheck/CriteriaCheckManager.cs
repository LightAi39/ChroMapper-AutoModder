using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Info;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using Parser.Map;
using Parser.Map.Difficulty.V3.Base;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Light = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Light;
using Parity = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Parity;
using Slider = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Slider;
using beatleader_analyzer;
using beatleader_analyzer.BeatmapScanner.Data;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck
{
    public class CriteriaCheckManager
    {
        public static string Difficulty { get; set; }
        public static string Characteristic { get; set; }
        public static readonly List<string> DiffOrder = new(){ "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };

        public void CheckAllCriteria()
        {
            BeatmapV3.Instance.Difficulties.OrderBy(x => DiffOrder.IndexOf(x.Difficulty));
            CheckResults.Instance.InfoCriteriaResult = AutoInfoCheck();

            foreach (var diff in BeatmapV3.Instance.Difficulties)
            {
                Difficulty = diff.Difficulty;
                Characteristic = diff.Characteristic;

                CheckResults.Instance.DifficultyCriteriaResults.Add(new(diff.Difficulty, diff.Characteristic, AutoDiffCheck(diff.Characteristic, diff.Difficulty)));
            }

            CheckResults.Instance.CheckFinished = true;
            // Debug.Log(JsonConvert.SerializeObject(CheckResults.Instance, Formatting.Indented));
        }


        public InfoCrit AutoInfoCheck()
        {
            InfoCrit infoCrit = new()
            {
                SongName = SongName.Check(BeatmapV3.Instance.Info._songName),
                SubName = SubName.Check(BeatmapV3.Instance.Info._songName, BeatmapV3.Instance.Info._songAuthorName),
                SongAuthor = SongAuthor.Check(BeatmapV3.Instance.Info._songAuthorName),
                Creator = Creator.Check(BeatmapV3.Instance.Info._levelAuthorName),
                Offset = Offset.Check(BeatmapV3.Instance.Info._songTimeOffset),
                BPM = BPM.Check(BeatmapV3.Instance.Info._beatsPerMinute),
                DifficultyOrdering = DiffOrdering.Check(BeatmapV3.Instance.Difficulties, BeatmapV3.Instance.Info._beatsPerMinute),
                Preview = SongPreview.Check(BeatmapV3.Instance.Info._previewStartTime, BeatmapV3.Instance.Info._previewDuration)
            };

            return infoCrit;
        }

        public DiffCrit AutoDiffCheck(string characteristic, string difficulty)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;

            // Debug.Log("Current diff: " + Difficulty + Characteristic);

            Parser.Map.Difficulty.V3.Base.DifficultyV3 diff = BeatmapV3.Instance.Difficulties.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).FirstOrDefault().Data;

            BeatPerMinute bpm = BeatPerMinute.Create(BeatmapV3.Instance.Info._beatsPerMinute, diff.bpmEvents.Where(x => x.m < 10000 && x.m > 0).ToList(), BeatmapV3.Instance.Info._songTimeOffset);

            //Debug.Log(JsonConvert.SerializeObject(diff, Formatting.Indented));

            DiffAnalysis diffAnalysis;
            List<SwingData> swings;

            if (Enum.TryParse(difficulty, true, out BeatmapDifficultyRank difficultyRank))
            {
                string infoDat = JsonConvert.SerializeObject(BeatmapV3.Instance.Info);
                string diffDat = JsonConvert.SerializeObject(diff);

                diffAnalysis = new(infoDat, diffDat, difficultyRank);
                
                swings = diffAnalysis.GetSwingData();
            }
            else
            {
                throw new Exception("Difficulty could not be parsed to BeatmapDifficultyRank");
            }

            List<Ratings> BeatmapScannerData = new();

            if (diff.colorNotes.Any())
            {
                BeatmapScannerData = Analyze.GetDataOneDiff(diff, characteristic, difficulty, BeatmapV3.Instance.Info._beatsPerMinute);
                Helper.CreateNoteData(diff.colorNotes);
            } else
            {
                return new(); // temporary since it also load lightshow diff, etc.
            }

            List<BeatmapGridObject> allNoteObjects = new();
            allNoteObjects.AddRange(diff.colorNotes);
            allNoteObjects.AddRange(diff.bombNotes);
            // allNoteObjects.AddRange(diff.burstSliders);

            _Difficultybeatmaps difficultyBeatmap = BeatmapV3.Instance.Info._difficultyBeatmapSets.Where(x => x._beatmapCharacteristicName == Characteristic).FirstOrDefault()._difficultyBeatmaps.Where(x => x._difficulty == Difficulty).FirstOrDefault();

            // Debug.Log(JsonConvert.SerializeObject(difficultyBeatmap, Formatting.Indented));

            DiffCrit diffCrit = new()
            {
                HotStart = HotStart.Check(allNoteObjects, diff.obstacles),
                ColdEnd = ColdEnd.Check(allNoteObjects, diff.obstacles, (float)BeatmapV3.Instance.SongLength),
                MinSongDuration = SongDuration.Check(diff.colorNotes),
                Slider = Slider.Check(),
                DifficultyLabelSize = DifficultyLabelSize.Check(difficultyBeatmap._customData?._difficultyLabel),
                DifficultyName = DifficultyLabelName.Check(difficultyBeatmap._customData?._difficultyLabel),
                Requirement = Requirements.Check(difficultyBeatmap._customData?._requirements),
                NJS = NJS.Check(swings, (float)BeatmapV3.Instance.SongLength, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset),
                FusedObject = FusedObject.Check(diff.colorNotes, diff.bombNotes, diff.obstacles, diff.burstSliders, difficultyBeatmap._noteJumpMovementSpeed),
                Outside = Outside.Check((float)BeatmapV3.Instance.SongLength, diff.colorNotes, diff.burstSliders, diff.bombNotes, diff.obstacles),
                Light = Light.Check((float)BeatmapV3.Instance.SongLength, diff.basicBeatmapEvents, diff.lightColorEventBoxGroups, diff.bombNotes),
                Wall = Wall.Check(diff.colorNotes, diff.obstacles, diff.bombNotes),
                Chain = Chain.Check(diff.burstSliders, diff.colorNotes),
                Parity = Parity.Check(swings, diff.colorNotes),
                VisionBlock = VisionBlock.Check(allNoteObjects, BeatmapScannerData[0].Pass, BeatmapScannerData[0].Tech),
                ProlongedSwing = ProlongedSwing.Check(diff.colorNotes, diff.burstSliders),
                Loloppe = Loloppe.Check(diff.colorNotes),
                SwingPath = SwingPath.Check(allNoteObjects, swings, diff.colorNotes),
                Hitbox = Hitbox.HitboxCheck(diff.colorNotes, difficultyBeatmap._noteJumpMovementSpeed),
                HandClap = Handclap.Check(diff.colorNotes)
            };

            Offbeat.Check(diff.colorNotes);
            RollingEBPM.Check(swings, diff.colorNotes);

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

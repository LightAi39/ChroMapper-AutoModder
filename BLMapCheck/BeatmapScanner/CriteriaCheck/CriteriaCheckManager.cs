using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Info;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.MapVersion;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.MapVersion.Info;
using BLMapCheck.Classes.Results;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DifficultyV3 = BLMapCheck.Classes.MapVersion.Difficulty.DifficultyV3;
using Light = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Light;
using Parity = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Parity;
using Slider = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Slider;

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

            DifficultyV3 diff = BeatmapV3.Instance.Difficulties.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).FirstOrDefault().Data;

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

            (double pass, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) BeatmapScannerData;

            if (diff.colorNotes.Any())
            {
                //diff.colorNotes = diff.colorNotes.OrderBy(o => o.b).ToList();
                // disabled for now, but if we have problems we can sort the objects, but it's probably not necessary, but it was done in the old method

                BeatmapScannerData = BeatmapScanner.Analyzer(diff.colorNotes, diff.burstSliders, diff.bombNotes, diff.obstacles, BeatmapV3.Instance.Info._beatsPerMinute);
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
                ColdEnd = ColdEnd.Check(allNoteObjects, diff.obstacles, BeatmapV3.Instance.SongLength),
                MinSongDuration = SongDuration.Check(),
                Slider = Slider.Check(diff.colorNotes),
                DifficultyLabelSize = DifficultyLabelSize.Check(difficultyBeatmap._customData?._difficultyLabel),
                DifficultyName = DifficultyLabelName.Check(difficultyBeatmap._customData?._difficultyLabel),
                Requirement = Requirements.Check(difficultyBeatmap._customData?._requirements),
                NJS = NJS.Check(swings, BeatmapV3.Instance.SongLength, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset),
                FusedObject = FusedObject.Check(diff.colorNotes, diff.bombNotes, diff.obstacles, diff.burstSliders, difficultyBeatmap._noteJumpMovementSpeed),
                Outside = Outside.Check(BeatmapV3.Instance.SongLength),
                Light = Light.Check(BeatmapV3.Instance.SongLength, diff.basicBeatmapEvents, diff.lightColorEventBoxGroups),
                Wall = Wall.Check(diff.colorNotes),
                Chain = Chain.Check(),
                Parity = Parity.Check(swings, diff.colorNotes),
                VisionBlock = VisionBlock.Check(allNoteObjects, BeatmapScannerData.pass, BeatmapScannerData.tech),
                ProlongedSwing = ProlongedSwing.Check(diff.colorNotes),
                Loloppe = Loloppe.Check(diff.colorNotes),
                SwingPath = SwingPath.Check(allNoteObjects, swings),
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
                    new("Pass", BeatmapScannerData.pass.ToString()),
                    new("Tech", BeatmapScannerData.tech.ToString()),
                    new("EBPM", diffAnalysis.GetAverageEBPM().ToString()),
                    new("Slider", BeatmapScannerData.slider.ToString()),
                    new("BombReset", BeatmapScannerData.reset.ToString()),
                    new("Reset", diffAnalysis.GetResetCount().ToString()),
                    new("Crouch", BeatmapScannerData.crouch.ToString()),
                    new("Linear", BeatmapScannerData.linear.ToString()),
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

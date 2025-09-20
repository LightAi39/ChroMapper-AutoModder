using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser.Timescale;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Info;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using JoshaParity;
using Parser.Map;
using Parser.Map.Difficulty.V3.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using DifficultyV3 = Parser.Map.Difficulty.V3.Base.DifficultyV3;
using Lights = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Lights;
using Parity = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Parity;
using Slider = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Slider;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck
{
    public class CriteriaCheckManager
    {
        public static Timescale Timescale;

        public static string Characteristic;
        public static string Difficulty;
        public static int DifficultyRank;

        public void CheckAllCriteria()
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            // Sort difficulty based on their rank index
            BLMapChecker.map.Difficulties = BLMapChecker.map.Difficulties.OrderBy(x => x.BeatMap._difficultyRank).ToList();
            CheckResults.Instance.InfoCriteriaResult = AutoInfoCheck();

            foreach (var diff in BLMapChecker.map.Difficulties)
            {
                Difficulty = diff.Difficulty;
                DifficultyRank = diff.BeatMap._difficultyRank;
                Characteristic = diff.Characteristic;

                CheckResults.Instance.DifficultyCriteriaResults.Add(new(Difficulty, Characteristic, AutoDiffCheck(Characteristic, Difficulty, DifficultyRank)));
            }

            CheckResults.Instance.CheckFinished = true;
        }

        public void CheckSongInfo()
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            CheckResults.Instance.InfoCriteriaResult = AutoInfoCheck();

            CheckResults.Instance.CheckFinished = true;
        }

        public void CheckSingleDifficulty(string characteristic, string difficulty, int difficultyRank)
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            CheckResults.Instance.DifficultyCriteriaResults.Add(new(difficulty, characteristic, AutoDiffCheck(characteristic, difficulty, difficultyRank)));

            CheckResults.Instance.CheckFinished = true;
        }

        public void CheckDifficultyStatistics(string characteristic, string difficulty, int difficultyRank)
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            CheckResults.Instance.Results.Add(GetDiffStatistics(characteristic, difficulty, difficultyRank));

            CheckResults.Instance.CheckFinished = true;
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

        public DiffCrit AutoDiffCheck(string characteristic, string difficulty, int difficultyRank)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;
            DifficultyRank = difficultyRank;

            DifficultySet diffSet = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic);
            DifficultyV3 diff = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

            Timescale = Timescale.Create(BLMapChecker.map.Info._beatsPerMinute, diff.bpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), BLMapChecker.map.Info._songTimeOffset);

            DiffAnalysis diffAnalysis = new(BLMapChecker.map, diffSet);
            List<SwingData> swings = diffAnalysis.GetSwingData();

            List<Ratings> BeatmapScannerData;

            _Difficultybeatmaps difficultyBeatmap = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == difficulty);
            int diffCount = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.Count();

            if (diff.Notes.Count >= 20)
            {
                BeatmapScannerData = BLMapChecker.analyzer.GetRating(diff, characteristic, difficulty, BLMapChecker.map.Info._beatsPerMinute, difficultyBeatmap._noteJumpMovementSpeed);
                Helper.CreateNoteData(diff.Notes, swings);
            }
            else
            {
                return new(); // temporary since it also load lightshow diff, etc.
            }

            List<BeatmapGridObject> allNoteObjects = new();
            allNoteObjects.AddRange(diff.Notes);
            allNoteObjects.AddRange(diff.Bombs);

            List<BeatmapGridObject> allObjects = new();
            allObjects.AddRange(allNoteObjects);
            allObjects.AddRange(diff.Chains);
            allObjects.AddRange(diff.Walls);

            // Criteria Check
            DiffCrit diffCrit = new()
            {
                // Song Boundary
                HotStart = HotStart.Check(allNoteObjects, diff.Walls),
                ColdEnd = ColdEnd.Check(allNoteObjects, diff.Walls, (float)BLMapChecker.map.SongLength),
                Outside = Outside.Check((float)BLMapChecker.map.SongLength, diff.Notes, diff.Chains, diff.Bombs, diff.Walls),
                MinSongDuration = SongDuration.Check(diff.Notes),
                // Difficulty Label
                DifficultyLabelSize = DifficultyLabelSize.Check(difficultyBeatmap._customData?._difficultyLabel, diffCount),
                DifficultyName = DifficultyLabelName.Check(difficultyBeatmap._customData?._difficultyLabel),
                Slider = Slider.Check(),
                // Object
                FusedObject = FusedObject.Check(diff.Notes, diff.Bombs, diff.Walls, diff.Chains),
                Wall = Obstacle.Check(diff.Notes, diff.Walls, diff.Bombs),
                Chain = Chains.Check(diff.Chains, diff.Notes),
                Parity = Parity.Check(swings, diff.Notes),
                VisionBlock = VisionBlock.Check(allNoteObjects, diff.Chains, BeatmapScannerData[0].Pass, BeatmapScannerData[0].Tech, difficultyBeatmap._noteJumpStartBeatOffset),
                ProlongedSwing = ProlongedSwing.Check(diff.Notes, diff.Chains),
                Loloppe = Loloppe.Check(diff.Notes),
                SwingPath = SwingPath.Check(allNoteObjects, swings, diff.Notes),
                Hitbox = Hitbox.Check(diff.Notes, difficultyBeatmap._noteJumpMovementSpeed),
                HandClap = Handclap.Check(diff.Notes),
                // Other
                Light = Lights.Check((float)BLMapChecker.map.SongLength, diff.Lights, diff.lightColorEventBoxGroups, diff.Bombs),
                Requirement = Requirements.Check(difficultyBeatmap._customData?._requirements),
                NJS = NJS.Check(difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset, allObjects)
            };

            // Info Check
            if (Config.Instance.HighlightOffbeat) Offbeat.Check();
            if (Config.Instance.HighlightInline) Inline.Check(diff.Notes);
            if (Config.Instance.DisplayFlick) RollingEBPM.Check(swings, diff.Notes);
            if (Config.Instance.DisplayFlick) Flick.Check(diff.Notes);
            if (Config.Instance.DisplayAngleOffset) AngleOffset.Check(diff.Notes);
            if (Config.Instance.DisplayShrado) Shrado.Check();
            if (Config.Instance.ChainConsistency) ChainConsistency.Check(diff.Chains);

            CheckResults.Instance.AddResult(WriteDifficultyStatistics(BeatmapScannerData, diffAnalysis));

            return diffCrit;
        }


        private CheckResult GetDiffStatistics(string characteristic, string difficulty, int difficultyRank)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;
            DifficultyRank = difficultyRank;

            DifficultyV3 diff = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

            Timescale = Timescale.Create(BLMapChecker.map.Info._beatsPerMinute, diff.bpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), BLMapChecker.map.Info._songTimeOffset);

            DiffAnalysis diffAnalysis;
            List<SwingData> swings;

            DifficultySet diffSet = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic);

            diffAnalysis = new(BLMapChecker.map, diffSet);

            swings = diffAnalysis.GetSwingData();

            List<Ratings> BeatmapScannerData = new();

            if (diff.Notes.Count >= 20)
            {
                _Difficultybeatmaps difficultyBeatmap = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == difficulty);
                BeatmapScannerData = BLMapChecker.analyzer.GetRating(diff, characteristic, difficulty, BLMapChecker.map.Info._beatsPerMinute, difficultyBeatmap._noteJumpMovementSpeed);
            } else
            {
                throw new Exception("Must have at least 20 notes");
            }


            return WriteDifficultyStatistics(BeatmapScannerData, diffAnalysis);
        }

        private CheckResult WriteDifficultyStatistics(List<Ratings> beatmapScannerData, DiffAnalysis diffAnalysis)
        {
            List<SwingData> source = diffAnalysis.swingContainer.LeftHandSwings.ToList();
            source.AddRange(diffAnalysis.swingContainer.RightHandSwings.ToList());
            var PeakEBPM = source.Max((SwingData x) => x.swingEBPM);
            return new CheckResult()
            {
                Name = "Statistical Data",
                Difficulty = Difficulty,
                difficultyRank = DifficultyRank,
                Characteristic = Characteristic,
                Severity = Severity.Data,
                CheckType = "Statistics",
                Description = "Statistical data of the difficulty",
                ResultData = new()
                {
                    new("Pass", Math.Round(beatmapScannerData[0].Pass, 2).ToString()),
                    new("Tech", Math.Round(beatmapScannerData[0].Tech, 2).ToString()),
                    new("EBPM", Math.Round(diffAnalysis.GetAverageEBPM(), 2).ToString()),
                    new("PEBPM", Math.Round(PeakEBPM, 2).ToString()),
                    new("SPS", Math.Round(diffAnalysis.GetSPS(), 2).ToString()),
                    new("Handness", $"{Math.Round(diffAnalysis.GetHandedness().Y, 2)}/{Math.Round(diffAnalysis.GetHandedness().X, 2)}"),
                    new("Duration", Math.Round(Timescale.BPM.ToRealTime(0.0625f, false) * 1000, 2).ToString())
                }
            };
        }
    }
}

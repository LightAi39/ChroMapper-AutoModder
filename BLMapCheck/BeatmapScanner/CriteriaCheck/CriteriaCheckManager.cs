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
using DifficultyV3 = Parser.Map.Difficulty.V3.Base.DifficultyV3;
using static JoshaParity.DiffAnalysis;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck
{
    public class CriteriaCheckManager
    {
        public static string Difficulty { get; set; }
        public static string Characteristic { get; set; }
        public static readonly List<string> DiffOrder = new(){ "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };
        public static Timescale timescale { get; set; }

        public void CheckAllCriteria()
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            BLMapChecker.map.Difficulties.OrderBy(x => DiffOrder.IndexOf(x.Difficulty));
            CheckResults.Instance.InfoCriteriaResult = AutoInfoCheck();

            foreach (var diff in BLMapChecker.map.Difficulties)
            {
                Difficulty = diff.Difficulty;
                Characteristic = diff.Characteristic;

                CheckResults.Instance.DifficultyCriteriaResults.Add(new(diff.Difficulty, diff.Characteristic, AutoDiffCheck(diff.Characteristic, diff.Difficulty)));
            }

            CheckResults.Instance.CheckFinished = true;
            // Debug.Log(JsonConvert.SerializeObject(CheckResults.Instance, Formatting.Indented));
        }

        public void CheckSongInfo()
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            CheckResults.Instance.InfoCriteriaResult = AutoInfoCheck();

            CheckResults.Instance.CheckFinished = true;
        }

        public void CheckSingleDifficulty(string characteristic, string difficulty)
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            CheckResults.Instance.DifficultyCriteriaResults.Add(new(difficulty, characteristic, AutoDiffCheck(characteristic, difficulty)));

            CheckResults.Instance.CheckFinished = true;
        }

        public void CheckDifficultyStatistics(string characteristic, string difficulty)
        {
            if (CheckResults.Instance.CheckFinished) CheckResults.Reset();

            CheckResults.Instance.Results.Add(GetDiffStatistics(characteristic, difficulty));

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

        public DiffCrit AutoDiffCheck(string characteristic, string difficulty)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;
            
            // Debug.Log("Current diff: " + Difficulty + Characteristic);
            
            DifficultyV3 diff = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

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

            _Difficultybeatmaps difficultyBeatmap = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == Characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == Difficulty);
            int diffCount = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic)._difficultyBeatmaps.Count();

            if (diff.Notes.Count >= 20)
            {
                BeatmapScannerData = BLMapChecker.analyzer.GetRating(diff, characteristic, difficulty, BLMapChecker.map.Info._beatsPerMinute, difficultyBeatmap._noteJumpMovementSpeed);
                Helper.CreateNoteData(diff.Notes, swings);
            } else
            {
                return new(); // temporary since it also load lightshow diff, etc.
            }

            List<BeatmapGridObject> allNoteObjects = new();
            allNoteObjects.AddRange(diff.Notes);
            allNoteObjects.AddRange(diff.Bombs);
            // allNoteObjects.AddRange(diff.Chains);

            // Debug.Log(JsonConvert.SerializeObject(difficultyBeatmap, Formatting.Indented));

            DiffCrit diffCrit = new()
            {
                HotStart = HotStart.Check(allNoteObjects, diff.Walls),
                ColdEnd = ColdEnd.Check(allNoteObjects, diff.Walls, (float)BLMapChecker.map.SongLength),
                MinSongDuration = SongDuration.Check(diff.Notes),
                Slider = Slider.Check(),
                DifficultyLabelSize = DifficultyLabelSize.Check(difficultyBeatmap._customData?._difficultyLabel, diffCount),
                DifficultyName = DifficultyLabelName.Check(difficultyBeatmap._customData?._difficultyLabel),
                Requirement = Requirements.Check(difficultyBeatmap._customData?._requirements),
                NJS = NJS.Check(swings, (float)BLMapChecker.map.SongLength, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset),
                FusedObject = FusedObject.Check(diff.Notes, diff.Bombs, diff.Walls, diff.Chains, difficultyBeatmap._noteJumpMovementSpeed),
                Outside = Outside.Check((float)BLMapChecker.map.SongLength, diff.Notes, diff.Chains, diff.Bombs, diff.Walls),
                Light = Lights.Check((float)BLMapChecker.map.SongLength, diff.Lights, diff.lightColorEventBoxGroups, diff.Bombs),
                Wall = CriteriaCheck.Difficulty.Obstacle.Check(diff.Notes, diff.Walls, diff.Bombs),
                Chain = Chains.Check(diff.Chains, diff.Notes),
                Parity = Parity.Check(swings, diff.Notes),
                VisionBlock = VisionBlock.Check(allNoteObjects, diff.Chains, BeatmapScannerData[0].Pass, BeatmapScannerData[0].Tech, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset),
                ProlongedSwing = ProlongedSwing.Check(diff.Notes, diff.Chains),
                Loloppe = Loloppe.Check(diff.Notes),
                SwingPath = SwingPath.Check(allNoteObjects, swings, diff.Notes),
                Hitbox = Hitbox.HitboxCheck(diff.Notes, difficultyBeatmap._noteJumpMovementSpeed),
                HandClap = Handclap.Check(diff.Notes)
            };
            Offbeat.Check(diff.Notes);
            Inline.Check(diff.Notes, difficultyBeatmap._noteJumpMovementSpeed);
            RollingEBPM.Check(swings, diff.Notes);
            Flick.Check(diff.Notes);
            AngleOffset.Check(diff.Notes);
            Shrado.Check(diff.Notes);

            CheckResults.Instance.AddResult(WriteDifficultyStatistics(BeatmapScannerData, diffAnalysis));

            return diffCrit;
        }


        private CheckResult GetDiffStatistics(string characteristic, string difficulty)
        {
            Difficulty = difficulty;
            Characteristic = characteristic;

            DifficultyV3 diff = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

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

            if (diff.Notes.Count >= 20)
            {
                _Difficultybeatmaps difficultyBeatmap = BLMapChecker.map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == Characteristic)._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == Difficulty);
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
                Characteristic = Characteristic,
                Severity = Severity.Info,
                CheckType = "Statistics",
                Description = "Statistical data of the difficulty",
                ResultData = new()
                {
                    new("Pass", Math.Round(beatmapScannerData[0].Pass, 2).ToString()),
                    new("Tech", Math.Round(beatmapScannerData[0].Tech, 2).ToString()),
                    new("EBPM", Math.Round(diffAnalysis.GetAverageEBPM(), 2).ToString()),
                    new("PEBPM", Math.Round(PeakEBPM, 2).ToString()),
                    new("SPS", Math.Round(diffAnalysis.GetSPS(), 2).ToString()),
                    new("Handness", $"{Math.Round(diffAnalysis.GetHandedness().Y, 2)}/{Math.Round(diffAnalysis.GetHandedness().X, 2)}")
                }
            };
        }
    }
}

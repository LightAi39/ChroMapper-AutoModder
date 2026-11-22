using Analyzer.BeatmapScanner.Data;
using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser.Timescale;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Optional;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Info;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
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
    internal class CriteriaCheckManager
    {
        public CheckContext Context { get; }

        private BeatmapV3 Map => Context.Map;

        public CriteriaCheckManager(CheckContext context)
        {
            Context = context;
        }

        public void CheckAllCriteria()
        {
            Context.Results.InfoCriteriaResult = AutoInfoCheck();

            // Check difficulties based on their rank index
            foreach (var diff in Map.Difficulties.OrderBy(x => x.BeatMap._difficultyRank))
            {
                Context.Results.DifficultyCriteriaResults.Add(new(diff.Difficulty, diff.Characteristic, AutoDiffCheck(diff.Characteristic, diff.Difficulty, diff.BeatMap._difficultyRank)));
            }
        }

        public void CheckSongInfo()
        {
            Context.Results.InfoCriteriaResult = AutoInfoCheck();
        }

        public void CheckSingleDifficulty(string characteristic, string difficulty, int difficultyRank)
        {
            Context.Results.DifficultyCriteriaResults.Add(new(difficulty, characteristic, AutoDiffCheck(characteristic, difficulty, difficultyRank)));
        }

        public void CheckDifficultyStatistics(string characteristic, string difficulty, int difficultyRank)
        {
            // No statistics for difficulties that can't be rated
            var statistics = GetDiffStatistics(characteristic, difficulty, difficultyRank);
            if (statistics != null) Context.Results.AddResult(statistics);
        }

        public InfoCrit AutoInfoCheck()
        {
            InfoCrit infoCrit = new()
            {
                SongName = SongName.Check(Context, Map.Info._songName),
                SubName = SubName.Check(Context, Map.Info._songName, Map.Info._songAuthorName, Map.Info._songSubName),
                SongAuthor = SongAuthor.Check(Context, Map.Info._songAuthorName),
                Creator = Creator.Check(Context, Map.Info._levelAuthorName),
                Offset = Offset.Check(Context, Map.Info._songTimeOffset),
                // The BPM and the notes must line up with the song
                BPM = (InfoCrit.CritResult)Math.Max((int)BPM.Check(Context, Map.Info._beatsPerMinute, Map.Info._songFilename), 
                    Math.Max((int)NoteTiming.Check(Context, Map.Info._songFilename, Map.Difficulties), (int)GridDrift.Check(Context, Map.Info._songFilename, Map.Difficulties))),
                DifficultyOrdering = DiffOrdering.Check(Context, Map.Difficulties),
                DifficultySimilarity = DiffSimilarity.Check(Context, Map.Difficulties),
                Preview = SongPreview.Check(Context, Map.Info._previewStartTime, Map.Info._previewDuration),
                // Info.dat values include the audio file: its format, and its quality
                InfoValues = (InfoCrit.CritResult)Math.Max((int)InfoValues.Check(Context, Map.Info), (int)SongAudio.Check(Context, Map.Info._songFilename)),
                CoverImage = CoverImage.Check(Context, Map.Info._coverImageFilename)
            };

            return infoCrit;
        }

        public DiffCrit AutoDiffCheck(string characteristic, string difficulty, int difficultyRank)
        {
            DifficultySet diffSet = SelectDifficulty(characteristic, difficulty, difficultyRank);
            DifficultyV3 diff = diffSet.Data;

            // Lightshows and other difficulties with too few notes can't be rated
            if (diff.Notes.Count < 20)
            {
                Context.Results.CreateDiffResult("Not Checked", Severity.Inconclusive, "Difficulty", "Difficulty has less than 20 notes, the criteria were not checked",
                    new() { new("NoteCount", diff.Notes.Count.ToString()) });
                return DiffCrit.NotChecked();
            }

            var difficultySet = Map.Info._difficultyBeatmapSets.FirstOrDefault(x => x._beatmapCharacteristicName == characteristic);
            _Difficultybeatmaps difficultyBeatmap = difficultySet._difficultyBeatmaps.FirstOrDefault(x => x._difficulty == difficulty);
            int diffCount = difficultySet._difficultyBeatmaps.Count();

            // The parser keeps objects in file order, but most checks expect them to be sorted by beat
            var notes = diff.Notes.OrderBy(x => x.Beats).ToList();
            var bombs = diff.Bombs.OrderBy(x => x.Beats).ToList();
            var chains = diff.Chains.OrderBy(x => x.Beats).ToList();
            var walls = diff.Walls.OrderBy(x => x.Beats).ToList();

            InfoCrit.CritResult CheckRequirements() => (InfoCrit.CritResult)Math.Max((int)Requirements.Check(Context, difficultyBeatmap._customData?._requirements),
                (int)ModdedObjects.Check(Context, difficultyBeatmap._beatmapFilename, notes, bombs, chains, diff.Arcs, walls, diff.Rotations, diff.SourceVersion));

            // Noodle Extensions and Mapping Extensions place notes freely, the grid based checks would flag almost every note
            if (ModdedObjects.IsModdedDifficulty(difficultyBeatmap._customData?._requirements, notes))
            {
                Context.Results.CreateDiffResult("Not Checked", Severity.Inconclusive, "Difficulty", "This difficulty uses Noodle Extensions or Mapping Extensions, the gameplay criteria were not checked");
                var moddedCrit = DiffCrit.NotChecked();
                moddedCrit.Requirement = CheckRequirements();
                return moddedCrit;
            }

            // Unsupported values (Mapping Extensions angles for example) can make the analyzer fail,
            // the other criteria depend on its swings so only the requirements are checked
            Ratings BeatmapScannerData = Context.GetRating(diffSet);
            if (BeatmapScannerData == null)
            {
                Context.Results.CreateDiffResult("Not Checked", Severity.Inconclusive, "Difficulty", "The analyzer could not rate this difficulty, it may contain unsupported values. The criteria were not checked");
                var notChecked = DiffCrit.NotChecked();
                notChecked.Requirement = CheckRequirements();
                return notChecked;
            }

            List<SwingInfo> swings = SwingInfo.FromRatings(BeatmapScannerData, bombs);
            Context.NotesData = Helper.CreateNoteData(swings);
            // The detected precision is per difficulty and never written to the config, so a manual value is kept
            Context.SliderPrecision = (Context.Config.AutomaticSliderPrecision ? Helper.DetectSliderPrecision(Context.NotesData) : null) ?? Context.Config.SliderPrecision;

            List<BeatmapGridObject> allNoteObjects = new();
            allNoteObjects.AddRange(notes);
            allNoteObjects.AddRange(bombs);
            allNoteObjects = allNoteObjects.OrderBy(x => x.Beats).ToList();

            List<BeatmapGridObject> allObjects = new();
            allObjects.AddRange(allNoteObjects);
            allObjects.AddRange(chains);
            allObjects.AddRange(walls);

            // Criteria Check
            DiffCrit diffCrit = new()
            {
                // Song Boundary
                HotStart = HotStart.Check(Context, allNoteObjects, walls),
                ColdEnd = ColdEnd.Check(Context, allNoteObjects, chains, walls, (float)Map.SongLength),
                Outside = Outside.Check(Context, (float)Map.SongLength, notes, chains, diff.Arcs, bombs, walls, diff),
                MinSongDuration = SongDuration.Check(Context, notes, chains),
                // Difficulty Label
                DifficultyLabelSize = DifficultyLabelSize.Check(Context, difficultyBeatmap._customData?._difficultyLabel, diffCount),
                DifficultyName = DifficultyLabelName.Check(Context, difficultyBeatmap._customData?._difficultyLabel, difficultyBeatmap._beatmapFilename, diff.customData?.bookmarks),
                Slider = Slider.Check(Context, swings, chains),
                // Object
                FusedObject = FusedObject.Check(Context, notes, bombs, walls, chains),
                Wall = Obstacle.Check(Context, notes, walls, bombs, chains, diff.SourceVersion),
                Chain = Chains.Check(Context, chains, notes, diff.Arcs, bombs),
                Bomb = Bombs.Check(Context, bombs, notes, chains, swings, diff.Lights, diff.lightColorEventBoxGroups),
                Parity = Parity.Check(Context, swings, diff.Arcs),
                VisionBlock = VisionBlock.Check(Context, allNoteObjects, chains, swings, BeatmapScannerData.PassRating, BeatmapScannerData.TechRating, difficultyBeatmap._noteJumpStartBeatOffset),
                ProlongedSwing = ProlongedSwing.Check(Context, notes, chains, swings),
                SwingPath = SwingPath.Check(Context, allNoteObjects, notes, chains, swings),
                Hitbox = Hitbox.Check(Context, notes, difficultyBeatmap._noteJumpMovementSpeed, swings, chains),
                HandClap = Handclap.Check(Context, notes),
                // Other
                Light = Lights.Check(Context, (float)Map.SongLength, diff.Lights, diff.lightColorEventBoxGroups),
                Requirement = CheckRequirements(),
                NJS = NJS.Check(Context, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset, allObjects, diff.njsEvents)
            };

            // Info Check
            if (Context.Config.HighlightOffbeat) Offbeat.Check(Context);
            if (Context.Config.HighlightInline) Inline.Check(Context, notes);
            if (Context.Config.DisplayFlick) RollingEBPM.Check(Context, swings, notes);
            if (Context.Config.DisplayFlick) Flick.Check(Context, notes);
            if (Context.Config.DisplayAngleOffset) AngleOffset.Check(Context, notes);
            if (Context.Config.DisplayShrado) Shrado.Check(Context);
            if (Context.Config.ChainConsistency) ChainConsistency.Check(Context, chains);

            Context.Results.AddResult(WriteDifficultyStatistics(diffSet, BeatmapScannerData, swings));

            return diffCrit;
        }


        private CheckResult GetDiffStatistics(string characteristic, string difficulty, int difficultyRank)
        {
            DifficultySet diffSet = SelectDifficulty(characteristic, difficulty, difficultyRank);
            DifficultyV3 diff = diffSet.Data;

            // Lightshows and other difficulties with too few notes can't be rated
            if (diff.Notes.Count < 20) return null;

            Ratings BeatmapScannerData = Context.GetRating(diffSet);
            if (BeatmapScannerData == null) return null;

            return WriteDifficultyStatistics(diffSet, BeatmapScannerData, SwingInfo.FromRatings(BeatmapScannerData, diff.Bombs));
        }

        // Set the difficulty as the current one in the context, and return it
        private DifficultySet SelectDifficulty(string characteristic, string difficulty, int difficultyRank)
        {
            Context.SetDifficulty(characteristic, difficulty, difficultyRank);

            DifficultySet diffSet = Map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic)
                ?? throw new Exception($"Difficulty {characteristic} {difficulty} not found");

            Context.SetBpmEvents(diffSet.Data.bpmEvents);
            Context.NotesData = new();
            WindowSnap.Apply(diffSet.Data.Notes);

            return diffSet;
        }

        private CheckResult WriteDifficultyStatistics(DifficultySet diffSet, Ratings beatmapScannerData, List<SwingInfo> swings)
        {
            // Without the acc AI, there's no star or acc rating (0)
            var acc = Context.GetAccRating(diffSet);
            var left = swings.Where(x => !x.RightHand).ToList();
            var right = swings.Where(x => x.RightHand).ToList();
            // Swings per second of each hand, over the time that hand is used
            double SPS(List<SwingInfo> hand) => hand.Count < 2 ? 0 : hand.Count / (hand.Last().EndSeconds - hand.First().StartSeconds);
            double Handedness(List<SwingInfo> hand) => swings.Count == 0 ? 0 : (double)hand.Count / swings.Count * 100;
            return new CheckResult()
            {
                Name = "Statistical Data",
                Difficulty = Context.Results.CurrentDifficulty,
                difficultyRank = Context.Results.CurrentDifficultyRank,
                Characteristic = Context.Results.CurrentCharacteristic,
                Severity = Severity.Data,
                CheckType = "Statistics",
                Description = "Statistical data of the difficulty",
                ResultData = new()
                {
                    new("Stars", Math.Round(acc?.Stars ?? 0, 2).ToString()),
                    new("Pass", Math.Round(beatmapScannerData.PassRating, 2).ToString()),
                    new("Tech", Math.Round(beatmapScannerData.TechRating, 2).ToString()),
                    new("Acc", Math.Round(acc?.AccRating ?? 0, 2).ToString()),
                    new("EBPM", Math.Round(swings.Count == 0 ? 0 : swings.Average(x => x.EBPM), 2).ToString()),
                    new("PEBPM", Math.Round(swings.Count == 0 ? 0 : swings.Max(x => x.EBPM), 2).ToString()),
                    new("SPS", Math.Round(SPS(left) + SPS(right), 2).ToString()),
                    new("Handness", $"{Math.Round(Handedness(left), 2)}/{Math.Round(Handedness(right), 2)}"),
                    new("Duration", Math.Round(Context.Timescale.BPM.ToRealTime(0.0625f, false) * 1000, 2).ToString())
                }
            };
        }
    }
}

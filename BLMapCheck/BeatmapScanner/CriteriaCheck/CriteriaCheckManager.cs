using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser.Timescale;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty;
using BLMapCheck.BeatmapScanner.CriteriaCheck.Info;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using JoshaParity;
using Newtonsoft.Json;
using Parser.Map;
using Parser.Map.Difficulty.V3.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DifficultyV3 = Parser.Map.Difficulty.V3.Base.DifficultyV3;
using Lights = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Lights;
using Parity = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Parity;
using Slider = BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty.Slider;

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

        static public readonly string NumberPattern = @"^\d+([.,]\d+)?";
        static public readonly string NumberPattern2 = @"\d+([.,]\d+)?";
        static public readonly string SpecialCharPattern = @"^[\s\W-]+";

        public DiffCrit ImportMod(string characteristic, string difficulty, List<string> mod)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;
            DiffCrit diffCrit = new();
            Severity severity = Severity.Info;

            bool loop;
            bool keyword;
            float beat;
            int endIndex;
            string comment;
            string substring;
            List<float> beats = new();

            foreach (var line in mod)
            {
                substring = line.Trim();
                keyword = false;
                comment = "";

                // SS-style format
                if (line.StartsWith("(X)"))
                {
                    severity = Severity.Error;
                }
                else if (line.StartsWith("(?)"))
                {
                    severity = Severity.Inconclusive;
                }
                else if (line.StartsWith("(S)"))
                {
                    severity = Severity.Suggestion;
                }

                // Search for first beat
                Match match = Regex.Match(line, NumberPattern2, RegexOptions.Compiled);
                if (match.Success)
                {
                    beat = TryParseFloat(match.Value);
                    if (beat >= 0)
                    {
                        do
                        {
                            loop = false;
                            beats.Add(beat);
                            endIndex = match.Index + match.Length;
                            substring = substring.Substring(endIndex).TrimStart(',', '.').Trim();

                            // If the word "and" is found, get the second number and end loop
                            if (substring.StartsWith("and"))
                            {
                                // Remove and and white space
                                substring = substring.Substring(3).Trim();
                                match = Regex.Match(substring, NumberPattern, RegexOptions.Compiled);
                                if (match.Success)
                                {
                                    keyword = true;
                                    beats.Add(TryParseFloat(match.Value));
                                    endIndex = match.Index + match.Length;
                                    substring = substring.Substring(endIndex).TrimStart(',', '.').Trim();
                                }
                            }
                            else
                            {
                                // Search for more beats on same line
                                match = Regex.Match(substring, NumberPattern, RegexOptions.Compiled);
                                if (match.Success)
                                {
                                    beat = TryParseFloat(match.Value);
                                    if (beat >= 0 && match.Index <= 2) loop = true;
                                }
                            }
                        } while (loop);
                    }

                    // Check for range
                    if (!keyword && substring.StartsWith("to"))
                    {
                        // Remove to and white space
                        string sub = substring.Substring(2).Trim();
                        // Write the whole thing on both beat
                        match = Regex.Match(sub, NumberPattern, RegexOptions.Compiled);
                        if (match.Success)
                        {
                            beats.Add(TryParseFloat(match.Value));
                        }
                        comment = line;
                    }
                    else if (!keyword && substring.StartsWith("-"))
                    {
                        // Remove to and white space
                        string sub = substring.Substring(1).Trim();
                        // Write the whole thing on both beat
                        match = Regex.Match(sub, NumberPattern, RegexOptions.Compiled);
                        if (match.Success)
                        {
                            beats.Add(TryParseFloat(match.Value));
                            comment = line;
                        }
                    }
                    else comment = substring.Trim();

                    // Remove special symbol and white space
                    comment = Regex.Replace(comment, SpecialCharPattern, "");

                    // BeatLeader difficulty compare format
                    if (line.StartsWith("- Removed"))
                    {
                        comment = comment.Insert(0, "Removed ");
                    }
                    else if (line.StartsWith("+ Added"))
                    {
                        comment = comment.Insert(0, "Added ");
                    }
                    else if (line.StartsWith("/ Modified"))
                    {
                        comment = comment.Insert(0, "Modified ");
                    }

                    DifficultyV3 current = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

                    foreach (var b in beats)
                    {
                        // Highlight all objects on beat
                        List<BeatmapObject> beatmapObject = new();
                        beatmapObject.AddRange(current.Notes.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Bombs.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Arcs.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Chains.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Walls.Where(x => x.Beats == b));
                        // Create a fake object if necessary, otherwise there won't be any comment
                        if (beatmapObject.Count == 0)
                        {
                            Parser.Map.Difficulty.V3.Grid.Note obj = new()
                            {
                                Beats = b,
                                x = 0,
                                y = 0,
                                Color = 0
                            };
                            beatmapObject.Add(obj);
                        }

                        // This only highlight the first object of the list, better than nothing.
                        CheckResults.Instance.AddResult(new CheckResult()
                        {
                            Characteristic = Characteristic,
                            Difficulty = Difficulty,
                            Name = "Mod",
                            Severity = severity,
                            CheckType = "Mod",
                            Description = comment,
                            ResultData = new(),
                            BeatmapObjects = beatmapObject
                        });
                    }
                    beats.Clear();
                }
            }

            CheckResults.Instance.CheckFinished = true;
            return diffCrit;
        }

        public float TryParseFloat(string line)
        {
            CultureInfo frenchCulture = new CultureInfo("fr-FR");
            if (float.TryParse(line, NumberStyles.Float, frenchCulture, out float result))
            {
                return result;
            }
            else if (float.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            else
            {
                string periodSeparatedNumber = line.Replace(',', '.');
                if (float.TryParse(periodSeparatedNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
                else
                {
                    return -1;
                }
            }
        }

        public DiffCrit CompareTimings(string characteristic, string difficulty)
        {
            Characteristic = characteristic;
            Difficulty = difficulty;
            DiffCrit diffCrit = new();

            DifficultyV3 target = BLMapChecker.map.Difficulties.Where(x => x.Characteristic == characteristic).OrderBy(x => DiffOrder.IndexOf(x.Difficulty)).Last().Data;
            DifficultyV3 current = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;
            if (current != null && target != null) {
                foreach (var note in current.Notes)
                {
                    if (target.Notes.Exists(x => x.Beats == note.Beats)) continue;
                    var previous = target.Notes.Where(x => x.Beats < note.Beats)?.Select(x => x.Beats).DefaultIfEmpty().Aggregate((x, y) => Math.Abs(x - note.Beats) < Math.Abs(y - note.Beats) ? x : y);
                    var next = target.Notes.Where(x => x.Beats > note.Beats)?.Select(x => x.Beats).DefaultIfEmpty().Aggregate((x, y) => Math.Abs(x - note.Beats) < Math.Abs(y - note.Beats) ? x : y);
                    List<KeyValuePair> results = new();
                    if (previous != null) results.Add(new("Previous:", previous.ToString()));
                    if (next != null) results.Add(new("Next:", next.ToString()));

                    CheckResults.Instance.AddResult(new CheckResult()
                    {
                        Characteristic = Characteristic,
                        Difficulty = Difficulty,
                        Name = "Timing",
                        Severity = Severity.Info,
                        CheckType = "Timing",
                        Description = "Timing doesn't exist in top diff",
                        ResultData = results,
                        BeatmapObjects = new() { note }
                    });
                }
            }
            CheckResults.Instance.CheckFinished = true;

            return diffCrit;
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
                Outside = Outside.Check((float)BLMapChecker.map.SongLength, diff.Notes, diff.Chains, diff.Bombs, diff.Walls),
                MinSongDuration = SongDuration.Check(diff.Notes),
                Slider = Slider.Check(),
                DifficultyLabelSize = DifficultyLabelSize.Check(difficultyBeatmap._customData?._difficultyLabel, diffCount),
                DifficultyName = DifficultyLabelName.Check(difficultyBeatmap._customData?._difficultyLabel),
                Requirement = Requirements.Check(difficultyBeatmap._customData?._requirements),
                NJS = NJS.Check(swings, (float)BLMapChecker.map.SongLength, difficultyBeatmap._noteJumpMovementSpeed, difficultyBeatmap._noteJumpStartBeatOffset),
                FusedObject = FusedObject.Check(diff.Notes, diff.Bombs, diff.Walls, diff.Chains, difficultyBeatmap._noteJumpMovementSpeed),
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
            Chains.Consistency(diff.Chains);

            CheckResults.Instance.AddResult(WriteDifficultyStatistics(BeatmapScannerData, diffAnalysis));

            return diffCrit;
        }


        private CheckResult GetDiffStatistics(string characteristic, string difficulty)
        {
            Difficulty = difficulty;
            Characteristic = characteristic;

            DifficultyV3 diff = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

            timescale = Timescale.Create(BLMapChecker.map.Info._beatsPerMinute, diff.bpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), BLMapChecker.map.Info._songTimeOffset);

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
                    new("Handness", $"{Math.Round(diffAnalysis.GetHandedness().Y, 2)}/{Math.Round(diffAnalysis.GetHandedness().X, 2)}"),
                    new("Duration", Math.Round(timescale.BPM.ToRealTime(0.0625f, false) * 1000, 2).ToString())
                }
            };
        }
    }
}

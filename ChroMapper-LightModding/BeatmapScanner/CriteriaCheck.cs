using Beatmap.Base;
using Beatmap.Enums;
using ChroMapper_LightModding.BeatmapScanner.Data;
using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.BeatmapScanner.MapCheck;
using ChroMapper_LightModding.Models;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;
using Parity = ChroMapper_LightModding.BeatmapScanner.MapCheck.Parity;

namespace ChroMapper_LightModding.BeatmapScanner
{
    internal class CriteriaCheck
    {
        #region Properties
        private Plugin plugin;
        private string characteristic;
        private int difficultyRank;
        private string difficulty;
        private BaseDifficulty baseDifficulty;
        private double songOffset;
        private BeatPerMinute bpm;
        private MapAnalyser analysedMap;
        private List<JoshaParity.SwingData> swings;
        private double averageSliderDuration;
        #endregion

        #region Constructors
        public CriteriaCheck(Plugin plugin)
        {
            this.plugin = plugin;
        }
        #endregion

        #region Method
        public InfoCrit AutoInfoCheck()
        {
            InfoCrit infoCrit = new()
            {
                SongName = SongNameCheck(),
                SubName = SubNameCheck(),
                SongAuthor = SongAuthorCheck(),
                Creator = CreatorCheck(),
                Offset = OffsetCheck(),
                BPM = BPMCheck(),
                DifficultyOrdering = DifficultyOrderingCheck(),
                Preview = PreviewCheck()
            };

            return infoCrit;
        }

        public DiffCrit AutoDiffCheck(string characteristic, int difficultyRank, string difficulty)
        {
            this.characteristic = characteristic;
            this.difficultyRank = difficultyRank;
            this.difficulty = difficulty;

            var song = plugin.BeatSaberSongContainer.Song;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            songOffset = BeatSaberSongContainer.Instance.Song.SongTimeOffset;
            bpm = BeatPerMinute.Create(BeatSaberSongContainer.Instance.Song.BeatsPerMinute, baseDifficulty.BpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), songOffset);
            analysedMap = new(song.Directory);
            swings = analysedMap.GetSwingData((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());

            DiffCrit diffCrit = new()
            {
                HotStart = HotStartCheck(),
                ColdEnd = ColdEndCheck(),
                MinSongDuration = MinSongDurationCheck(),
                Slider = SliderCheck(),
                DifficultyLabelSize = DifficultyLabelSizeCheck(),
                DifficultyName = DifficultyNameCheck(),
                Requirement = RequirementsCheck(),
                NJS = NJSCheck(),
                FusedObject = FusedObjectCheck(),
                Outside = OutsideCheck(),
                Light = LightCheck(),
                Wall = WallCheck(),
                Chain = ChainCheck(),
                Parity = ParityCheck(),
                VisionBlock = VisionBlockCheck(),
                ProlongedSwing = ProlongedSwingCheck(),
                Loloppe = LoloppeCheck(),
                SwingPath = SwingPathCheck(),
                Hitbox = HitboxCheck(),
                HandClap = HandClapCheck()
            };

            if (Plugin.configs.HighlightOffbeat)
            {
                HighlightOffbeat();
            }
            FuseBombComments();

            return diffCrit;
        }

        #endregion

        #region Info

        #region SongName

        public Severity SongNameCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongName.Count() == 0)
            {
                CreateSongInfoComment("R7A - Song Name field is empty", CommentTypesEnum.Issue);
                return Severity.Fail;
            }

            return Severity.Success;
        }

        #endregion

        #region SubName

        public Severity SubNameCheck()
        {
            var issue = Severity.Success;
            var name = BeatSaberSongContainer.Instance.Song.SongName.ToLower();
            var author = BeatSaberSongContainer.Instance.Song.SongAuthorName.ToLower();
            if (name.Count() != 0)
            {
                if (name.Contains("remix") || name.Contains("ver.") || name.Contains("feat.") || name.Contains("ft.") || name.Contains("featuring") || name.Contains("cover"))
                {
                    CreateSongInfoComment("R7B - Song Name - Tags should be in the Sub Name field", CommentTypesEnum.Issue);
                    issue = Severity.Fail;
                }
            }
            if (author.Count() != 0)
            {
                if (author.Contains("remix") || author.Contains("ver.") || author.Contains("feat.") || author.Contains("ft.") || author.Contains("featuring") || author.Contains("cover"))
                {
                    CreateSongInfoComment("R7B - Song Author - Tags should be in the Sub Name field", CommentTypesEnum.Issue);
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region SongAuthor

        public Severity SongAuthorCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongAuthorName.Count() == 0)
            {
                CreateSongInfoComment("R7C - Song Author field is empty", CommentTypesEnum.Issue);
                return Severity.Fail;
            }
            return Severity.Success;
        }

        #endregion

        #region Creator

        public Severity CreatorCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.LevelAuthorName.Count() == 0)
            {
                CreateSongInfoComment("R7C - Creator field is empty", CommentTypesEnum.Issue);
                return Severity.Fail;
            }
            if (BeatSaberSongContainer.Instance.Song.LevelAuthorName.Count() > Plugin.configs.MaxChar)
            {
                CreateSongInfoComment("R7C - Creator field is too long. Maybe use a group name instead?", CommentTypesEnum.Suggestion);
                return Severity.Warning;
            }
            return Severity.Success;
        }

        #endregion

        #region Offset

        public Severity OffsetCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongTimeOffset != 0)
            {
                CreateSongInfoComment("R7C - Song Time Offset should be 0. This is a deprecated feature", CommentTypesEnum.Issue);
                return Severity.Fail;
            }
            return Severity.Success;
        }

        #endregion

        #region BPM

        public Severity BPMCheck()
        {
            // TODO: Add automatic BPM detection
            CreateSongInfoComment("R1A - The map's BPM must be set to one of the song's BPM or a multiple of the song's BPM", CommentTypesEnum.Unsure);
            return Severity.Warning;
        }

        #endregion

        #region DiffOrdering

        // Analyze the pass rating of each difficulty and check if the order match.
        public Severity DifficultyOrderingCheck()
        {
            var passStandard = new List<double>();

            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                // TODO: make it work for each characteristic
                if (diffset.BeatmapCharacteristicName == "Standard")
                {
                    foreach (var diff in diffset.DifficultyBeatmaps)
                    {
                        BaseDifficulty baseDifficulty = BeatSaberSongContainer.Instance.Song.GetMapFromDifficultyBeatmap(diff);

                        if (baseDifficulty.Notes.Any())
                        {
                            List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
                            notes = notes.OrderBy(o => o.JsonTime).ToList();

                            if (notes.Count > 0)
                            {
                                List<BaseSlider> chains = baseDifficulty.Chains.Cast<BaseSlider>().ToList();
                                chains = chains.OrderBy(o => o.JsonTime).ToList();

                                List<BaseNote> bombs = baseDifficulty.Notes.Where(n => n.Type == 3).ToList();
                                bombs = bombs.OrderBy(b => b.JsonTime).ToList();

                                List<BaseObstacle> obstacles = baseDifficulty.Obstacles.ToList();
                                obstacles = obstacles.OrderBy(o => o.JsonTime).ToList();

                                var data = BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
                                passStandard.Add(data.diff);
                            }
                        }
                    }
                }
            }

            var order = passStandard.ToList();
            order.Sort();
            if (passStandard.SequenceEqual(order))
            {
                return Severity.Success;
            }

            CreateSongInfoComment("R7E - Difficulty Ordering is wrong\nCurrent order: " + string.Join(",", passStandard.ToArray()) + "\nExpected order: " +
                    string.Join(",", order.ToArray()), CommentTypesEnum.Issue);
            return Severity.Fail;
        }

        #endregion

        #region Preview

        public Severity PreviewCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.PreviewStartTime == 12 && BeatSaberSongContainer.Instance.Song.PreviewDuration == 10)
            {
                CreateSongInfoComment("R7C - Modify Default Song Preview", CommentTypesEnum.Suggestion);
                return Severity.Warning;
            }
            return Severity.Success;
        }

        #endregion

        #endregion

        #region Difficulty

        #region HotStart

        // Detect objects that are too early in the map, configurable setting is available
        public Severity HotStartCheck()
        {
            var issue = Severity.Success;
            var cube = BeatmapScanner.Cubes;
            cube = cube = cube.OrderBy(c => c.Time).ToList();
            var wall = BeatmapScanner.Walls;
            wall = wall.OrderBy(w => w.JsonTime).ToList();
            var limit = bpm.ToBeatTime(Plugin.configs.HotStartDuration, true);
            foreach (var c in cube)
            {
                if (c.Time < limit)
                {
                    CreateDiffCommentNote("R1E - Hot Start", CommentTypesEnum.Issue, c);
                    issue = Severity.Fail;
                }
                else break;
            }
            foreach (var w in wall)
            {
                if (w.JsonTime < limit && ((w.PosX + w.Width >= 2 && w.PosX < 2) || w.PosX == 1 || w.PosX == 2))
                {
                    CreateDiffCommentObstacle("R1E - Hot Start", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                else break;
            }
            return issue;
        }

        #endregion

        #region ColdEnd

        // Detect objects that are too late in the map, configurable setting is available
        public Severity ColdEndCheck()
        {
            var issue = Severity.Success;
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderByDescending(c => c.Time).ToList();
            var wall = BeatmapScanner.Walls;
            wall = wall.OrderByDescending(w => w.JsonTime).ToList();
            var limit = bpm.ToBeatTime(BeatSaberSongContainer.Instance.LoadedSongLength - Plugin.configs.ColdEndDuration, true);
            foreach (var c in cube)
            {
                if (c.Time > limit)
                {
                    CreateDiffCommentNote("R1E - Cold End", CommentTypesEnum.Issue, c);
                    issue = Severity.Fail;
                }
                else break;
            }
            foreach (var w in wall)
            {
                if (w.JsonTime + w.Duration > limit && ((w.PosX + w.Width >= 2 && w.PosX < 2) || w.PosX == 1 || w.PosX == 2))
                {
                    CreateDiffCommentObstacle("R1E - Cold End", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                else break;
            }
            return issue;
        }

        #endregion

        #region MinSongDuration

        // Detect if the mapped duration is above the minimum required, from first note to last note, configurable setting is available
        public Severity MinSongDurationCheck()
        {
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderBy(c => c.Time).ToList();
            var duration = bpm.ToRealTime(cube.Last().Time - cube.First().Time, true);
            if (duration < Plugin.configs.MinSongDuration)
            {
                ExtendOverallComment("R1F - Current map duration is " + duration.ToString() + "s. Minimum required duration is " + Plugin.configs.MinSongDuration.ToString() + "s.");
                return Severity.Fail;
            }

            return Severity.Success;
        }

        #endregion

        #region Slider

        // Get the average sliders precision and warn if it's not applied to all sliders in the map.
        // Also check if sliders is above 45 degree (that could use some work)
        public Severity SliderCheck()
        {
            var issue = Severity.Success;
            var cube = BeatmapScanner.Cubes.Where(c => c.Slider && !c.Head);
            cube = cube.OrderBy(c => c.Time).ToList();
            var temp = cube.Where(c => c.Spacing == 0).Select(c => c.Precision).ToList();
            if (temp.Count() == 0)
            {
                temp = cube.Where(c => c.Spacing == 1).Select(c => c.Precision).ToList();
                if (temp.Count() == 0)
                {
                    temp = cube.Where(c => c.Spacing == 2).Select(c => c.Precision).ToList();
                    if (temp.Count() == 0)
                    {
                        temp = cube.Where(c => c.Spacing == 3).Select(c => c.Precision).ToList();
                        averageSliderDuration = ScanMethod.Mode(temp).FirstOrDefault() / 4;
                    }
                    else
                    {
                        averageSliderDuration = ScanMethod.Mode(temp).FirstOrDefault() / 3;
                    }
                }
                else
                {
                    averageSliderDuration = ScanMethod.Mode(temp).FirstOrDefault() / 2;
                }
            }
            else
            {
                averageSliderDuration = ScanMethod.Mode(temp).FirstOrDefault();
            }
            if (averageSliderDuration <= 0.01785714285)
            {
                averageSliderDuration = 0.015625;
            }
            else if (averageSliderDuration <= 0.025)
            {
                averageSliderDuration = 0.02083333333;
            }
            else if (averageSliderDuration <= 0.03571428571)
            {
                averageSliderDuration = 0.03125;
            }
            else if (averageSliderDuration <= 0.05)
            {
                averageSliderDuration = 0.04166666666;
            }
            else
            {
                averageSliderDuration = 0.0625;
            }
            foreach (var c in cube)
            {
                if (c.Slider && !c.Head)
                {
                    if (!(c.Precision <= ((c.Spacing + 1) * averageSliderDuration) + 0.01 && c.Precision >= ((c.Spacing + 1) * averageSliderDuration) - 0.01))
                    {
                        // var reality = ScanMethod.RealToFraction(c.Precision, 0.01);
                        var expected = ScanMethod.RealToFraction(((c.Spacing + 1) * averageSliderDuration), 0.01);
                        CreateDiffCommentNote("R2A - Expected " + expected.N.ToString() + "/" + expected.D.ToString(), CommentTypesEnum.Unsure, c);
                        issue = Severity.Warning;
                    }
                }
            }

            cube = BeatmapScanner.Cubes.Where(c => c.Slider);
            cube = cube.OrderBy(c => c.Time).ToList();
            var red = cube.Where(c => c.Type == 0).ToList();
            var blue = cube.Where(c => c.Type == 1).ToList();

            // TODO: This could probably be done way better but idk
            for (int i = 0; i < red.Count(); i++)
            {
                List<double> dir = new();
                if (red[i].Head)
                {
                    if (red[i].CutDirection != 8)
                    {
                        dir.Add(red[i].Direction);
                    }
                    else
                    {
                        dir.Add(ScanMethod.FindAngleViaPosition(red, i + 1, i));
                    }

                    do
                    {
                        i++;
                        if (red.Count() == i)
                        {
                            break;
                        }
                        if (red[i].Head || !red[i].Pattern)
                        {
                            break;
                        }

                        dir.Add(ScanMethod.FindAngleViaPosition(red, i, i - 1));
                    } while (!red[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(degree, dir[j], 45))
                        {
                            CreateDiffCommentNote("R3F - Slider over 45°", CommentTypesEnum.Issue, red[i - dir.Count() + j]);
                            issue = Severity.Fail;
                        }
                    }

                    i--;
                }
            }

            for (int i = 0; i < blue.Count(); i++)
            {
                List<double> dir = new();

                if (blue[i].Head)
                {
                    if (blue[i].CutDirection != 8)
                    {
                        dir.Add(blue[i].Direction);
                    }
                    else
                    {
                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i + 1, i));
                    }

                    do
                    {
                        i++;
                        if (blue.Count() == i)
                        {
                            break;
                        }
                        if (blue[i].Head || !blue[i].Pattern)
                        {
                            break;
                        }

                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i, i - 1));
                    } while (!blue[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(degree, dir[j], 45))
                        {
                            CreateDiffCommentNote("R3F - Slider over 45°", CommentTypesEnum.Issue, blue[i - dir.Count() + j]);
                            issue = Severity.Fail;
                        }
                    }

                    i--;
                }
            }

            return issue;
        }

        #endregion

        #region DifficultyLabelSize

        public Severity DifficultyLabelSizeCheck()
        {
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            if (diff != null)
            {
                var data = diff.GetOrCreateCustomData();
                if (data.HasKey("_difficultyLabel"))
                {
                    if (data["_difficultyLabel"].ToString().Count() > Plugin.configs.MaxChar)
                    {
                        ExtendOverallComment("R7E - " + diff.BeatmapFilename + " difficulty label is too long. Current is " + diff.CustomData["_difficultyLabel"].ToString().Count() + " characters. Maximum " + Plugin.configs.MaxChar.ToString() + " characters.");
                        return Severity.Fail;
                    }
                }
            }
            return Severity.Success;
        }

        #endregion

        #region DifficultyName

        // Compare current label name with a list of offensive words.
        public Severity DifficultyNameCheck()
        {
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            if (diff != null)
            {
                var data = diff.GetOrCreateCustomData();
                if (data.HasKey("_difficultyLabel"))
                {
                    var label = data["_difficultyLabel"].ToString();
                    ProfanityFilter.ProfanityFilter pf = new();
                    var isProfanity = pf.ContainsProfanity(label);
                    if (isProfanity)
                    {
                        ExtendOverallComment("R7G - Difficulty name must not contain obscene content");
                        return Severity.Fail;
                    }
                }
            }

            return Severity.Success;
        }

        #endregion

        #region Requirements

        public Severity RequirementsCheck()
        {
            var issue = Severity.Success;

            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            var data = diff.GetOrCreateCustomData();
            if (data.HasKey("_requirements"))
            {
                foreach (var req in diff.CustomData["_requirements"].Values)
                {
                    CreateSongInfoComment("R1C - " + diff.BeatmapFilename + " has " + req + " requirement", CommentTypesEnum.Issue);
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region NJS

        // Warn the user if the current NJS and RT set doesn't match BeatLeader recommended value chart.
        public Severity NJSCheck()
        {
            var issue = Severity.Success;

            List<JoshaParity.SwingData> list = swings.ToList();

            List<double> sps = new();

            for (int i = 0; i < BeatSaberSongContainer.Instance.LoadedSongLength - 1; i++)
            {
                bpm.SetCurrentBPM(bpm.ToRealTime(i, true));
                var secInBeat = BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 60;
                sps.Add(list.Where(s => s.swingStartBeat > i * secInBeat && s.swingStartBeat < (i + 1) * secInBeat).Count());
            }
            sps.Sort();
            sps.Reverse();
            var peak = sps.Take((int)(6.25f / 100 * sps.Count())).Average();
            (double min, double max) NJS = (0, 0);
            (double min, double max) RT = (0, 0);

            foreach (var val in Recommended.Values)
            {
                if (val.SPS < peak)
                {
                    NJS = val.NJS;
                    RT = val.RT;
                }
            }

            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            if (diff.NoteJumpMovementSpeed <= 0)
            {
                ExtendOverallComment("R1A - NJS is currently " + diff.NoteJumpMovementSpeed);
                issue = Severity.Fail;
            }
            else
            {
                if (diff.NoteJumpMovementSpeed < NJS.min || diff.NoteJumpMovementSpeed > NJS.max)
                {
                    ExtendOverallComment("R1A - Warning - Recommended NJS is " + NJS.min.ToString() + " - " + NJS.max.ToString());
                    issue = Severity.Warning;
                }
                var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(diff.NoteJumpMovementSpeed, diff.NoteJumpStartBeatOffset, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
                var beatms = 60000 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                var reactionTime = beatms * halfJumpDuration;
                if (reactionTime < RT.min || reactionTime > RT.max)
                {
                    ExtendOverallComment("R1A - Warning - Recommended RT is " + RT.min.ToString() + " - " + RT.max.ToString());
                    issue = Severity.Warning;
                }
            }

            if (issue == Severity.Success)
            {
                ExtendOverallComment("R1A - Recommended NJS is " + NJS.min.ToString() + " - " + NJS.max.ToString());
                ExtendOverallComment("R1A - Recommended RT is " + RT.min.ToString() + " - " + RT.max.ToString());
            }

            bpm.ResetCurrentBPM();
            return issue;
        }

        #endregion

        #region FusedObject

        // Detect if objects are too close. Configurable margin (in ms)
        // TODO: There's probably a way better way to do this, can someone clean this mess
        public Severity FusedObjectCheck()
        {
            var issue = Severity.Success;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            var bombs = BeatmapScanner.Bombs.OrderBy(b => b.JsonTime).ToList();
            var walls = BeatmapScanner.Walls.OrderBy(w => w.JsonTime).ToList();
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            var njs = diff.NoteJumpMovementSpeed;

            foreach (var w in walls)
            {
                foreach (var c in cubes)
                {
                    bpm.SetCurrentBPM(c.Time);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (c.Time - (w.JsonTime + w.Duration) >= max)
                    {
                        break;
                    }
                    if (c.Time >= w.JsonTime - max && c.Time <= w.JsonTime + w.Duration + max && c.Line <= w.PosX + w.Width - 1 && c.Line >= w.PosX && c.Layer < w.PosY + w.Height && c.Layer >= w.PosY - 1)
                    {
                        CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c);
                        issue = Severity.Fail;
                    }
                }
                foreach (var b in bombs)
                {
                    bpm.SetCurrentBPM(b.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (b.JsonTime - (w.JsonTime + w.Duration) >= max)
                    {
                        break;
                    }
                    if (b.JsonTime >= w.JsonTime - max && b.JsonTime <= w.JsonTime + w.Duration + max && b.PosX <= w.PosX + w.Width - 1 && b.PosX >= w.PosX && b.PosY < w.PosY + w.Height && b.PosY >= w.PosY - 1)
                    {
                        CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b);
                        issue = Severity.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    bpm.SetCurrentBPM(c.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (c.JsonTime - (w.JsonTime + w.Duration) >= max)
                    {
                        break;
                    }
                    if (c.JsonTime >= w.JsonTime - max && c.JsonTime <= w.JsonTime + w.Duration + max && c.TailPosX <= w.PosX + w.Width - 1 && c.TailPosX >= w.PosX && c.TailPosY < w.PosY + w.Height && c.TailPosY >= w.PosY - 1)
                    {
                        CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c);
                        issue = Severity.Fail;
                    }
                }
            }

            for (int i = 0; i < cubes.Count; i++)
            {
                var c = cubes[i];
                for (int j = i + 1; j < cubes.Count; j++)
                {
                    var c2 = cubes[j];
                    bpm.SetCurrentBPM(c2.Time);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (c2.Time - c.Time >= max)
                    {
                        break;
                    }
                    if (c.Time >= c2.Time - max && c.Time <= c2.Time + max && c.Line == c2.Line && c.Layer == c2.Layer)
                    {
                        CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
                for (int j = 0; j < bombs.Count; j++)
                {
                    var b = bombs[j];
                    bpm.SetCurrentBPM(b.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (b.JsonTime - c.Time >= max)
                    {
                        break;
                    }
                    if (c.Time >= b.JsonTime - max && c.Time <= b.JsonTime + max && c.Line == b.PosX && c.Layer == b.PosY)
                    {
                        CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b);
                        issue = Severity.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    bpm.SetCurrentBPM(c2.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (c2.JsonTime - c.Time >= max)
                    {
                        break;
                    }
                    if (c.Time >= c2.JsonTime - max && c.Time <= c2.JsonTime + max && c.Line == c2.TailPosX && c.Layer == c2.TailPosY)
                    {
                        CreateDiffCommentNote("R3A - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
            }

            for (int i = 0; i < bombs.Count; i++)
            {
                var b = bombs[i];
                for (int j = i + 1; j < bombs.Count; j++)
                {
                    var b2 = bombs[j];
                    bpm.SetCurrentBPM(b2.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (b2.JsonTime - b.JsonTime >= max)
                    {
                        break;
                    }
                    if (b.JsonTime >= b2.JsonTime - max && b.JsonTime <= b2.JsonTime + max && b.PosX == b2.PosX && b.PosY == b2.PosY)
                    {
                        CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b);
                        CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b2);
                        issue = Severity.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    bpm.SetCurrentBPM(c2.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (c2.JsonTime - b.JsonTime >= max)
                    {
                        break;
                    }
                    if (b.JsonTime >= c2.JsonTime - max && b.JsonTime <= c2.JsonTime + max && b.PosX == c2.TailPosX && b.PosY == c2.TailPosY)
                    {
                        CreateDiffCommentBomb("R5D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, b);
                        CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
            }

            for (int i = 0; i < chains.Count; i++)
            {
                var c = chains[i];
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    bpm.SetCurrentBPM(c2.JsonTime);
                    var max = Math.Round(bpm.ToBeatTime(1) / njs * Plugin.configs.FusedDistance, 3);
                    if (c2.JsonTime - c.JsonTime >= max)
                    {
                        break;
                    }
                    if (c.JsonTime >= c2.JsonTime - max && c.JsonTime <= c2.JsonTime + max && c.TailPosX == c2.TailPosX && c.TailPosY == c2.TailPosY)
                    {
                        CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentLink("R2D - Cannot collide within " + max + " in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
            }

            bpm.ResetCurrentBPM();
            return issue;
        }

        #endregion

        #region Outside

        // Detect objects that are outside of the audio boundary
        public Severity OutsideCheck()
        {
            var issue = Severity.Success;
            var cubes = BeatmapScanner.Cubes;
            var chains = BeatmapScanner.Chains;
            var bombs = BeatmapScanner.Bombs;
            var walls = BeatmapScanner.Walls;

            var end = bpm.ToBeatTime(BeatSaberSongContainer.Instance.LoadedSongLength, true);
            if (cubes.Exists(c => c.Time < 0 || c.Time > end) || chains.Exists(c => c.JsonTime < 0 || c.JsonTime > end)
                || bombs.Exists(b => b.JsonTime < 0 || b.JsonTime > end) || walls.Exists(w => w.JsonTime < 0 || w.JsonTime + w.Duration > end))
            {
                ExtendOverallComment("R1B - Object outside of playable length");
                issue = Severity.Fail;
            }

            return issue;
        }

        #endregion

        #region Light

        // Fetch the average event per beat, and compare it to a configurable value
        // Also check for well-lit bombs
        public Severity LightCheck()
        {
            var issue = Severity.Success;
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            BaseDifficulty baseDifficulty = BeatSaberSongContainer.Instance.Song.GetMapFromDifficultyBeatmap(diff);
            var v3events = baseDifficulty.LightColorEventBoxGroups.OrderBy(e => e.JsonTime).ToList();
            var events = baseDifficulty.Events.OrderBy(e => e.JsonTime).ToList();
            var end = bpm.ToBeatTime(BeatSaberSongContainer.Instance.LoadedSongLength, true);
            var bombs = BeatmapScanner.Bombs.OrderBy(b => b.JsonTime).ToList();
            if (!events.Any() || !events.Exists(e => e.Type >= 0 && e.Type <= 5))
            {
                ExtendOverallComment("R6A - Map has no light");
                return Severity.Fail;
            }
            else
            {
                var lights = events.Where(e => e.Type >= 0 && e.Type <= 5).OrderBy(e => e.JsonTime).ToList();
                var average = lights.Count() / end;
                if (v3events.Count > 0)
                {
                    average = v3events.Count() / end;
                }
                if (average < Plugin.configs.AverageLightPerBeat)
                {
                    ExtendOverallComment("R6A - Map doesn't have enough light");
                    issue = Severity.Fail;
                }
                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var eventLitTime = new List<List<EventLitTime>>();
                if (v3events.Count > 0)
                {
                    ExtendOverallComment("R6A - Warning - V3 Lights detected. Bombs visibility won't be checked.");
                    issue = Severity.Warning;
                }
                else
                {
                    for (var i = 0; i < 12; i++)
                    {
                        eventLitTime.Add(new());
                    }
                    for (int i = 0; i < lights.Count; i++)
                    {
                        var ev = lights[i];
                        bpm.SetCurrentBPM(ev.JsonTime);
                        var fadeTime = bpm.ToBeatTime(Plugin.configs.LightFadeDuration, true);
                        var reactTime = bpm.ToBeatTime(Plugin.configs.LightBombReactionTime, true);
                        if (ev.IsOn || ev.IsFlash || ev.IsFade)
                        {
                            eventLitTime[ev.Type].Add(new(ev.JsonTime, true));
                            if (ev.IsFade)
                            {
                                eventLitTime[ev.Type].Add(new(ev.JsonTime + fadeTime, false));
                            }
                        }
                        if (ev.FloatValue < 0.25 || ev.IsOff)
                        {
                            eventLitTime[ev.Type].Add(new(ev.JsonTime + reactTime, false));
                        }
                    }
                    foreach (var elt in eventLitTime)
                    {
                        elt.Reverse();
                    }
                    for (int i = 0; i < bombs.Count; i++)
                    {
                        var bomb = bombs[i];
                        var isLit = false;
                        foreach (var el in eventLitTime)
                        {
                            var t = el.Find(e => e.Time < bomb.JsonTime);
                            if (t != null)
                            {
                                isLit = isLit || t.State;
                            }
                        }
                        if (!isLit)
                        {
                            CreateDiffCommentBomb("R5B - Light missing for bomb", CommentTypesEnum.Issue, bomb);
                            issue = Severity.Fail;
                        }
                    }
                }
            }
            bpm.ResetCurrentBPM();
            return issue;
        }

        public class EventState
        {
            public bool State { get; set; } = false;
            public double Time { get; set; } = 0;
            public double FadeTime { get; set; } = 0;

            public EventState(bool state, double time, double fade)
            {
                State = state;
                Time = time;
                FadeTime = fade;
            }
        }
        public class EventLitTime
        {
            public double Time { get; set; } = 0;
            public bool State { get; set; } = false;

            public EventLitTime(double time, bool state)
            {
                Time = time;
                State = state;
            }
        }

        #endregion

        #region Wall

        // Calculate dodge wall per seconds, objects hidden behind walls, walls that force players outside of boundary, walls that are too short in middle lane and negative walls.
        // Subjective and max dodge wall, min wall duration and trail duration is configurable
        // TODO: I think the dodge wall calc is pretty bad
        public Severity WallCheck()
        {
            var issue = Severity.Success;

            var walls = BeatmapScanner.Walls;
            var notes = BeatmapScanner.Cubes;
            var bombs = BeatmapScanner.Bombs;

            var leftWall = walls.Where(w => w.PosX == 1 && w.Width == 1);
            var rightWall = walls.Where(w => w.PosX == 2 && w.Width == 1);

            foreach (var w in leftWall)
            {
                var note = notes.Where(n => n.Line == 0 && !(n.Layer == 0 && w.PosY == 0 && w.Height == 1) && ((n.Layer >= w.PosY && n.Layer < w.PosY + w.Height) || (n.Layer >= 0 && w.PosY == 0 && w.Height > 1)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    CreateDiffCommentNote("R3B - Hidden behind wall", CommentTypesEnum.Issue, n);
                    issue = Severity.Fail;
                }
                var bomb = bombs.Where(b => b.PosX == 0 && !(b.PosY == 0 && w.PosY == 0 && w.Height == 1) && ((b.PosY >= w.PosY && b.PosY < w.PosY + w.Height) || (b.PosY >= 0 && w.PosY == 0 && w.Height > 1)) && b.JsonTime > w.JsonTime && b.JsonTime <= w.JsonTime + w.Duration).ToList();
                foreach (var b in bomb)
                {
                    CreateDiffCommentBomb("R5E - Hidden behind wall", CommentTypesEnum.Issue, b);
                    issue = Severity.Fail;
                }
            }

            foreach (var w in rightWall)
            {
                var note = notes.Where(n => n.Line == 3 && !(n.Layer == 0 && w.PosY == 0 && w.Height == 1) && ((n.Layer >= w.PosY && n.Layer < w.PosY + w.Height) || (n.Layer >= 0 && w.PosY == 0 && w.Height > 1)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    CreateDiffCommentNote("R3B - Hidden behind wall", CommentTypesEnum.Issue, n);
                    issue = Severity.Fail;
                }
                var bomb = bombs.Where(b => b.PosX == 3 && !(b.PosY == 0 && w.PosY == 0 && w.Height == 1) && ((b.PosY >= w.PosY && b.PosY < w.PosY + w.Height) || (b.PosY >= 0 && w.PosY == 0 && w.Height > 1)) && b.JsonTime > w.JsonTime && b.JsonTime <= w.JsonTime + w.Duration).ToList();
                foreach (var b in bomb)
                {
                    CreateDiffCommentBomb("R5E - Hidden behind wall", CommentTypesEnum.Issue, b);
                    issue = Severity.Fail;
                }
            }

            BaseObstacle previous = null;
            foreach (var w in walls)
            {
                bpm.SetCurrentBPM(w.JsonTime);
                var min = bpm.ToBeatTime(Plugin.configs.MinimumWallDuration);
                var max = bpm.ToBeatTime(Plugin.configs.ShortWallTrailDuration);

                if (w.PosY <= 0 && w.Height > 1 && ((w.PosX + w.Width == 2 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 3 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime)) ||
                    (w.PosX + w.Width == 3 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 2 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime))))
                {
                    CreateDiffCommentObstacle("R4C - Force the player to move into the outer lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                else if (w.PosY <= 0 && w.Height > 1 && ((w.Width >= 3 && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3 || w.PosX == 1)) || (w.Width >= 2 && w.PosX == 1 && w.PosY == 0 && w.Height > 0) || (w.Width >= 4 && w.PosX + w.Width >= 4 && w.PosX <= 0 && w.PosY == 0)))
                {
                    CreateDiffCommentObstacle("R4C - Force the player to move into the outer lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                if (w.Width <= 0 || w.Duration <= 0 || // Negative width or duration
                    (w.Height <= 0 && w.PosX >= 0 && w.PosX <= 3 && (w.PosY > 0 || w.PosY + w.Height >= 0)) // In or above with negative height
                    || ((w.PosX == 1 || w.PosX == 2 || (w.PosX + w.Width >= 2 && w.PosX <= 3)) && w.Height < 0)  // Under middle lane with negative height
                    || (w.PosX + w.Width >= 1 && w.PosX <= 4) && w.PosY + w.Height >= 0 && w.Height < 0) // Stretch above with negative height
                {
                    CreateDiffCommentObstacle("R4D - Must have positive width, height and duration", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                if (w.Duration < min && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3) && w.PosY + w.Height > 1 &&
                    !walls.Exists(wa => wa != w && wa.PosX + wa.Width >= w.PosX + w.Width && wa.PosX <= w.PosX + w.Width && wa.Duration >= min && w.JsonTime >= wa.JsonTime && w.JsonTime <= wa.JsonTime + wa.Duration + max))
                {
                    CreateDiffCommentObstacle("R4E - Shorter than 13.8ms in the middle two lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }

                previous = w;
            }

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                var dodge = 0d;
                var side = 0;
                var w = walls[i];
                bpm.SetCurrentBPM(w.JsonTime);
                var sec = bpm.ToBeatTime(1);
                // All the walls under 1 second
                var wallinsec = walls.Where(x => x.JsonTime < w.JsonTime && x.JsonTime >= w.JsonTime - sec).ToList();
                wallinsec.Reverse();
                if (w.PosX + w.Width == 2 && w.PosY <= 2 && w.PosY + w.Height >= 3)
                {
                    side = 2;
                    dodge++;
                }
                else if (w.PosX == 2 && w.PosY <= 2 && w.PosY + w.Height >= 3)
                {
                    side = 1;
                    dodge++;
                }
                if (dodge == 1) // Ignore non-dodge walls
                {
                    // Count the amount of dodge in the last second
                    foreach (var wall in wallinsec)
                    {
                        if (wall.PosX + wall.Width == 2 && side != 2 && wall.PosY <= 2 && wall.PosY + wall.Height >= 3)
                        {
                            side = 2;
                            dodge++;
                        }
                        else if (wall.PosX == 2 && side != 1 && wall.PosY <= 2 && wall.PosY + wall.Height >= 3)
                        {
                            side = 1;
                            dodge++;
                        }
                    }
                    if (dodge >= Plugin.configs.MaximumDodgeWallPerSecond)
                    {
                        CreateDiffCommentObstacle("R4B - Over the " + Plugin.configs.MaximumDodgeWallPerSecond + " dodge per second limit", CommentTypesEnum.Issue, w);
                        issue = Severity.Fail;
                    }
                    else if (dodge >= Plugin.configs.SubjectiveDodgeWallPerSecond)
                    {
                        CreateDiffCommentObstacle("Y4A - " + Plugin.configs.SubjectiveDodgeWallPerSecond + "+ dodge per second need justification", CommentTypesEnum.Suggestion, w);
                        issue = Severity.Warning;
                    }
                }
            }

            bpm.ResetCurrentBPM();
            return issue;
        }

        #endregion

        #region Chain

        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public Severity ChainCheck()
        {
            var issue = Severity.Success;
            var links = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            var notes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();

            if (notes.Count >= 16)
            {
                var link = links.Where(l => l.JsonTime <= notes[15].Time).ToList();
                foreach (var l in link)
                {
                    CreateDiffCommentLink("R2D - Cannot be part of the first 16 notes", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }
            else if (links.Any())
            {
                var link = links.Where(l => l.JsonTime >= notes.Last().Time).Take(16 - notes.Count).ToList();
                foreach (var l in link)
                {
                    CreateDiffCommentLink("R2D - Cannot be part of the first 16 notes", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }

            // TODO: Make this mess better idk
            foreach (var l in links)
            {
                var chain = (BaseChain)l;
                var x = Math.Abs(l.TailPosX - l.PosX) * chain.Squish;
                var y = Math.Abs(l.TailPosY - l.PosY) * chain.Squish;
                var distance = Math.Sqrt(x * x + y * y);
                var value = distance / (chain.SliceCount - 1);
                // Difference between expected and current distance, multiplied by current squish to know maximum value
                double max;
                if(l.TailPosY == l.PosY) max = Math.Round(Plugin.configs.ChainLinkVsAir / value * chain.Squish, 2);
                else max = Math.Round(Plugin.configs.ChainLinkVsAir * 1.1 / value * chain.Squish, 2);
                if (chain.Squish - 0.01 > max)
                {
                    CreateDiffCommentLink("R2D - Link spacing issue. Maximum squish for placement: " + max, CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
                var newX = l.PosX + (l.TailPosX - l.PosX) * chain.Squish;
                var newY = l.PosY + (l.TailPosY - l.PosY) * chain.Squish;
                if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                {
                    CreateDiffCommentLink("R2D - Lead too far", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
                if (l.TailJsonTime < l.JsonTime)
                {
                    CreateDiffCommentLink("R2D - Reverse Direction", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
                var note = notes.Find(x => x.Time >= l.TailJsonTime && x.Type == l.Color);
                if (note != null)
                {
                    if (l.TailJsonTime + (l.TailJsonTime - l.JsonTime) > note.Time)
                    {
                        CreateDiffCommentLink("R2D - Duration between tail and next note is too short", CommentTypesEnum.Issue, l);
                        issue = Severity.Fail;
                    }
                }

                var temp = new Cube(notes.First())
                {
                    Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[l.CutDirection] + l.AngleOffset, 360),
                    Line = l.PosX,
                    Layer = l.PosY
                };
                var temp2 = new Cube(notes.First())
                {
                    Line = l.TailPosX,
                    Layer = l.TailPosY
                };
                var temp3 = new List<Cube>
                {
                    temp,
                    temp2
                };
                if (!ScanMethod.IsSameDirection(ScanMethod.ReverseCutDirection(ScanMethod.FindAngleViaPosition(temp3, 0, 1)), temp.Direction, Plugin.configs.MaxChainRotation))
                {
                    CreateDiffCommentLink("R2D - Over 45°", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region Parity

        // JoshaParity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public Severity ParityCheck()
        {
            bool hadIssue = false;
            bool hadWarning = false;

            foreach (var swing in swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                CreateDiffCommentNotes("R2 - Parity Error", CommentTypesEnum.Issue, swing.notes);
                hadIssue = true;
            }
            foreach (var swing in swings.Where(x => x.swingEBPM == float.PositiveInfinity).ToList())
            {
                CreateDiffCommentNotes("R2 - Parity Mismatch on same beat", CommentTypesEnum.Issue, swing.notes);
                hadIssue = true;
            }

            List<JoshaParity.SwingData> rightHandSwings = swings.Where(x => x.rightHand).ToList();
            List<JoshaParity.SwingData> leftHandSwings = swings.Where(x => !x.rightHand).ToList();

            for (int i = 0; i < rightHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = rightHandSwings[i].startPos.rotation - rightHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Plugin.configs.ParityWarningAngle)
                    {
                        CreateDiffCommentNotes("Parity Warning - " + Plugin.configs.ParityWarningAngle + " degree difference", CommentTypesEnum.Unsure, rightHandSwings[i].notes);
                        hadWarning = true;
                    }
                    else if (Math.Abs(rightHandSwings[i].startPos.rotation) > 135 || Math.Abs(rightHandSwings[i].endPos.rotation) > 135)
                    {
                        if(Plugin.configs.ParityInvertedWarning)
                        {
                            CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, rightHandSwings[i].notes);
                        }
                        hadWarning = true;
                    }
                }
            }

            for (int i = 0; i < leftHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = leftHandSwings[i].startPos.rotation - leftHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= Plugin.configs.ParityWarningAngle)
                    {
                        CreateDiffCommentNotes("Parity Warning - " + Plugin.configs.ParityWarningAngle + " degree difference", CommentTypesEnum.Unsure, leftHandSwings[i].notes);
                        hadWarning = true;
                    }
                    else if (Math.Abs(leftHandSwings[i].startPos.rotation) > 135 || Math.Abs(leftHandSwings[i].endPos.rotation) > 135)
                    {
                        if (Plugin.configs.ParityInvertedWarning)
                        {
                            CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, leftHandSwings[i].notes);
                        }
                        hadWarning = true;
                    }
                }
            }

            if (Plugin.configs.ParityDebug)
            {
                foreach (var swing in swings)
                {
                    var swingWithoutNotes = swing;
                    swingWithoutNotes.notes = null;
                    string message = JsonConvert.SerializeObject(swingWithoutNotes);
                    CommentTypesEnum commentType = CommentTypesEnum.Info;
                    if (swing.resetType == ResetType.Rebound) commentType = CommentTypesEnum.Issue;
                    if (Math.Abs(swing.endPos.rotation) > 135 || Math.Abs(swing.endPos.rotation) > 135) commentType = CommentTypesEnum.Unsure;
                    CreateDiffCommentNotes(message, commentType, swing.notes);
                }
            }

            if (hadIssue)
            {
                return Severity.Fail;
            }
            else if (hadWarning)
            {
                return Severity.Warning;
            }
            else
            {
                return Severity.Success;
            }
        }

        #endregion

        #region VisionBlock

        // Detect notes and bombs VB based on BeatLeader current criteria
        // Most of the minimum and maximum duration are configurable
        public Severity VisionBlockCheck()
        {
            var issue = Severity.Success;
            var song = plugin.BeatSaberSongContainer.Song;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList(); ;
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).ToList();
                List<BaseNote> lastMidL = new();
                List<BaseNote> lastMidR = new();
                List<BaseNote> arr = new();
                for (var i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    bpm.SetCurrentBPM(note.JsonTime);
                    var MaxBottomNoteTime = bpm.ToBeatTime(Plugin.configs.VBMinBottomNoteTime);
                    var MaxOuterNoteTime = bpm.ToBeatTime(Plugin.configs.VBMaxOuterNoteTime);
                    var MaxPatternTime = bpm.ToBeatTime(Plugin.configs.VBMinPatternTime);
                    var MinTimeNote = bpm.ToBeatTime(Plugin.configs.VBMinNoteTime);
                    var Overall = bpm.ToBeatTime(Plugin.configs.VBMinimum);
                    lastMidL.RemoveAll(l => note.JsonTime - l.JsonTime > MinTimeNote);
                    lastMidR.RemoveAll(l => note.JsonTime - l.SongBpmTime > MinTimeNote);
                    if (lastMidL.Count > 0)
                    {
                        if (note.JsonTime - lastMidL.First().JsonTime <= MinTimeNote && note.JsonTime - lastMidL.First().JsonTime >= Overall) // Closer than 0.25
                        {
                            if (note.PosX == 0 && note.JsonTime - lastMidL.First().JsonTime <= MaxOuterNoteTime) // Closer than 0.15 in outer lane
                            {
                                // Fine
                            }
                            else if (note.PosX == 1 && note.PosY == 0 && note.JsonTime - lastMidL.First().JsonTime <= MaxBottomNoteTime) // Closer than 0.075 at bottom layer
                            {
                                // Also fine
                            }
                            else
                            {
                                var s = swings.Where(x => x.notes.Exists(y => y.b == notes[i].JsonTime && y.d == notes[i].CutDirection && y.c == notes[i].Type && y.x == notes[i].PosX && y.y == notes[i].PosY)).ToList();
                                if (s.Count > 0)
                                {
                                    var n = s.FirstOrDefault().notes.Where(y => y.b == notes[i].JsonTime && y.d == notes[i].CutDirection && y.c == notes[i].Type && y != s.FirstOrDefault().notes.FirstOrDefault()).FirstOrDefault();
                                    if (n != null)
                                    {
                                        if (!arr.Exists(x => x.JsonTime == n.b && x.CutDirection == n.d && x.Type == n.c && x.PosX == n.x && x.PosY == n.y)
                                        && note.JsonTime - lastMidL.First().JsonTime >= MaxPatternTime) // Further than 0.020 and pattern head visible
                                        {
                                            // Also fine
                                        }
                                        else if (note.PosX < 2)
                                        {
                                            arr.Add(note);
                                            if (note.Type == 0 || note.Type == 1)
                                            {
                                                CreateDiffCommentNote("R2B - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue,
                                                    cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer));
                                                issue = Severity.Fail;
                                            }
                                        }
                                    }
                                    else if (note.PosX < 2)
                                    {
                                        arr.Add(note);
                                        if (note.Type == 0 || note.Type == 1)
                                        {
                                            CreateDiffCommentNote("R2B - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue,
                                                cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer));
                                            issue = Severity.Fail;
                                        }
                                    }
                                }
                                else if (note.PosX < 2)
                                {
                                    arr.Add(note);
                                    if (note.Type == 0 || note.Type == 1)
                                    {
                                        CreateDiffCommentNote("R2B - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue,
                                            cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer));
                                        issue = Severity.Fail;
                                    }
                                }
                            }
                        }
                    }
                    if (lastMidR.Count > 0)
                    {
                        if (note.JsonTime - lastMidR.First().JsonTime <= MinTimeNote && note.JsonTime - lastMidR.First().JsonTime >= Overall)
                        {
                            if (note.PosX == 3 && note.JsonTime - lastMidR.First().JsonTime <= MaxOuterNoteTime)
                            {
                                // Fine
                            }
                            else if (note.PosX == 2 && note.PosY == 0 && note.JsonTime - lastMidR.First().JsonTime <= MaxBottomNoteTime)
                            {
                                // Also fine
                            }
                            else
                            {
                                var s = swings.Where(x => x.notes.Exists(y => y.b == notes[i].JsonTime && y.d == notes[i].CutDirection && y.c == notes[i].Type && y.x == notes[i].PosX && y.y == notes[i].PosY)).ToList();
                                if (s.Count > 0)
                                {
                                    var n = s.FirstOrDefault().notes.Where(y => y.b == notes[i].JsonTime && y.d == notes[i].CutDirection && y.c == notes[i].Type && y != s.FirstOrDefault().notes.FirstOrDefault()).FirstOrDefault();
                                    if (n != null)
                                    {
                                        if (!arr.Exists(x => x.JsonTime == n.b && x.CutDirection == n.d && x.Type == n.c && x.PosX == n.x && x.PosY == n.y)
                                        && note.JsonTime - lastMidR.First().JsonTime >= MaxPatternTime) // Further than 0.020 and pattern head visible
                                        {
                                            // Also fine
                                        }
                                        else if (note.PosX > 1)
                                        {
                                            arr.Add(note);
                                            if (note.Type == 0 || note.Type == 1)
                                            {
                                                CreateDiffCommentNote("R2B - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue,
                                                    cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer));
                                                issue = Severity.Fail;
                                            }
                                        }
                                    }
                                    else if (note.PosX > 1)
                                    {
                                        arr.Add(note);
                                        if (note.Type == 0 || note.Type == 1)
                                        {
                                            CreateDiffCommentNote("R2B - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue,
                                                cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer));
                                            issue = Severity.Fail;
                                        }
                                    }
                                }
                                else if (note.PosX > 1)
                                {
                                    arr.Add(note);
                                    if (note.Type == 0 || note.Type == 1)
                                    {
                                        CreateDiffCommentNote("R2B - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue,
                                            cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer));
                                        issue = Severity.Fail;
                                    }
                                }
                            }
                        }
                    }
                    if (note.PosY == 1 && note.PosX == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.PosY == 1 && note.PosX == 2)
                    {
                        lastMidR.Add(note);
                    }
                }

                lastMidL = new List<BaseNote>();
                lastMidR = new List<BaseNote>();
                arr = new();
                for (var i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (note.Type == 3)
                    {
                        bpm.SetCurrentBPM(note.JsonTime);
                        var MaxTimeBomb = bpm.ToBeatTime(Plugin.configs.VBMaxBombTime);
                        var MinTimeBomb = bpm.ToBeatTime(Plugin.configs.VBMinBombTime);
                        var Overall = bpm.ToBeatTime(Plugin.configs.VBMinimum);
                        var left = notes.Where(x => x.JsonTime < note.JsonTime && x.Type == 0).OrderBy(o => o.JsonTime).LastOrDefault();
                        var right = notes.Where(x => x.JsonTime < note.JsonTime && x.Type == 1).OrderBy(o => o.JsonTime).LastOrDefault();
                        lastMidL.RemoveAll(l => note.JsonTime - l.JsonTime > MinTimeBomb);
                        lastMidR.RemoveAll(l => note.JsonTime - l.JsonTime > MinTimeBomb);
                        if (lastMidL.Count > 0)
                        {
                            if (note.JsonTime - lastMidL.First().JsonTime <= MinTimeBomb) // Closer than 0.20
                            {
                                if (note.PosX == 0 && note.JsonTime - lastMidL.First().JsonTime <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((note.PosX != 1 || note.PosY != 1) && note.JsonTime - lastMidL.First().JsonTime <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (note.PosX < 2)
                                {
                                    if (left != null)
                                    {
                                        if (left.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - left.PosX, 2) + Math.Pow(note.PosY - left.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                issue = Severity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.PosX, left.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.CutDirection))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            issue = Severity.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - right.PosX, 2) + Math.Pow(note.PosY - right.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                issue = Severity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.PosX, right.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.CutDirection))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            issue = Severity.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        if (lastMidR.Count > 0)
                        {
                            if (note.JsonTime - lastMidR.First().JsonTime <= MinTimeBomb) // Closer than 0.20
                            {
                                if (note.PosX == 3 && note.JsonTime - lastMidR.First().JsonTime <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((note.PosX != 2 || note.PosY != 1) && note.JsonTime - lastMidR.First().JsonTime <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (note.PosX > 1)
                                {
                                    if (left != null)
                                    {
                                        if (left.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - left.PosX, 2) + Math.Pow(note.PosY - left.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                issue = Severity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.PosX, left.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.CutDirection))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            issue = Severity.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - right.PosX, 2) + Math.Pow(note.PosY - right.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                issue = Severity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.PosX, right.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.CutDirection))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            issue = Severity.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (note.PosY == 1 && note.PosX == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.PosY == 1 && note.PosX == 2)
                    {
                        lastMidR.Add(note);
                    }
                }
            }

            bpm.ResetCurrentBPM();
            return issue;
        }

        #endregion

        #region ProlongedSwing

        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public Severity ProlongedSwingCheck()
        {
            var issue = false;
            var unsure = false;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            var slider = false;
            if (cubes.Exists(x => x.Slider))
            {
                slider = true;
            }
            foreach (var ch in chains)
            {
                if (ch.TailJsonTime - ch.JsonTime >= averageSliderDuration * 4.2)
                {
                    if (slider)
                    {
                        CreateDiffCommentLink("R2D - Duration is too high", CommentTypesEnum.Issue, ch);
                        issue = true;
                    }
                    else if (ch.TailJsonTime - ch.JsonTime > 0.125)
                    {
                        CreateDiffCommentLink("R2D - Duration might be too high", CommentTypesEnum.Unsure, ch);
                        unsure = true;
                    }
                }
                else if (ch.TailJsonTime - ch.JsonTime >= averageSliderDuration * 3.15)
                {
                    if (slider)
                    {
                        CreateDiffCommentLink("Y2A - Recommend shorter chain", CommentTypesEnum.Suggestion, ch);
                        unsure = true;
                    }
                    else if (ch.TailJsonTime - ch.JsonTime > 0.125)
                    {
                        CreateDiffCommentLink("Y2A - Duration might be too high", CommentTypesEnum.Unsure, ch);
                        unsure = true;
                    }
                }
                if (!cubes.Exists(c => c.Time == ch.JsonTime && c.Type == ch.Color && c.Line == ch.PosX && c.Layer == ch.PosY))
                {
                    // Link spam maybe idk
                    CreateDiffCommentLink("R2D - No head note", CommentTypesEnum.Issue, ch);
                    issue = true;
                }
            }
            // Dot spam and pauls maybe
            var leftCube = cubes.Where(d => d.Type == 0).ToList();
            var rightCube = cubes.Where(d => d.Type == 1).ToList();
            Cube previous = null;
            foreach (var left in leftCube)
            {
                if (previous != null)
                {
                    if (((left.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(left.Direction, previous.Direction, 67.5)) || (left.Time - previous.Time <= 0.142857)) && left.Time != previous.Time && left.Line == previous.Line && left.Layer == previous.Layer)
                    {
                        if (left.CutDirection == 8)
                        {
                            CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Unsure, left);
                            unsure = true;
                        }
                        else
                        {
                            CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Issue, left);
                            issue = true;
                        }
                    }
                }

                previous = left;
            }

            previous = null;
            foreach (var right in rightCube)
            {
                if (previous != null)
                {
                    if (((right.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(right.Direction, previous.Direction, 67.5)) || (right.Time - previous.Time <= 0.142857)) && right.Time != previous.Time && right.Line == previous.Line && right.Layer == previous.Layer)
                    {
                        if (right.CutDirection == 8)
                        {
                            CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Unsure, right);
                            unsure = true;
                        }
                        else
                        {
                            CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Issue, right);
                            issue = true;
                        }
                    }
                }

                previous = right;
            }

            if (issue)
            {
                return Severity.Fail;
            }
            else if (unsure)
            {
                return Severity.Warning;
            }

            return Severity.Success;
        }

        #endregion

        #region Loloppe

        // Detect parallel notes
        public Severity LoloppeCheck()
        {
            var issue = Severity.Success;


            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var red = cubes.Where(c => c.Type == 0).ToList();
            var blue = cubes.Where(c => c.Type == 1).ToList();
            for (int i = 1; i < red.Count; i++)
            {
                if (red[i].CutDirection == 8 || red[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (red[i].Time - red[i - 1].Time < 0.125)
                {
                    var sliderAngle = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle - red[i].Direction) >= 90)
                    {
                        var temp = red[i];
                        red[i] = red[i - 1];
                        red[i - 1] = temp;
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - red[i].Direction) >= 45 && Math.Abs(sliderAngle2 - red[i].Direction) <= 90)
                    {
                        CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, red[i - 1]);
                        CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, red[i]);
                        issue = Severity.Fail;
                    }
                }
            }
            for (int i = 1; i < blue.Count; i++)
            {
                if (blue[i].CutDirection == 8 || blue[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (blue[i].Time - blue[i - 1].Time < 0.125)
                {
                    var sliderAngle = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle - blue[i].Direction) >= 90)
                    {
                        var temp = blue[i];
                        blue[i] = blue[i - 1];
                        blue[i - 1] = temp;
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - blue[i].Direction) >= 45 && Math.Abs(sliderAngle2 - blue[i].Direction) <= 90)
                    {
                        CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, blue[i - 1]);
                        CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, blue[i]);
                        issue = Severity.Fail;
                    }
                }
            }

            return issue;
        }

        #endregion

        #region SwingPath

        public static bool NearestPointOnFiniteLine(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 direction = B - A;
            Vector2 pointAP = P - A;

            float t = Vector2.Dot(pointAP, direction) / Vector2.Dot(direction, direction);
            if (t < 0)
            {
                // Before A
            }
            else if (t > 1)
            {
                // After B
                Vector2 closestPoint = B;
                float distance = Vector2.Distance(P, closestPoint);
                if (distance < 0.4) return true;
            }
            else
            {
                // In between
                Vector2 closestPoint = A + t * direction;
                float distance = Vector2.Distance(P, closestPoint);
                if (distance < 0.4) return true;
            }
            return false;
        }

        // Check if a note block the swing path of another note of a different color
        public Severity SwingPathCheck()
        {
            var issue = Severity.Success;

            var song = plugin.BeatSaberSongContainer.Song;
            var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                List<List<Cube>> doubleNotes = new();
                foreach (var note in cubes) // Find the double and group them together
                {
                    var n = cubes.Where(n => n.Time == note.Time && n != note && ((n.Type == 0 && note.Type == 1) || (n.Type == 1 && note.Type == 0))).FirstOrDefault();
                    if (n != null)
                    {
                        if(doubleNotes.Count == 0)
                        {
                            doubleNotes.Add(new());
                        }
                        else if (!doubleNotes.Last().Exists(x => x.Time == n.Time))
                        {
                            doubleNotes.Add(new());
                            doubleNotes[doubleNotes.Count - 1] = new();
                        }
                        if (!doubleNotes.Last().Contains(note)) doubleNotes.Last().Add(note);
                        if (!doubleNotes.Last().Contains(n)) doubleNotes.Last().Add(n);
                    }
                }

                // No dot support for now
                foreach (var group in doubleNotes)
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        var note = group[i];
                        for (int j = 0; j < group.Count; j++)
                        {
                            if (i == j) continue;
                            var note2 = group[j];
                            if (note.Time != note2.Time) break; // Not a double anymore
                            if (note.Type == note2.Type) continue; // Same color
                                                                   // Fetch previous note, simulate swing
                            var previous = cubes.Where(c => c.Time < note2.Time && c.Type == note2.Type).LastOrDefault();
                            if (previous != null)
                            {
                                var angleOfAttack = note2.Direction;
                                var prevDirection = ScanMethod.ReverseCutDirection(previous.Direction);
                                // This is calculating the angle between the previous note + extra swing and the next note
                                if (note2.CutDirection != 8 && previous.CutDirection != 8)
                                {
                                    var di = Math.Abs(prevDirection - angleOfAttack);
                                    di = Math.Min(di, 360 - di) / 4;
                                    if (angleOfAttack < prevDirection)
                                    {
                                        if (prevDirection - angleOfAttack < 180)
                                        {
                                            angleOfAttack += di;
                                        }
                                        else
                                        {
                                            angleOfAttack -= di;
                                        }
                                    }
                                    else
                                    {
                                        if (angleOfAttack - prevDirection < 180)
                                        {
                                            angleOfAttack -= di;
                                        }
                                        else
                                        {
                                            angleOfAttack += di;
                                        }
                                    }
                                    // Simulate the position of the line based on the new angle found
                                    var simulatedLineOfAttack = ScanMethod.SimulateSwingPos(note2.Line, note2.Layer, ScanMethod.ReverseCutDirection(angleOfAttack), 2);
                                    // Check if the other note is close to the line
                                    var InPath = NearestPointOnFiniteLine(new(note2.Line, note2.Layer), new((float)simulatedLineOfAttack.x, (float)simulatedLineOfAttack.y), new(note.Line, note.Layer));
                                    if (InPath)
                                    {
                                        CreateDiffCommentNote("Swing Path", CommentTypesEnum.Info, note);
                                    }
                                }
                            }
                        }
                    }
                }
                
                List<BaseNote> arr = new();
                List<BaseNote> arr2 = new();
                var lastTime = 0d;
                
                for (int i = 0; i < notes.Count; i++)
                {
                    var currentNote = notes[i];
                    if (currentNote.Type == 3 || (currentNote.JsonTime / bpm * 60) < lastTime + 0.01)
                    {
                        continue;
                    }
                    for (int j = i + 1; j < notes.Count; j++)
                    {
                        var compareTo = notes[j];
                        if ((compareTo.JsonTime / bpm * 60) > (currentNote.JsonTime / bpm * 60) + 0.01)
                        {
                            break;
                        }
                        if ((compareTo.Type == 0 || compareTo.Type == 1) && currentNote.Type == compareTo.Type)
                        {
                            continue;
                        }
                        double[,] angle = { { 45, 1 }, { 15, 2 } };
                        double[,] angle2 = { { 45, 1 }, { 15, 1.5 } };
                        var IsDiagonal = false;
                        var dX = Math.Abs(currentNote.PosX - compareTo.PosX);
                        var dY = Math.Abs(currentNote.PosY - compareTo.PosY);
                        if (dX == dY)
                        {
                            IsDiagonal = true;
                        }
                        var a = swings.Where(x => x.notes.Any(y => y.b == currentNote.JsonTime && y.c == currentNote.Type && y.d == currentNote.CutDirection && y.x == currentNote.PosX && y.y == currentNote.PosY)).FirstOrDefault();
                        var b = swings.Where(x => x.notes.Any(y => y.b == compareTo.JsonTime && y.c == compareTo.Type && y.d == compareTo.CutDirection && y.x == compareTo.PosX && y.y == compareTo.PosY)).FirstOrDefault();
                        var d = Math.Sqrt(Math.Pow(currentNote.PosX - compareTo.PosX, 2) + Math.Pow(currentNote.PosY - compareTo.PosY, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {
                            if(currentNote.CutDirection == compareTo.CutDirection && SwingType.Diagonal.Contains(currentNote.CutDirection))
                            {
                                arr.Add(currentNote);
                                lastTime = (currentNote.JsonTime / bpm * 60);
                                continue;
                            }
                        }
                        if(IsDiagonal)
                        {
                            var pos = (currentNote.PosX, currentNote.PosY);
                            var target = (compareTo.PosX, compareTo.PosY);
                            var index = 1;
                            var rev = Reverse.Get(currentNote.CutDirection);
                            if(currentNote.CutDirection != 8)
                            {
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(currentNote, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        arr2.Add(currentNote);
                                        lastTime = (currentNote.JsonTime / bpm * 60);
                                        continue;
                                    }
                                }
                            }
                            if(compareTo.CutDirection != 8)
                            {
                                target = (currentNote.PosX, currentNote.PosY);
                                pos = (compareTo.PosX, compareTo.PosY);
                                index = 1;
                                rev = Reverse.Get(compareTo.CutDirection);
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(compareTo, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        arr2.Add(currentNote);
                                        lastTime = (currentNote.JsonTime / bpm * 60);
                                        continue;
                                    }
                                }
                            }
                        }
                        if (((currentNote.PosY == compareTo.PosY || currentNote.PosX == compareTo.PosX) && Swing.IsIntersect(currentNote, compareTo, angle, 2)) ||
                                (IsDiagonal && Swing.IsIntersect(currentNote, compareTo, angle2, 2)))
                        {
                            arr.Add(currentNote);
                            lastTime = (currentNote.JsonTime / bpm * 60);
                        }
                    }
                }
                foreach (var item in arr)
                {
                    CreateDiffCommentNote("R3E - Swing Path", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        // Implementation of Kival Evan hitboxInline.ts, hitboxStair.ts and hitboxReverseStaircase.ts
        public Severity HitboxCheck()
        {
            var issue = Severity.Success;

            var song = plugin.BeatSaberSongContainer.Song;
            var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);

            if (baseDifficulty.Notes.Any())
            {
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                var njs = diff.NoteJumpMovementSpeed;
                BaseNote[] lastNote = { null, null };
                List<List<BaseNote>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<BaseNote>();

                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (lastNote[note.Type] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Type], bpm, swingNoteArray[note.Type]))
                        {
                            swingNoteArray[note.Type].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Type + 1) % 2])
                    {
                        if (other.Type != 0 && other.Type != 1)
                        {
                            continue;
                        }
                        var isInline = false;
                        var distance = Math.Sqrt(Math.Pow(note.PosX - other.PosX, 2) + Math.Pow(note.PosY - other.PosY, 2));
                        if (distance <= 0.5)
                        {
                            isInline = true;
                        }
                        if (njs < 1.425 / ((60 * (note.JsonTime - other.JsonTime)) / bpm) && isInline)
                        {
                            arr.Add(note);
                            break;
                        }
                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr)
                {
                    CreateDiffCommentNote("R3G - Low NJS Inline", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }

                var hitboxTime = (0.15 * bpm) / 60;
                int[] lastNoteDirection = { -1, -1 };
                double[] lastSpeed = { -1, -1 };
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                Cube[] noteOccupy = { new(), new() };
                arr.Clear();
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (lastNote[note.Type] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Type], bpm, swingNoteArray[note.Type]))
                        {
                            lastSpeed[note.Type] = note.JsonTime - lastNote[note.Type].JsonTime;
                            if (note.CutDirection != NoteDirection.ANY)
                            {
                                noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(note.CutDirection)[0];
                                noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(note.CutDirection)[1];
                            }
                            else
                            {
                                noteOccupy[note.Type].Line = -1;
                                noteOccupy[note.Type].Layer = -1;
                            }
                            swingNoteArray[note.Type].Clear();
                            lastNoteDirection[note.Type] = note.CutDirection;
                        }
                        else if (Parity.IsEnd(note, lastNote[note.Type], lastNoteDirection[note.Type]))
                        {
                            if (note.CutDirection != NoteDirection.ANY)
                            {
                                noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(note.CutDirection)[0];
                                noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(note.CutDirection)[1];
                                lastNoteDirection[note.Type] = note.CutDirection;
                            }
                            else
                            {
                                noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(lastNoteDirection[note.Type])[0];
                                noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(lastNoteDirection[note.Type])[1];
                            }
                        }
                        if (lastNote[(note.Type + 1) % 2] != null)
                        {
                            if (note.JsonTime - lastNote[(note.Type + 1) % 2].JsonTime != 0 &&
                                note.JsonTime - lastNote[(note.Type + 1) % 2].JsonTime < Math.Min(hitboxTime, lastSpeed[(note.Type + 1) % 2]))
                            {
                                if (note.PosX == noteOccupy[(note.Type + 1) % 2].Line && note.PosY == noteOccupy[(note.Type + 1) % 2].Layer && !Swing.IsDouble(note, notes, i))
                                {
                                    arr.Add(note);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (note.CutDirection != NoteDirection.ANY)
                        {
                            noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(note.CutDirection)[0];
                            noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(note.CutDirection)[1];
                        }
                        else
                        {
                            noteOccupy[note.Type].Line = -1;
                            noteOccupy[note.Type].Layer = -1;
                        }
                        lastNoteDirection[note.Type] = note.CutDirection;
                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr)
                {
                    CreateDiffCommentNote("R3G - Staircase", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }

                var constant = 0.03414823529;
                var constantDiagonal = 0.03414823529;
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                arr.Clear();
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (lastNote[note.Type] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Type], bpm, swingNoteArray[note.Type]))
                        {
                            swingNoteArray[note.Type].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Type + 1) % 2])
                    {
                        if (other.Type != 0 && other.Type != 1)
                        {
                            continue;
                        }
                        if (other.CutDirection != NoteDirection.ANY)
                        {
                            if (!((note.JsonTime / bpm * 60) > (other.JsonTime / bpm * 60) + 0.01))
                            {
                                continue;
                            }
                            var isDiagonal = Swing.NoteDirectionAngle[other.CutDirection] % 90 > 15 && Swing.NoteDirectionAngle[other.CutDirection] % 90 < 75;
                            double[,] value = { { 15, 1.5 } };
                            if (njs < 1.425 / ((60 * (note.JsonTime - other.JsonTime)) / bpm + (isDiagonal ? constantDiagonal : constant)) &&
                                Swing.IsIntersect(note, other, value, 1))
                            {
                                arr.Add(other);
                                break;
                            }
                        }

                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr)
                {
                    CreateDiffCommentNote("R3G - Reverse Staircase", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }
            }

            return issue;
        }

        // Attempt to detect specific note and angle placement based on BeatLeader criteria
        public Severity HandClapCheck()
        {
            var issue = Severity.Success;
            var song = plugin.BeatSaberSongContainer.Song;
            var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                BaseNote previous = notes[0];
                BaseNote[] lastNote = { null, null };
                List<List<BaseNote>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<BaseNote>();
                var arr2 = new List<BaseNote>();
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (note.CutDirection == 8)
                    {
                        continue;
                    }
                    if (lastNote[note.Type] != null)
                    {
                        if (note.JsonTime != lastNote[note.Type].JsonTime)
                        {
                            swingNoteArray[note.Type].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Type + 1) % 2])
                    {
                        if (other.CutDirection == 8)
                        {
                            continue;
                        }
                        if (other.Type != 0 && other.Type != 1)
                        {
                            continue;
                        }
                        if (note.JsonTime != other.JsonTime)
                        {
                            continue;
                        }
                        var d = Math.Sqrt(Math.Pow(note.PosX - other.PosX, 2) + Math.Pow(note.PosY - other.PosY, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {

                            if (other.PosX == note.PosX)
                            {
                                if ((SwingType.Up.Contains(note.CutDirection) && SwingType.Down.Contains(other.CutDirection)) ||
                                    (SwingType.Down.Contains(note.CutDirection) && SwingType.Up.Contains(other.CutDirection)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                            else if (other.PosY == note.PosY)
                            {
                                if ((SwingType.Left.Contains(note.CutDirection) && SwingType.Right.Contains(other.CutDirection)) ||
                                    (SwingType.Right.Contains(note.CutDirection) && SwingType.Left.Contains(other.CutDirection)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d >= 1.001 && d < 2) // Diagonal
                        {
                            if (((note.CutDirection == 6 && note.PosY > other.PosY && note.PosX > other.PosX) ||
                                (note.CutDirection == 7 && note.PosY > other.PosY && note.PosX < other.PosX) ||
                                (note.CutDirection == 4 && note.PosY < other.PosY && note.PosX > other.PosX) ||
                                (note.CutDirection == 5 && note.PosY < other.PosY && note.PosX < other.PosX)) && Reverse.Get(note.CutDirection) == other.CutDirection)
                            {
                                arr.Add(other);
                                arr.Add(note);
                                break;
                            }
                        }
                        else if (d >= 2 && d <= 2.99) // 1-2 wide
                        {
                            if (NoteDirection.Move(note) == NoteDirection.Move(other))
                            {
                                if ((note.Type == 0 && note.PosX > other.PosX) || (note.Type == 1 && note.PosX < other.PosX)) // Crossover
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                                else if ((note.PosX == other.PosX + 2 && note.PosY == other.PosY + 2) || (other.PosX == note.PosX + 2 && other.PosY == note.PosY + 2) // Facing directly
                                || (note.PosX == other.PosX + 2 && note.PosY == other.PosY - 2) || (other.PosX == note.PosX + 2 && other.PosY == note.PosY - 2)
                                || (note.PosX == other.PosX && note.PosY == other.PosY + 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (note.PosX == other.PosX && note.PosY == other.PosY - 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (other.PosY == note.PosY && other.PosX == note.PosX + 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (other.PosY == note.PosY && other.PosX == note.PosX - 2 && Reverse.Get(note.CutDirection) == other.CutDirection))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d > 2.99 && ((note.Type == 0 && note.PosX > 2) || (note.Type == 1 && note.PosX < 1))) // 3-wide
                        {
                            // TODO: This is trash, could easily be done better
                            if (other.PosY == note.PosY)
                            {
                                if (((SwingType.Up_Left.Contains(note.CutDirection) && SwingType.Up_Right.Contains(other.CutDirection) && note.Type == 1) ||
                                    (SwingType.Up_Right.Contains(note.CutDirection) && SwingType.Up_Left.Contains(other.CutDirection) && note.Type == 0)))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }
                                if ((SwingType.Down_Left.Contains(note.CutDirection) && SwingType.Down_Right.Contains(other.CutDirection) && note.Type == 1) ||
                                (SwingType.Down_Right.Contains(note.CutDirection) && SwingType.Down_Left.Contains(other.CutDirection) && note.Type == 0))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }

                            }
                        }
                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr2)
                {
                    CreateDiffCommentNote("R3D - Hand clap", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }

                foreach (var item in arr)
                {
                    CreateDiffCommentNote("R3D - Hand clap", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        public static readonly float[] AllowedSnap = { 0, 0.125f, 0.167f, 0.25f, 0.333f, 0.375f, 0.5f, 0.625f, 0.667f, 0.75f, 0.833f, 0.875f };

        public void HighlightOffbeat()
        {
            var swings = BeatmapScanner.Datas.OrderBy(c => c.Time).ToList();
            if (swings.Any())
            {
                foreach (var swing in swings)
                {
                    var precision = (float)Math.Round(swing.Start.Time % 1, 3);
                    if (!AllowedSnap.Contains(precision))
                    {
                        var reality = ScanMethod.RealToFraction(precision, 0.01);
                        CreateDiffCommentNote(reality.N.ToString() + "/" + reality.D.ToString(), CommentTypesEnum.Info, swing.Start);
                    }
                }
            }
        }

        #endregion

        #region Comments

        /// <summary>
        /// Create a comment in the mapsetreview file
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="type">the type</param>
        private void CreateSongInfoComment(string message, CommentTypesEnum type)
        {
            string id = Guid.NewGuid().ToString();


            Comment comment = new()
            {
                Id = id,
                StartBeat = 0,
                Objects = new(),
                Type = type,
                Message = message,
                IsAutogenerated = true
            };
            List<Comment> comments = plugin.currentMapsetReview.Comments;
            comments.Add(comment);
            comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }


        /// <summary>
        /// Create a comment in a difficultyreview for a note
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="cube">the cube</param>
        private void CreateDiffCommentNote(string message, CommentTypesEnum type, Cube cube)
        {
            string id = Guid.NewGuid().ToString();

            SelectedObject note = new()
            {
                Beat = cube.Time,
                PosX = cube.Line,
                PosY = cube.Layer,
                Color = cube.Type,
                ObjectType = ObjectType.Note
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = cube.Time,
                Objects = new() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                comments.Add(comment);
                comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a note
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="cube">the cube</param>
        private void CreateDiffCommentLink(string message, CommentTypesEnum type, BaseSlider chainLink)
        {
            string id = Guid.NewGuid().ToString();

            SelectedObject note = new()
            {
                Beat = chainLink.JsonTime,
                PosX = chainLink.PosX,
                PosY = chainLink.PosY,
                Color = chainLink.Color,
                ObjectType = ObjectType.Chain
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = chainLink.JsonTime,
                Objects = new() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                comments.Add(comment);
                comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a bomb
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="bomb">the bomb</param>
        private void CreateDiffCommentBomb(string message, CommentTypesEnum type, BaseNote bomb)
        {
            string id = Guid.NewGuid().ToString();

            SelectedObject note = new()
            {
                Beat = bomb.JsonTime,
                PosX = bomb.PosX,
                PosY = bomb.PosY,
                Color = 3,
                ObjectType = bomb.ObjectType
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = bomb.JsonTime,
                Objects = new() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                comments.Add(comment);
                comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a wall
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="wall">the wall</param>
        private void CreateDiffCommentObstacle(string message, CommentTypesEnum type, BaseObstacle wall)
        {
            string id = Guid.NewGuid().ToString();

            SelectedObject note = new()
            {
                Beat = wall.JsonTime,
                PosX = wall.PosX,
                PosY = wall.PosY,
                Color = 0,
                ObjectType = wall.ObjectType
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = wall.JsonTime,
                Objects = new() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                comments.Add(comment);
                comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }
        }

        /// <summary>
        /// Add another line to the OverallComment in the difficultyreview
        /// </summary>
        /// <param name="message">the message</param>
        private void ExtendOverallComment(string message)
        {
            DifficultyReview review = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault();

            review.OverallComment += $" \n{message}";
        }

        private bool CheckIfCommentAlreadyExists(Comment comment)
        {
            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            return comments.Any(c => comment.Message == c.Message && c.Objects.Any(o => String.Equals(o.ToStringFull().ToLower(), comment.Objects.FirstOrDefault().ToStringFull().ToLower(), StringComparison.InvariantCulture)));
        }

        private void FuseBombComments()
        {
            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
            var bombComments = comments.Where(c => c.Objects.All(o => o.Color == 3)).ToList(); // Only bombs comments
            for (int i = bombComments.Count() - 2; i >= 0; i--)
            {
                if (bombComments[i + 1].Message == bombComments[i].Message && bombComments[i + 1].StartBeat >= bombComments[i].StartBeat && bombComments[i + 1].StartBeat <= bombComments[i].StartBeat + 0.25)
                {
                    bombComments[i + 1].Objects.ForEach(o => bombComments[i].Objects.Add(o));
                    comments.Remove(bombComments[i + 1]);
                }
            }
        }

        private void CreateDiffCommentNotes(string message, CommentTypesEnum type, List<Note> notes)
        {
            if (notes.Count == 0) return;
            string id = Guid.NewGuid().ToString();

            List<SelectedObject> objects = new();

            foreach (var note in notes)
            {
                objects.Add(new()
                {
                    Beat = note.b,
                    PosX = note.x,
                    PosY = note.y,
                    Color = note.c,
                    ObjectType = ObjectType.Note
                });
            }

            Comment comment = new()
            {
                Id = id,
                StartBeat = objects.FirstOrDefault().Beat,
                Objects = objects,
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            if (!CheckIfCommentAlreadyExists(comment))
            {
                comments.Add(comment);
                comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }
        }

        #endregion
    }
}

using Beatmap.Base;
using ChroMapper_LightModding.BeatmapScanner.Data;
using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;
using ChroMapper_LightModding.BeatmapScanner.MapCheck;
using JoshaParity;
using Parity = ChroMapper_LightModding.BeatmapScanner.MapCheck.Parity;
using Newtonsoft.Json;
using UnityEngine;

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
                Requirement = RequirementsCheck(),
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
            bpm = BeatPerMinute.Create(BeatSaberSongContainer.Instance.Song.BeatsPerMinute, baseDifficulty.BpmEvents, songOffset);

            DiffCrit diffCrit = new()
            {
                HotStart = HotStartCheck(),
                ColdEnd = ColdEndCheck(),
                MinSongDuration = MinSongDurationCheck(),
                Slider = SliderCheck(),
                DifficultyLabelSize = DifficultyLabelSizeCheck(),
                DifficultyName = DifficultyNameCheck(),
                NJS = NJSCheck(),
                FusedElement = FusedElementCheck(),
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

            var order = passStandard.OrderBy(x => x).ToList();
            if (passStandard.SequenceEqual(order))
            {
                return Severity.Success;
            }

            CreateSongInfoComment("R7E - Difficulty Ordering is wrong\nCurrent order: " + string.Join(",", passStandard.ToArray()) + "\nExpected order: " +
                    string.Join(",", order.ToArray()), CommentTypesEnum.Issue);
            return Severity.Fail;
        }

        #endregion

        #region Requirements

        public Severity RequirementsCheck()
        {
            var issue = Severity.Success;

            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffset.DifficultyBeatmaps)
                {
                    var data = diff.GetOrCreateCustomData();
                    if (data.HasKey("_requirements"))
                    {
                        foreach (var req in diff.CustomData["_requirements"].Values)
                        {
                            CreateSongInfoComment("R1C - " + diff.BeatmapFilename + " has " + req + " requirement", CommentTypesEnum.Issue);
                            issue = Severity.Fail;
                        }
                    }
                }
            }

            return issue;
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

        public Severity HotStartCheck()
        {
            var issue = Severity.Success;
            var cube = BeatmapScanner.Cubes;
            cube = cube = cube.OrderBy(c => c.Time).ToList();
            var wall = BeatmapScanner.Walls;
            wall = wall.OrderBy(w => w.JsonTime).ToList();
            var limit = bpm.ToBeatTime(Plugin.configs.HotStartDuration);
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
                if (w.JsonTime < limit && (w.PosX == 1 || w.PosX == 2))
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
                if (w.JsonTime + w.Duration > limit && (w.PosX == 1 || w.PosX == 2))
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

        public Severity MinSongDurationCheck()
        {
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderBy(c => c.Time).ToList();
            var duration = bpm.ToRealTime(cube.Last().Time - cube.First().Time);
            if (duration < Plugin.configs.MinSongDuration)
            {
                ExtendOverallComment("R1F - Current map duration is " + duration.ToString() + "s. Minimum required duration is " + Plugin.configs.MinSongDuration.ToString() + "s.");
                return Severity.Fail;
            }

            return Severity.Success;
        }

        #endregion

        #region Slider

        public Severity SliderCheck()
        {
            var issue = Severity.Success;
            var precision = 0d;
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
                        precision = ScanMethod.Mode(temp).FirstOrDefault() / 4;
                    }
                    else
                    {
                        precision = ScanMethod.Mode(temp).FirstOrDefault() / 3;
                    }
                }
                else
                {
                    precision = ScanMethod.Mode(temp).FirstOrDefault() / 2;
                }
            }
            else
            {
                precision = ScanMethod.Mode(temp).FirstOrDefault();
            }

            foreach (var c in cube)
            {
                if (c.Slider && !c.Head)
                {
                    if (!(c.Precision <= ((c.Spacing + 1) * precision) + 0.001 && c.Precision >= ((c.Spacing + 1) * precision) - 0.001))
                    {
                        var reality = ScanMethod.RealToFraction(c.Precision, 0.01);
                        var expected = ScanMethod.RealToFraction(((c.Spacing + 1) * precision), 0.01);
                        CreateDiffCommentNote("R2A - " + c.Time + " is " + reality.N.ToString() + "/" + reality.D.ToString() + ". Expected precision is " + expected.N.ToString() + "/" + expected.D.ToString() + ".", CommentTypesEnum.Unsure, c);
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
                    dir.Add(red[i].Direction);

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

                        dir.Add(ScanMethod.FindAngleViaPosition(red, i - 1, i, red[i - 1].Direction, true));
                    } while (!red[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(dir[j], degree, 45) || !ScanMethod.IsSameDirection(dir[j - 1], dir[j], 45))
                        {
                            CreateDiffCommentNote("R3F - Multiple notes of the same color on the same swing must not differ by more than 45°", CommentTypesEnum.Issue, red[i - dir.Count() + j]);
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
                    dir.Add(blue[i].Direction);

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

                        dir.Add(ScanMethod.FindAngleViaPosition(blue, i - 1, i, blue[i - 1].Direction, true));
                    } while (!blue[i].Head);
                    var degree = dir.FirstOrDefault();
                    for (int j = 1; j < dir.Count(); j++)
                    {
                        if (!ScanMethod.IsSameDirection(dir[j], degree, 45) || !ScanMethod.IsSameDirection(dir[j - 1], dir[j], 45))
                        {
                            CreateDiffCommentNote("R3F - Multiple notes of the same color on the same swing must not differ by more than 45°", CommentTypesEnum.Issue, blue[i - dir.Count() + j]);
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

        #region NJS

        public Severity NJSCheck()
        {
            var issue = Severity.Success;
            var swingData = BeatmapScanner.Datas;
            List<double> sps = new();
            var secInBeat = BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 60;
            for (int i = 0; i < BeatSaberSongContainer.Instance.LoadedSongLength - 1; i++)
            {
                sps.Add(swingData.Where(s => s.Time > i * secInBeat && s.Time < (i + 1) * secInBeat).Count());
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
            if (diff.NoteJumpMovementSpeed == 0)
            {
                ExtendOverallComment("R1A - NJS is currently 0");
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

            return issue;
        }

        #endregion

        #region FusedElement

        public Severity FusedElementCheck()
        {
            var issue = Severity.Success;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            var bombs = BeatmapScanner.Bombs.OrderBy(b => b.JsonTime).ToList();
            var walls = BeatmapScanner.Walls.OrderBy(w => w.JsonTime).ToList();

            var beatms = bpm.ToBeatTime(Plugin.configs.FusedElementDuration, false);

            foreach (var w in walls)
            {
                foreach (var c in cubes)
                {
                    if (c.Time > w.JsonTime + w.Duration + beatms)
                    {
                        break;
                    }
                    if (c.Time >= w.JsonTime - beatms && c.Time <= w.JsonTime + w.Duration + beatms && c.Line <= w.PosX + w.Width - 1 && c.Line >= w.PosX && c.Layer <= w.PosY + w.Height && c.Layer >= w.PosY - 1)
                    {
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        issue = Severity.Fail;
                    }
                }
                foreach (var b in bombs)
                {
                    if (b.JsonTime > w.JsonTime + w.Duration + beatms)
                    {
                        break;
                    }
                    if (b.JsonTime >= w.JsonTime - beatms && b.JsonTime <= w.JsonTime + w.Duration + beatms && b.PosX <= w.PosX + w.Width - 1 && b.PosX >= w.PosX && b.PosY <= w.PosY + w.Height && b.PosY >= w.PosY - 1)
                    {
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
                        issue = Severity.Fail;
                    }
                }
                foreach (var c in chains)
                {
                    if (c.JsonTime > w.JsonTime + w.Duration + beatms)
                    {
                        break;
                    }
                    if (c.JsonTime >= w.JsonTime - beatms && c.JsonTime <= w.JsonTime + w.Duration + beatms && c.TailPosX <= w.PosX + w.Width - 1 && c.TailPosX >= w.PosX && c.TailPosY <= w.PosY + w.Height && c.TailPosY >= w.PosY - 1)
                    {
                        CreateDiffCommentLink("R2D - Links cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
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
                    if (c2.Time > c.Time + beatms)
                    {
                        break;
                    }
                    if (c.Time >= c2.Time - beatms && c.Time <= c2.Time + beatms && c.Line == c2.Line && c.Layer == c2.Layer)
                    {
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
                for (int j = 0; j < bombs.Count; j++)
                {
                    var b = bombs[j];
                    if (b.JsonTime > c.Time + beatms)
                    {
                        break;
                    }
                    if (b.JsonTime >= c.Time - beatms && b.JsonTime <= c.Time + beatms && c.Line == b.PosX && c.Layer == b.PosY)
                    {
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
                        issue = Severity.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    if (c2.JsonTime > c.Time + beatms)
                    {
                        break;
                    }
                    if (c.Time >= c2.JsonTime - beatms && c.Time <= c2.JsonTime + beatms && c.Line == c2.TailPosX && c.Layer == c2.TailPosY)
                    {
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentLink("R2D - Links cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c2);
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
                    if (b2.JsonTime > b.JsonTime + beatms)
                    {
                        break;
                    }
                    if (b.JsonTime >= b2.JsonTime - beatms && b.JsonTime <= b2.JsonTime + beatms && b.PosX == b2.PosX && b.PosY == b2.PosY)
                    {
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b2);
                        issue = Severity.Fail;
                    }
                }
                for (int j = i + 1; j < chains.Count; j++)
                {
                    var c2 = chains[j];
                    if (c2.JsonTime > b.JsonTime + beatms)
                    {
                        break;
                    }
                    if (b.JsonTime >= c2.JsonTime - beatms && b.JsonTime <= c2.JsonTime + beatms && b.PosX == c2.TailPosX && b.PosY == c2.TailPosY)
                    {
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
                        CreateDiffCommentLink("R2D - Links cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c2);
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
                    if (c2.JsonTime > c.JsonTime + beatms)
                    {
                        break;
                    }
                    if (c.JsonTime >= c2.JsonTime - beatms && c.JsonTime <= c2.JsonTime + beatms && c.TailPosX == c2.TailPosX && c.TailPosY == c2.TailPosY)
                    {
                        CreateDiffCommentLink("R2D - Links cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentLink("R2D - Links cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
            }

            return issue;
        }

        #endregion

        #region Outside

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
                ExtendOverallComment("R1B - Object detected outside of playable timeframe");
                issue = Severity.Fail;
            }

            return issue;
        }

        #endregion

        #region Light

        public Severity LightCheck()
        {
            var issue = Severity.Success;
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            BaseDifficulty baseDifficulty = BeatSaberSongContainer.Instance.Song.GetMapFromDifficultyBeatmap(diff);
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
                if (average < Plugin.configs.AverageLightPerBeat)
                {
                    ExtendOverallComment("R6A - Map doesn't have enough light");
                    issue = Severity.Fail;
                }
                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var fadeTime = bpm.ToBeatTime(Plugin.configs.LightFadeDuration, false);
                var reactTime = bpm.ToBeatTime(Plugin.configs.LightBombReactionTime, false);
                var eventState = new List<EventState>();
                var eventLitTime = new List<List<EventLitTime>>();
                for (var i = 0; i < 12; i++)
                {
                    EventState es = new(false, 0, 0);
                    eventState.Add(es);
                    eventLitTime.Add(new());
                }
                for (int i = 0; i < lights.Count; i++)
                {
                    var ev = lights[i];
                    if ((ev.IsOn || ev.IsFlash) && eventState[ev.Type].State == false)
                    {
                        eventState[ev.Type].State = true;
                        eventState[ev.Type].Time = ev.JsonTime;
                        eventState[ev.Type].FadeTime = 0;
                        var elt = eventLitTime[ev.Type].Find(e => e.Time >= ev.JsonTime);
                        if (elt != null)
                        {
                            elt.Time = ev.JsonTime;
                            elt.State = true;
                        }
                        else
                        {
                            eventLitTime[ev.Type].Add(new(ev.JsonTime, true));
                        }
                    }
                    if (ev.IsFade)
                    {
                        eventState[ev.Type].State = false;
                        eventState[ev.Type].Time = ev.JsonTime;
                        eventState[ev.Type].FadeTime = fadeTime;
                        var elt = eventLitTime[ev.Type].Find(e => e.Time >= ev.JsonTime);
                        if (elt != null)
                        {
                            elt.Time = ev.JsonTime;
                            elt.State = true;
                        }
                        else
                        {
                            eventLitTime[ev.Type].Add(new(ev.JsonTime, true));
                        }
                        eventLitTime[ev.Type].Add(new(ev.JsonTime + fadeTime, false));
                    }
                    if ((ev.FloatValue < 0.25 || ev.IsOff) && eventState[ev.Type].State != false)
                    {
                        eventState[ev.Type].FadeTime = eventState[ev.Type].State == true ? reactTime : Math.Min(reactTime, eventState[ev.Type].FadeTime);
                        eventState[ev.Type].State = false;
                        eventState[ev.Type].Time = ev.JsonTime;
                        eventLitTime[ev.Type].Add(new(ev.JsonTime + (eventState[ev.Type].State == true ? reactTime : Math.Min(reactTime, eventState[ev.Type].FadeTime)), false));
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
                        var t = el.Find(e => e.Time <= bomb.JsonTime);
                        if (t != null)
                        {
                            isLit = isLit || t.State;
                        }
                    }
                    if (!isLit)
                    {
                        CreateDiffCommentBomb("R5B - There must be sufficient lighting whenever bombs are present", CommentTypesEnum.Issue, bomb);
                        issue = Severity.Fail;
                    }
                }
            }

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
                var note = notes.Where(n => n.Line == 0 && (n.Layer >= 2 || (n.Layer >= 0 && w.PosY == 0)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration).ToList();
                foreach (var n in note)
                {
                    CreateDiffCommentNote("R3B - Notes cannot be hidden behind walls", CommentTypesEnum.Issue, n);
                    issue = Severity.Fail;
                }
                var bomb = bombs.Where(b => b.PosX == 0 && (b.PosY >= 1 || (b.PosY >= 0 && w.PosY == 0)) && b.JsonTime > w.JsonTime && b.JsonTime <= w.JsonTime + w.Duration).ToList();
                foreach (var b in bomb)
                {
                    CreateDiffCommentBomb("R5E - Bombs cannot be hidden behind walls", CommentTypesEnum.Issue, b);
                    issue = Severity.Fail;
                }
            }

            foreach (var w in rightWall)
            {
                var note = notes.Where(n => n.Line == 3 && (n.Layer >= 2 || (n.Layer >= 0 && w.PosY == 0)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration).ToList();
                foreach (var n in note)
                {
                    CreateDiffCommentNote("R3B - Notes cannot be hidden behind walls", CommentTypesEnum.Issue, n);
                    issue = Severity.Fail;
                }
                var bomb = bombs.Where(b => b.PosX == 3 && (b.PosY >= 1 || (b.PosY >= 0 && w.PosY == 0)) && b.JsonTime > w.JsonTime && b.JsonTime <= w.JsonTime + w.Duration).ToList();
                foreach (var b in bomb)
                {
                    CreateDiffCommentBomb("R5E - Bombs cannot be hidden behind walls", CommentTypesEnum.Issue, b);
                    issue = Severity.Fail;
                }
            }
            
            var min = bpm.ToBeatTime(Plugin.configs.MinimumWallDuration);
            var max = bpm.ToBeatTime(Plugin.configs.ShortWallTrailDuration);
            var dodge = 0d;
            var side = 0;
            var sec = bpm.ToBeatTime(1, true);
            bool start = false;
            BaseObstacle previous = null;
            // Won't work properly in some very specific situation probably, but I did my best..
            foreach (var w in walls)
            {
                if (w.PosY <= 0 && w.Height > 1 && ((w.PosX + w.Width == 2 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 3 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime)) ||
                    (w.PosX + w.Width == 3 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 2 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime))))
                {
                    CreateDiffCommentObstacle("R4C - Walls cannot be placed to force the player to move into the outer lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                else if (w.PosY <= 0 && w.Height > 1 && (w.Width >= 3 && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3 || w.PosX == 1)) || (w.Width >= 2 && w.PosX == 1 && w.PosY == 0 && w.Height > 0) || (w.Width >= 4 && w.PosX + w.Width >= 4 && w.PosX <= 0 && w.PosY == 0))
                {
                    CreateDiffCommentObstacle("R4C - Walls cannot be placed to force the player to move into the outer lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                if (w.Width <= 0 || w.Duration <= 0 || (w.Height <= 0 && w.PosX >= 0 && w.PosX <= 3 && ((w.PosY > 0 && w.PosY <= 2) || (w.PosY + w.Height >= 0 && w.PosY + w.Height <= 2))))
                {
                    CreateDiffCommentObstacle("R4D - Walls must have a positive width, height and duration", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                if (w.Duration < min && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3) &&
                    !walls.Exists(wa => wa != w && wa.PosX + wa.Width >= w.PosX + w.Width && wa.PosX <= w.PosX + w.Width && wa.Duration >= min && w.JsonTime >= wa.JsonTime && w.JsonTime <= wa.JsonTime + wa.Duration + max))
                {
                    CreateDiffCommentObstacle("R4E - Walls shorter than 13.8ms in the middle two lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                if(previous != null)
                {
                    if ((dodge > 0 && w.JsonTime > previous.JsonTime + sec) || start) // more than a sec happen
                    {
                        start = true;
                        var amount = (w.JsonTime - previous.JsonTime) / sec;
                        if(amount < 1)
                        {
                            dodge *= amount;
                        }
                        else
                        {
                            dodge = 0;
                        }
                        if(dodge == 0)
                        {
                            start = false;
                        }
                    }
                }
                if (w.PosX + w.Width == 2 && side != 2)
                {
                    side = 2;
                    dodge++;
                }
                else if (w.PosX == 2 && side != 1)
                {
                    side = 1;
                    dodge++;
                }
                else
                {
                    side = 0;
                }
                Debug.Log(w.JsonTime + " - " + dodge);
                if (dodge >= Plugin.configs.MaximumDodgeWallPerSecond && side != 0)
                {
                    CreateDiffCommentObstacle("R4B - Dodge walls must not force the players head to move more than 3.5 times per second", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                else if (dodge >= Plugin.configs.SubjectiveDodgeWallPerSecond && side != 0)
                {
                    CreateDiffCommentObstacle("Y4A - Dodge walls that force the players head to move more than 2 times per second need justification", CommentTypesEnum.Suggestion, w);
                    issue = Severity.Warning;
                }

                previous = w;
            }

            return issue;
        }

        #endregion

        #region Chain

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
                    CreateDiffCommentLink("R2D - Chains and their links cannot be part of the first 16 notes of the map", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }
            else if (links.Any())
            {
                var link = links.Where(l => l.JsonTime >= notes.Last().Time).Take(16 - notes.Count).ToList();
                foreach (var l in link)
                {
                    CreateDiffCommentLink("R2D - Chains and their links cannot be part of the first 16 notes of the map", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }

            // TODO: Make this mess better idk
            foreach (var l in links)
            {
                var chain = (BaseChain)l;
                var spacing = Math.Round(Math.Max(Math.Max(Math.Abs(l.TailPosX - l.PosX) * chain.Squish, Math.Abs(l.TailPosY - l.PosY) * chain.Squish), 0), 0);
                Debug.Log("Spacing is: " + spacing + " calc is: " + (chain.SliceCount / spacing).ToString() + " settings is: " + Plugin.configs.ChainLinkVsAir);
                if ((chain.SliceCount - 1) / spacing < Plugin.configs.ChainLinkVsAir)
                {
                    CreateDiffCommentLink("R2D - Chains must be at least 12.5% links versus air/empty-space", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
                var horizontal = Math.Abs(l.PosX - l.TailPosX) * (chain.Squish / 2 + 0.5) * (chain.Squish / 2 + 0.5);
                var vertical = Math.Abs(l.PosY - l.TailPosY) * chain.Squish * chain.Squish;
                var newX = l.PosX + (horizontal * Math.Cos(ScanMethod.ConvertDegreesToRadians(ScanMethod.DirectionToDegree[l.CutDirection] + chain.AngleOffset)));
                var newY = l.PosY + (vertical * Math.Sin(ScanMethod.ConvertDegreesToRadians(ScanMethod.DirectionToDegree[l.CutDirection] + chain.AngleOffset)));
                if (newX > 4 || newX < -1 || newY > 2 || newY < 0)
                {
                    CreateDiffCommentLink("R2D - Chains links can lead outside of the grid, but not further than an extra lane", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
                if (l.TailJsonTime < l.JsonTime)
                {
                    CreateDiffCommentLink("R2D - Chains must not have a reverse direction", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
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
                if (!ScanMethod.IsSameDirection(ScanMethod.FindAngleViaPosition(temp3, 0, 1, temp.Direction, true) * (chain.Squish / 2 + 0.5), temp.Direction, Plugin.configs.MaxChainRotation))
                {
                    CreateDiffCommentLink("R2D - Chains cannot change in direction by more than 45°", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region Parity

        public Severity ParityCheck()
        {
            // implementation of JoshaParity
            // TODO: make configurable
            bool hadIssue = false;
            bool hadWarning = false;

            var song = plugin.BeatSaberSongContainer.Song;

            MapAnalyser analysedMap = new(song.Directory);

            List<JoshaParity.SwingData> swings = analysedMap.GetSwingData((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());

            foreach (var swing in swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                CreateDiffCommentNotes("R2 - Parity Error", CommentTypesEnum.Issue, swing.notes);
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
                        CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, rightHandSwings[i].notes);
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
                        CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, leftHandSwings[i].notes);
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
                    CommentTypesEnum commentType = CommentTypesEnum.Suggestion;
                    if (swing.resetType == ResetType.Rebound) commentType = CommentTypesEnum.Issue;
                    if (Math.Abs(swing.endPos.rotation) > 135 || Math.Abs(swing.endPos.rotation) > 135) commentType = CommentTypesEnum.Unsure;
                    CreateDiffCommentNotes(message, commentType, swing.notes);
                }
            }

            if (hadIssue)
            {
                return Severity.Fail;
            } else if (hadWarning)
            {
                return Severity.Warning;
            } else
            {
                return Severity.Success;
            }
        }

        #endregion

        #region VisionBlock

        public Severity VisionBlockCheck()
        {
            var issue = Severity.Success;
            var song = plugin.BeatSaberSongContainer.Song;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                var bombs = BeatmapScanner.Bombs.OrderBy(b => b.JsonTime).ToList();
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).OrderBy(n => n.JsonTime).ToList();
                
                var MinBottomTimeNote = bpm.ToBeatTime(Plugin.configs.VBMinBottomNoteTime);
                var MinTimeNote = bpm.ToBeatTime(Plugin.configs.VBMinimumNoteTime);
                var MaxTimeNote = bpm.ToBeatTime(Plugin.configs.VBMaximumNoteTime);
                var MinTimeBomb = bpm.ToBeatTime(Plugin.configs.VBMinimumBombTime);
                var MaxTimeBomb = bpm.ToBeatTime(Plugin.configs.VBMaximumBombTime);
                var OverallMin = bpm.ToBeatTime(Plugin.configs.VBAllowedMinimum);

                List<BaseNote> lastMidL = new();
                List<BaseNote> lastMidR = new();
                List<BaseNote> arr = new();
                for (var i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (lastMidL.Count > 0)
                    {
                        if (note.JsonTime - lastMidL.First().JsonTime <= MaxTimeNote)
                        {
                            if (note.PosX == 0 && note.JsonTime - lastMidL.First().JsonTime <= MinTimeNote)
                            {
                                // Fine
                            }
                            else if (note.PosX == 1 && note.PosY == 0 && note.JsonTime - lastMidL.First().JsonTime <= MinBottomTimeNote)
                            {
                                // Also fine
                            }
                            else if ((note.PosX != 1 || note.PosY != 1) && note.JsonTime - lastMidL.First().JsonTime <= OverallMin)
                            {
                                // Also fine
                            }
                            else if (note.PosX < 2)
                            {
                                arr.Add(note);
                            }
                        }
                    }
                    if (lastMidR.Count > 0)
                    {
                        if (note.JsonTime - lastMidR.First().JsonTime <= MaxTimeNote)
                        {
                            if (note.PosX == 3 && note.JsonTime - lastMidR.First().JsonTime <= MinTimeNote)
                            {
                                // Fine
                            }
                            else if (note.PosX == 2 && note.PosY == 0 && note.JsonTime - lastMidR.First().JsonTime <= MinBottomTimeNote)
                            {
                                // Also fine
                            }
                            else if ((note.PosX != 2 || note.PosY != 1) && note.JsonTime - lastMidR.First().JsonTime <= OverallMin)
                            {
                                // Also fine
                            }
                            else if (note.PosX > 1)
                            {
                                arr.Add(note);
                            }
                        }
                    }
                    lastMidL.RemoveAll(l => note.JsonTime - l.JsonTime > MaxTimeNote);
                    lastMidR.RemoveAll(l => note.JsonTime - l.JsonTime > MaxTimeNote);
                    if (note.PosY == 1 && note.PosX == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.PosY == 1 && note.PosX == 2)
                    {
                        lastMidR.Add(note);
                    }
                }

                foreach (var n in arr)
                {
                    if (n.Type == 0 || n.Type == 1)
                    {
                        CreateDiffCommentNote("R2B - Notes must be placed to give the player acceptable time to react", CommentTypesEnum.Issue,
                            cubes.Find(c => c.Time == n.JsonTime && c.Type == n.Type && n.PosX == c.Line && n.PosY == c.Layer));
                        issue = Severity.Fail;
                    }
                }

                lastMidL = new();
                lastMidR = new();
                arr = new();
                for (var i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (lastMidL.Count > 0)
                    {
                        if (note.JsonTime - lastMidL.First().JsonTime <= MaxTimeBomb)
                        {
                            if (note.PosX == 0 && note.JsonTime - lastMidL.First().JsonTime <= MinTimeBomb)
                            {
                                // Fine
                            }
                            else if ((note.PosX != 1 || note.PosY != 1) && note.JsonTime - lastMidL.First().JsonTime <= OverallMin)
                            {
                                // Also fine
                            }
                            else if (note.PosX < 2)
                            {
                                arr.Add(note);
                            }
                        }
                    }
                    if (lastMidR.Count > 0)
                    {
                        if (note.JsonTime - lastMidR.First().JsonTime <= MaxTimeBomb)
                        {
                            if (note.PosX == 3 && note.JsonTime - lastMidR.First().JsonTime <= MinTimeBomb)
                            {
                                // Fine
                            }
                            else if ((note.PosX != 2 || note.PosY != 1) && note.JsonTime - lastMidR.First().JsonTime <= OverallMin)
                            {
                                // Also fine
                            }
                            else if (note.PosX > 1)
                            {
                                arr.Add(note);
                            }
                        }
                    }
                    lastMidL.RemoveAll(l => note.JsonTime - l.JsonTime > MaxTimeNote);
                    lastMidR.RemoveAll(l => note.JsonTime - l.JsonTime > MaxTimeNote);
                    if (note.PosY == 1 && note.PosX == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.PosY == 1 && note.PosX == 2)
                    {
                        lastMidR.Add(note);
                    }
                }

                foreach (var n in arr)
                {
                    if (n.Type == 3)
                    {
                        CreateDiffCommentBomb("R5E - Bombs must be placed to give the player acceptable time to react", CommentTypesEnum.Issue,
                            bombs.Find(b => b.JsonTime == n.JsonTime && b.Type == n.Type && b.PosX == n.PosX && b.PosY == n.PosY));
                        issue = Severity.Fail;
                    }
                }
            }

            return issue;
        }

        #endregion

        #region ProlongedSwing

        public Severity ProlongedSwingCheck()
        {
            var issue = false;
            var unsure = false;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            foreach (var ch in chains)
            {
                if (ch.TailJsonTime - ch.JsonTime >= Plugin.configs.MaxChainBeatLength)
                {
                    // Slow chains
                    CreateDiffCommentLink("R2A - Swing speed should be consistent throughout the map", CommentTypesEnum.Issue, ch);
                    issue = true;
                }
                if (!cubes.Exists(c => c.Time == ch.JsonTime && c.Type == ch.Color && c.Line == ch.PosX && c.Layer == ch.PosY))
                {
                    // Link spam maybe idk
                    CreateDiffCommentLink("R2D - Chain links must have a head note", CommentTypesEnum.Issue, ch);
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
                        if(left.CutDirection == 8)
                        {
                            CreateDiffCommentNote("R2A - Swing speed should be consistent throughout the map", CommentTypesEnum.Unsure, left);
                            unsure = true;
                        }
                        else
                        {
                            CreateDiffCommentNote("R2A - Swing speed should be consistent throughout the map", CommentTypesEnum.Issue, left);
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
                            CreateDiffCommentNote("R2A - Swing speed should be consistent throughout the map", CommentTypesEnum.Unsure, right);
                            unsure = true;
                        }
                        else
                        {
                            CreateDiffCommentNote("R2A - Swing speed should be consistent throughout the map", CommentTypesEnum.Issue, right);
                            issue = true;
                        }
                    }
                }

                previous = right;
            }

            if(issue)
            {
                return Severity.Fail;
            }
            else if(unsure)
            {
                return Severity.Warning;
            }

            return Severity.Success;
        }

        #endregion

        #region Loloppe

        public Severity LoloppeCheck()
        {
            var issue = Severity.Success;

            var song = plugin.BeatSaberSongContainer.Song;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                var red = notes.Where(c => c.Type == 0).ToList();
                var blue = notes.Where(c => c.Type == 1).ToList();
                // TODO: Add diagonal detection, but this is a pain, probably not worth the effort.
                if (red.Count > 0)
                {
                    var previous = red[0];
                    for (int i = 1; i < red.Count; i++)
                    {
                        if (red[i].JsonTime == previous.JsonTime && red[i].CutDirection != 8 && previous.CutDirection != 8)
                        {
                            if (previous.PosY == red[i].PosY)
                            {
                                if ((SwingType.Up.Contains(red[i].CutDirection) && SwingType.Up.Contains(previous.CutDirection)) ||
                                SwingType.Down.Contains(red[i].CutDirection) && SwingType.Down.Contains(previous.CutDirection))
                                {
                                    CreateDiffCommentNote("R3C - Multiple notes of the same color on the same swing must not be parallel", CommentTypesEnum.Issue,
                                    cubes.Find(c => c.Time == red[i].JsonTime && c.Type == red[i].Type
                                    && red[i].PosX == c.Line && red[i].PosY == c.Layer));
                                    issue = Severity.Fail;
                                }
                            }
                            else if (previous.PosX == red[i].PosX)
                            {
                                if ((SwingType.Left.Contains(red[i].CutDirection) && SwingType.Left.Contains(previous.CutDirection)) ||
                                SwingType.Right.Contains(red[i].CutDirection) && SwingType.Right.Contains(previous.CutDirection))
                                {
                                    CreateDiffCommentNote("R3C - Multiple notes of the same color on the same swing must not be parallel", CommentTypesEnum.Issue,
                                    cubes.Find(c => c.Time == red[i].JsonTime && c.Type == red[i].Type
                                    && red[i].PosX == c.Line && red[i].PosY == c.Layer));
                                    issue = Severity.Fail;
                                }
                            }
                        }

                        previous = red[i];
                    }
                }

                if (blue.Count > 0)
                {
                    var previous = blue[0];
                    for (int i = 1; i < blue.Count; i++)
                    {
                        if (blue[i].JsonTime == previous.JsonTime && blue[i].CutDirection != 8 && previous.CutDirection != 8)
                        {
                            if (previous.PosY == blue[i].PosY)
                            {
                                if ((SwingType.Up.Contains(blue[i].CutDirection) && SwingType.Up.Contains(previous.CutDirection)) ||
                                SwingType.Down.Contains(blue[i].CutDirection) && SwingType.Down.Contains(previous.CutDirection))
                                {
                                    CreateDiffCommentNote("R3C - Multiple notes of the same color on the same swing must not be parallel", CommentTypesEnum.Issue,
                                    cubes.Find(c => c.Time == blue[i].JsonTime && c.Type == blue[i].Type
                                    && blue[i].PosX == c.Line && blue[i].PosY == c.Layer));
                                    issue = Severity.Fail;
                                }
                            }
                            else if (previous.PosX == blue[i].PosX)
                            {
                                if ((SwingType.Left.Contains(blue[i].CutDirection) && SwingType.Left.Contains(previous.CutDirection)) ||
                                SwingType.Right.Contains(blue[i].CutDirection) && SwingType.Right.Contains(previous.CutDirection))
                                {
                                    CreateDiffCommentNote("R3C - Multiple notes of the same color on the same swing must not be parallel", CommentTypesEnum.Issue,
                                    cubes.Find(c => c.Time == blue[i].JsonTime && c.Type == blue[i].Type
                                    && blue[i].PosX == c.Line && blue[i].PosY == c.Layer));
                                    issue = Severity.Fail;
                                }
                            }
                        }

                        previous = blue[i];
                    }
                }
            }

            return issue;
        }

        #endregion

        #region SwingPath

        public Severity SwingPathCheck()
        {
            var issue = Severity.Success;

            // https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/notes/hitboxPath.ts
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

                List<BaseNote> arr = new();
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
                    CreateDiffCommentNote("R3E - Notes cannot be placed in the path of a bomb or another note", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        public Severity HitboxCheck()
        {
            var issue = Severity.Success;

            var song = plugin.BeatSaberSongContainer.Song;
            var bpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);

            if (baseDifficulty.Notes.Any())
            {
                // https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/notes/hitboxInline.ts
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
                    CreateDiffCommentNote("R3G - Inline - Hitbox abusive patterns are not allowed", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }

                // https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/notes/hitboxStair.ts
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
                    CreateDiffCommentNote("R3G - Staircase - Hitbox abusive patterns are not allowed", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }

                // https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/notes/hitboxReverseStair.ts
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
                    CreateDiffCommentNote("R3G - Reverse Staircase - Hitbox abusive patterns are not allowed", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }
            }

            return issue;
        }

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
                                arr.Add(other);
                                arr.Add(note);
                                break;
                            }
                        }
                        else if (d > 2.99 && ((note.Type == 0 && note.PosX > 2) || (note.Type == 1 && note.PosX < 1))) // 3-wide
                        {
                            // TODO: This is trash, could easily be done better
                            if (other.PosY == note.PosY)
                            {
                                if (((SwingType.Up_Left.Contains(note.CutDirection) && SwingType.Up_Right.Contains(other.CutDirection)) ||
                                    (SwingType.Up_Right.Contains(note.CutDirection) && SwingType.Up_Left.Contains(other.CutDirection))))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }
                                if ((SwingType.Down_Left.Contains(note.CutDirection) && SwingType.Down_Right.Contains(other.CutDirection)) ||
                                (SwingType.Down_Right.Contains(note.CutDirection) && SwingType.Down_Left.Contains(other.CutDirection)))
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
                    CreateDiffCommentNote("R3D - Patterns must not encourage hand clapping", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Warning;
                }

                foreach (var item in arr)
                {
                    CreateDiffCommentNote("R3D - Patterns must not encourage hand clapping", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                                        && item.PosX == c.Line && item.PosY == c.Layer));
                    issue = Severity.Fail;
                }
            }

            return issue;
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
                ObjectType = Beatmap.Enums.ObjectType.Note
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
                ObjectType = Beatmap.Enums.ObjectType.Chain
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
            for(int i = bombComments.Count() - 2; i >= 0; i--)
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
                    ObjectType = Beatmap.Enums.ObjectType.Note
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

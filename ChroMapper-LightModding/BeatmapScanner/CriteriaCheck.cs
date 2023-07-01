using Beatmap.Base;
using ChroMapper_LightModding.BeatmapScanner.Data;
using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;
using System.ComponentModel;

namespace ChroMapper_LightModding.BeatmapScanner
{
    internal class CriteriaCheck
    {
        #region Properties
        private Plugin plugin;
        private string characteristic;
        private int difficultyRank;
        private string difficulty;
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
                Chain = ChainCheck()
            };
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
            // Add config
            var maximum = 30;
            if (BeatSaberSongContainer.Instance.Song.LevelAuthorName.Count() > maximum)
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
                    if (diff.CustomData != null)
                    {
                        if (diff.CustomData.HasKey("_requirements"))
                        {
                            foreach (var req in diff.CustomData["_requirements"].Values)
                            {
                                CreateSongInfoComment("R1C - " + diff.BeatmapFilename + " has " + req + " requirement", CommentTypesEnum.Issue);
                                issue = Severity.Fail;
                            }
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
            // TODO: Make the 1.33f a config instead
            var limit = 1.33f / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
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
            // TODO: Make the 2f a config instead
            var limit = (BeatSaberSongContainer.Instance.LoadedSongLength - 2f) / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
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

            // TODO: Make the 45 a config instead
            var limit = 45;
            var duration = (cube.Last().Time - cube.First().Time) * (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
            if (duration < limit)
            {
                ExtendOverallComment("R1F - Current map duration is " + duration.ToString() + "s. Minimum required duration is " + limit.ToString() + "s.");
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
            // TODO: Add config
            var maximum = 30;
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            if (diff != null)
            {
                if (diff.CustomData.HasKey("_difficultyLabel"))
                {
                    if (diff.CustomData["_difficultyLabel"].ToString().Count() > 30)
                    {
                        ExtendOverallComment("R7E - " + diff.BeatmapFilename + " difficulty label is too long. Current is " + diff.CustomData["_difficultyLabel"].ToString().Count() + " characters. Maximum " + maximum.ToString() + " characters.");
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
            // TODO: Add auto detect for obscene content
            var diff = BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            if (diff != null)
            {
                if (diff.CustomData.HasKey("_difficultyLabel"))
                {
                    ExtendOverallComment("R7G - Warning - Difficulty name must not contain obscene content");
                    return Severity.Warning;
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
            // TODO: Make the 30ms configurable
            var beatms = 0.03f / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);

            foreach (var w in walls)
            {
                foreach (var c in cubes)
                {
                    if(c.Time > w.JsonTime + w.Duration + beatms)
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

            for(int i = 0; i < bombs.Count; i++)
            {
                var b = bombs[i];
                for(int j = i + 1; j < bombs.Count; j++)
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

            var end = BeatSaberSongContainer.Instance.LoadedSongLength / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);

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
            var end = BeatSaberSongContainer.Instance.LoadedSongLength / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
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
                if(average < 1) // TODO: Make the average a config
                {
                    ExtendOverallComment("R6A - Map doesn't have enough light");
                    issue = Severity.Fail;
                }
                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                // TODO: make fadetime and reactime config
                var fadeTime = 1f * (BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 60);
                var reactTime = 0.25f * (BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 60);
                var eventState = new List<EventState>();
                var eventLitTime = new List<List<EventLitTime>>();
                for(var i = 0; i < 12; i++)
                {
                    EventState es = new(false, 0, 0);
                    eventState.Add(es);
                    eventLitTime.Add(new());
                }
                for(int i = 0; i < lights.Count; i++)
                {
                    var ev = lights[i];
                    if((ev.IsOn || ev.IsFlash) && eventState[ev.Type].State == false)
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
                    if(ev.IsFade)
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
                foreach(var elt in eventLitTime)
                {
                    elt.Reverse();
                }
                for(int i = 0; i < bombs.Count; i++)
                {
                    var bomb = bombs[i];
                    var isLit = false;
                    foreach(var el in eventLitTime)
                    {
                        var t = el.Find(e => e.Time <= bomb.JsonTime);
                        if(t != null)
                        {
                            isLit = isLit || t.State;
                        }
                    }
                    if(!isLit)
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

            foreach(var w in leftWall)
            {
                var note = notes.Where(n => n.Line == 0 && (n.Layer >= 2 || (n.Layer >= 0 && w.PosY == 0)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration).ToList();
                foreach(var n in note)
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
            // TODO: Make wall min/max duration a config
            var min = 0.0138f / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
            var max = 0.250f / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
            // TODO: Make dodge limit per second configurable
            var limit = 2;
            var last = 0d;
            var dodge = 0;
            var side = 0;
            var beat = 60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            // Won't work properly in some very specific situation probably, but I did my best..
            foreach (var w in walls)
            {
                if (w.PosY == 0 && w.Height > 0 && ((w.PosX + w.Width == 2 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 3 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime)) ||
                    (w.PosX + w.Width == 3 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 2 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime))))
                {
                    CreateDiffCommentObstacle("R4C - Walls cannot be placed to force the player to move into the outer lanes", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
                else if ((w.Width >= 3 && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3 || w.PosX == 1)) || (w.Width >= 2 && w.PosX == 1 && w.PosY == 0 && w.Height > 0) || (w.Width >= 4 && w.PosX + w.Width >= 4 && w.PosX <= 0 && w.PosY == 0))
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
                if (dodge > 0 && w.JsonTime >= last + beat)
                {
                    dodge--;
                }
                if (w.PosX + w.Width == 2 && side != 2)
                {
                    last = w.JsonTime;
                    side = 2;
                    dodge++;
                }
                else if (w.PosX == 2 && side != 1)
                {
                    last = w.JsonTime;
                    side = 1;
                    dodge++;
                }
                if (dodge > limit)
                {
                    CreateDiffCommentObstacle("R4B - Dodge walls must not force the players head to move more than 2 times per second", CommentTypesEnum.Issue, w);
                    issue = Severity.Fail;
                }
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
            // TODO: Make max degree a config, also no idea how to actually calculate that properly, 15 is around 45 degree atm.
            var limit = 15;

            if(notes.Count >= 16)
            {
                var link = links.Where(l => l.JsonTime <= notes[15].Time).ToList();
                foreach (var l in link)
                {
                    CreateDiffCommentLink("R2D - Chains and their links cannot be part of the first 16 notes of the map", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }
            else if(links.Any())
            {
                var link = links.Where(l => l.JsonTime >= notes.Last().Time).Take(16 - notes.Count).ToList();
                foreach (var l in link)
                {
                    CreateDiffCommentLink("R2D - Chains and their links cannot be part of the first 16 notes of the map", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }
            
            foreach(var l in links)
            {
                var spacing = Math.Max(Math.Max(Math.Abs(l.TailPosX - l.PosX), Math.Abs(l.TailPosY - l.PosY)), 0);
                var chain = (BaseChain)l;
                if ((spacing == 1 && chain.SliceCount < 3) || (spacing == 2 && chain.SliceCount < 5) || (spacing == 3 && chain.SliceCount < 6))
                {
                    CreateDiffCommentLink("R2D - Chains must be >25% links versus air/empty-space to improve chain recognition", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
                var horizontal = Math.Abs(l.PosX - l.TailPosX) * chain.Squish;
                var vertical = Math.Abs(l.PosY - l.TailPosY) * chain.Squish;
                var newX = l.PosX + (horizontal * Math.Cos(ScanMethod.ConvertDegreesToRadians(ScanMethod.DirectionToDegree[l.CutDirection])));
                var newY = l.PosY + (vertical * Math.Sin(ScanMethod.ConvertDegreesToRadians(ScanMethod.DirectionToDegree[l.CutDirection])));
                if (newX > 4 || newX < -1 || newY > 3 || newY < 0)
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
                    CutDirection = ScanMethod.DirectionToDegree[l.CutDirection],
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
                if (!ScanMethod.IsSameDirection(ScanMethod.FindAngleViaPosition(temp3, 0, 1, temp.CutDirection, true), temp.CutDirection, limit))
                {
                    CreateDiffCommentLink("R2D - Chains cannot change in direction by more than 45°", CommentTypesEnum.Issue, l);
                    issue = Severity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #endregion

        #region Comments

        /// <summary>
        /// Create a comment in the mapsetreview file
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="type">the type</param>
        public void CreateSongInfoComment(string message, CommentTypesEnum type)
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
        public void CreateDiffCommentNote(string message, CommentTypesEnum type, Cube cube)
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

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a note
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="cube">the cube</param>
        public void CreateDiffCommentLink(string message, CommentTypesEnum type, BaseSlider chainLink)
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

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a bomb
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="bomb">the bomb</param>
        public void CreateDiffCommentBomb(string message, CommentTypesEnum type, BaseNote bomb)
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

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a wall
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="wall">the wall</param>
        public void CreateDiffCommentObstacle(string message, CommentTypesEnum type, BaseObstacle wall)
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

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        }

        /// <summary>
        /// Add another line to the OverallComment in the difficultyreview
        /// </summary>
        /// <param name="message">the message</param>
        public void ExtendOverallComment(string message)
        {
            DifficultyReview review = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault();

            review.OverallComment += $" \n{message}";
        }

        #endregion
    }
}

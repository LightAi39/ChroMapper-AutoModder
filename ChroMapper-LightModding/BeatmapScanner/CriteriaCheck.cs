using Beatmap.Base;
using ChroMapper_LightModding.BeatmapScanner.Data;
using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using System.Windows;
using UnityEngine;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;
using static UnityEngine.UI.GridLayoutGroup;

namespace ChroMapper_LightModding.BeatmapScanner
{
    internal class CriteriaCheck
    {
        private Plugin plugin;
        private string characteristic;
        private int difficultyRank;
        private string difficulty;

        public CriteriaCheck(Plugin plugin)
        {
            this.plugin = plugin;
        }

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
                FusedElement = FusedElementCheck()
            };
            return diffCrit;
        }

        public Severity SongNameCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongName.Count() == 0)
            {
                CreateSongInfoComment("R7A - Song Name field is empty", CommentTypesEnum.Issue);
                return Severity.Fail;
            }

            return Severity.Success;
        }

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

        public Severity SongAuthorCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongAuthorName.Count() == 0)
            {
                CreateSongInfoComment("R7C - Song Author field is empty", CommentTypesEnum.Issue);
                return Severity.Fail;
            }
            return Severity.Success;
        }

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

        public Severity OffsetCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongTimeOffset != 0)
            {
                CreateSongInfoComment("R7C - Song Time Offset is not 0. This is a deprecated feature", CommentTypesEnum.Issue);
                return Severity.Fail;
            }
            return Severity.Success;
        }

        public Severity BPMCheck()
        {
            CreateSongInfoComment("R1A - The map's BPM must be set to one of the song's BPM or a multiple of the song's BPM", CommentTypesEnum.Unsure);
            return Severity.Warning;
        }

        public Severity DifficultyOrderingCheck()
        {
            var passStandard = new List<double>();

            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
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
                                List<BaseNote> bombs = baseDifficulty.Notes.Where(n => n.Type == 3).ToList();
                                bombs = bombs.OrderBy(b => b.JsonTime).ToList();

                                List<BaseObstacle> obstacles = baseDifficulty.Obstacles.ToList();
                                obstacles = obstacles.OrderBy(o => o.JsonTime).ToList();

                                var data = BeatmapScanner.Analyzer(notes, bombs, obstacles, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
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

        public Severity RequirementsCheck()
        {
            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffset.DifficultyBeatmaps)
                {
                    if (diff.CustomData != null)
                    {
                        if (diff.CustomData.HasKey("_requirements"))
                        {
                            bool real = false;
                            foreach (var req in diff.CustomData["_requirements"].Values)
                            {
                                CreateSongInfoComment("R1C - " + diff.BeatmapFilename + " has " + req + " requirement", CommentTypesEnum.Issue);
                                real = true;
                            }
                            if (real)
                            {
                                return Severity.Fail;
                            }
                        }
                    }
                }
            }

            return Severity.Success;
        }

        public Severity PreviewCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.PreviewStartTime == 12 && BeatSaberSongContainer.Instance.Song.PreviewDuration == 10)
            {
                CreateSongInfoComment("R7C - Modify Default Song Preview", CommentTypesEnum.Suggestion);
                return Severity.Warning;
            }
            return Severity.Success;
        }

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
                        if (!ScanMethod.IsSameDirectionRestrained(dir[j], degree) || !ScanMethod.IsSameDirectionRestrained(dir[j - 1], dir[j]))
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
                        if (!ScanMethod.IsSameDirectionRestrained(dir[j], degree) || !ScanMethod.IsSameDirectionRestrained(dir[j - 1], dir[j]))
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

        public Severity DifficultyNameCheck()
        {
            ExtendOverallComment("R7G - Warning - Difficulty name must not contain obscene content");
            return Severity.Warning;
        }

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
            var peak = sps.Take((int)((6.25f / 100) * sps.Count())).Average();
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

            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffset.DifficultyBeatmaps)
                {
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
                }
            }

            return issue;
        }

        public Severity FusedElementCheck()
        {
            var issue = Severity.Success;
            // TODO: Add chains?
            var cubes = BeatmapScanner.Cubes;
            var bombs = BeatmapScanner.Bombs;
            var walls = BeatmapScanner.Walls;
            // TODO: Make the 30ms configurable
            var beatms = 0.03f / (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);

            foreach (var w in walls)
            {
                foreach (var c in cubes)
                {
                    if (c.Time >= w.JsonTime - beatms && c.Time <= w.JsonTime + w.Duration + beatms && c.Line <= w.PosX + w.Width - 1 && c.Line >= w.PosX && c.Layer <= w.PosY + w.Height && c.Layer >= w.PosY - 1)
                    {
                        Debug.Log(c.Layer);
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        issue = Severity.Fail;
                    }
                }
                foreach (var b in bombs)
                {
                    if (b.JsonTime >= w.JsonTime - beatms && b.JsonTime <= w.JsonTime + w.Duration + beatms && b.PosX <= w.PosX + w.Width - 1 && b.PosX >= w.PosX && b.PosY <= w.PosY + w.Height && b.PosY >= w.PosY - 1)
                    {
                        Debug.Log("a " + b.PosY);
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
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
                    if (c.Time >= c2.Time && c.Time <= c2.Time + beatms && c.Line == c2.Line && c.Layer == c2.Layer && c.Type != c2.Type)
                    {
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c2);
                        issue = Severity.Fail;
                    }
                }
                for (int j = 0; j < bombs.Count; j++)
                {
                    var b = bombs[j];
                    if (b.JsonTime >= c.Time - beatms && b.JsonTime <= c.Time + beatms && c.Line == b.PosX && c.Layer == b.PosY)
                    {
                        CreateDiffCommentNote("R3FA-B - Notes cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, c);
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
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
                    if (b.JsonTime >= b2.JsonTime - beatms && b.JsonTime <= b2.JsonTime + beatms && b.PosX == b2.PosX && b.PosY == b2.PosY)
                    {
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b);
                        CreateDiffCommentBomb("R5D - Bombs cannot collide with notes, walls, or bombs within 30ms in the same line", CommentTypesEnum.Issue, b2);
                        issue = Severity.Fail;
                    }
                }
            }

            return issue;
        }

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
                Objects = new List<SelectedObject>(),
                Type = type,
                Message = message,
                IsAutogenerated = true
            };
            List<Comment> comments = plugin.currentMapsetReview.Comments;
            comments.Add(comment);
            comments = comments.OrderBy(f => f.Type).ToList();
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

            SelectedObject note = new SelectedObject()
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
                Objects = new List<SelectedObject>() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments = comments.OrderBy(f => f.StartBeat).ToList();
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

            SelectedObject note = new SelectedObject()
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
                Objects = new List<SelectedObject>() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments = comments.OrderBy(f => f.StartBeat).ToList();
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

            SelectedObject note = new SelectedObject()
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
                Objects = new List<SelectedObject>() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

            comments.Add(comment);
            comments = comments.OrderBy(f => f.StartBeat).ToList();
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
    }
}

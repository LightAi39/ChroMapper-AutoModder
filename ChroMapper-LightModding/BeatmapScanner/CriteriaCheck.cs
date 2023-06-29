using Beatmap.Base;
using ChroMapper_LightModding.BeatmapScanner.Data;
using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;

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
                NJS = NJSCheck()
            };
            return diffCrit;
        }

        public static Severity SongNameCheck()
        {
            if(BeatSaberSongContainer.Instance.Song.SongName.Count() == 0)
            {
                // TODO
                Debug.Log("Error found: Song Name is empty.");
                return Severity.Fail;
            }

            return Severity.Success;
        }

        public static Severity SubNameCheck()
        {
            var issue = Severity.Success;
            var name = BeatSaberSongContainer.Instance.Song.SongName.ToLower();
            var author = BeatSaberSongContainer.Instance.Song.SongAuthorName.ToLower();
            if (name.Count() != 0)
            {
                if (name.Contains("remix") || name.Contains("ver.") || name.Contains("feat.") || name.Contains("ft.") || name.Contains("featuring") || name.Contains("cover"))
                {
                    // TODO
                    Debug.Log("Error found in the Song Name field: Tags should be in the Sub Name field.");
                    issue = Severity.Fail;
                }  
            }
            if (author.Count() != 0)
            {
                if (author.Contains("remix") || author.Contains("ver.") || author.Contains("feat.") || author.Contains("ft.") || author.Contains("featuring") || author.Contains("cover"))
                {
                    // TODO
                    Debug.Log("Error found in the Song Author field: Tags should be in the Sub Name field.");
                    issue = Severity.Fail;
                }    
            }

            return issue;
        }

        public static Severity SongAuthorCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongAuthorName.Count() == 0)
            {
                // TODO
                Debug.Log("Error found: The Song Author field is empty.");
                return Severity.Fail;
            }
            return Severity.Success;
        }

        public static Severity CreatorCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.LevelAuthorName.Count() == 0)
            {
                // TODO
                Debug.Log("Error found: The Creator field is empty.");
                return Severity.Fail;
            }
            // Add config
            var maximum = 30;
            if (BeatSaberSongContainer.Instance.Song.LevelAuthorName.Count() > maximum)
            {
                // TODO
                Debug.Log("Error found: The Creator field is too long. Maybe use a group name instead?");
                return Severity.Warning;
            }
            return Severity.Success;
        }

        public static Severity OffsetCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.SongTimeOffset != 0)
            {
                // TODO
                Debug.Log("Error found: Song Time Offset is not 0. This is a deprecated feature.");
                return Severity.Fail;
            }
            return Severity.Success;
        }

        public static Severity BPMCheck()
        {
            // TODO idk
            return Severity.Success;
        }

        public static Severity DifficultyOrderingCheck()
        {
            // TODO: Run BeatmapScanner on all diffs from the song menu to fetch the pass rating of each, and then use that info to check if the ordering is right. Ascending order.
            return Severity.Success;
        }

        public static Severity RequirementsCheck()
        {
            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffset.DifficultyBeatmaps)
                {
                    if (diff.CustomData.HasKey("_requirements"))
                    {
                        foreach (var req in diff.CustomData["_requirements"].Values)
                        {
                            // TODO
                            Debug.Log("Error found: " + diff.BeatmapFilename + " has mod requirement.");
                            return Severity.Fail;
                        }
                    }
                }
            }
 
            return Severity.Success;
        }

        public static Severity PreviewCheck()
        {
            if (BeatSaberSongContainer.Instance.Song.PreviewStartTime == 12 && BeatSaberSongContainer.Instance.Song.PreviewDuration == 10)
            {
                // TODO
                Debug.Log("The Preview duration is currently default value, is that right?");
                return Severity.Warning;
            }
            return Severity.Success;
        }

        

        public static Severity HotStartCheck()
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
                    // TODO: Add error message on the note here
                    Debug.Log("Error found: note at " + c.Time + " beat is a hot start. Minimum " + limit.ToString() + " beat.");
                    issue = Severity.Fail;
                }
                else break;
            }
            foreach (var w in wall)
            {
                if (w.JsonTime < limit && (w.PosX == 1 || w.PosX == 2))
                {
                    // TODO: Add error message on the wall here
                    Debug.Log("Error found: wall at " + w.JsonTime + " is a hot start. Minimum " + limit.ToString() + " beat.");
                    issue = Severity.Fail;
                }
                else break;
            }
            return issue;
        }

        public static Severity ColdEndCheck()
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
                    // TODO: Add error message on the note here
                    Debug.Log("Error found: note at " + c.Time + " beat is a cold end. Maximum " + limit.ToString() + " beat.");
                    issue = Severity.Fail;
                }
                else break;
            }
            foreach (var w in wall)
            {
                if (w.JsonTime + w.Duration > limit && (w.PosX == 1 || w.PosX == 2))
                {
                    // TODO: Add error message on the wall here
                    Debug.Log("Error found: wall at " + w.JsonTime + " beat is a cold end. Maximum " + limit.ToString() + " beat.");
                    issue = Severity.Fail;
                }
                else break;
            }
            return issue;
        }

        public static Severity MinSongDurationCheck()
        {
            var cube = BeatmapScanner.Cubes;
            cube = cube.OrderBy(c => c.Time).ToList();

            // TODO: Make the 45 a config instead
            var limit = 45;
            var duration = (cube.Last().Time - cube.First().Time) * (60 / BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
            if (duration < limit)
            {
                // TODO: Add error message on the note here
                Debug.Log("Error found: Current map duration is " + duration.ToString() + "s. Expected map duration is " + limit.ToString() + "s.");
                return Severity.Fail;
            }

            return Severity.Success;
        }

        public static Severity SliderCheck()
        {
            var issue = Severity.Success;
            var precision = 0d;
            var cube = BeatmapScanner.Cubes.Where(c => c.Slider && !c.Head);
            cube = cube.OrderBy(c => c.Time).ToList();
            var temp = cube.Where(c => c.Spacing == 0).Select(c => c.Precision).ToList();
            if(temp.Count() == 0)
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
            
            foreach(var c in cube)
            {
                if(c.Slider && !c.Head)
                {
                    if(!(c.Precision <= ((c.Spacing + 1) * precision) + 0.001 && c.Precision >= ((c.Spacing + 1) * precision) - 0.001))
                    {
                        var reality = ScanMethod.RealToFraction(c.Precision, 0.01);
                        var expected = ScanMethod.RealToFraction(((c.Spacing + 1) * precision), 0.01);
                        // TODO: Add error message on the note here
                        Debug.Log("Error found: " + c.Time + " is " + reality.N.ToString() + "/" + reality.D.ToString() + " but should of been " + expected.N.ToString() + "/" + expected.D.ToString() + ".");
                        issue = Severity.Warning; // Maybe fail idk
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
                            // TODO: Add error message on the note here
                            Debug.Log("Error found: Slider note " + red[i - dir.Count() + j].Time + " is over 45 degrees.");
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
                        if(blue.Count() == i)
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
                            // TODO: Add error message on the note here
                            Debug.Log("Error found: Slider note " + blue[i - dir.Count() + j].Time + " is over 45 degrees.");
                            issue = Severity.Fail;
                        }
                    }

                    i--;
                }
            }

            return issue;
        }

        public static Severity DifficultyLabelSizeCheck()
        {
            // TODO: Add config
            var maximum = 30;
            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffset.DifficultyBeatmaps)
                {
                    // TODO add a check here to only check the select diff
                    if (diff.CustomData.HasKey("_difficultyLabel"))
                    {
                        if (diff.CustomData["_difficultyLabel"].ToString().Count() > 30)
                        {
                            // TODO
                            Debug.Log("Error found: " + diff.BeatmapFilename + " difficulty label is too long. Current is " + diff.CustomData["_difficultyLabel"].ToString().Count() + " characters. Maximum " + maximum.ToString() + " characters.");
                            return Severity.Fail;
                        }
                    }
                }
            }
            return Severity.Success;
        }

        public static Severity DifficultyNameCheck()
        {
            // TODO: Ask the mapper to check that it is obscene-free. Could test the label in some kind of database but probably not worth the effort.
            return Severity.Warning;
        }

        public static Severity NJSCheck()
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

            Debug.Log(peak);

            foreach (var diffset in BeatSaberSongContainer.Instance.Song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffset.DifficultyBeatmaps)
                {
                    if (diff.NoteJumpMovementSpeed == 0)
                    {
                        // TODO
                        Debug.Log("Error: NJS for X beatmap is currently 0");
                        issue = Severity.Fail;
                    }
                    else
                    {
                        if (diff.NoteJumpMovementSpeed < NJS.min || diff.NoteJumpMovementSpeed > NJS.max)
                        {
                            // TODO
                            Debug.Log("Warning: Recommended NJS for X beatmap is " + NJS.min.ToString() + " - " + NJS.max.ToString());
                            issue = Severity.Warning;
                        }
                        var currRT = diff.NoteJumpStartBeatOffset / (2.0f * diff.NoteJumpMovementSpeed) * 1000.0f;
                        if (currRT < RT.min || currRT > RT.max)
                        {
                            // TODO
                            Debug.Log("Warning: Recommended RT for X beatmap is " + RT.min.ToString() + " - " + RT.max.ToString());
                            Debug.Log("Current RT is " + currRT);
                            issue = Severity.Warning;
                        }
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
                Message = message
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
                Message = message
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
                Color = 2,
                ObjectType = bomb.ObjectType
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = bomb.JsonTime,
                Objects = new List<SelectedObject>() { note },
                Type = type,
                Message = message
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
                Message = message
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

            review.OverallComment += $"\n{message}";
        }
    }
}

﻿using Beatmap.Enums;
using BLMapCheck;
using BLMapCheck.Classes.Results;
using ChroMapper_LightModding.Models;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChroMapper_LightModding.Helpers
{
    internal class AutocheckHelper
    {
        private Plugin plugin;
        private BLMapChecker criteriaCheck;
        private FileHelper fileHelper;

        private string characteristic;
        private int difficultyRank;
        private string difficulty;
        private string lastLoaded = "";
        private Comment lastFoundBombFusedComment = null;

        public AutocheckHelper(Plugin plugin, FileHelper fileHelper)
        {
            this.plugin = plugin;
            criteriaCheck = new BLMapChecker();
            this.fileHelper = fileHelper;
        }

        // this is temporary
        public (double pass, double tech, double ebpm, double pebpm, double sps, string handness, double duration) RunAutoCheck(bool isAutoCheckOnInfo, bool isAutoCheckOnDiff, bool isForMapCheckStats, bool isTimingCheck, string characteristic = "", int difficultyRank = 0, string difficulty = "")
        {
            // So it doesn't reload the map on every button press
            if(lastLoaded != plugin.currentlyLoadedFolderPath)
            {
                lastLoaded = plugin.currentlyLoadedFolderPath;
                criteriaCheck.LoadMap(plugin.currentlyLoadedFolderPath);
            }
            CheckResults results; //= criteriaCheck.CheckAllCriteria();

            if (isTimingCheck)
            {
                // Find the diff and only overwrite that specific diff
                if (lastLoaded == plugin.currentlyLoadedFolderPath)
                {
                    var diff = BLMapChecker.map.Difficulties.Where(x => x.Characteristic == characteristic && x.Difficulty == difficulty).FirstOrDefault();
                    var newDiff = BLMapChecker.parser.TryLoadPath(plugin.currentlyLoadedFolderPath, characteristic, difficulty);
                    diff.Data = newDiff.Difficulty.Data;
                }
                results = criteriaCheck.CompareTimings(characteristic, difficulty);
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).ToList());
            }
            else if (isAutoCheckOnInfo)
            {
                results = criteriaCheck.CheckSongInfo();
                RemovePastAutoCheckCommentsSongInfo();
                plugin.currentMapsetReview.Criteria = results.InfoCriteriaResult;
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == null && x.Characteristic == null).ToList());
            }
            else if (isAutoCheckOnDiff)
            {
                // Find the diff and only overwrite that specific diff
                if (lastLoaded == plugin.currentlyLoadedFolderPath)
                {
                    var diff = BLMapChecker.map.Difficulties.Where(x => x.Characteristic == characteristic && x.Difficulty == difficulty).FirstOrDefault();
                    var newDiff = BLMapChecker.parser.TryLoadPath(plugin.currentlyLoadedFolderPath, characteristic, difficulty);
                    diff.Data = newDiff.Difficulty.Data;
                }
                results = criteriaCheck.CheckSingleDifficulty(characteristic, difficulty);
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Critera = results.DifficultyCriteriaResults.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).FirstOrDefault().Crit;
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).ToList());
            }
            else if (isForMapCheckStats)
            {
                results = criteriaCheck.CheckDifficultyStatistics(characteristic, difficulty);
                var resultData = results.Results.Where(x => x.Name == "Statistical Data" && x.Characteristic == characteristic && x.Difficulty == difficulty).FirstOrDefault().ResultData;
                return (
                    Convert.ToDouble(resultData.Where(x => x.Key == "Pass").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Tech").FirstOrDefault().Value) * 10,
                    Convert.ToDouble(resultData.Where(x => x.Key == "EBPM").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "PEBPM").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "SPS").FirstOrDefault().Value),
                    resultData.Where(x => x.Key == "Handness").FirstOrDefault().Value,
                    Convert.ToDouble(resultData.Where(x => x.Key == "Duration").FirstOrDefault().Value)
                    );
            }
            return (0, 0, 0, 0, 0, "", 0);

        }

        public void RunCompareTimings(string characteristic, int difficultyRank, string difficulty)
        {
            RunAutoCheck(false, false, false, true, characteristic, difficultyRank, difficulty);
        }

        public void RunAutoCheckOnInfo()
        {
            //RemovePastAutoCheckCommentsSongInfo();
            //plugin.currentMapsetReview.Criteria = criteriaCheck.AutoInfoCheck();
            RunAutoCheck(true, false, false, false);
        }

        public void RunAutoCheckOnDiff(string characteristic, int difficultyRank, string difficulty)
        {
            //fileHelper.CheckDifficultyReviewsExist();

            //var result = RunBeatmapScanner(characteristic, difficultyRank, difficulty);

            //if (result != (-1, -1, -1, -1, -1, -1, -1, -1, "-1"))
            //{
            //    RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
            //    plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Critera = criteriaCheck.AutoDiffCheck(characteristic, difficultyRank, difficulty);
            //}
            RunAutoCheck(false, true, false, false, characteristic, difficultyRank, difficulty);
            plugin.editorUI.RunBeatmapScannerOnThisDiff(); // this is temporary
        }

        public (double pass, double tech, double ebpm, double pebpm, double sps, string handness, double duration) RunBeatmapScanner(string characteristic, int difficultyRank, string difficulty)
        {
            //var song = plugin.BeatSaberSongContainer.Song;
            //BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();

            //BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);

            //if (baseDifficulty.Notes.Any())
            //{
            //    List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
            //    notes = notes.OrderBy(o => o.JsonTime).ToList();

            //    if (notes.Count > 0)
            //    {
            //        List<BaseSlider> chains = baseDifficulty.Chains.Cast<BaseSlider>().ToList();
            //        chains = chains.OrderBy(o => o.JsonTime).ToList();

            //        List<BaseNote> bombs = baseDifficulty.Notes.Where(n => n.Type == 3).ToList();
            //        bombs = bombs.OrderBy(b => b.JsonTime).ToList();

            //        List<BaseObstacle> obstacles = baseDifficulty.Obstacles.ToList();
            //        obstacles = obstacles.OrderBy(o => o.JsonTime).ToList();

            //        var data = BeatmapScanner.BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
            //        var analysedMap = new MapAnalyser(song.Directory);
            //        var swings = analysedMap.GetSwingData((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());
            //        data.sps = Math.Round(analysedMap.GetSPS((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower()), 2);
            //        var temp = analysedMap.GetHandedness((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());
            //        data.handness = Math.Round(temp.Y, 2).ToString() + "/" + Math.Round(temp.X, 2).ToString();
            //        data.reset = Math.Round((double)analysedMap.GetResetCount((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower(), ResetType.Bomb) / swings.Count() * 100, 2);
            //        data.ebpm = Math.Round(analysedMap.GetAverageEBPM((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower()), 2);
            //        return data;
            //    }
            //}
            //return (-1, -1, -1, -1, -1, -1, -1, -1, "-1");

            return RunAutoCheck(false, false, true, false, characteristic, difficultyRank, difficulty);
        }

        public void RemovePastAutoCheckCommentsSongInfo()
        {
            plugin.currentMapsetReview.Comments = plugin.currentMapsetReview.Comments.Where(x => x.IsAutogenerated == false || x.Response != "" || x.MarkAsSuppressed).ToList();
        }

        public void RemovePastAutoCheckCommentsOnDiff(string characteristic, int difficultyRank, string difficulty)
        {
            plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().OverallComment = "";
            plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments.Where(x => x.IsAutogenerated == false || x.Response != "" || x.MarkAsSuppressed).ToList();
        }


        public void CreateCommentsFromNewData(List<CheckResult> checkResults)
        {
            checkResults = checkResults.Where(x => x.Name != "Statistical Data").ToList();
            // song info comments
            if (checkResults.Where(x => x.Difficulty == null && x.Characteristic == null).ToList().Count != 0)
            {
                plugin.currentMapsetReview.Comments = new();
            }
            foreach (var item in checkResults.Where(x => x.Difficulty == null && x.Characteristic == null).ToList())
            {
                CommentTypesEnum? commentType;
                switch (item.Severity)
                {
                    case Severity.Passed:
                        commentType = null;
                        break;
                    case Severity.Info:
                        commentType = CommentTypesEnum.Info;
                        break;
                    case Severity.Suggestion:
                        commentType = CommentTypesEnum.Suggestion;
                        break;
                    case Severity.Warning:
                        commentType = CommentTypesEnum.Unsure;
                        break;
                    case Severity.Error:
                        commentType = CommentTypesEnum.Issue;
                        break;
                    case Severity.Inconclusive:
                        commentType = CommentTypesEnum.Unsure;
                        break;
                    default:
                        commentType = null;
                        break;
                }
                if(commentType != null)
                {
                    CreateSongInfoComment(item.Description, (CommentTypesEnum)commentType);
                }
            }


            // difficulty comments
            foreach (var item in checkResults.Where(x => x.Difficulty != null && x.Characteristic != null).ToList())
            {
                difficulty = item.Difficulty;
                characteristic = item.Characteristic;
                switch (item.Difficulty)
                {
                    case "Easy":
                        difficultyRank = 1;
                        break;
                    case "Normal":
                        difficultyRank = 3;
                        break;
                    case "Hard":
                        difficultyRank = 5;
                        break;
                    case "Expert":
                        difficultyRank = 7;
                        break;
                    case "ExpertPlus":
                        difficultyRank = 9;
                        break;
                    default:
                        break;
                }

                CommentTypesEnum? commentType;
                switch (item.Severity)
                {
                    case Severity.Passed:
                        commentType = null;
                        break;
                    case Severity.Info:
                        commentType = CommentTypesEnum.Info;
                        break;
                    case Severity.Suggestion:
                        commentType = CommentTypesEnum.Suggestion;
                        break;
                    case Severity.Warning:
                        commentType = CommentTypesEnum.Unsure;
                        break;
                    case Severity.Error:
                        commentType = CommentTypesEnum.Issue;
                        break;
                    case Severity.Inconclusive:
                        commentType = CommentTypesEnum.Unsure;
                        break;
                    default:
                        commentType = null;
                        break;
                }

                if (item.BeatmapObjects == null || item.BeatmapObjects.Count == 0)
                {
                    if (commentType != null)
                    {
                        ExtendOverallComment(item.Description);
                    }
                }
                else if (item.BeatmapObjects[0] is Note note)
                {
                    if (commentType != null)
                    {
                        if (item.BeatmapObjects.Count > 1)
                        {
                            CreateDiffCommentNotes(item.Description, (CommentTypesEnum)commentType, item);
                        }
                        CreateDiffCommentNote(item.Description, (CommentTypesEnum)commentType, item);
                    }
                    
                }
                else if (item.BeatmapObjects[0] is Bomb bomb)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentBomb(item.Description, (CommentTypesEnum)commentType, item);
                    }
                }
                else if (item.BeatmapObjects[0] is Chain slider)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentLink(item.Description, (CommentTypesEnum)commentType, item);
                    }
                }
                else if (item.BeatmapObjects[0] is Wall wall)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentObstacle(item.Description, (CommentTypesEnum)commentType, item);
                    }
                }
            }
        }


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
        private void CreateDiffCommentNote(string message, CommentTypesEnum type, CheckResult result)
        {
            string id = Guid.NewGuid().ToString();

            Note cube = (Note)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = cube.Beats,
                PosX = cube.x,
                PosY = cube.y,
                Color = cube.Color,
                ObjectType = ObjectType.Note
            };

            Comment comment;
            if (result.ResultData.Any(x => x.Key == "currentReactionTime"))
            {
                message += "\n  Current RT: " + result.ResultData.Where(x => x.Key == "currentReactionTime").FirstOrDefault().Value;
            }
            if (result.ResultData.Any(x => x.Key == "targetReactionTime"))
            {
                message += "\n  Target RT: " + result.ResultData.Where(x => x.Key == "targetReactionTime").FirstOrDefault().Value;
            }

            foreach (var item in result.ResultData.Where(x => x.Key != "currentReactionTime" && x.Key != "targetReactionTime"))
            {
                message += "\n  " + item.Key + ": " + item.Value;
            }

            comment = new()
            {
                Id = id,
                StartBeat = cube.Beats,
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
        private void CreateDiffCommentLink(string message, CommentTypesEnum type, CheckResult result)
        {
            string id = Guid.NewGuid().ToString();

            Chain chainLink = (Chain)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = chainLink.Beats,
                PosX = chainLink.x,
                PosY = chainLink.y,
                Color = chainLink.Color,
                ObjectType = ObjectType.Chain
            };

            if (result.ResultData.Any(x => x.Key == "currentReactionTime"))
            {
                message += "\n  Current RT: " + result.ResultData.Where(x => x.Key == "currentReactionTime").FirstOrDefault().Value;
            }
            if (result.ResultData.Any(x => x.Key == "targetReactionTime"))
            {
                message += "\n  Target RT: " + result.ResultData.Where(x => x.Key == "targetReactionTime").FirstOrDefault().Value;
            }

            foreach (var item in result.ResultData.Where(x => x.Key != "currentReactionTime" && x.Key != "targetReactionTime"))
            {
                message += "\n  " + item.Key + ": " + item.Value;
            }

            Comment comment = new()
            {
                Id = id,
                StartBeat = chainLink.Beats,
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
        private void CreateDiffCommentBomb(string message, CommentTypesEnum type, CheckResult result)
        {
            string id = Guid.NewGuid().ToString();

            Bomb bomb = (Bomb)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = bomb.Beats,
                PosX = bomb.x,
                PosY = bomb.y,
                Color = 3,
                ObjectType = ObjectType.Note
            };

            if (result.ResultData.Any(x => x.Key == "currentReactionTime"))
            {
                message += "\n  Current RT: " + result.ResultData.Where(x => x.Key == "currentReactionTime").FirstOrDefault().Value;
            }
            if (result.ResultData.Any(x => x.Key == "targetReactionTime"))
            {
                message += "\n  Target RT: " + result.ResultData.Where(x => x.Key == "targetReactionTime").FirstOrDefault().Value;
            }

            foreach (var item in result.ResultData.Where(x => x.Key != "currentReactionTime" && x.Key != "targetReactionTime"))
            {
                message += "\n  " + item.Key + ": " + item.Value;
            }

            Comment comment = new()
            {
                Id = id,
                StartBeat = bomb.Beats,
                Objects = new() { note },
                Type = type,
                Message = message,
                IsAutogenerated = true
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview?.DifficultyReviews?.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                // Preferably this would be Message instead of Type, but message currently include Response, which can have different values.
                bool found = false;
                if (lastFoundBombFusedComment == null)
                {
                    found = comments.Exists(x => x.Type == comment.Type && x.StartBeat <= comment.StartBeat && x.StartBeat >= comment.StartBeat - 0.25 && x.Objects.Exists(o => o.Color == 3));
                    lastFoundBombFusedComment = comment;
                }
                else
                {
                    if (lastFoundBombFusedComment.Type == comment.Type && lastFoundBombFusedComment.StartBeat <= comment.StartBeat && lastFoundBombFusedComment.StartBeat >= comment.StartBeat - 0.25)
                    {
                        found = true;
                        lastFoundBombFusedComment = comment;
                    }
                    else
                    {
                        lastFoundBombFusedComment = null;
                    }
                }
                
                // Skip that comment if already found on a previous bomb.
                if (found) return;
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
        private void CreateDiffCommentObstacle(string message, CommentTypesEnum type, CheckResult result)
        {
            string id = Guid.NewGuid().ToString();

            Wall wall = (Wall)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = wall.Beats,
                PosX = wall.x,
                PosY = wall.y,
                Color = 0,
                ObjectType = ObjectType.Obstacle
            };

            if (result.ResultData.Any(x => x.Key == "currentReactionTime"))
            {
                message += "\n  Current RT: " + result.ResultData.Where(x => x.Key == "currentReactionTime").FirstOrDefault().Value;
            }
            if (result.ResultData.Any(x => x.Key == "targetReactionTime"))
            {
                message += "\n  Target RT: " + result.ResultData.Where(x => x.Key == "targetReactionTime").FirstOrDefault().Value;
            }

            foreach (var item in result.ResultData.Where(x => x.Key != "currentReactionTime" && x.Key != "targetReactionTime"))
            {
                message += "\n  " + item.Key + ": " + item.Value;
            }

            Comment comment = new()
            {
                Id = id,
                StartBeat = wall.Beats,
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

        private void CreateDiffCommentNotes(string message, CommentTypesEnum type, CheckResult result )
        {
            List<Note> notes = result.BeatmapObjects.Where(x => x is Note).Cast<Note>().ToList();

            if (notes.Count == 0) return;
            string id = Guid.NewGuid().ToString();

            List<SelectedObject> objects = new();

            foreach (var note in notes)
            {
                objects.Add(new()
                {
                    Beat = note.Beats,
                    PosX = note.x,
                    PosY = note.y,
                    Color = note.Color,
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
    }
}

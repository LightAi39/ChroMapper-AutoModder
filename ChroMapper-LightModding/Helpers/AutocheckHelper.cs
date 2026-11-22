using Beatmap.Enums;
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
        // Best song BPM found by the last song info auto check, shown in the info menu. Null if not detected for the song currently open.
        public string DetectedBpm => detectedBpmFolder == plugin.currentlyLoadedFolderPath ? detectedBpm : null;
        private string detectedBpm;
        private string detectedBpmFolder;
        // Last bomb comment of each check in the current run, close bombs of the same check are merged into it
        private readonly Dictionary<string, (Comment Comment, float LastBeat)> lastBombComments = new();
        // Bombs closer than this (in beats) to the previous bomb of the same check share a comment
        private const float BombMergeDistance = 0.25f;

        public AutocheckHelper(Plugin plugin, FileHelper fileHelper)
        {
            this.plugin = plugin;
            criteriaCheck = new BLMapChecker();
            this.fileHelper = fileHelper;
        }

        // this is temporary
        public (double stars, double pass, double tech, double acc, double ebpm, double pebpm, double sps, string handness, double duration) RunAutoCheck(bool isAutoCheckOnInfo, 
            bool isAutoCheckOnDiff, bool isForMapCheckStats, bool isTimingCheck, 
            string characteristic = "", int difficultyRank = 0, string difficulty = "",
            List<string> mod = null, string targetChar = "", string targetDiff = "")
        {
            criteriaCheck.Config = Plugin.checkerConfig;

            // So it doesn't reload the map on every button press
            if(lastLoaded != plugin.currentlyLoadedFolderPath)
            {
                lastLoaded = plugin.currentlyLoadedFolderPath;
                criteriaCheck.LoadMap(plugin.currentlyLoadedFolderPath);
                detectedBpm = null;
            }
            CheckResults results; //= criteriaCheck.CheckAllCriteria();

            if (mod != null)
            {
                // Only reload that specific diff
                criteriaCheck.ReloadDifficulty(plugin.currentlyLoadedFolderPath, characteristic, difficulty);
                results = criteriaCheck.ImportMod(characteristic, difficulty, difficultyRank, mod);
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).ToList(), false);
            }
            else if (isTimingCheck)
            {
                // Only reload that specific diff
                criteriaCheck.ReloadDifficulty(plugin.currentlyLoadedFolderPath, characteristic, difficulty);
                results = criteriaCheck.CompareTimings(characteristic, difficulty, difficultyRank, targetChar, targetDiff);
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).ToList());
            }
            else if (isAutoCheckOnInfo)
            {
                results = criteriaCheck.CheckSongInfo();
                // The candidates are listed best first
                var detectedData = results.Results.FirstOrDefault(x => x.Name == "Detected BPM")?.ResultData;
                detectedBpm = detectedData?.FirstOrDefault(x => x.Key == "DetectedBPM")?.Value.Split(',')[0].Trim();
                detectedBpmFolder = plugin.currentlyLoadedFolderPath;
                plugin.currentMapsetReview.DetectedTempo = detectedData?.FirstOrDefault(x => x.Key == "DetectedTempo")?.Value;
                RemovePastAutoCheckCommentsSongInfo();
                plugin.currentMapsetReview.Criteria = results.InfoCriteriaResult;
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == null && x.Characteristic == null).ToList());
            }
            else if (isAutoCheckOnDiff)
            {
                // Only reload that specific diff
                criteriaCheck.ReloadDifficulty(plugin.currentlyLoadedFolderPath, characteristic, difficulty);
                results = criteriaCheck.CheckSingleDifficulty(characteristic, difficulty, difficultyRank);
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Critera = results.DifficultyCriteriaResults.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).FirstOrDefault().Crit;
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).ToList());
            }
            else if (isForMapCheckStats)
            {
                results = criteriaCheck.CheckDifficultyStatistics(characteristic, difficulty, difficultyRank);
                var resultData = results.Results.Where(x => x.Name == "Statistical Data" && x.Characteristic == characteristic && x.Difficulty == difficulty).FirstOrDefault()?.ResultData;
                // Difficulties with too few notes, or that the analyzer can't rate, have no statistics
                if (resultData == null) return (0, 0, 0, 0, 0, 0, 0, "", 0);
                return (
                    Convert.ToDouble(resultData.Where(x => x.Key == "Stars").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Pass").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Tech").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Acc").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "EBPM").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "PEBPM").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "SPS").FirstOrDefault().Value),
                    resultData.Where(x => x.Key == "Handness").FirstOrDefault().Value,
                    Convert.ToDouble(resultData.Where(x => x.Key == "Duration").FirstOrDefault().Value)
                    );
            }
            return (0, 0, 0, 0, 0, 0, 0, "", 0);

        }

        public void RunImportMod(string characteristic, int difficultyRank, string difficulty, List<string> mod)
        {
            RunAutoCheck(false, false, false, false, characteristic, difficultyRank, difficulty, mod);
        }

        public void RunCompareTimings(string characteristic, int difficultyRank, string difficulty, string targetChar, string targetDiff)
        {
            RunAutoCheck(false, false, false, true, characteristic, difficultyRank, difficulty, null, targetChar, targetDiff);
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

        public (double stars, double pass, double tech, double acc, double ebpm, double pebpm, double sps, string handness, double duration) RunBeatmapScanner(string characteristic, int difficultyRank, string difficulty)
        {
            //var song = plugin.BeatSaberSongContainer.Info;
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

            //        var data = BeatmapScanner.BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatSaberSongContainer.Instance.Info.BeatsPerMinute);
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

        public void CreateCommentsFromNewData(List<CheckResult> checkResults, bool autogenerated = true)
        {
            lastBombComments.Clear();
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
                        commentType = CommentTypesEnum.Note;
                        break;
                    case Severity.Suggestion:
                        commentType = CommentTypesEnum.Suggestion;
                        break;
                    case Severity.Warning:
                        commentType = CommentTypesEnum.Questionable;
                        break;
                    case Severity.Error:
                        commentType = CommentTypesEnum.Unrankable;
                        break;
                    case Severity.Inconclusive:
                        commentType = CommentTypesEnum.Questionable;
                        break;
                    case Severity.Data: 
                        commentType = CommentTypesEnum.Data;
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
                difficultyRank = item.difficultyRank;

                CommentTypesEnum? commentType;
                switch (item.Severity)
                {
                    case Severity.Passed:
                        commentType = null;
                        break;
                    case Severity.Info:
                        commentType = CommentTypesEnum.Note;
                        break;
                    case Severity.Suggestion:
                        commentType = CommentTypesEnum.Suggestion;
                        break;
                    case Severity.Warning:
                        commentType = CommentTypesEnum.Questionable;
                        break;
                    case Severity.Error:
                        commentType = CommentTypesEnum.Unrankable;
                        break;
                    case Severity.Inconclusive:
                        commentType = CommentTypesEnum.Questionable;
                        break;
                    case Severity.Data:
                        commentType = CommentTypesEnum.Data;
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
                            CreateDiffCommentNotes(item.Description, (CommentTypesEnum)commentType, item, autogenerated);
                        }
                        else CreateDiffCommentNote(item.Description, (CommentTypesEnum)commentType, item, autogenerated);
                    }
                }
                else if (item.BeatmapObjects[0] is Bomb bomb)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentBomb(item.Description, (CommentTypesEnum)commentType, item, autogenerated);
                    }
                }
                else if (item.BeatmapObjects[0] is Chain slider)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentLink(item.Description, (CommentTypesEnum)commentType, item, autogenerated);
                    }
                }
                else if (item.BeatmapObjects[0] is Wall wall)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentObstacle(item.Description, (CommentTypesEnum)commentType, item, autogenerated);
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
            AddSorted(comments, comment);
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a note
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="cube">the cube</param>
        private void CreateDiffCommentNote(string message, CommentTypesEnum type, CheckResult result, bool autogenerated)
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
                IsAutogenerated = autogenerated
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                AddSorted(comments, comment);
            }
        }

        private void CreateDiffCommentNotes(string message, CommentTypesEnum type, CheckResult result, bool autogenerated)
        {
            List<SelectedObject> objects = ToSelectedObjects(result);

            if (objects.Count == 0) return;
            string id = Guid.NewGuid().ToString();

            Comment comment = new()
            {
                Id = id,
                StartBeat = objects.FirstOrDefault().Beat,
                Objects = objects,
                Type = type,
                Message = message,
                IsAutogenerated = autogenerated
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                AddSorted(comments, comment);
            }
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a note
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="cube">the cube</param>
        private void CreateDiffCommentLink(string message, CommentTypesEnum type, CheckResult result, bool autogenerated)
        {
            string id = Guid.NewGuid().ToString();

            Chain chainLink = (Chain)result.BeatmapObjects[0];

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
                Objects = ToSelectedObjects(result),
                Type = type,
                Message = message,
                IsAutogenerated = autogenerated
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                AddSorted(comments, comment);
            }
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a bomb
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="bomb">the bomb</param>
        private void CreateDiffCommentBomb(string message, CommentTypesEnum type, CheckResult result, bool autogenerated)
        {
            string id = Guid.NewGuid().ToString();

            Bomb bomb = (Bomb)result.BeatmapObjects[0];
            // Bomb clusters are merged per check, so the key is the check message before the result data is added
            string mergeKey = type + "|" + message;

            // Every object of the result, a fused pair has two
            List<SelectedObject> bombObjects = ToSelectedObjects(result);

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
                Objects = bombObjects,
                Type = type,
                Message = message,
                IsAutogenerated = autogenerated
            };

            // Close to the previous bomb of the same check: add the bombs to that comment instead of creating a new one.
            // Only comments created in this run are merged, so other checks and manual comments are never affected.
            if (lastBombComments.TryGetValue(mergeKey, out var last) && bomb.Beats >= last.Comment.StartBeat && bomb.Beats - last.LastBeat <= BombMergeDistance)
            {
                foreach (var obj in bombObjects.Where(o => !last.Comment.Objects.Exists(e => e.Beat == o.Beat && e.PosX == o.PosX && e.PosY == o.PosY)))
                {
                    last.Comment.Objects.Add(obj);
                }
                lastBombComments[mergeKey] = (last.Comment, Math.Max(last.LastBeat, bombObjects.Max(o => o.Beat)));
                return;
            }

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview?.DifficultyReviews?.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                AddSorted(comments, comment);
                lastBombComments[mergeKey] = (comment, bombObjects.Max(o => o.Beat));
            }
        }

        /// <summary>
        /// Create a comment in a difficultyreview for a wall
        /// </summary>
        /// <param name="message">the mesasge</param>
        /// <param name="type">the severity</param>
        /// <param name="wall">the wall</param>
        private void CreateDiffCommentObstacle(string message, CommentTypesEnum type, CheckResult result, bool autogenerated)
        {
            string id = Guid.NewGuid().ToString();

            Wall wall = (Wall)result.BeatmapObjects[0];

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
                Objects = ToSelectedObjects(result),
                Type = type,
                Message = message,
                IsAutogenerated = autogenerated
            };

            if (!CheckIfCommentAlreadyExists(comment))
            {
                List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
                AddSorted(comments, comment);
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

            string target = comment.Objects.FirstOrDefault()?.ToStringFull();
            return comments.Any(c => comment.Message == c.Message && c.Objects.Any(o => String.Equals(o.ToStringFull(), target, StringComparison.OrdinalIgnoreCase)));
        }

        // Every object of a result as comment objects, so mixed results (a note fused with a bomb or a wall) highlight all of them
        private static List<SelectedObject> ToSelectedObjects(CheckResult result)
        {
            List<SelectedObject> objects = new();
            foreach (var obj in result.BeatmapObjects)
            {
                switch (obj)
                {
                    case Note note:
                        objects.Add(new() { Beat = note.Beats, PosX = note.x, PosY = note.y, Color = note.Color, ObjectType = ObjectType.Note });
                        break;
                    case Bomb bomb:
                        objects.Add(new() { Beat = bomb.Beats, PosX = bomb.x, PosY = bomb.y, Color = 3, ObjectType = ObjectType.Note });
                        break;
                    case Chain chain:
                        objects.Add(new() { Beat = chain.Beats, PosX = chain.x, PosY = chain.y, Color = chain.Color, ObjectType = ObjectType.Chain });
                        break;
                    case Wall wall:
                        objects.Add(new() { Beat = wall.Beats, PosX = wall.x, PosY = wall.y, Color = 0, ObjectType = ObjectType.Obstacle });
                        break;
                }
            }
            return objects;
        }

        // Keeps the list sorted by beat without sorting it again for every comment, auto check can add thousands of them
        private static void AddSorted(List<Comment> comments, Comment comment)
        {
            int lo = 0, hi = comments.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (comments[mid].StartBeat <= comment.StartBeat) lo = mid + 1;
                else hi = mid;
            }
            comments.Insert(lo, comment);
        }
    }
}

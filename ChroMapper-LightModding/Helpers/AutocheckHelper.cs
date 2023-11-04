using Beatmap.Base;
using Beatmap.Enums;
using BLMapCheck;
using BLMapCheck.Classes.MapVersion.Difficulty;
using BLMapCheck.Classes.Results;
using ChroMapper_LightModding.Models;
using JoshaParity;
using System;
using System.Collections.Generic;
using System.Linq;
using Obstacle = BLMapCheck.Classes.MapVersion.Difficulty.Obstacle;

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

        public AutocheckHelper(Plugin plugin, FileHelper fileHelper)
        {
            this.plugin = plugin;
            criteriaCheck = new BLMapChecker();
            this.fileHelper = fileHelper;
        }

        public (double diff, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) RunAutoCheck(bool isAutoCheckOnInfo, bool isAutoCheckOnDiff, bool isForMapCheckStats, string characteristic = "", int difficultyRank = 0, string difficulty = "")
        {
            criteriaCheck.LoadMap(plugin.currentlyLoadedFolderPath, BeatSaberSongContainer.Instance.LoadedSongLength);
            var results = criteriaCheck.CheckAllCriteria();

            if (isAutoCheckOnInfo)
            {
                RemovePastAutoCheckCommentsSongInfo();
                plugin.currentMapsetReview.Criteria = results.InfoCriteriaResult;
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == null && x.Characteristic == null).ToList());
            }
            if (isAutoCheckOnDiff)
            {
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Critera = results.DifficultyCriteriaResults.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).FirstOrDefault().Crit;
                CreateCommentsFromNewData(results.Results.Where(x => x.Difficulty == difficulty && x.Characteristic == characteristic).ToList());
            }
            if (isForMapCheckStats)
            {
                var resultData = results.Results.Where(x => x.Name == "Statistical Data" && x.Characteristic == characteristic && x.Difficulty == difficulty).FirstOrDefault().ResultData;
                return (
                    Convert.ToDouble(resultData.Where(x => x.Key == "Pass").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Tech").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "EBPM").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Slider").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "BombReset").FirstOrDefault().Value),
                    Convert.ToInt32(resultData.Where(x => x.Key == "Crouch").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "Linear").FirstOrDefault().Value),
                    Convert.ToDouble(resultData.Where(x => x.Key == "SPS").FirstOrDefault().Value),
                    resultData.Where(x => x.Key == "Handness").FirstOrDefault().Value
                    );
            }
            return (0, 0, 0, 0, 0, 0, 0, 0, "");

        }

        public void RunAutoCheckOnInfo()
        {
            //RemovePastAutoCheckCommentsSongInfo();
            //plugin.currentMapsetReview.Criteria = criteriaCheck.AutoInfoCheck();
            RunAutoCheck(true, false, false);
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
            RunAutoCheck(false, true, false, characteristic, difficultyRank, difficulty);
        }

        public (double diff, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) RunBeatmapScanner(string characteristic, int difficultyRank, string difficulty)
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

            return RunAutoCheck(false, false, true, characteristic, difficultyRank, difficulty);
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
                    ExtendOverallComment(item.Description);
                }
                else if (item.BeatmapObjects[0] is Colornote note)
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
                else if (item.BeatmapObjects[0] is Bombnote bomb)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentBomb(item.Description, (CommentTypesEnum)commentType, item);
                    }
                }
                else if (item.BeatmapObjects[0] is Burstslider slider)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentLink(item.Description, (CommentTypesEnum)commentType, item);
                    }
                }
                else if (item.BeatmapObjects[0] is Obstacle wall)
                {
                    if (commentType != null)
                    {
                        CreateDiffCommentObstacle(item.Description, (CommentTypesEnum)commentType, item);
                    }
                }
            }

            //FuseBombComments(); TODO: THIS DOESNT WORK ANYMORE IDK WHY
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

            Colornote cube = (Colornote)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = cube.b,
                PosX = cube.x,
                PosY = cube.y,
                Color = cube.c,
                ObjectType = ObjectType.Note
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = cube.b,
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

            Burstslider chainLink = (Burstslider)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = chainLink.b,
                PosX = chainLink.x,
                PosY = chainLink.y,
                Color = chainLink.c,
                ObjectType = ObjectType.Chain
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = chainLink.b,
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

            Bombnote bomb = (Bombnote)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = bomb.b,
                PosX = bomb.x,
                PosY = bomb.y,
                Color = 3,
                ObjectType = ObjectType.Note
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = bomb.b,
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
        private void CreateDiffCommentObstacle(string message, CommentTypesEnum type, CheckResult result)
        {
            string id = Guid.NewGuid().ToString();

            Obstacle wall = (Obstacle)result.BeatmapObjects[0];

            SelectedObject note = new()
            {
                Beat = wall.b,
                PosX = wall.x,
                PosY = wall.y,
                Color = 0,
                ObjectType = ObjectType.Obstacle
            };

            Comment comment = new()
            {
                Id = id,
                StartBeat = wall.b,
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
            List<Comment> comments = plugin.currentMapsetReview?.DifficultyReviews?.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
            if (comments?.Count == 0) return;
            
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

        private void CreateDiffCommentNotes(string message, CommentTypesEnum type, CheckResult result )
        {
            List<Colornote> notes = result.BeatmapObjects.Cast<Colornote>().ToList();

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
    }
}

using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using Parity = BLMapCheck.BeatmapScanner.MapCheck.Parity;
using Configs = BLMapCheck.Configs.Config;
using BLMapCheck.Classes.ChroMapper;
using BLMapCheck.Classes.Unity;

namespace BLMapCheck.BeatmapScanner
{
    internal class CriteriaCheck
    {
        #region Variables
        public static Configs config = new();
        #endregion

        #region Properties
        //private Plugin plugin;
        private string characteristic;
        private int difficultyRank;
        private string difficulty;
        //private BaseDifficulty baseDifficulty;
        private double songOffset;
        private BeatPerMinute bpm;
        private MapAnalyser analysedMap;
        private List<JoshaParity.SwingData> swings;
        private double averageSliderDuration;
        private (double pass, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) BeatmapScannerData;
        #endregion

        #region Constructors
        public CriteriaCheck()
        {
            //this.plugin = plugin;
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

            /* TODO: Rewrite this song loading code
            var song = plugin.BeatSaberSongContainer.Song;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            songOffset = BeatSaberSongContainer.Instance.Song.SongTimeOffset;
            bpm = BeatPerMinute.Create(BeatSaberSongContainer.Instance.Song.BeatsPerMinute, baseDifficulty.BpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), songOffset);
            analysedMap = new(song.Directory);
            swings = analysedMap.GetSwingData((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());

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

                    BeatmapScannerData = BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
                }
            }
            */

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

            if (/*Plugin.configs.HighlightOffbeat TODO: fix*/ true)
            {
                HighlightOffbeat();
            }
            if (/*Plugin.configs.DisplayFlick TODO: fix */ true)
            {
                RollingEBPM();
            }

            FuseBombComments();

            return diffCrit;
        }

        #endregion

        #region Comments
        // THIS IS ALL OLD TODO: USE NEW METHOD

        /// <summary>
        /// Create a comment in the mapsetreview file
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="type">the type</param>
        //private void CreateSongInfoComment(string message, CommentTypesEnum type)
        //{
        //    string id = Guid.NewGuid().ToString();


        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = 0,
        //        Objects = new(),
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };
        //    List<Comment> comments = plugin.currentMapsetReview.Comments;
        //    comments.Add(comment);
        //    comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //}


        ///// <summary>
        ///// Create a comment in a difficultyreview for a note
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="cube">the cube</param>
        //private void CreateDiffCommentNote(string message, CommentTypesEnum type, Cube cube)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = cube.Time,
        //        PosX = cube.Line,
        //        PosY = cube.Layer,
        //        Color = cube.Type,
        //        ObjectType = ObjectType.Note
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = cube.Time,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Create a comment in a difficultyreview for a note
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="cube">the cube</param>
        //private void CreateDiffCommentLink(string message, CommentTypesEnum type, BaseSlider chainLink)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = chainLink.JsonTime,
        //        PosX = chainLink.PosX,
        //        PosY = chainLink.PosY,
        //        Color = chainLink.Color,
        //        ObjectType = ObjectType.Chain
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = chainLink.JsonTime,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Create a comment in a difficultyreview for a bomb
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="bomb">the bomb</param>
        //private void CreateDiffCommentBomb(string message, CommentTypesEnum type, BaseNote bomb)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = bomb.JsonTime,
        //        PosX = bomb.PosX,
        //        PosY = bomb.PosY,
        //        Color = 3,
        //        ObjectType = bomb.ObjectType
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = bomb.JsonTime,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Create a comment in a difficultyreview for a wall
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="wall">the wall</param>
        //private void CreateDiffCommentObstacle(string message, CommentTypesEnum type, BaseObstacle wall)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = wall.JsonTime,
        //        PosX = wall.PosX,
        //        PosY = wall.PosY,
        //        Color = 0,
        //        ObjectType = wall.ObjectType
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = wall.JsonTime,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Add another line to the OverallComment in the difficultyreview
        ///// </summary>
        ///// <param name="message">the message</param>
        //private void ExtendOverallComment(string message)
        //{
        //    DifficultyReview review = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault();

        //    review.OverallComment += $" \n{message}";
        //}

        //private bool CheckIfCommentAlreadyExists(Comment comment)
        //{
        //    List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

        //    return comments.Any(c => comment.Message == c.Message && c.Objects.Any(o => String.Equals(o.ToStringFull().ToLower(), comment.Objects.FirstOrDefault().ToStringFull().ToLower(), StringComparison.InvariantCulture)));
        //}

        //private void FuseBombComments()
        //{
        //    List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //    var bombComments = comments.Where(c => c.Objects.All(o => o.Color == 3)).ToList(); // Only bombs comments
        //    for (int i = bombComments.Count() - 2; i >= 0; i--)
        //    {
        //        if (bombComments[i + 1].Message == bombComments[i].Message && bombComments[i + 1].StartBeat >= bombComments[i].StartBeat && bombComments[i + 1].StartBeat <= bombComments[i].StartBeat + 0.25)
        //        {
        //            bombComments[i + 1].Objects.ForEach(o => bombComments[i].Objects.Add(o));
        //            comments.Remove(bombComments[i + 1]);
        //        }
        //    }
        //}

        //private void CreateDiffCommentNotes(string message, CommentTypesEnum type, List<Note> notes)
        //{
        //    if (notes.Count == 0) return;
        //    string id = Guid.NewGuid().ToString();

        //    List<SelectedObject> objects = new();

        //    foreach (var note in notes)
        //    {
        //        objects.Add(new()
        //        {
        //            Beat = note.b,
        //            PosX = note.x,
        //            PosY = note.y,
        //            Color = note.c,
        //            ObjectType = ObjectType.Note
        //        });
        //    }

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = objects.FirstOrDefault().Beat,
        //        Objects = objects,
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        #endregion
    }
}

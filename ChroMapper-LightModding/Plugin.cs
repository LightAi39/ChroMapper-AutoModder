using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChroMapper_LightModding.Models;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ChroMapper_LightModding
{

    [Plugin("LightModding")]
    public class Plugin
    {
        static public int backupLimit = 3;

        static public BeatSaberSongContainer _beatSaberSongContainer = null!;
        private NoteGridContainer _noteGridContainer = null!;
        private ObstacleGridContainer _obstacleGridContainer = null!;

        private Scene currentScene;
        private bool inEditorScene;

        private DifficultyReview currentReview = null;
        private string currentlyLoadedFilePath = null;


        string text;

        [Init]
        private void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;

            // register a button in the side tab menu
            ExtensionButton button = ExtensionButtons.AddButton(LoadSprite("ChroMapper_LightModding.Assets.Icon.png"), "LightModding", ShowMainUI);

        }

        [Exit]
        private void Exit()
        {

        }

        #region Event Handlers

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentScene = scene;

            if (scene.buildIndex == 3) // the editor scene
            {
                inEditorScene = true;
                _noteGridContainer = UnityEngine.Object.FindObjectOfType<NoteGridContainer>();
                _obstacleGridContainer = UnityEngine.Object.FindObjectOfType<ObstacleGridContainer>();
                _beatSaberSongContainer = UnityEngine.Object.FindObjectOfType<BeatSaberSongContainer>();

                // check in the map folder for any existing review files for this difficulty, then load it if it is not a backup
                try
                {
                    List<string> files = Directory.GetFiles(_beatSaberSongContainer.Song.Directory, "*.lreview").ToList();
                    List<(DifficultyReview, string)> reviews = new();

                    if (files.Count == 0)
                    {
                        Debug.Log("No review files found in this map file");
                        return;
                    }

                    foreach (string file in files)
                    {
                        if (!file.Contains("AUTOMATIC_BACKUP.lreview"))
                        {
                            reviews.Add((JsonConvert.DeserializeObject<DifficultyReview>(File.ReadAllText(file)), file));
                        }
                    }

                    reviews = reviews.OrderByDescending(f => f.Item1.FinalizationDate).ToList();

                    var correctReviewFilePair = reviews.First(x => x.Item1.DifficultyRank == _beatSaberSongContainer.DifficultyData.DifficultyRank);

                    currentReview = correctReviewFilePair.Item1;
                    currentlyLoadedFilePath = correctReviewFilePair.Item2;
                    Debug.Log("Loaded existing review file.");

                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message == "Sequence contains no matching element")
                    {
                        Debug.Log("No review files found in this map file for the current difficulty");
                    }
                    
                }
            }
            else
            {
                if (currentReview != null)
                {
                    BackupFile();
                }
                inEditorScene = false;
                currentReview = null;
                currentlyLoadedFilePath = null;
            }
        }

        #endregion Event Handlers

        #region UI

        private void ShowCreateUI(float beat, int posX, int posY)
        {
            CommentTypesEnum type = CommentTypesEnum.Issue;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Add comment to note");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Beat {beat} - row {posY} - lane {posX}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .OnChanged((string s) => { text = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>()
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(HandleCreateComment, "Create");

            dialog.Open();
        }

        // soontm supported
        private void ShowCreateUIMultiple(float beat1, int posX1, int posY1, float beat2, int posX2, int posY2)
        {
            CommentTypesEnum type = CommentTypesEnum.Issue;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Add comment to multiple notes");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"First note at Beat {beat1} - row {posY1} - lane {posX1}");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Last note at Beat {beat2} - row {posY2} - lane {posX2}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .OnChanged((string s) => { text = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>()
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(HandleCreateComment, "Create");

            dialog.Open();
        }

        private void ShowMainUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Main UI");

            dialog.AddFooterButton(null, "Close");

            if (currentReview == null)
            {
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"No review file found!");

                dialog.AddFooterButton(ShowCreateFileUI, "Create review file");
            } else
            {
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Existing review file loaded!");
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue(currentlyLoadedFilePath);

                dialog.AddFooterButton(ShowDeleteFileUI, "Remove review file");

                dialog.AddFooterButton(ShowSaveFileUI, "Save review file");
            }

            dialog.Open();
        }

        private void ShowCreateFileUI()
        {
            var song = _beatSaberSongContainer.Song;
            var difficultyData = _beatSaberSongContainer.DifficultyData;

            string title = $"Mod of {song.SongName} {song.SongSubName} by {song.SongAuthorName} - {difficultyData.Difficulty} ({difficultyData.DifficultyRank}) mapped by {song.LevelAuthorName}";
            string author = "Your name";
            ReviewTypeEnum type = ReviewTypeEnum.Feedback;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Create review file");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Title")
                .WithInitialValue(title)
                .OnChanged((string s) => { title = s; });

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Author")
                .WithInitialValue(author)
                .OnChanged((string s) => { author = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Review Type")
                .WithOptions<ReviewTypeEnum>()
                .OnChanged((int i) => { type = (ReviewTypeEnum)i; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() =>
            {
                HandleCreateFile(title, author, type);
            }, "Create");

            dialog.Open();
            
        }

        private void ShowSaveFileUI()
        {
            bool overwrite = true;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Save review file");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Do you want to overwrite the current file or keep it?");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Overwriting cannot be undone.");

            dialog.AddComponent<ToggleComponent>()
                .WithLabel("Overwrite?")
                .WithInitialValue(overwrite)
                .OnChanged((bool o) => { overwrite = o; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() =>
            {
                SaveFile(overwrite);
                dialog.Close();
            }, "Save");
            dialog.Open();
        }

        private void ShowDeleteFileUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Delete review file");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Are you sure you want to delete the currently loaded review file?");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"This cannot be undone.");
            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() =>
            {
                RemoveFile(currentlyLoadedFilePath);
                dialog.Close();
            }, "Delete");
            dialog.Open();
        }

        #endregion UI

        #region Comment Handling

        private void HandleCreateComment()
        {

        }

        #endregion Comment Handling

        #region File Handling

        private void HandleCreateFile(string title, string author, ReviewTypeEnum type)
        {
            var song = _beatSaberSongContainer.Song;
            var difficultyData = _beatSaberSongContainer.DifficultyData;

            DifficultyReview review = new()
            {
                Title = title,
                Author = author,
                MapName = song.Directory.Split(Path.DirectorySeparatorChar).Last(),
                Difficulty = difficultyData.Difficulty,
                DifficultyRank = difficultyData.DifficultyRank,
                ReviewType = type,
                Comments = new()
            };

            Debug.Log($"Exporting a new review file:");
            Debug.Log(JsonConvert.SerializeObject(review, Formatting.Indented));

            currentReview = review;

            string newFilePath = $"{song.Directory}/{review.MapName} [{review.Difficulty} {review.DifficultyRank}] {review.ReviewType} {review.Author} {review.FinalizationDate.Day}-{review.FinalizationDate.Month}-{review.FinalizationDate.Year} {review.FinalizationDate.Hour}.{review.FinalizationDate.Minute}.{review.FinalizationDate.Second}.lreview";
            File.WriteAllText(newFilePath, JsonConvert.SerializeObject(review, Formatting.Indented));
            currentlyLoadedFilePath = newFilePath;

        }

        private void SaveFile(bool overwrite)
        {
            var review = currentReview;
            review.FinalizationDate = DateTime.UtcNow;
            string newFilePath = $"{_beatSaberSongContainer.Song.Directory}/{review.MapName} [{review.Difficulty} {review.DifficultyRank}] {review.ReviewType} {review.Author} {review.FinalizationDate.Day}-{review.FinalizationDate.Month}-{review.FinalizationDate.Year} {review.FinalizationDate.Hour}.{review.FinalizationDate.Minute}.{review.FinalizationDate.Second}.lreview";
            File.WriteAllText(newFilePath, JsonConvert.SerializeObject(review, Formatting.Indented));

            if (overwrite)
            {
                File.Delete(currentlyLoadedFilePath);
            }

            currentlyLoadedFilePath = newFilePath;
        }

        private void BackupFile()
        {
            // preparation for the backup limit
            List<string> files = Directory.GetFiles(_beatSaberSongContainer.Song.Directory, "*AUTOMATIC_BACKUP.lreview").ToList();

            List<(DifficultyReview, string)> reviews = new();

            foreach (string file in files)
            {
                reviews.Add((JsonConvert.DeserializeObject<DifficultyReview>(File.ReadAllText(file)), file));
            }

            reviews = reviews.OrderBy(f => f.Item1.FinalizationDate).ToList();

            var correctReviewFilePairs = reviews.Where(x => x.Item1.DifficultyRank == _beatSaberSongContainer.DifficultyData.DifficultyRank).ToList();

            // enforcing the backup limit
            if (correctReviewFilePairs.Count >= backupLimit)
            {
                File.Delete(correctReviewFilePairs[0].Item2);
            }

            var review = currentReview;
            review.FinalizationDate = DateTime.UtcNow;
            File.WriteAllText($"{_beatSaberSongContainer.Song.Directory}/{review.MapName} [{review.Difficulty} {review.DifficultyRank}] {review.ReviewType} {review.Author} {review.FinalizationDate.Day}-{review.FinalizationDate.Month}-{review.FinalizationDate.Year} {review.FinalizationDate.Hour}.{review.FinalizationDate.Minute}.{review.FinalizationDate.Second} AUTOMATIC_BACKUP.lreview", JsonConvert.SerializeObject(review, Formatting.Indented));
        }

        private void RemoveFile(string path)
        {
            File.Delete(path);
            currentReview = null;
            currentlyLoadedFilePath = null;
        }

        #endregion File Handling

        #region Other

        public static Sprite LoadSprite(string asset) // taken from Moizac's Extended LightIDs code because i didn't want to figure it out myself
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(asset);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.LoadImage(data);

            return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
        }

        #endregion Other
    }
}

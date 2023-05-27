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
using static UnityEngine.InputSystem.InputRemoting;
using Beatmap.Enums;
using Beatmap.V3;
using Beatmap.Base;
using UnityEngine.InputSystem;
using System.Xml.Linq;
using System.ComponentModel;
using ChroMapper_LightModding.Export;
using System.Windows.Media;
using Color = UnityEngine.Color;

namespace ChroMapper_LightModding
{

    [Plugin("LightModding")]
    public class Plugin
    {
        static public int backupLimit = 3;
        public bool showOutlines = true;

        static public string fileVersion = "0.0.1";

        static public BeatSaberSongContainer _beatSaberSongContainer = null!;
        private NoteGridContainer _noteGridContainer = null!;
        private ObstacleGridContainer _obstacleGridContainer = null!;
        private EventGridContainer _eventGridContainer = null!;
        private ArcGridContainer _arcGridContainer = null!;
        private ChainGridContainer _chainGridContainer = null!;
        private BPMChangeGridContainer _bpmChangeGridContainer = null!;
        private BeatmapObjectContainerCollection _beatmapObjectContainerCollection = null!;

        private HashSet<BaseObject> selectionCache;

        private Scene currentScene;
        private bool inEditorScene;

        private DifficultyReview currentReview = null;
        private string currentlyLoadedFilePath = null;

        private Exporter exporter = new();

        InputAction addCommentAction;
        InputAction openCommentAction;

        [Init]
        private void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;

            // register a button in the side tab menu
            ExtensionButton button = ExtensionButtons.AddButton(LoadSprite("ChroMapper_LightModding.Assets.Icon.png"), "LightModding", ShowMainUI);

            addCommentAction = new InputAction("Add Comment", type: InputActionType.Button);
            addCommentAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("modifier", "<Keyboard>/ctrl")
                .With("button", "<Keyboard>/g");
            addCommentAction.performed += _ => { AddCommentKeyEvent(); };
            addCommentAction.Enable();

            openCommentAction = new InputAction("Open Comment", type: InputActionType.Button);
            openCommentAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("modifier", "<Keyboard>/alt")
                .With("button", "<Keyboard>/g");
            openCommentAction.performed += _ => { OpenCommentKeyEvent(); };

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
                addCommentAction.Enable();
                openCommentAction.Enable();
                _noteGridContainer = UnityEngine.Object.FindObjectOfType<NoteGridContainer>();
                _obstacleGridContainer = UnityEngine.Object.FindObjectOfType<ObstacleGridContainer>();
                _eventGridContainer = UnityEngine.Object.FindObjectOfType<EventGridContainer>();
                _bpmChangeGridContainer = UnityEngine.Object.FindObjectOfType<BPMChangeGridContainer>();
                _beatSaberSongContainer = UnityEngine.Object.FindObjectOfType<BeatSaberSongContainer>();
                _beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
                _arcGridContainer = UnityEngine.Object.FindObjectOfType<ArcGridContainer>();
                _chainGridContainer = UnityEngine.Object.FindObjectOfType<ChainGridContainer>();

                // check in the map folder for any existing review files for this difficulty, then load it if it is not a backup
                try
                {
                    if (!Directory.Exists($"{_beatSaberSongContainer.Song.Directory}/reviews"))
                    {
                        Debug.Log("No review files folder found in this map file");
                        return;
                    }
                    List<string> files = Directory.GetFiles($"{_beatSaberSongContainer.Song.Directory}/reviews", "*.lreview").ToList();
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

                    reviews = reviews.Where(f => f.Item1.Version == fileVersion).ToList();

                    if (reviews.Count == 0)
                    {
                        Debug.Log("No review files found in this map file with the correct file version");
                        return;
                    }

                    reviews = reviews.OrderByDescending(f => f.Item1.FinalizationDate).ToList();

                    var correctReviewFilePair = reviews.First(x => x.Item1.DifficultyRank == _beatSaberSongContainer.DifficultyData.DifficultyRank);

                    currentReview = correctReviewFilePair.Item1;
                    currentlyLoadedFilePath = correctReviewFilePair.Item2;
                    _beatmapObjectContainerCollection.ContainerSpawnedEvent += SetOutlineIfInReview;
                    _obstacleGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
                    _eventGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
                    _bpmChangeGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
                    _arcGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
                    _chainGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
                    SelectionController.ObjectWasSelectedEvent += UpdateSelectionCache;
                    SelectionController.SelectionChangedEvent += ManageSelectionCacheAndOutlines;
                    selectionCache = new();
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
                addCommentAction.Disable();
                openCommentAction.Disable();
                selectionCache = null;
            }
        }

        public void AddCommentKeyEvent()
        {
            if (currentReview == null) { Debug.Log("Comment Creation not executed, no file loaded.");  return; }
            var selection = SelectionController.SelectedObjects;
            

            if (SelectionController.HasSelectedObjects())
            {
                List<SelectedObject> selectedObjects = GetSelectedObjectListFromSelection(selection);

                if (selectedObjects.Count > 0)
                {
                    Debug.Log(JsonConvert.SerializeObject(selectedObjects, Formatting.Indented));

                    if (currentReview.Comments.Any(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)))
                    {
                        Debug.Log("Comment with that selection already exists, going to edit mode");
                        ShowEditCommentUI(currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).First(), true);
                    } else
                    {
                        Debug.Log("Opening Comment Creation UI");
                        ShowCreateCommentUI(selectedObjects);
                    }

                } else
                {
                    Debug.Log("Comment Creation cancelled, no supported objects selected.");
                }
            } else
            {
                Debug.Log("Comment Creation not executed, selection is empty.");
            }

            SelectionController.DeselectAll();
        }

        public void OpenCommentKeyEvent()
        {
            if (currentReview == null) { Debug.Log("Comment Loading not executed, no file loaded."); return; }
            var selection = SelectionController.SelectedObjects;


            if (SelectionController.HasSelectedObjects())
            {
                List<SelectedObject> selectedObjects = GetSelectedObjectListFromSelection(selection);

                if (selectedObjects.Count > 1)
                {
                    if (currentReview.Comments.Any(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)))
                    {
                        if (currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).Count() == 1)
                        {
                            Debug.Log("Opening Review UI");
                            ShowReviewCommentUI(currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).First().Id);
                        } else
                        {
                            Debug.Log("Opening Review choice UI");
                            ShowReviewChooseUI(currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).ToList());
                        }
                    }
                    else
                    {
                        Debug.Log("No Comments found");
                    }

                } else if (selectedObjects.Count == 1)
                {
                    if (currentReview.Comments.Any(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(selectedObjects[0]))))
                    {
                        if (currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(selectedObjects[0]))).Count() == 1)
                        {
                            Debug.Log("Opening Review UI");
                            ShowReviewCommentUI(currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(selectedObjects[0]))).First().Id);
                        }
                        else
                        {
                            Debug.Log("Opening Review choice UI");
                            ShowReviewChooseUI(currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(selectedObjects[0]))).ToList());
                        }
                    }
                    else
                    {
                        Debug.Log("No Comments found");
                    }
                }
                else
                {
                    Debug.Log("Comment Loading cancelled, no supported objects selected.");
                }
            }
            else
            {
                Debug.Log("Comment Loading not executed, selection is empty.");
            }

            SelectionController.DeselectAll();
        }

        #endregion Event Handlers

        #region UI

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

                dialog.AddComponent<ButtonComponent>()
                    .WithLabel("Copy comments to clipboard")
                    .OnClick(() => { exporter.ExportToDiscordMD(currentReview); });

                dialog.AddComponent<ButtonComponent>()
                    .WithLabel("Show all Comments")
                    .OnClick(ShowAllCommentsMainUI);

                dialog.AddComponent<ButtonComponent>()
                    .WithLabel("Edit file information")
                    .OnClick(() =>
                    {
                        dialog.Close();
                        EditFileInformationUI();
                    });

                dialog.AddComponent<ToggleComponent>()
                    .WithLabel("Show outlines")
                    .WithInitialValue(showOutlines)
                    .OnChanged((bool o) => { showOutlines = o; });


                dialog.AddFooterButton(ShowDeleteFileUI, "Remove review file");

                dialog.AddFooterButton(ShowSaveFileUI, "Save review file");
            }

            dialog.Open();
        }

        private void EditFileInformationUI()
        {
            string title = currentReview.Title;
            string author = currentReview.Author;
            ReviewTypeEnum type = currentReview.ReviewType;
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Edit file information");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Title:")
                .WithInitialValue(title)
                .OnChanged((string s) => { title = s; });

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Author:")
                .WithInitialValue(author)
                .OnChanged((string s) => { author = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<ReviewTypeEnum>()
                .WithInitialValue(Convert.ToInt32(type))
                .OnChanged((int i) => { type = (ReviewTypeEnum)i; });

            dialog.AddFooterButton(null, "Close");
            dialog.AddFooterButton(() =>
            {
                currentReview.Title = title;
                currentReview.Author = author;
                currentReview.ReviewType = type;
            }, "Save Changes");

            dialog.Open();
        }

        private void ShowAllCommentsMainUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("All Comments");
            List<Comment> comments = currentReview.Comments.Take(5).ToList();

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsRead)
                {
                    read = " - Marked As Read";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
            }

            dialog.AddFooterButton(ShowAllCommentsMainUI, "<-");
            dialog.AddFooterButton(null, "Close");
            if (currentReview.Comments.Count > 5)
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllCommentsMoreUI(5);
                }, "->");
            } else
            {
                dialog.AddFooterButton(ShowAllCommentsMainUI, "->");
            }
            

            dialog.Open();
        }

        private void ShowAllCommentsMoreUI(int startIndex)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("All Comments");
            int count = 5;
            bool lastTab = false;
            if (currentReview.Comments.Count < startIndex+count)
            {
                count = currentReview.Comments.Count - startIndex;
                lastTab = true;
            }
            List<Comment> comments = currentReview.Comments.GetRange(startIndex, count).ToList();

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsRead)
                {
                    read = " - Marked As Read";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
            }

            if (startIndex == 5)
            {
                dialog.AddFooterButton(ShowAllCommentsMainUI, "<-");
            } else
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllCommentsMoreUI(startIndex-5);
                }, "<-");
            }
            
            dialog.AddFooterButton(null, "Close");
            if (lastTab)
            {
                dialog.AddFooterButton(() => ShowAllCommentsMoreUI(startIndex), "->");
            } else
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllCommentsMoreUI(startIndex + 5);
                }, "->");
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


        private void ShowCreateCommentUI(List<SelectedObject> selectedObjects)
        {
            CommentTypesEnum type = CommentTypesEnum.Note;
            string message = "Comment";

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Add comment");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Objects: " + string.Join(", ", selectedObjects.ConvertAll(p => p.ToString())));

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>()
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() => { HandleCreateComment(type, message, selectedObjects, true); }, "Create");

            dialog.Open();
        }

        private void ShowReviewCommentUI(string id)
        {
            Comment comment = currentReview.Comments.Where(x => x.Id == id).First();
            string message = comment.Response;
            bool read = comment.MarkAsRead;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("View comment");
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())));

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Type: {comment.Type}");

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Comment: {comment.Message}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Response:")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            dialog.AddComponent<ToggleComponent>()
                .WithLabel("Mark as read")
                .WithInitialValue(read)
                .OnChanged((bool o) => { read = o; });

            dialog.AddFooterButton(null, "Close");
            dialog.AddFooterButton(() =>
            {
                comment.Response = message;
                comment.MarkAsRead = read;
                ShowEditCommentUI(comment);
            }, "Edit comment");
            dialog.AddFooterButton(() =>
            {
                comment.Response = message;
                comment.MarkAsRead = read;
                HandleUpdateComment(comment);
            }, "Update reply");

            SetOutlineColor(comment.Objects, ChooseOutlineColor(comment.Type)); // we do this to make sure the color of the current comment is shown when a note is in multiple comments

            dialog.Open();

        }

        private void ShowReviewChooseUI(List<Comment> comments)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Choose a Comment");

            foreach (var comment in comments)
            {
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
            }

            dialog.AddFooterButton(null, "Close");

            dialog.Open();
        }

        private void ShowEditCommentUI(Comment comment, bool showAlreadyExistedMessage = false)
        {
            CommentTypesEnum type = comment.Type;
            string message = comment.Message;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Edit comment");
            if (showAlreadyExistedMessage)
            {
                dialog.AddComponent<TextComponent>()
                .WithInitialValue("A comment with that selection already existed!");
            }
            
            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())));

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>()
                .WithInitialValue(Convert.ToInt32(comment.Type))
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });

            dialog.AddFooterButton(() => { ShowReviewCommentUI(comment.Id); }, "Cancel");
            dialog.AddFooterButton(() =>
            {
                ShowDeleteCommentUI(comment);
            }, "Delete comment");
            dialog.AddFooterButton(() =>
            {
                comment.Message = message;
                comment.Type = type;
                comment.MarkAsRead = false;
                HandleUpdateComment(comment);
            }, "Save edit");

            dialog.Open();
        }

        private void ShowDeleteCommentUI(Comment comment)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Delete review file");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Are you sure you want to delete the comment?");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"This cannot be undone.");
            dialog.AddFooterButton(() => { ShowEditCommentUI(comment); }, "Cancel");
            dialog.AddFooterButton(() =>
            {
                HandleDeleteComment(comment.Id);
                dialog.Close();
            }, "Delete");

            SetOutlineColor(comment.Objects, ChooseOutlineColor(comment.Type)); // we do this to make sure the color of the current comment is shown when a note is in multiple comments

            dialog.Open();
        }

        #endregion UI

        #region Comment Handling

        private void HandleUpdateComment(Comment comment)
        {
            currentReview.Comments.Remove(currentReview.Comments.First(x => x.Id == comment.Id));
            currentReview.Comments.Add(comment);
            currentReview.Comments = currentReview.Comments.OrderBy(f => f.StartBeat).ToList();
            ShowReviewCommentUI(comment.Id);
            if (comment.MarkAsRead)
            {
                SetOutlineColor(comment.Objects, Color.gray);
            } else
            {
                SetOutlineColor(comment.Objects, ChooseOutlineColor(comment.Type));
            }
        }

        private void HandleCreateComment(CommentTypesEnum type, string message, List<SelectedObject> selectedNotes, bool redirect = false)
        {

            string id = Guid.NewGuid().ToString();
            Comment comment = new()
            {
                Id = id,
                StartBeat = selectedNotes.OrderBy(f => f.Beat).First().Beat,
                Objects = selectedNotes,
                Type = type,
                Message = message
            };
            
            currentReview.Comments.Add(comment);
            currentReview.Comments = currentReview.Comments.OrderBy(f => f.StartBeat).ToList();

            SetOutlineColor(selectedNotes, ChooseOutlineColor(type));

            if (redirect)
            {
                ShowReviewCommentUI(id);
            }
        }

        private void HandleDeleteComment(string commentId)
        {
            currentReview.Comments.Remove(currentReview.Comments.First(x => x.Id == commentId));
        }

        #endregion Comment Handling

        #region Outlines

        private void UpdateSelectionCache(BaseObject baseObject)
        {
            selectionCache.Add(baseObject);
        }

        private void ManageSelectionCacheAndOutlines()
        {
            foreach (var item in selectionCache.ToList())
            {
                if(!SelectionController.SelectedObjects.Contains(item))
                {
                    selectionCache.Remove(item);
                    SetOutlineIfInReview(item);
                }
            }
        }

        private void SetOutlineIfInReview(BaseObject baseObject)
        {
            if (!showOutlines)
            {
                return;
            }

            SelectedObject spawnedObject = null;

            if (baseObject is BaseNote note)
            {
                spawnedObject = new()
                {
                    Beat = note.SongBpmTime,
                    PosX = note.PosX,
                    PosY = note.PosY,
                    ObjectType = note.ObjectType,
                    Color = note.Color
                };
            }

            if (baseObject is BaseObstacle wall)
            {
                spawnedObject = new()
                {
                    Beat = wall.SongBpmTime,
                    PosX = wall.PosX,
                    PosY = wall.PosY,
                    ObjectType = wall.ObjectType,
                    Color = 0
                };
            }

            if (baseObject is BaseSlider slider)
            {
                spawnedObject = new()
                {
                    Beat = slider.SongBpmTime,
                    PosX = slider.PosX,
                    PosY = slider.PosY,
                    ObjectType = slider.ObjectType,
                    Color = slider.Color
                };
            }

            if (baseObject is BaseBpmEvent bpm)
            {
                spawnedObject = new()
                {
                    Beat = bpm.SongBpmTime,
                    PosX = 0,
                    PosY = 0,
                    ObjectType = bpm.ObjectType,
                    Color = 0
                };
            }

            try
            {
                if (currentReview.Comments.Any(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(spawnedObject))))
                {
                    Comment comment = currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(spawnedObject))).FirstOrDefault();
                    SelectedObject selectedObject = comment.Objects.Where(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(spawnedObject)).FirstOrDefault();

                    if (comment.MarkAsRead)
                    {
                        SetOutlineColor(selectedObject, Color.gray);
                    }
                    else
                    {
                        SetOutlineColor(selectedObject, ChooseOutlineColor(comment.Type));
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            
        }

        private Color ChooseOutlineColor(CommentTypesEnum type)
        {
            switch (type)
            {
                case CommentTypesEnum.Note:
                    return Color.blue;
                case CommentTypesEnum.Suggestion:
                    return Color.green;
                case CommentTypesEnum.Warning:
                    return Color.yellow;
                case CommentTypesEnum.Issue:
                    return Color.red;
                default:
                    return Color.clear;
            }
        }

        private void SetOutlineColor(SelectedObject mapObject, Color color)
        {
            try
            {
                var collection = BeatmapObjectContainerCollection.GetCollectionForType(mapObject.ObjectType);

                if (mapObject.ObjectType == ObjectType.Note)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseNote note)
                        {
                            if (note.SongBpmTime == mapObject.Beat && note.PosX == mapObject.PosX && note.PosY == mapObject.PosY && note.Color == mapObject.Color)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
                else if (mapObject.ObjectType == ObjectType.Obstacle)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseGrid gridItem)
                        {
                            if (gridItem.SongBpmTime == mapObject.Beat && gridItem.PosX == mapObject.PosX && gridItem.PosY == mapObject.PosY)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
                else if (mapObject.ObjectType == ObjectType.Arc || mapObject.ObjectType == ObjectType.Chain)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseSlider slider)
                        {
                            if (slider.SongBpmTime == mapObject.Beat && slider.PosX == mapObject.PosX && slider.PosY == mapObject.PosY && slider.Color == mapObject.Color)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
                else if (mapObject.ObjectType == ObjectType.BpmChange)
                {
                    var container = collection.LoadedContainers.Where((item) =>
                    {
                        if (item.Key is BaseBpmEvent bpmEvent)
                        {
                            if (bpmEvent.SongBpmTime == mapObject.Beat)
                            {
                                return true;
                            }
                        }
                        return false;
                    }).First().Value;
                    container.SetOutlineColor(color);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message != "Sequence contains no elements")
                {
                    throw;
                }
                // dont need to do anything, objects just not inside the loaded range.
            }
            
        }

        private void SetOutlineColor(List<SelectedObject> mapObjects, Color color)
        {
            foreach (var mapObject in mapObjects)
            {
                SetOutlineColor(mapObject, color);
            }
        }

        private void ClearOutlineColor(SelectedObject mapObject)
        {
            SetOutlineColor(mapObject, Color.clear);
        }

        private void ClearOutlineColor(List<SelectedObject> mapObjects)
        {
            foreach (var mapObject in mapObjects)
            {
                ClearOutlineColor(mapObject);
            }
        }

        #endregion Outlines

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
                Version = fileVersion,
                Comments = new()
            };

            Debug.Log($"Exporting a new review file:");
            Debug.Log(JsonConvert.SerializeObject(review, Formatting.Indented));

            currentReview = review;

            if (!Directory.Exists($"{_beatSaberSongContainer.Song.Directory}/reviews"))
            {
                Directory.CreateDirectory($"{_beatSaberSongContainer.Song.Directory}/reviews");
            }

            string newFilePath = $"{song.Directory}/reviews/{review.MapName} [{review.Difficulty} {review.DifficultyRank}] {review.ReviewType} {review.Author} {review.FinalizationDate.Day}-{review.FinalizationDate.Month}-{review.FinalizationDate.Year} {review.FinalizationDate.Hour}.{review.FinalizationDate.Minute}.{review.FinalizationDate.Second}.lreview";
            File.WriteAllText(newFilePath, JsonConvert.SerializeObject(review, Formatting.Indented));
            currentlyLoadedFilePath = newFilePath;
            _beatmapObjectContainerCollection.ObjectSpawnedEvent += SetOutlineIfInReview;
            _beatmapObjectContainerCollection.ContainerSpawnedEvent += SetOutlineIfInReview;
            _obstacleGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _eventGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _bpmChangeGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _arcGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _chainGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            SelectionController.ObjectWasSelectedEvent += UpdateSelectionCache;
            SelectionController.SelectionChangedEvent += ManageSelectionCacheAndOutlines;
            selectionCache = new();
        }

        private void SaveFile(bool overwrite)
        {
            var review = currentReview;
            review.FinalizationDate = DateTime.UtcNow;
            string newFilePath = $"{_beatSaberSongContainer.Song.Directory}/reviews/{review.MapName} [{review.Difficulty} {review.DifficultyRank}] {review.ReviewType} {review.Author} {review.FinalizationDate.Day}-{review.FinalizationDate.Month}-{review.FinalizationDate.Year} {review.FinalizationDate.Hour}.{review.FinalizationDate.Minute}.{review.FinalizationDate.Second}.lreview";
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
            List<string> files = Directory.GetFiles(_beatSaberSongContainer.Song.Directory + "/reviews", "*AUTOMATIC_BACKUP.lreview").ToList();

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
            File.WriteAllText($"{_beatSaberSongContainer.Song.Directory}/reviews/{review.MapName} [{review.Difficulty} {review.DifficultyRank}] {review.ReviewType} {review.Author} {review.FinalizationDate.Day}-{review.FinalizationDate.Month}-{review.FinalizationDate.Year} {review.FinalizationDate.Hour}.{review.FinalizationDate.Minute}.{review.FinalizationDate.Second} AUTOMATIC_BACKUP.lreview", JsonConvert.SerializeObject(review, Formatting.Indented));
        }

        private void RemoveFile(string path)
        {
            File.Delete(path);
            currentReview = null;
            currentlyLoadedFilePath = null;
        }

        #endregion File Handling

        #region Other

        public List<SelectedObject> GetSelectedObjectListFromSelection(HashSet<BaseObject> selection)
        {
            List<SelectedObject> selectedObjects = new List<SelectedObject>();

            foreach (var mapObj in selection)
            {
                if (mapObj is BaseNote note)
                {
                    selectedObjects.Add(new()
                    {
                        Beat = note.SongBpmTime,
                        PosX = note.PosX,
                        PosY = note.PosY,
                        ObjectType = note.ObjectType,
                        Color = note.Color
                    });
                }

                if (mapObj is BaseObstacle wall)
                {
                    selectedObjects.Add(new()
                    {
                        Beat = wall.SongBpmTime,
                        PosX = wall.PosX,
                        PosY = wall.PosY,
                        ObjectType = wall.ObjectType,
                        Color = 0
                    });
                }

                if (mapObj is BaseSlider slider)
                {
                    selectedObjects.Add(new()
                    {
                        Beat = slider.SongBpmTime,
                        PosX = slider.PosX,
                        PosY = slider.PosY,
                        ObjectType = slider.ObjectType,
                        Color = slider.Color
                    });
                }

                if (mapObj is BaseBpmEvent bpm)
                {
                    selectedObjects.Add(new()
                    {
                        Beat = bpm.SongBpmTime,
                        PosX = 0,
                        PosY = 0,
                        ObjectType = bpm.ObjectType,
                        Color = 0
                    });
                }
            }
            selectedObjects = selectedObjects.OrderBy(f => f.Beat).ToList();

            return selectedObjects;
        }

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

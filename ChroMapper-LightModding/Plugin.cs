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
using System.Collections;
using ChroMapper_LightModding.UI;

namespace ChroMapper_LightModding
{

    [Plugin("LightModding")]
    public class Plugin
    {
        static public int backupLimit = 3;
        public bool showOutlines = true;

        static public string fileVersion = "0.1.0";

        private BeatSaberSongContainer _beatSaberSongContainer = null!;
        private NoteGridContainer _noteGridContainer = null!;
        private ObstacleGridContainer _obstacleGridContainer = null!;
        private EventGridContainer _eventGridContainer = null!;
        private ArcGridContainer _arcGridContainer = null!;
        private ChainGridContainer _chainGridContainer = null!;
        private BPMChangeGridContainer _bpmChangeGridContainer = null!;
        private BeatmapObjectContainerCollection _beatmapObjectContainerCollection = null!;

        public BeatSaberSongContainer BeatSaberSongContainer { get => _beatSaberSongContainer; }
        public NoteGridContainer NoteGridContainer { get => _noteGridContainer; }
        public ObstacleGridContainer ObstacleGridContainer { get => _obstacleGridContainer; }
        public EventGridContainer EventGridContainer { get => _eventGridContainer; }
        public ArcGridContainer ArcGridContainer { get => _arcGridContainer; }
        public ChainGridContainer ChainGridContainer { get => _chainGridContainer; }
        public BPMChangeGridContainer BPMChangeGridContainer { get => _bpmChangeGridContainer; }
        public BeatmapObjectContainerCollection BeatmapObjectContainerCollection { get => _beatmapObjectContainerCollection; }


        private HashSet<BaseObject> selectionCache;

        private Scene currentScene;
        private bool inEditorScene;

        private bool subscribedToEvents = false;

        public DifficultyReview currentReview = null;
        public string currentlyLoadedFilePath = null;

        private Exporter exporter = new();
        private EditorUI editorUI;

        InputAction addCommentAction;
        InputAction openCommentAction;

        [Init]
        private void Init()
        {
            editorUI = new(this);
            SceneManager.sceneLoaded += SceneLoaded;

            // register a button in the side tab menu
            ExtensionButton button = ExtensionButtons.AddButton(LoadSprite("ChroMapper_LightModding.Assets.Icon.png"), "LightModding", editorUI.ShowMainUI);

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
            if (currentReview != null)
            {
                BackupFile();
            }
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
                    SubscribeToEvents();
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
                if (subscribedToEvents)
                {
                    UnsubscribeFromEvents();
                }
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
                        editorUI.ShowEditCommentUI(currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).First(), true);
                    } else
                    {
                        Debug.Log("Opening Comment Creation UI");
                        editorUI.ShowCreateCommentUI(selectedObjects);
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
                            editorUI.ShowReviewCommentUI(currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).First().Id);
                        } else
                        {
                            Debug.Log("Opening Review choice UI");
                            editorUI.ShowReviewChooseUI(currentReview.Comments.Where(c => JsonConvert.SerializeObject(c.Objects) == JsonConvert.SerializeObject(selectedObjects)).ToList());
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
                            editorUI.ShowReviewCommentUI(currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(selectedObjects[0]))).First().Id);
                        }
                        else
                        {
                            Debug.Log("Opening Review choice UI");
                            editorUI.ShowReviewChooseUI(currentReview.Comments.Where(c => c.Objects.Any(o => JsonConvert.SerializeObject(o) == JsonConvert.SerializeObject(selectedObjects[0]))).ToList());
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

        #region Comment Handling

        public void HandleUpdateComment(Comment comment)
        {
            currentReview.Comments.Remove(currentReview.Comments.First(x => x.Id == comment.Id));
            currentReview.Comments.Add(comment);
            currentReview.Comments = currentReview.Comments.OrderBy(f => f.StartBeat).ToList();
            editorUI.ShowReviewCommentUI(comment.Id);
            if (comment.MarkAsRead)
            {
                SetOutlineColor(comment.Objects, Color.gray);
            } else
            {
                SetOutlineColor(comment.Objects, ChooseOutlineColor(comment.Type));
            }
        }

        public void HandleCreateComment(CommentTypesEnum type, string message, List<SelectedObject> selectedNotes, bool redirect = false)
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
                editorUI.ShowReviewCommentUI(id);
            }
        }

        public void HandleDeleteComment(string commentId)
        {
            ClearOutlineColor(currentReview.Comments.First(x => x.Id == commentId).Objects);
            currentReview.Comments.Remove(currentReview.Comments.First(x => x.Id == commentId));
        }

        #endregion Comment Handling

        #region Outlines

        public void UpdateSelectionCache(BaseObject baseObject)
        {
            selectionCache.Add(baseObject);
        }

        public void ManageSelectionCacheAndOutlines()
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

        public void SetOutlineIfInReview(BaseObject baseObject)
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

        public Color ChooseOutlineColor(CommentTypesEnum type)
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

        public void SetOutlineColor(SelectedObject mapObject, Color color)
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

        public void SetOutlineColor(List<SelectedObject> mapObjects, Color color)
        {
            foreach (var mapObject in mapObjects)
            {
                SetOutlineColor(mapObject, color);
            }
        }

        public void ClearOutlineColor(SelectedObject mapObject)
        {
            SetOutlineColor(mapObject, Color.clear);
        }

        public void ClearOutlineColor(List<SelectedObject> mapObjects)
        {
            foreach (var mapObject in mapObjects)
            {
                ClearOutlineColor(mapObject);
            }
        }

        #endregion Outlines

        #region File Handling

        public void HandleCreateFile(string title, string author, ReviewTypeEnum type)
        {
            var song = _beatSaberSongContainer.Song;
            var difficultyData = _beatSaberSongContainer.DifficultyData;

            DifficultyReview review = new()
            {
                Title = title,
                Author = author,
                OverallComment = "",
                MapName = song.SongName,
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
            SubscribeToEvents();
            selectionCache = new();
        }

        public void SaveFile(bool overwrite)
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

        public void BackupFile()
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

        public void RemoveFile(string path)
        {
            File.Delete(path);
            currentReview = null;
            currentlyLoadedFilePath = null;
        }

        #endregion File Handling

        #region Other

        private void SubscribeToEvents()
        {
            _beatmapObjectContainerCollection.ContainerSpawnedEvent += SetOutlineIfInReview;
            _obstacleGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _eventGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _bpmChangeGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _arcGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            _chainGridContainer.ContainerSpawnedEvent += SetOutlineIfInReview;
            SelectionController.ObjectWasSelectedEvent += UpdateSelectionCache;
            SelectionController.SelectionChangedEvent += ManageSelectionCacheAndOutlines;
            subscribedToEvents = true;
        }

        private void UnsubscribeFromEvents()
        {
            _beatmapObjectContainerCollection.ContainerSpawnedEvent -= SetOutlineIfInReview;
            _obstacleGridContainer.ContainerSpawnedEvent -= SetOutlineIfInReview;
            _eventGridContainer.ContainerSpawnedEvent -= SetOutlineIfInReview;
            _bpmChangeGridContainer.ContainerSpawnedEvent -= SetOutlineIfInReview;
            _arcGridContainer.ContainerSpawnedEvent -= SetOutlineIfInReview;
            _chainGridContainer.ContainerSpawnedEvent -= SetOutlineIfInReview;
            SelectionController.ObjectWasSelectedEvent -= UpdateSelectionCache;
            SelectionController.SelectionChangedEvent -= ManageSelectionCacheAndOutlines;
            subscribedToEvents = false;
        }

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

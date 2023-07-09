using System;
using System.Collections.Generic;
using System.Linq;
using ChroMapper_LightModding.Models;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using Beatmap.Base;
using UnityEngine.InputSystem;
using Color = UnityEngine.Color;
using ChroMapper_LightModding.UI;
using ChroMapper_LightModding.Helpers;
using static UnityEngine.InputSystem.InputRemoting;
using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.BeatmapScanner;
using UnityEngine.UI;
using System.Windows.Input;
using System.Configuration;
using System.Xml.Linq;
using System.Runtime.Remoting.Messaging;

namespace ChroMapper_LightModding
{

    [Plugin("AutoModder")]
    public class Plugin
    {
        static public int backupLimit = 3;
        public bool showOutlines = true;

        static public string fileVersion = "0.1.2";

        private BeatSaberSongContainer _beatSaberSongContainer = null!;
        private NoteGridContainer _noteGridContainer = null!;
        private ObstacleGridContainer _obstacleGridContainer = null!;
        private EventGridContainer _eventGridContainer = null!;
        private ArcGridContainer _arcGridContainer = null!;
        private ChainGridContainer _chainGridContainer = null!;
        private BPMChangeGridContainer _bpmChangeGridContainer = null!;
        private BeatmapObjectContainerCollection _beatmapObjectContainerCollection = null!;
        private AudioTimeSyncController _audioTimeSyncController = null!;

        public BeatSaberSongContainer BeatSaberSongContainer { get => _beatSaberSongContainer; }
        public NoteGridContainer NoteGridContainer { get => _noteGridContainer; }
        public ObstacleGridContainer ObstacleGridContainer { get => _obstacleGridContainer; }
        public EventGridContainer EventGridContainer { get => _eventGridContainer; }
        public ArcGridContainer ArcGridContainer { get => _arcGridContainer; }
        public ChainGridContainer ChainGridContainer { get => _chainGridContainer; }
        public BPMChangeGridContainer BPMChangeGridContainer { get => _bpmChangeGridContainer; }
        public BeatmapObjectContainerCollection BeatmapObjectContainerCollection { get => _beatmapObjectContainerCollection; }
        public AudioTimeSyncController AudoTimeSyncController { get => _audioTimeSyncController; }

        private bool subscribedToEvents = false;
        private bool hasLoadedIntoEditor = false;

        public DifficultyReview currentReview { get => MapsetDifficultyReviewLoader(); set => MapsetDifficultyReviewUpdater(value); }
        public MapsetReview currentMapsetReview = null;
        public string currentlyLoadedFilePath = null;
        public static Configs.Configs configs = new();

        private EditorUI editorUI;
        private SongInfoUI songInfoUI;
        private OutlineHelper outlineHelper;
        private FileHelper fileHelper;
        private Exporter exporter;
        private AutocheckHelper autocheckHelper;
        private CriteriaCheck criteriaCheck;
        public GridMarkerHelper gridMarkerHelper;

        InputAction addCommentAction;
        InputAction openCommentAction;
        InputAction quickMarkUnsureAction;
        InputAction quickMarkIssueAction;

        public Action CommentsUpdated;


        [Init]
        private void Init()
        {
            exporter = new();
            criteriaCheck = new(this);
            fileHelper = new(this);
            autocheckHelper = new(this, criteriaCheck, fileHelper);
            outlineHelper = new(this);
            editorUI = new(this, outlineHelper, fileHelper, exporter, autocheckHelper);
            songInfoUI = new(this, fileHelper, exporter, autocheckHelper);

            SceneManager.sceneLoaded += SceneLoaded;

            // config
            var path = AppDomain.CurrentDomain.BaseDirectory + "/Plugins/AutoModderConf.json";
            if(File.Exists(path))
            {
                TextReader reader = null;
                try
                {
                    reader = new StreamReader(path);
                    var fileContents = reader.ReadToEnd();
                    configs = JsonConvert.DeserializeObject<Configs.Configs>(fileContents);
                    using StreamWriter file = File.CreateText(@path);
                    JsonSerializer serializer = new();
                    serializer.Serialize(file, configs);
                }
                catch // Error during reading, use default instead
                {
                    configs = new();
                }
                finally
                {
                    reader?.Close();
                }
            }
            else
            {
                try
                {
                    using StreamWriter file = File.CreateText(@path);
                    JsonSerializer serializer = new();
                    serializer.Serialize(file, configs);
                }
                catch // Error during writing, use default instead
                {
                    configs = new();
                }
            }

            // register a button in the side tab menu
            ExtensionButton button = ExtensionButtons.AddButton(LoadSprite("ChroMapper_LightModding.Assets.Icon.png"), "AutoModder", editorUI.ShowMainUI);

            // registering keybinds
            addCommentAction = new InputAction("Add Comment", type: InputActionType.Button);
            addCommentAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("modifier", "<Keyboard>/ctrl")
                .With("button", "<Keyboard>/e");
            addCommentAction.AddCompositeBinding("ButtonWithOneModifier") // keeping this assigned for a bit so people arent confused
                .With("modifier", "<Keyboard>/ctrl")
                .With("button", "<Keyboard>/g");
            addCommentAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("modifier", "<Keyboard>/ctrl")
                .With("button", "<Keyboard>/space");
            addCommentAction.performed += _ => { AddCommentKeyEvent(); };

            openCommentAction = new InputAction("Open Comment", type: InputActionType.Button);
            openCommentAction.AddCompositeBinding("ButtonWithOneModifier")
                .With("modifier", "<Keyboard>/alt")
                .With("button", "<Keyboard>/e");
            openCommentAction.AddCompositeBinding("ButtonWithOneModifier") // keeping this assigned for a bit so people arent confused
                .With("modifier", "<Keyboard>/alt")
                .With("button", "<Keyboard>/g");
            openCommentAction.performed += _ => { OpenCommentKeyEvent(); };

            quickMarkUnsureAction = new InputAction("Quick mark unsure", type: InputActionType.Button);
            quickMarkUnsureAction.AddBinding("<Keyboard>/f9");
            quickMarkUnsureAction.performed += _ => { QuickMarkUnsureEvent(); };

            quickMarkIssueAction = new InputAction("Quick mark issue", type: InputActionType.Button);
            quickMarkIssueAction.AddBinding("<Keyboard>/f10");
            quickMarkIssueAction.performed += _ => { QuickMarkIssueEvent(); };

        }

        [Exit]
        private void Exit()
        {
            if (currentMapsetReview != null)
            {
                fileHelper.MapsetReviewBackupSaver();
            }
        }

        #region Event Handlers

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 3 || (scene.buildIndex == 4 && hasLoadedIntoEditor)) // the editor scene OR settings scene during editor scene
            {
                LoadedIntoSongEditor();
            }
            else if (scene.buildIndex == 2) // song info screen
            {
                LoadedIntoSongInfo();
            }
            else
            {
                if (currentMapsetReview != null)
                {
                    fileHelper.MapsetReviewBackupSaver();
                }

                songInfoUI.Disable();
                currentlyLoadedFilePath = null;
                currentMapsetReview = null;
                ResetAfterLeavingEditor();
            }
        }

        public void LoadedIntoSongEditor()
        {
            if (hasLoadedIntoEditor) return; // apparently we were running this every time we exited from the settings menu

            songInfoUI.Disable();
            hasLoadedIntoEditor = true;
            addCommentAction.Enable();
            openCommentAction.Enable();
            quickMarkUnsureAction.Enable();
            quickMarkIssueAction.Enable();

            _noteGridContainer = UnityEngine.Object.FindObjectOfType<NoteGridContainer>();
            _obstacleGridContainer = UnityEngine.Object.FindObjectOfType<ObstacleGridContainer>();
            _eventGridContainer = UnityEngine.Object.FindObjectOfType<EventGridContainer>();
            _bpmChangeGridContainer = UnityEngine.Object.FindObjectOfType<BPMChangeGridContainer>();
            _beatSaberSongContainer = UnityEngine.Object.FindObjectOfType<BeatSaberSongContainer>();
            _beatmapObjectContainerCollection = UnityEngine.Object.FindObjectOfType<BeatmapObjectContainerCollection>();
            _arcGridContainer = UnityEngine.Object.FindObjectOfType<ArcGridContainer>();
            _chainGridContainer = UnityEngine.Object.FindObjectOfType<ChainGridContainer>();
            _audioTimeSyncController = UnityEngine.Object.FindObjectOfType<AudioTimeSyncController>();

            if (currentReview != null)
            {
                SubscribeToEditorEvents();
                outlineHelper.selectionCache = new();
                gridMarkerHelper = new(this);

                MapEditorUI mapEditorUI = UnityEngine.Object.FindObjectOfType<MapEditorUI>();
                editorUI.Enable(mapEditorUI.transform.Find("Timeline Canvas").transform.Find("Song Timeline"), mapEditorUI.transform.Find("Pause Menu Canvas").transform.Find("Extras Menu"), mapEditorUI.transform.Find("Right Bar Canvas"));
            }
        }

        public void LoadedIntoSongInfo()
        {
            _beatSaberSongContainer = UnityEngine.Object.FindObjectOfType<BeatSaberSongContainer>();
            
            GameObject songInfoPanel = GameObject.Find("SongInfoPanel");
            GameObject difficultyPanel = GameObject.Find("DifficultyPanel");
            songInfoUI.Enable(songInfoPanel.transform.Find("Header"), songInfoPanel.transform.Find("Save"), difficultyPanel.transform.Find("Save"));

            ResetAfterLeavingEditor();
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

        public void QuickMarkUnsureEvent()
        {
            if (currentReview == null) { Debug.Log("Comment Creation not executed, no file loaded."); return; }
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
                    }
                    else
                    {
                        Debug.Log("Quick Creating comment of type Unsure");
                        HandleCreateComment(CommentTypesEnum.Unsure, "", selectedObjects);
                    }

                }
                else
                {
                    Debug.Log("Comment Creation cancelled, no supported objects selected.");
                }
            }
            else
            {
                Debug.Log("Comment Creation not executed, selection is empty.");
            }

            SelectionController.DeselectAll();
        }

        public void QuickMarkIssueEvent()
        {
            if (currentReview == null) { Debug.Log("Comment Creation not executed, no file loaded."); return; }
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
                    }
                    else
                    {
                        Debug.Log("Quick Creating comment of type Issue");
                        HandleCreateComment(CommentTypesEnum.Issue, "", selectedObjects);
                    }

                }
                else
                {
                    Debug.Log("Comment Creation cancelled, no supported objects selected.");
                }
            }
            else
            {
                Debug.Log("Comment Creation not executed, selection is empty.");
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
            if (comment.MarkAsSuppressed)
            {
                outlineHelper.SetOutlineColor(comment.Objects, Color.gray);
            } else
            {
                outlineHelper.SetOutlineColor(comment.Objects, outlineHelper.ChooseOutlineColor(comment.Type));
            }
            editorUI.RefreshTimelineMarkers();
            CommentsUpdated.Invoke();
        }

        public string HandleCreateComment(CommentTypesEnum type, string message, List<SelectedObject> selectedNotes)
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

            outlineHelper.SetOutlineColor(selectedNotes, outlineHelper.ChooseOutlineColor(type));
            editorUI.RefreshTimelineMarkers();
            CommentsUpdated.Invoke();
            return id;
        }

        public void HandleDeleteComment(string commentId)
        {
            outlineHelper.ClearOutlineColor(currentReview.Comments.First(x => x.Id == commentId).Objects);
            currentReview.Comments.Remove(currentReview.Comments.First(x => x.Id == commentId));
            editorUI.RefreshTimelineMarkers();
            CommentsUpdated.Invoke();
        }
        

        public void HandleUpdateSongInfoComment(Comment comment)
        {
            currentMapsetReview.Comments.Remove(currentMapsetReview.Comments.First(x => x.Id == comment.Id));
            currentMapsetReview.Comments.Add(comment);
            currentMapsetReview.Comments = currentMapsetReview.Comments.OrderBy(f => f.StartBeat).ToList();
            songInfoUI.ShowReviewCommentUI(comment.Id);
        }

        #endregion Comment Handling

        #region Helper functions that belong here i swear
        /// <summary>
        /// Function to dynamically use the legacy DifficultyReview variable
        /// </summary>
        public DifficultyReview MapsetDifficultyReviewLoader()
        {
            if (currentMapsetReview == null)
            {
                return null;
            }

            var difficultyData = _beatSaberSongContainer.DifficultyData;
            try
            {
                return currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyRank == difficultyData.DifficultyRank && x.DifficultyCharacteristic == difficultyData.ParentBeatmapSet.BeatmapCharacteristicName).First();
            }
            catch (InvalidOperationException)
            {
                currentMapsetReview.DifficultyReviews.Add(new DifficultyReview
                {
                    DifficultyCharacteristic = difficultyData.ParentBeatmapSet.BeatmapCharacteristicName,
                    Difficulty = difficultyData.Difficulty,
                    DifficultyRank = difficultyData.DifficultyRank,
                });
                currentMapsetReview.DifficultyReviews = currentMapsetReview.DifficultyReviews.OrderByDescending(x => x.DifficultyRank).ToList();

                return currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyRank == difficultyData.DifficultyRank).First();
            }

        }

        /// <summary>
        /// Function to dynamically use the legacy DifficultyReview variable
        /// </summary>
        public void MapsetDifficultyReviewUpdater(DifficultyReview difficultyReview)
        {
            if (difficultyReview == null)
            {
                return;
            }

            var difficultyData = _beatSaberSongContainer.DifficultyData;
            DifficultyReview reviewToUpdate = currentMapsetReview.DifficultyReviews.FirstOrDefault(x => x.DifficultyRank == difficultyData.DifficultyRank && x.DifficultyCharacteristic == difficultyData.ParentBeatmapSet.BeatmapCharacteristicName);

            if (reviewToUpdate != null)
            {
                reviewToUpdate = difficultyReview;
            }
        }
        #endregion

        #region Other
        private void ResetAfterLeavingEditor()
        {
            editorUI.Disable();
            hasLoadedIntoEditor = false;
            addCommentAction.Disable();
            openCommentAction.Disable();
            quickMarkUnsureAction.Disable();
            quickMarkIssueAction.Disable();
            outlineHelper.selectionCache = null;
            if (gridMarkerHelper != null) 
            {
                gridMarkerHelper.Dispose();
            }
            if (subscribedToEvents)
            {
                UnsubscribeFromEditorEvents();
            }
        }
        
        public void SubscribeToEditorEvents()
        {
            _beatmapObjectContainerCollection.ContainerSpawnedEvent += outlineHelper.SetOutlineIfInReview;
            _obstacleGridContainer.ContainerSpawnedEvent += outlineHelper.SetOutlineIfInReview;
            _eventGridContainer.ContainerSpawnedEvent += outlineHelper.SetOutlineIfInReview;
            _bpmChangeGridContainer.ContainerSpawnedEvent += outlineHelper.SetOutlineIfInReview;
            _arcGridContainer.ContainerSpawnedEvent += outlineHelper.SetOutlineIfInReview;
            _chainGridContainer.ContainerSpawnedEvent += outlineHelper.SetOutlineIfInReview;
            SelectionController.ObjectWasSelectedEvent += outlineHelper.UpdateSelectionCache;
            SelectionController.SelectionChangedEvent += outlineHelper.ManageSelectionCacheAndOutlines;
            _audioTimeSyncController.TimeChanged += editorUI.CheckBeatForComment;
            CommentsUpdated += editorUI.CheckBeatForComment;
            subscribedToEvents = true;
        }

        public void UnsubscribeFromEditorEvents()
        {
            _beatmapObjectContainerCollection.ContainerSpawnedEvent -= outlineHelper.SetOutlineIfInReview;
            _obstacleGridContainer.ContainerSpawnedEvent -= outlineHelper.SetOutlineIfInReview;
            _eventGridContainer.ContainerSpawnedEvent -= outlineHelper.SetOutlineIfInReview;
            _bpmChangeGridContainer.ContainerSpawnedEvent -= outlineHelper.SetOutlineIfInReview;
            _arcGridContainer.ContainerSpawnedEvent -= outlineHelper.SetOutlineIfInReview;
            _chainGridContainer.ContainerSpawnedEvent -= outlineHelper.SetOutlineIfInReview;
            SelectionController.ObjectWasSelectedEvent -= outlineHelper.UpdateSelectionCache;
            SelectionController.SelectionChangedEvent -= outlineHelper.ManageSelectionCacheAndOutlines;
            _audioTimeSyncController.TimeChanged -= editorUI.CheckBeatForComment;
            CommentsUpdated -= editorUI.CheckBeatForComment;
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
                        Beat = note.JsonTime,
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
                        Beat = wall.JsonTime,
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
                        Beat = slider.JsonTime,
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
                        Beat = bpm.JsonTime,
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

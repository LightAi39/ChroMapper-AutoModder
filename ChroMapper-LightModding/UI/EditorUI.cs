using beatleader_parser.Timescale;
using Beatmap.Base;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using Parser.Map.Difficulty.V3.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using Object = UnityEngine.Object;

namespace ChroMapper_LightModding.UI
{
    internal class EditorUI
    {
        private Plugin plugin;
        private Exporter exporter;
        private OutlineHelper outlineHelper;
        private FileHelper fileHelper;
        private AutocheckHelper autocheckHelper;

        private GameObject _timelineMarkers;
        private GameObject _criteriaMenu;
        private GameObject _timingComparesMenu;
        private GameObject _settingMenu;
        // Vertical offset of the menus shown in the pause menu (criteria, settings, mod, timing), the ratings follow the criteria menu
        private const float PauseMenuOffsetY = 20;
        private GameObject _modMenu;
        private GameObject _ratingsMenu;
        private GameObject _commentMenu;
        private GameObject _commentSelectMenu;

        private List<CharacteristicInfo> timingCharInfos;
        private UIDropdown timingCharDropdown;
        private UIDropdown timingDiffDropdown;

        private string currentCommentMenuId;
        private string currentLoadedMod = null;

        private Transform _songTimeline;
        private Transform _pauseMenu;
        private Transform _rightBar;
        public bool enabled = false;

        private bool showTimelineMarkers = true;
        private bool showGridMarkers = true;

        private (double stars, double pass, double tech, double acc, double ebpm, double pebpm, double sps, string handness, double duration) stats;

        public EditorUI(Plugin plugin, OutlineHelper outlineHelper, FileHelper fileHelper, Exporter exporter, AutocheckHelper autocheckHelper)
        {
            this.plugin = plugin;
            this.outlineHelper = outlineHelper;
            this.fileHelper = fileHelper;
            this.exporter = exporter;
            this.autocheckHelper = autocheckHelper;
        }

        #region CMUI
        public void ShowMainUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Automodder");

            dialog.AddFooterButton(null, "Close");

            if (plugin.currentReview == null)
            {
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"No review file loaded!");
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Load a review file in song info to get started.");
            }
            else
            {
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Review file loaded!");

                dialog.AddComponent<ButtonComponent>()
                .WithLabel("Copy comments to clipboard")
                    .OnClick(() => { exporter.ExportToBeatLeaderComment(plugin.currentReview); });

                dialog.AddComponent<ButtonComponent>()
                    .WithLabel("Show all Comments")
                    .OnClick(ShowAllCommentsMainUI);

                dialog.AddComponent<ButtonComponent>()
                    .WithLabel("Edit overall comment")
                    .OnClick(EditFileInformationUI);

                dialog.AddComponent<ToggleComponent>()
                    .WithLabel("Show outlines")
                    .WithInitialValue(plugin.showOutlines)
                    .OnChanged((bool o) => {
                        if (o != plugin.showOutlines)
                        {
                            plugin.showOutlines = o;
                            outlineHelper.RefreshOutlines();
                        }
                    });
                dialog.AddComponent<ToggleComponent>()
                    .WithLabel("Show timeline markers")
                    .WithInitialValue(showTimelineMarkers)
                    .OnChanged((bool o) => {
                        if (o != showTimelineMarkers)
                        {
                            showTimelineMarkers = o;
                            ToggleTimelineMarkers();
                        }
                    });
                dialog.AddComponent<ToggleComponent>()
                    .WithLabel("Show grid markers")
                    .WithInitialValue(showGridMarkers)
                    .OnChanged((bool o) => {
                        if (o != showGridMarkers)
                        {
                            showGridMarkers = o;
                            ToggleGridMarkers();
                        }
                    });

                dialog.AddFooterButton(ShowSaveFileUI, "Save review file");
            }

            dialog.Open();
        }

        public void EditFileInformationUI()
        {
            string overallComment = plugin.currentReview.OverallComment;
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Edit file information");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Overall comment:")
                .WithInitialValue(overallComment)
                .OnChanged((string s) => { overallComment = s; });

            dialog.AddFooterButton(null, "Close");
            dialog.AddFooterButton(() =>
            {
                plugin.currentReview.OverallComment = overallComment;
            }, "Save Changes");

            dialog.Open();
        }

        public void ShowAllCommentsMainUI()
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("All Comments");
            List<Comment> comments = plugin.currentReview.Comments.Take(5).ToList();

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsSuppressed)
                {
                    read = " - Marked As Solved";
                }
                string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
                if (beats.Length > 64)
                {
                    beats = beats.Substring(0, 61);
                    beats += "...";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Beats: {beats} | {comment.Type} - {comment.Message}{read}")
                    .OnClick(() => { plugin.AudioTimeSyncController.MoveToJsonTime(comment.StartBeat); });
            }

            if (plugin.currentReview.Comments.Count == 0)
            {
                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"No comments found!");
            }

            dialog.AddFooterButton(ShowAllCommentsMainUI, "<-");
            dialog.AddFooterButton(null, "Close");
            if (plugin.currentReview.Comments.Count > 5)
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllCommentsMoreUI(5);
                }, "->");
            }
            else
            {
                dialog.AddFooterButton(ShowAllCommentsMainUI, "->");
            }


            dialog.Open();
        }

        public void ShowAllCommentsMoreUI(int startIndex)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("All Comments");
            int count = 5;
            bool lastTab = false;
            if (plugin.currentReview.Comments.Count < startIndex + count)
            {
                count = plugin.currentReview.Comments.Count - startIndex;
                lastTab = true;
            }
            List<Comment> comments = plugin.currentReview.Comments.GetRange(startIndex, count).ToList();

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsSuppressed)
                {
                    read = " - Marked As Solved";
                }
                string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
                if (beats.Length > 64)
                {
                    beats = beats.Substring(0, 61);
                    beats += "...";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Beats: {beats} | {comment.Type} - {comment.Message}{read}")
                    .OnClick(() => { plugin.AudioTimeSyncController.MoveToJsonTime(comment.StartBeat); });
            }

            if (startIndex == 5)
            {
                dialog.AddFooterButton(ShowAllCommentsMainUI, "<-");
            }
            else
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllCommentsMoreUI(startIndex - 5);
                }, "<-");
            }

            dialog.AddFooterButton(null, "Close");
            if (lastTab)
            {
                dialog.AddFooterButton(() => ShowAllCommentsMoreUI(startIndex), "->");
            }
            else
            {
                dialog.AddFooterButton(() =>
                {
                    ShowAllCommentsMoreUI(startIndex + 5);
                }, "->");
            }

            dialog.Open();
        }

        public void ShowSaveFileUI()
        {
            bool overwrite = true;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Save review file");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Do you want to overwrite the current file?");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Overwriting cannot be undone.");

            dialog.AddComponent<ToggleComponent>()
                .WithLabel("Overwrite?")
                .WithInitialValue(overwrite)
                .OnChanged((bool o) => { overwrite = o; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() =>
            {
                fileHelper.MapsetReviewSaver(overwrite);
                dialog.Close();
            }, "Save");
            dialog.Open();
        }

        public static CommentTypesEnum lastSelectedType = CommentTypesEnum.Suggestion;

        public void ShowCreateCommentUI(List<SelectedObject> selectedObjects)
        {
            CommentTypesEnum type = CommentTypesEnum.Suggestion;
            string message = "Comment";

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Add comment");

            string beats = string.Join(", ", selectedObjects.ConvertAll(p => p.ToString()));
            if (beats.Length > 64)
            {
                beats = beats.Substring(0, 61);
                beats += "...";
            }

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Beats: {beats}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            var dropdown = dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions(Enum.GetValues(typeof(CommentTypesEnum)).Cast<CommentTypesEnum>().Where(t => t != CommentTypesEnum.Data).Select(t => Exporter.CommentTypeName(t)).ToList())
                .OnChanged((int i) => { type = lastSelectedType = (CommentTypesEnum)i; });

            dropdown.Value = (int)lastSelectedType;

			dialog.AddFooterButton(null, "Cancel");
			dialog.AddFooterButton(() => { plugin.HandleCreateComment(type, message, selectedObjects); }, "Create");
			if (plugin.HasCommentClipboard)
			{
				dialog.AddFooterButton(() => { plugin.PasteClipboardToSelection(selectedObjects); dialog.Close(); }, "Paste");
			}

            dialog.Open();
        }

        public void ShowReviewCommentUI(string id)
        {
            Comment comment = plugin.currentReview.Comments.Where(x => x.Id == id).First();
            string message = comment.Response;
            bool read = comment.MarkAsSuppressed;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("View comment");

            string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
            if (beats.Length > 64)
            {
                beats = beats.Substring(0, 61);
                beats += "...";
            }

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Beats: {beats}");

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Type: {comment.Type}");

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Comment: {comment.Message}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Response:")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            dialog.AddComponent<ToggleComponent>()
                .WithLabel("Mark as Solved")
                .WithInitialValue(read)
                .OnChanged((bool o) => { read = o; });

            dialog.AddComponent<ButtonComponent>()
                    .WithLabel("Go to beat")
                    .OnClick(() => { plugin.AudioTimeSyncController.MoveToJsonTime(comment.StartBeat); });

            dialog.AddFooterButton(null, "Close");
            dialog.AddFooterButton(() =>
            {
                comment.Response = message;
                comment.MarkAsSuppressed = read;
                ShowEditCommentUI(comment);
            }, "Edit comment");
            dialog.AddFooterButton(() =>
            {
                comment.Response = message;
                comment.MarkAsSuppressed = read;
                plugin.HandleUpdateComment(comment);
            }, "Update reply");

            outlineHelper.SetOutlineColor(comment.Objects, outlineHelper.ChooseOutlineColor(comment.Type)); // we do this to make sure the color of the current comment is shown when a note is in multiple comments

            dialog.Open();
        }

        public void ShowReviewChooseUI(List<Comment> comments)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Choose a Comment");

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsSuppressed)
                {
                    read = " - Marked As Solved";
                }
                string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
                if (beats.Length > 64)
                {
                    beats = beats.Substring(0, 61);
                    beats += "...";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Beats: {beats} | {comment.Type} - {comment.Message}{read}")
                    .OnClick(() => { plugin.AudioTimeSyncController.MoveToJsonTime(comment.StartBeat); });
            }

            dialog.AddFooterButton(null, "Close");

            dialog.Open();
        }

        public void ShowEditCommentUI(Comment comment, bool showAlreadyExistedMessage = false)
        {
            CommentTypesEnum type = comment.Type;
            string message = comment.Message;

            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Edit comment");
            if (showAlreadyExistedMessage)
            {
                dialog.AddComponent<TextComponent>()
                .WithInitialValue("A comment with that selection already exists!");
            }

            string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
            if (beats.Length > 64)
            {
                beats = beats.Substring(0, 61);
                beats += "...";
            }

            dialog.AddComponent<TextComponent>()
                .WithInitialValue($"Beats: {beats}");

            dialog.AddComponent<TextBoxComponent>()
                .WithLabel("Comment")
                .WithInitialValue(message)
                .OnChanged((string s) => { message = s; });

            var typeSelector = dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions(Enum.GetValues(typeof(CommentTypesEnum)).Cast<CommentTypesEnum>().Where(t => t != CommentTypesEnum.Data).Select(t => Exporter.CommentTypeName(t)).ToList())
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });
            typeSelector.Value = (int)comment.Type;

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() =>
            {
                ShowDeleteCommentUI(comment);
            }, "Delete comment");
			dialog.AddFooterButton(() =>
			{
				comment.Message = message;
				comment.Type = type;
				comment.MarkAsSuppressed = false;
				plugin.HandleUpdateComment(comment);
			}, "Save edit");

            dialog.Open();
        }

        public void ShowDeleteCommentUI(Comment comment)
        {
            DialogBox dialog = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Delete review file");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Are you sure you want to delete the comment?");
            dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"This cannot be undone.");
            dialog.AddFooterButton(() => { ShowEditCommentUI(comment); }, "Cancel");
            dialog.AddFooterButton(() =>
            {
                plugin.HandleDeleteComment(comment.Id);
                dialog.Close();
            }, "Delete");

            outlineHelper.SetOutlineColor(comment.Objects, outlineHelper.ChooseOutlineColor(comment.Type)); // we do this to make sure the color of the current comment is shown when a note is in multiple comments

            dialog.Open();
        }
        #endregion

        #region New UI
        public void Enable(Transform songTimeline, Transform pauseMenu, Transform rightBar)
        {
            if (enabled) { return; }
            enabled = true;
            _songTimeline = songTimeline;
            _pauseMenu = pauseMenu;
            _rightBar = rightBar;
            if (plugin.currentReview != null) RunBeatmapScannerOnThisDiff();
            CreateTimelineMarkers();
            CreateCriteriaMenu();
            if (showGridMarkers) plugin.gridMarkerHelper = new(plugin);
        }

        public void Disable()
        {
            if (!enabled) { return; }
            enabled = false;
            _songTimeline = null;
            _pauseMenu = null;
        }

        public void ToggleTimelineMarkers(bool destroyIfExists = true)
        {
            GameObject timelineMarkers = GameObject.Find("Automodder Timeline Markers");
            if (timelineMarkers != null)
            {
                if (destroyIfExists) RemoveTimelineMarkers();
            }
            else
            {
                CreateTimelineMarkers();
            }
        }

        public void RefreshTimelineMarkers()
        {
            RemoveTimelineMarkers();
            CreateTimelineMarkers();
        }

        private void RemoveTimelineMarkers()
        {
            GameObject timelineMarkers = GameObject.Find("Automodder Timeline Markers");
            Object.Destroy(timelineMarkers);
        }

        private void CreateTimelineMarkers()
        {
            if (!showTimelineMarkers || plugin.currentReview == null) return;
            AddTimelineMarkers(_songTimeline);
            _timelineMarkers.SetActive(true);
        }

        public void AddTimelineMarkers(Transform parent)
        {
            _timelineMarkers = new GameObject("Automodder Timeline Markers");
            _timelineMarkers.transform.parent = parent;
            _timelineMarkers.SetActive(false);

            RectTransform timelineCanvas = parent.parent.GetComponent<RectTransform>();

            float width = timelineCanvas.sizeDelta.x - 20f;

            UIHelper.AttachTransform(_timelineMarkers, width, 22, 0.5f, 0.9f, 0, 0, 0.5f, 1);

            //Image image = _timelineMarkers.AddComponent<Image>();
            //image.sprite = PersistentUI.Instance.Sprites.Background;
            //image.type = Image.Type.Sliced;
            //image.color = new Color(0.35f, 0.35f, 0.35f);

            var bpmChanges = plugin.BPMChangeGridContainer.MapObjects.ToList();
            if (bpmChanges.Count == 0) // apparently on intial load we are getting no bpm changes, so doing this for now to try and get them from the saved file anyway
            {
                var diff = plugin.BeatSaberSongContainer.Info.DifficultySets
                    .FirstOrDefault(x => x.Characteristic == plugin.currentReview.DifficultyCharacteristic)
                    .Difficulties
                    .FirstOrDefault(y => y.Difficulty == plugin.currentReview.Difficulty && y.DifficultyRank == plugin.currentReview.DifficultyRank);
                BaseDifficulty baseDifficulty = BeatSaberSongUtils.GetMapFromInfoFiles(plugin.BeatSaberSongContainer.Info, plugin.BeatSaberSongContainer.MapDifficultyInfo);
                bpmChanges = baseDifficulty.BpmEvents;
            }

            List<BpmEvent> bpmChangesChecker = new();
            foreach (var bpmChange in bpmChanges)
            {
                BpmEvent bpmevent = new BpmEvent
                {
                    Beats = bpmChange.JsonTime,
                    Bpm = bpmChange.Bpm
                };
                bpmChangesChecker.Add(bpmevent);
            }

            Timescale timescale = Timescale.Create(BeatSaberSongContainer.Instance.Info.BeatsPerMinute, bpmChangesChecker, BeatSaberSongContainer.Instance.Info.SongTimeOffset);

            var totalBeats = timescale.ToBeatTime(plugin.BeatSaberSongContainer.LoadedSongLength);

            var comments = plugin.currentReview.Comments.ToList();
            comments.Sort((x, y) => y.Type.CompareTo(x.Type));

            foreach (var comment in plugin.currentReview.Comments)
            {
                double cmbeat = timescale.ToBeatTime(timescale.ToRealTime(comment.StartBeat));
                Color color = Color.gray;
                if (!comment.MarkAsSuppressed)
                {
                    color = outlineHelper.ChooseOutlineColor(comment.Type);
                }

                float position = (float)(cmbeat / totalBeats * width - (width / 2));
                UIHelper.AddLabel(_timelineMarkers.transform, $"CommentMarker-{comment.Id}", "|", new Vector2(position, -14), new Vector2(0, 0), null, color);
            }
        }

        public void RefreshCriteriaMenu(bool configMenu = false)
        {
            RemoveCriteriaMenu();
            CreateCriteriaMenu();
            if (configMenu)
            {
                _criteriaMenu.SetActive(false);
                _settingMenu.SetActive(true);
            }
        }

        private void RemoveCriteriaMenu()
        {
            Object.Destroy(_criteriaMenu);
            Object.Destroy(_ratingsMenu);
        }

        private void CreateCriteriaMenu()
        {
            if (plugin.currentReview == null) return;
            AddCriteriaMenu(_pauseMenu);
            _criteriaMenu.SetActive(true);
            AddTimingComparesMenu(_pauseMenu);
            AddSettingMenu(_pauseMenu);
            AddModMenu(_pauseMenu);
            AddRatingsMenu(_criteriaMenu.transform);
            _ratingsMenu.SetActive(true);
        }

        public void AddCriteriaMenu(Transform parent)
        {
            _criteriaMenu = new GameObject("Automodder Criteria Menu");
            _criteriaMenu.transform.parent = parent;
            _criteriaMenu.SetActive(false);

            UIHelper.AttachTransform(_criteriaMenu, 572, 215, 0.05f, 1.20f, 0, PauseMenuOffsetY, 0, 1);

            Image image = _criteriaMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            #region Top left buttons
            UIHelper.AddButton(_criteriaMenu.transform, "SaveAMFile", "Save File", new Vector2(-250, -18), () =>
            {
                fileHelper.MapsetReviewSaver();
            });

            UIHelper.AddButton(_criteriaMenu.transform, "RunAutoCheck", "Auto Check", new Vector2(-188, -18), () =>
            {
                RunAutoCheckOnThisDiff();
                RefreshCriteriaMenu();
                outlineHelper.RefreshOutlines();
                RefreshTimelineMarkers();
            });

            UIHelper.AddButton(_criteriaMenu.transform, "RunBeatmapScanner", "Refresh Map Analytics", new Vector2(-126, -18), () =>
            {
                RunBeatmapScannerOnThisDiff();
                RefreshCriteriaMenu();
            });
            UIHelper.AddLabel(_criteriaMenu.transform, "FileSaveWarning", "Save the map before using these buttons!", new Vector2(-30, -18), new Vector2(100, 24), TextAlignmentOptions.Left);
            #endregion

            #region Clear comments button
            UIHelper.AddButton(_criteriaMenu.transform, "ClearAllComments", "Clear All Comments", new Vector2(64, -18), () =>
            {
                plugin.currentReview.Comments.Clear();
                plugin.CommentsUpdated.Invoke();
                outlineHelper.RefreshOutlines();
                RefreshTimelineMarkers();
            });
            #endregion

            #region Mod button
            UIHelper.AddButton(_criteriaMenu.transform, "ImportMod", "Import Mod", new Vector2(126, -18), () =>
            {
                if (_modMenu != null)
                {
                    _modMenu.SetActive(true);
                    _criteriaMenu.SetActive(false);
                }
            });
            #endregion

            #region Timings button
            UIHelper.AddButton(_criteriaMenu.transform, "CompareTimings", "Compare Timings", new Vector2(188, -18), () =>
            {
                if (_timingComparesMenu != null)
                {
                    _timingComparesMenu.SetActive(true);
                    _criteriaMenu.SetActive(false);
                }
            });
            #endregion

            #region Settings button

            UIHelper.AddButton(_criteriaMenu.transform, "OpenSettingsMenu", "Settings", new Vector2(250, -18), () =>
            {
                if (_settingMenu != null)
                {
                    _settingMenu.SetActive(true);
                    _criteriaMenu.SetActive(false);
                }
            });

            #endregion

            #region Criteria
            DiffCrit criteria = plugin.currentReview.Critera;
            float startPosY = -42, posY, offsetX = -80;
            string name;

            // ugly
            #region please collapse this
            posY = startPosY;
            name = "Hot Start";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.HotStart, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.HotStart = IncrementSeverity(criteria.HotStart);
                posY = startPosY;
                offsetX = -80;
                name = "Hot Start";
                CreateCriteriaStatusElement(criteria.HotStart, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26;
            name = "Cold End";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.ColdEnd, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.ColdEnd = IncrementSeverity(criteria.ColdEnd);
                posY = startPosY - 26;
                offsetX = -80;
                name = "Cold End";
                CreateCriteriaStatusElement(criteria.ColdEnd, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 2;
            name = "Min. Song Duration";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.MinSongDuration, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.MinSongDuration = IncrementSeverity(criteria.MinSongDuration);
                posY = startPosY - 26 * 2;
                offsetX = -80;
                name = "Min. Song Duration";
                CreateCriteriaStatusElement(criteria.MinSongDuration, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 3;
            name = "Outside Of Map";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Outside, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Outside = IncrementSeverity(criteria.Outside);
                posY = startPosY - 26 * 3;
                offsetX = -80;
                name = "Outside Of Map";
                CreateCriteriaStatusElement(criteria.Outside, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 4;
            name = "Prolonged Swing";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.ProlongedSwing, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.ProlongedSwing = IncrementSeverity(criteria.ProlongedSwing);
                posY = startPosY - 26 * 4;
                offsetX = -80;
                name = "Prolonged Swing";
                CreateCriteriaStatusElement(criteria.ProlongedSwing, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 5;
            name = "Vision Block";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.VisionBlock, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.VisionBlock = IncrementSeverity(criteria.VisionBlock);
                posY = startPosY - 26 * 5;
                offsetX = -80;
                name = "Vision Block";
                CreateCriteriaStatusElement(criteria.VisionBlock, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 6;
            name = "Parity";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Parity, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Parity = IncrementSeverity(criteria.Parity);
                posY = startPosY - 26 * 6;
                offsetX = -80;
                name = "Parity";
                CreateCriteriaStatusElement(criteria.Parity, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            // next column
            offsetX = 110;
            posY = startPosY;
            name = "Chain Issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Chain, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Chain = IncrementSeverity(criteria.Chain);
                posY = startPosY;
                offsetX = 110;
                name = "Chain Issues";
                CreateCriteriaStatusElement(criteria.Chain, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26;
            name = "Fused Object";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.FusedObject, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.FusedObject = IncrementSeverity(criteria.FusedObject);
                posY = startPosY - 26;
                offsetX = 110;
                name = "Fused Object";
                CreateCriteriaStatusElement(criteria.FusedObject, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 2;
            name = "Hand Clap";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.HandClap, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.HandClap = IncrementSeverity(criteria.HandClap);
                posY = startPosY - 26 * 2;
                offsetX = 110;
                name = "Hand Clap";
                CreateCriteriaStatusElement(criteria.HandClap, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 3;
            name = "Swing Path Issue";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.SwingPath, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.SwingPath = IncrementSeverity(criteria.SwingPath);
                posY = startPosY - 26 * 3;
                offsetX = 110;
                name = "Swing Path Issue";
                CreateCriteriaStatusElement(criteria.SwingPath, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 4;
            name = "Hitbox Issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Hitbox, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Hitbox = IncrementSeverity(criteria.Hitbox);
                posY = startPosY - 26 * 4;
                offsetX = 110;
                name = "Hitbox Issues";
                CreateCriteriaStatusElement(criteria.Hitbox, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 5;
            name = "Requirements";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Requirement, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Requirement = IncrementSeverity(criteria.Requirement);
                posY = startPosY - 26 * 5;
                offsetX = 110;
                name = "Requirements";
                CreateCriteriaStatusElement(criteria.Requirement, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            // next column
            offsetX = 300;
            posY = startPosY;
            name = "Slider Issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Slider, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Slider = IncrementSeverity(criteria.Slider);
                posY = startPosY;
                offsetX = 300;
                name = "Slider Issues";
                CreateCriteriaStatusElement(criteria.Slider, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26;
            name = "Wall Issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Wall, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Wall = IncrementSeverity(criteria.Wall);
                posY = startPosY - 26;
                offsetX = 300;
                name = "Wall Issues";
                CreateCriteriaStatusElement(criteria.Wall, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 2;
            name = "Insufficient Lighting";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Light, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Light = IncrementSeverity(criteria.Light);
                posY = startPosY - 26 * 2;
                offsetX = 300;
                name = "Insufficient Lighting";
                CreateCriteriaStatusElement(criteria.Light, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 3;
            name = "Difficulty Label Size";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.DifficultyLabelSize, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.DifficultyLabelSize = IncrementSeverity(criteria.DifficultyLabelSize);
                posY = startPosY - 26 * 3;
                offsetX = 300;
                name = "Difficulty Label Size";
                CreateCriteriaStatusElement(criteria.DifficultyLabelSize, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 4;
            name = "Obscene Content";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.DifficultyName, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.DifficultyName = IncrementSeverity(criteria.DifficultyName);
                posY = startPosY - 26 * 4;
                offsetX = 300;
                name = "Obscene Content";
                CreateCriteriaStatusElement(criteria.DifficultyName, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 5;
            name = "NJS";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.NJS, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.NJS = IncrementSeverity(criteria.NJS);
                posY = startPosY - 26 * 5;
                offsetX = 300;
                name = "NJS";
                CreateCriteriaStatusElement(criteria.NJS, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 6;
            name = "Bomb Issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Bomb, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Bomb = IncrementSeverity(criteria.Bomb);
                posY = startPosY - 26 * 6;
                offsetX = 300;
                name = "Bomb Issues";
                CreateCriteriaStatusElement(criteria.Bomb, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            #endregion
            #endregion

        }

        public void AddModMenu(Transform parent)
        {
            _modMenu = new GameObject("Automodder Mod Menu");
            _modMenu.transform.parent = parent;
            _modMenu.SetActive(false);

            UIHelper.AttachTransform(_modMenu, 572, 215, 0.05f, 1.20f, 0, PauseMenuOffsetY, 0, 1);

            Image image = _modMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            #region Settings button
            UIHelper.AddButton(_modMenu.transform, "ImportMod", "Import Mod", new Vector2(250, -18), () =>
            {
                if (currentLoadedMod != null && currentLoadedMod.Length > 0)
                {
                    List<string> mod = currentLoadedMod.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
                    ImportModOnThisDiff(mod);
                    outlineHelper.RefreshOutlines();
                    RefreshTimelineMarkers();
                }
            });

            UIHelper.AddTextInput(_modMenu.transform, "ModTextbox", "", new Vector2(-60, -107), "Paste mod here", (change) =>
            {
                currentLoadedMod = change;
            }, 500, 200);

            UIHelper.AddButton(_modMenu.transform, "CloseSettingsMenu", "Close Menu", new Vector2(250, -44), () =>
            {
                _criteriaMenu.SetActive(true);
                _modMenu.SetActive(false);
            });

            #endregion
        }

        internal class CharacteristicInfo
        {
            public string Name { get; set; }
            public List<string> Difficulties { get; set; } = new();

            public CharacteristicInfo(string characteristic, List<string> difficulties)
            {
                Name = characteristic;
                Difficulties = difficulties;
            }
        }

        public void AddTimingComparesMenu(Transform parent)
        {
            _timingComparesMenu = new GameObject("Automodder Timing Compares Menu");
            _timingComparesMenu.transform.parent = parent;
            _timingComparesMenu.SetActive(false);

            UIHelper.AttachTransform(_timingComparesMenu, 110, 85, 0.05f, 1.20f, 15, PauseMenuOffsetY, 0, 1);

            Image image = _timingComparesMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            List<CharacteristicInfo> charInfos = new();
            List<string> characteristics = new();
            foreach (var difficultySet in plugin.BeatSaberSongContainer.Info.DifficultySets)
            {
                if (difficultySet.Characteristic == "Lightshow") continue;

                List<string> difficulties = new();
                foreach (var difficulty in difficultySet.Difficulties)
                {
                    difficulties.Add(difficulty.Difficulty);
                }
                if (difficulties.Count > 0)
                {
                    difficulties.Reverse();
                    charInfos.Add(new CharacteristicInfo(difficultySet.Characteristic, difficulties));
                    characteristics.Add(difficultySet.Characteristic);
                }
            }
            timingCharInfos = charInfos;

            UnityAction<int> dropdownCharChanged = OnTimingCharacteristicChanged;

            #region Dropbox
            timingCharDropdown = UIHelper.AddDropdown(_timingComparesMenu.transform, "Characteristic", "Characteristic", new Vector2(1f, -16), characteristics, dropdownCharChanged);
            timingDiffDropdown = UIHelper.AddDropdown(_timingComparesMenu.transform, "Difficulty", "Difficulty", new Vector2(1f, -44), charInfos[0].Difficulties);
            #endregion

            #region Button
            UIHelper.AddButton(_timingComparesMenu.transform, "ApplyCompare", "Compare", new Vector2(1f, -70), () =>
            {
                string characteristic = timingCharDropdown.Dropdown.options[timingCharDropdown.Dropdown.value].text;
                string difficulty = timingDiffDropdown.Dropdown.options[timingDiffDropdown.Dropdown.value].text;
                CompareTimingsOnThisDiff(characteristic, difficulty);
                outlineHelper.RefreshOutlines();
                RefreshTimelineMarkers();
                _criteriaMenu.SetActive(true);
                _timingComparesMenu.SetActive(false);
            });
            #endregion
        }

        public void OnTimingCharacteristicChanged(int index)
        {
            string characteristic = timingCharDropdown.Dropdown.options[index].text;
            CharacteristicInfo info = timingCharInfos.Where(x => x.Name == characteristic).FirstOrDefault();
            timingDiffDropdown.SetOptions(info.Difficulties);
            timingDiffDropdown.Dropdown.SetValueWithoutNotify(0);
        }

        public void AddSettingMenu(Transform parent)
        {
            _settingMenu = new GameObject("Automodder Setting Menu");
            _settingMenu.transform.parent = parent;
            _settingMenu.SetActive(false);

            UIHelper.AttachTransform(_settingMenu, 572, 215, 0.05f, 1.20f, 0, PauseMenuOffsetY, 0, 1);

            Image image = _settingMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            #region Settings button
            UIHelper.AddButton(_settingMenu.transform, "SaveSettings", "Save To File", new Vector2(250, -18), () =>
            {
                Plugin.HandleConfigFile(true); 
            });

            UIHelper.AddButton(_settingMenu.transform, "ResetSettings", "Reset To Default", new Vector2(250, -44), () =>
            {
                Plugin.checkerConfig = new();
                RefreshCriteriaMenu(true);
            });

            UIHelper.AddButton(_settingMenu.transform, "CloseSettingsMenu", "Close Menu", new Vector2(250, -70), () =>
            {
                _criteriaMenu.SetActive(true);
                _settingMenu.SetActive(false);
            });

            #endregion

            // Settings are split in pages, selected with the buttons under Close Menu
            var pages = new List<GameObject>();
            GameObject AddPage(string name, string text, float buttonY)
            {
                var page = new GameObject("Automodder Setting Page " + name);
                page.transform.parent = _settingMenu.transform;
                UIHelper.AttachTransform(page, 572, 215, 0.5f, 0.5f, 0, 0);
                pages.Add(page);
                UIHelper.AddButton(_settingMenu.transform, "SettingPage" + name, text, new Vector2(250, buttonY), () =>
                {
                    foreach (var p in pages) p.SetActive(p == page);
                });
                page.SetActive(pages.Count == 1);
                return page;
            }
            void AddValue(GameObject page, string name, string text, float x, float y, double value, Action<double> set)
            {
                UIHelper.AddTextInput(page.transform, name, text, new Vector2(x, y), value.ToString(), (change) =>
                {
                    if (Double.TryParse(change, out double result)) set(result);
                });
            }
            // Centered over its column, which goes from the label (x - 52.5) to the end of the input (x + 55)
            void AddHeader(GameObject page, string name, string text, float x, float y)
            {
                UIHelper.AddLabel(page.transform, name, text, new Vector2(x + 1.25f, y), new Vector2(180, 24), TextAlignmentOptions.Center);
            }

            #region Options
            var options = AddPage("Options", "Options", -104);
            float startPosX = -220, startPosY = -35;
            UIHelper.AddCheckbox(options.transform, "HighlightOffbeat", "Display Offbeat", new Vector2(startPosX, startPosY), Plugin.checkerConfig.HighlightOffbeat, (check) =>
            {
                Plugin.checkerConfig.HighlightOffbeat = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "HighlightInline", "Display Inline", new Vector2(startPosX, startPosY), Plugin.checkerConfig.HighlightInline, (check) =>
            {
                Plugin.checkerConfig.HighlightInline = check;
            });
            AddValue(options, "InlineBeatPrecision", "1 / ", startPosX + 20, startPosY + 5, Plugin.checkerConfig.InlineBeatPrecision, v => Plugin.checkerConfig.InlineBeatPrecision = v);
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "DisplayFlick", "Display  Flick", new Vector2(startPosX, startPosY), Plugin.checkerConfig.DisplayFlick, (check) =>
            {
                Plugin.checkerConfig.DisplayFlick = check;
            });
            AddValue(options, "FlickBeatPrecision", "1 / ", startPosX + 20, startPosY + 5, Plugin.checkerConfig.FlickBeatPrecision, v => Plugin.checkerConfig.FlickBeatPrecision = v);
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "DisplayShrado", "Display  Shrado", new Vector2(startPosX, startPosY), Plugin.checkerConfig.DisplayShrado, (check) =>
            {
                Plugin.checkerConfig.DisplayShrado = check;
            });
            AddValue(options, "ShradoMaxBeat", "", startPosX + 20, startPosY + 5, Plugin.checkerConfig.ShradoMaxBeat, v => Plugin.checkerConfig.ShradoMaxBeat = v);
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "ChainConsistency", "Display  Chain", new Vector2(startPosX, startPosY), Plugin.checkerConfig.ChainConsistency, (check) =>
            {
                Plugin.checkerConfig.ChainConsistency = check;
            });
            AddValue(options, "ChainPrecision", "1 / ", startPosX + 20, startPosY + 5, Plugin.checkerConfig.ChainPrecision, v => Plugin.checkerConfig.ChainPrecision = v);
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "DisplayAngleOffset", "Angle   Offset", new Vector2(startPosX, startPosY), Plugin.checkerConfig.DisplayAngleOffset, (check) =>
            {
                Plugin.checkerConfig.DisplayAngleOffset = check;
            });
            UIHelper.AddCheckbox(options.transform, "AngleOffsetIgnore45", "Ignore 45 Offset", new Vector2(startPosX + 60, startPosY), Plugin.checkerConfig.AngleOffsetIgnore45, (check) =>
            {
                Plugin.checkerConfig.AngleOffsetIgnore45 = check;
            });

            startPosX = -80;
            startPosY = -35;
            var autoSliderPrecision = UIHelper.AddCheckbox(options.transform, "AutomaticSliderPrecision", "Auto Slider Precision", new Vector2(startPosX, startPosY), Plugin.checkerConfig.AutomaticSliderPrecision, (check) =>
            {
                Plugin.checkerConfig.AutomaticSliderPrecision = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "UseMapRT", "Use Map RT For VB", new Vector2(startPosX, startPosY), Plugin.checkerConfig.UseMapRT, (check) =>
            {
                Plugin.checkerConfig.UseMapRT = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "ParityInvertedWarning", "Parity Warning", new Vector2(startPosX, startPosY), Plugin.checkerConfig.ParityInvertedWarning, (check) =>
            {
                Plugin.checkerConfig.ParityInvertedWarning = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "ParityDebug", "Parity Debug", new Vector2(startPosX, startPosY), Plugin.checkerConfig.ParityDebug, (check) =>
            {
                Plugin.checkerConfig.ParityDebug = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "DetectModdedObjects", "Detect ME/NE", new Vector2(startPosX, startPosY), Plugin.checkerConfig.DetectModdedObjects, (check) =>
            {
                Plugin.checkerConfig.DetectModdedObjects = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(options.transform, "DetectSongBpm", "Detect Song BPM", new Vector2(startPosX, startPosY), Plugin.checkerConfig.DetectSongBpm, (check) =>
            {
                Plugin.checkerConfig.DetectSongBpm = check;
            });
            #endregion

            #region Values
            var values = AddPage("Values", "Values", -130);
            startPosX = -220;
            startPosY = -30;
            AddHeader(values, "VBSettings", "Allowed Vision Block (s)", startPosX, startPosY);
            startPosY -= 26;
            AddValue(values, "VBMinBottomNoteTime", "Bottom Row Note", startPosX, startPosY, Plugin.checkerConfig.VBMinBottomNoteTime, v => Plugin.checkerConfig.VBMinBottomNoteTime = v);
            startPosY -= 26;
            AddValue(values, "VBMaxOuterNoteTime", "Outer Lane Note", startPosX, startPosY, Plugin.checkerConfig.VBMaxOuterNoteTime, v => Plugin.checkerConfig.VBMaxOuterNoteTime = v);
            startPosY -= 26;
            AddValue(values, "VBMaxBombTime", "Outer Lane Bomb", startPosX, startPosY, Plugin.checkerConfig.VBMaxBombTime, v => Plugin.checkerConfig.VBMaxBombTime = v);
            startPosY -= 26;
            AddValue(values, "VBMinBombTime", "Bomb Further Than", startPosX, startPosY, Plugin.checkerConfig.VBMinBombTime, v => Plugin.checkerConfig.VBMinBombTime = v);
            startPosY -= 26;
            AddValue(values, "VBMinimum", "Minimum", startPosX, startPosY, Plugin.checkerConfig.VBMinimum, v => Plugin.checkerConfig.VBMinimum = v);

            startPosX = -110;
            startPosY = -30;
            AddHeader(values, "MapSettings", "Map", startPosX, startPosY);
            startPosY -= 26;
            UIHelper.AddTextInput(values.transform, "SliderPrecision", "Avg Slider Precision", new Vector2(startPosX, startPosY), Plugin.checkerConfig.SliderPrecision.ToString(), (change) =>
            {
                // A manual value replaces the detected one until automatic is enabled again
                if (Double.TryParse(change, out double result))
                {
                    Plugin.checkerConfig.SliderPrecision = result;
                    autoSliderPrecision.isOn = false;
                }
            });
            startPosY -= 26;
            AddValue(values, "HotStartDuration", "Hot Start Duration", startPosX, startPosY, Plugin.checkerConfig.HotStartDuration, v => Plugin.checkerConfig.HotStartDuration = v);
            startPosY -= 26;
            AddValue(values, "ColdEndDuration", "Cold End Duration", startPosX, startPosY, Plugin.checkerConfig.ColdEndDuration, v => Plugin.checkerConfig.ColdEndDuration = v);
            startPosY -= 26;
            AddValue(values, "MinSongDuration", "Min Song Duration", startPosX, startPosY, Plugin.checkerConfig.MinSongDuration, v => Plugin.checkerConfig.MinSongDuration = v);
            startPosY -= 26;
            AddValue(values, "FusedDistance", "Fused Distance", startPosX, startPosY, Plugin.checkerConfig.FusedDistance, v => Plugin.checkerConfig.FusedDistance = v);
            startPosY -= 26;
            AddValue(values, "MaxChar", "Max Name Length", startPosX, startPosY, Plugin.checkerConfig.MaxChar, v => Plugin.checkerConfig.MaxChar = (int)v);

            startPosX = 0;
            startPosY = -30;
            AddHeader(values, "ChainSettings", "Chains & NJS", startPosX, startPosY);
            startPosY -= 26;
            AddValue(values, "MaxChainRotation", "Max Chain Rotation", startPosX, startPosY, Plugin.checkerConfig.MaxChainRotation, v => Plugin.checkerConfig.MaxChainRotation = v);
            startPosY -= 26;
            AddValue(values, "ChainLinkVsAir", "Chain Link vs. Air", startPosX, startPosY, Plugin.checkerConfig.ChainLinkVsAir, v => Plugin.checkerConfig.ChainLinkVsAir = v);
            startPosY -= 26;
            AddValue(values, "VNJSHittableRange", "NJS Hittable Range", startPosX, startPosY, Plugin.checkerConfig.VNJSHittableRange, v => Plugin.checkerConfig.VNJSHittableRange = v);
            startPosY -= 26;
            AddValue(values, "VNJSFarDistance", "NJS Far Distance", startPosX, startPosY, Plugin.checkerConfig.VNJSFarDistance, v => Plugin.checkerConfig.VNJSFarDistance = v);
            startPosY -= 26;
            AddValue(values, "ParityWarningAngle", "Parity Warn Angle", startPosX, startPosY, Plugin.checkerConfig.ParityWarningAngle, v => Plugin.checkerConfig.ParityWarningAngle = v);
            #endregion

            #region Walls and lights
            var walls = AddPage("WallsLights", "Walls/Lights", -156);
            startPosX = -220;
            startPosY = -30;
            AddHeader(walls, "WallSettings", "Walls (s)", startPosX, startPosY);
            startPosY -= 26;
            AddValue(walls, "MinimumWallDuration", "Min Wall Duration", startPosX, startPosY, Plugin.checkerConfig.MinimumWallDuration, v => Plugin.checkerConfig.MinimumWallDuration = v);
            startPosY -= 26;
            AddValue(walls, "ShortWallTrailDuration", "Wall Trail Duration", startPosX, startPosY, Plugin.checkerConfig.ShortWallTrailDuration, v => Plugin.checkerConfig.ShortWallTrailDuration = v);
            startPosY -= 26;
            AddValue(walls, "ShortWallAnyTrailDuration", "Any Wall Trail", startPosX, startPosY, Plugin.checkerConfig.ShortWallAnyTrailDuration, v => Plugin.checkerConfig.ShortWallAnyTrailDuration = v);
            startPosY -= 26;
            AddValue(walls, "WallMinimumAvoidTime", "Wall Avoid Time", startPosX, startPosY, Plugin.checkerConfig.WallMinimumAvoidTime, v => Plugin.checkerConfig.WallMinimumAvoidTime = v);
            startPosY -= 26;
            AddValue(walls, "WallExceptionalAvoidTime", "Exceptional Avoid", startPosX, startPosY, Plugin.checkerConfig.WallExceptionalAvoidTime, v => Plugin.checkerConfig.WallExceptionalAvoidTime = v);
            startPosY -= 26;
            AddValue(walls, "SubjectiveDodgeWallPerSecond", "Soft Dodge Wall/s", startPosX, startPosY, Plugin.checkerConfig.SubjectiveDodgeWallPerSecond, v => Plugin.checkerConfig.SubjectiveDodgeWallPerSecond = v);

            startPosX = -110;
            startPosY = -30;
            AddHeader(walls, "LightSettings", "Lights & Bombs", startPosX, startPosY);
            startPosY -= 26;
            AddValue(walls, "AverageLightPerBeat", "Avg Light Per Beat", startPosX, startPosY, Plugin.checkerConfig.AverageLightPerBeat, v => Plugin.checkerConfig.AverageLightPerBeat = v);
            startPosY -= 26;
            AddValue(walls, "LightFadeDuration", "Light Fade Duration", startPosX, startPosY, Plugin.checkerConfig.LightFadeDuration, v => Plugin.checkerConfig.LightFadeDuration = v);
            startPosY -= 26;
            AddValue(walls, "LightBombReactionTime", "Light Bomb Allowed RT", startPosX, startPosY, Plugin.checkerConfig.LightBombReactionTime, v => Plugin.checkerConfig.LightBombReactionTime = v);
            startPosY -= 26;
            AddValue(walls, "LightBombMinimumBrightness", "Bomb Min Brightness", startPosX, startPosY, Plugin.checkerConfig.LightBombMinimumBrightness, v => Plugin.checkerConfig.LightBombMinimumBrightness = v);
            startPosY -= 26;
            AddValue(walls, "LightBombOffGrace", "Light Off Grace", startPosX, startPosY, Plugin.checkerConfig.LightBombOffGrace, v => Plugin.checkerConfig.LightBombOffGrace = v);
            startPosY -= 26;
            AddValue(walls, "BombSwingPathBeats", "Bomb Swing Path", startPosX, startPosY, Plugin.checkerConfig.BombSwingPathBeats, v => Plugin.checkerConfig.BombSwingPathBeats = v);

            startPosX = 0;
            startPosY = -30;
            AddHeader(walls, "CrouchSettings", "Crouch Walls", startPosX, startPosY);
            startPosY -= 26;
            AddValue(walls, "CrouchWallVisionBlockBeats", "Crouch VB Beats", startPosX, startPosY, Plugin.checkerConfig.CrouchWallVisionBlockBeats, v => Plugin.checkerConfig.CrouchWallVisionBlockBeats = v);
            #endregion
        }

        public void AddRatingsMenu(Transform parent)
        {
            _ratingsMenu = new GameObject("Automodder Ratings Menu");
            _ratingsMenu.transform.parent = parent;
            _ratingsMenu.SetActive(false);

            UIHelper.AttachTransform(_ratingsMenu, 400, 50, 0f, 0f, 0, 0, 0, 1);

            Image image = _ratingsMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            UIHelper.AddLabel(_ratingsMenu.transform, "BeatmapScannerValues", $"Stars: {stats.stars}☆ | Pass: {stats.pass}☆ | Tech: {stats.tech}☆ | Acc: {stats.acc}☆ | eBPM: {stats.ebpm} | Peak eBPM: {stats.pebpm}", new Vector2(0, -12), new Vector2(392, 24), TextAlignmentOptions.Left);
            UIHelper.AddLabel(_ratingsMenu.transform, "BeatmapScannerValues2", $"Swings Per Second: {stats.sps} | Left/Right Handness: {stats.handness}% | 1/16: {stats.duration}ms", new Vector2(0, -36), new Vector2(392, 24), TextAlignmentOptions.Left);
        }

        public void RefreshCommentMenu(Comment comment)
        {
            RemoveCommentMenu();
            CreateCommentMenu(comment);
        }

        private void RemoveCommentMenu()
        {
            Object.Destroy(_commentMenu);
            currentCommentMenuId = null;
        }

        private void CreateCommentMenu(Comment comment)
        {
            if (plugin.currentReview == null) return;
            AddCommentMenu(_rightBar, comment);
            _commentMenu.SetActive(true);
        }

        private void OpenCommentMenuFromSelectionMenu(Comment comment)
        {
            Object.Destroy(_commentSelectMenu);
            CreateCommentMenu(comment);
        }

        public void AddCommentMenu(Transform parent, Comment comment)
        {
            currentCommentMenuId = comment.Id;
            _commentMenu = new GameObject("Automodder Comment Menu");
            _commentMenu.transform.parent = parent;
            _commentMenu.SetActive(false);

            UIHelper.AttachTransform(_commentMenu, 325, 200, 1, 1, 0, 0, 1, 1);

            Image image = _commentMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
            if (beats.Length > 64)
            {
                beats = beats.Substring(0, 61);
                beats += "...";
            }

            UIHelper.AddLabel(_commentMenu.transform, "Beats", $"Beats: {beats}", new Vector2(0, -14), new Vector2(313, 24), TextAlignmentOptions.Left);

            UIHelper.AddLabel(_commentMenu.transform, "Type", $"Type: {comment.Type}", new Vector2(0, -38), new Vector2(313, 24), TextAlignmentOptions.Left);

            UIHelper.AddLabel(_commentMenu.transform, "Comment", $"Comment: {comment.Message}", new Vector2(0, -62), new Vector2(313, 24), TextAlignmentOptions.Left);

            if (comment.Response != "") { UIHelper.AddLabel(_commentMenu.transform, "Response", $"Response: {comment.Response}", new Vector2(0, -108), new Vector2(313, 24), TextAlignmentOptions.Left); }
            else UIHelper.AddLabel(_commentMenu.transform, "Response", $"No Response", new Vector2(0, -108), new Vector2(313, 24), TextAlignmentOptions.Left);

            if (comment.MarkAsSuppressed) UIHelper.AddLabel(_commentMenu.transform, "Solved", $"Marked as Solved", new Vector2(0, -159), new Vector2(313, 24), TextAlignmentOptions.Right);
            if (comment.IsAutogenerated) UIHelper.AddLabel(_commentMenu.transform, "Autogen", $"Marked as Autogenerated", new Vector2(0, -184), new Vector2(313, 24), TextAlignmentOptions.Right);

            UIHelper.AddButton(_commentMenu.transform, "OpenComment", "Open Comment", new Vector2(-128.5f, -159), () =>
            {
                ShowReviewCommentUI(comment.Id);
            });

            UIHelper.AddButton(_commentMenu.transform, "EditComment", "Edit Comment", new Vector2(-66.5f, -159), () =>
            {
                ShowEditCommentUI(comment);
            });

            UIHelper.AddButton(_commentMenu.transform, "SuppressComment", "Toggle Solved", new Vector2(-4.5f, -159), () =>
            {
                comment.MarkAsSuppressed = !comment.MarkAsSuppressed;
                RefreshCommentMenu(comment);
            });

            // Second row

			UIHelper.AddButton(_commentMenu.transform, "CopyComment", "Copy", new Vector2(-128.5f, -184), () =>
			{
				plugin.SetCommentClipboard(comment.Type, comment.Message);
			});

            UIHelper.AddButton(_commentMenu.transform, "DeleteComment", "Quick Delete", new Vector2(-66.5f, -184), () =>
            {
                plugin.HandleDeleteComment(comment.Id);
            });

            UIHelper.AddButton(_commentMenu.transform, "AutogenComment", "Toggle Autogen", new Vector2(-4.5f, -184), () =>
            {
                comment.IsAutogenerated = !comment.IsAutogenerated;
                RefreshCommentMenu(comment);
            });
        }

        public void RefreshCommentSelectMenu(List<Comment> comments)
        {
            RemoveCommentSelectMenu();
            CreateCommentSelectMenu(comments);
        }

        private void RemoveCommentSelectMenu()
        {
            Object.Destroy(_commentSelectMenu);
        }

        private void CreateCommentSelectMenu(List<Comment> comments)
        {
            if (plugin.currentReview == null) return;
            AddCommentSelectMenu(_rightBar, comments);
            _commentSelectMenu.SetActive(true);
        }

        public void AddCommentSelectMenu(Transform parent, List<Comment> comments)
        {
            _commentSelectMenu = new GameObject("Automodder Comment Select Menu");
            _commentSelectMenu.transform.parent = parent;
            _commentSelectMenu.SetActive(false);

            UIHelper.AttachTransform(_commentSelectMenu, 325, 175, 1, 1, 0, 0, 1, 1);

            Image image = _commentSelectMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            float height = -16f;

            foreach (var comment in comments)
            {
                string read = "";
                if (comment.MarkAsSuppressed)
                {
                    read = " - Marked As Solved";
                }
                string beats = string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()));
                if (beats.Length > 64)
                {
                    beats = beats.Substring(0, 61);
                    beats += "...";
                }
                UIHelper.AddButton(_commentSelectMenu.transform, $"OpenComment-{comment.Id}", $"Beats: " + beats + $" | {comment.Type} - {comment.Message}{read}", new Vector2(0, height), () =>
                {
                    OpenCommentMenuFromSelectionMenu(comment);
                }, 290, 24);
                height -= 26f;
            }

        }

        #endregion

        public void RunBeatmapScannerOnThisDiff() // TODO: THIS IS TEMPORARILY PUBLIC, IT SHOULD BE PRIVATE
        {
            var difficultyInfo = plugin.BeatSaberSongContainer.MapDifficultyInfo;

            stats = autocheckHelper.RunBeatmapScanner(difficultyInfo.Characteristic, difficultyInfo.DifficultyRank, difficultyInfo.Difficulty);
        }

        private void RunAutoCheckOnThisDiff()
        {
            var difficultyInfo = plugin.BeatSaberSongContainer.MapDifficultyInfo;

            autocheckHelper.RunAutoCheckOnDiff(difficultyInfo.Characteristic, difficultyInfo.DifficultyRank, difficultyInfo.Difficulty);
            plugin.CommentsUpdated.Invoke();
        }

        private void ImportModOnThisDiff(List<string> mod)
        {
            var difficultyInfo = plugin.BeatSaberSongContainer.MapDifficultyInfo;

            autocheckHelper.RunImportMod(difficultyInfo.Characteristic, difficultyInfo.DifficultyRank, difficultyInfo.Difficulty, mod);
            plugin.CommentsUpdated.Invoke();
        }

        private void CompareTimingsOnThisDiff(string characteristic, string difficulty)
        {
            var difficultyInfo = plugin.BeatSaberSongContainer.MapDifficultyInfo;

            autocheckHelper.RunCompareTimings(difficultyInfo.Characteristic, difficultyInfo.DifficultyRank, difficultyInfo.Difficulty, characteristic, difficulty);
            plugin.CommentsUpdated.Invoke();
        }

        private void CreateCriteriaStatusElement(CritResult severity, string name, Vector2 pos, Transform parent = null)
        {
            if (parent == null) parent = _criteriaMenu.transform;
            GameObject critStatusObj = GameObject.Find($"Crit_{name}_status");
            if (critStatusObj != null) Object.Destroy(critStatusObj);

            var (symbol, color) = UIHelper.CriteriaStatus(severity);
            UIHelper.AddLabel(parent, $"Crit_{name}_status", symbol, pos, new Vector2(25, 24), null, color, 12);
        }

        private CritResult IncrementSeverity(CritResult severity)
        {
            CritResult[] enumValues = (CritResult[])Enum.GetValues(typeof(CritResult));
            int currentIndex = Array.IndexOf(enumValues, severity);
            int nextIndex = (currentIndex + 1) % enumValues.Length;
            return enumValues[nextIndex];
        }

        public void CheckBeatForComment()
        {
            (float min, float max) beat = (plugin.AudioTimeSyncController.CurrentJsonTime - 0.01f, plugin.AudioTimeSyncController.CurrentJsonTime + 0.01f);

            List<Comment> comments = plugin.currentReview.Comments.Where(c => c.StartBeat >= beat.min && c.StartBeat <= beat.max).ToList();

            if (comments.Count == 0)
            {
                RemoveCommentMenu();
                RemoveCommentSelectMenu();
            }
            else if (comments.Count == 1)
            {
                // open top right comment UI
                RemoveCommentMenu();
                CreateCommentMenu(comments.FirstOrDefault());
                RemoveCommentSelectMenu();
            }
            else if (comments.Count > 1)
            {
                // open top right comment selection UI
                if (_commentSelectMenu == null)
                {
                    CreateCommentSelectMenu(comments);
                    RemoveCommentMenu();
                }
            }

        }

        private void ToggleGridMarkers()
        {
            if (showGridMarkers)
            {
                plugin.gridMarkerHelper = new(plugin);
            }
            else
            {
                plugin.gridMarkerHelper.Dispose();
            }
        }
    }
}

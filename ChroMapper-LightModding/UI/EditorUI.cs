using beatleader_parser.Timescale;
using Beatmap.Base;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using Parser.Map.Difficulty.V3.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
        private GameObject _settingMenu;
        private GameObject _ratingsMenu;
        private GameObject _commentMenu;
        private GameObject _commentSelectMenu;

        private string currentCommentMenuId;

        private Transform _songTimeline;
        private Transform _pauseMenu;
        private Transform _rightBar;
        public bool enabled = false;

        private bool showTimelineMarkers = true;
        private bool showGridMarkers = true;

        private (double diff, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) stats;

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

                dialog.AddComponent<TextComponent>()
                    .WithInitialValue($"Overall Comment: {plugin.currentReview.OverallComment}");

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

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>()
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });

            dialog.AddFooterButton(null, "Cancel");
            dialog.AddFooterButton(() => { plugin.HandleCreateComment(type, message, selectedObjects); }, "Create");

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

            dialog.AddComponent<DropdownComponent>()
                .WithLabel("Type")
                .WithOptions<CommentTypesEnum>()
                .WithInitialValue(Convert.ToInt32(comment.Type))
                .OnChanged((int i) => { type = (CommentTypesEnum)i; });

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
            if (!showTimelineMarkers) return;
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

            var bpmChanges = plugin.BPMChangeGridContainer.LoadedObjects.Cast<BaseBpmEvent>().ToList();
            if (bpmChanges.Count == 0) // apparently on intial load we are getting no bpm changes, so doing this for now to try and get them from the saved file anyway
            {
                BeatSaberSong.DifficultyBeatmap diff = plugin.BeatSaberSongContainer.Song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == plugin.currentReview.DifficultyCharacteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == plugin.currentReview.Difficulty && y.DifficultyRank == plugin.currentReview.DifficultyRank).FirstOrDefault();
                BaseDifficulty baseDifficulty = plugin.BeatSaberSongContainer.Song.GetMapFromDifficultyBeatmap(diff);
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

            Timescale timescale = Timescale.Create(BeatSaberSongContainer.Instance.Song.BeatsPerMinute, bpmChangesChecker, BeatSaberSongContainer.Instance.Song.SongTimeOffset);

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

        public void RefreshCriteriaMenu()
        {
            RemoveCriteriaMenu();
            CreateCriteriaMenu();
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
            AddSettingMenu(_pauseMenu);
            AddRatingsMenu(_criteriaMenu.transform);
            _ratingsMenu.SetActive(true);
        }

        public void AddCriteriaMenu(Transform parent)
        {
            _criteriaMenu = new GameObject("Automodder Criteria Menu");
            _criteriaMenu.transform.parent = parent;
            _criteriaMenu.SetActive(false);

            UIHelper.AttachTransform(_criteriaMenu, 572, 215, 0.05f, 1.20f, 0, 0, 0, 1);

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
            UIHelper.AddLabel(_criteriaMenu.transform, "FileSaveWarning", "Save the map before using these buttons!", new Vector2(0, -18), new Vector2(180, 24), TextAlignmentOptions.Left);
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
            name = "Loloppe";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Loloppe, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Loloppe = IncrementSeverity(criteria.Loloppe);
                posY = startPosY - 26 * 2;
                offsetX = 110;
                name = "Loloppe";
                CreateCriteriaStatusElement(criteria.Loloppe, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 3;
            name = "Hand Clap";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.HandClap, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.HandClap = IncrementSeverity(criteria.HandClap);
                posY = startPosY - 26 * 3;
                offsetX = 110;
                name = "Hand Clap";
                CreateCriteriaStatusElement(criteria.HandClap, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 4;
            name = "Swing Path Issue";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.SwingPath, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.SwingPath = IncrementSeverity(criteria.SwingPath);
                posY = startPosY - 26 * 4;
                offsetX = 110;
                name = "Swing Path Issue";
                CreateCriteriaStatusElement(criteria.SwingPath, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 5;
            name = "Hitbox Issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Hitbox, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Hitbox = IncrementSeverity(criteria.Hitbox);
                posY = startPosY - 26 * 5;
                offsetX = 110;
                name = "Hitbox Issues";
                CreateCriteriaStatusElement(criteria.Hitbox, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 6;
            name = "Requirements";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Requirement, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Requirement = IncrementSeverity(criteria.Requirement);
                posY = startPosY - 26 * 6;
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
            name = "Difficulty Name";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.DifficultyName, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.DifficultyName = IncrementSeverity(criteria.DifficultyName);
                posY = startPosY - 26 * 4;
                offsetX = 300;
                name = "Difficulty Name";
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

            #endregion
            #endregion

        }

        public void AddSettingMenu(Transform parent)
        {
            _settingMenu = new GameObject("Automodder Setting Menu");
            _settingMenu.transform.parent = parent;
            _settingMenu.SetActive(false);

            UIHelper.AttachTransform(_settingMenu, 572, 215, 0.05f, 1.20f, 0, 0, 0, 1);

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
                BLMapCheck.Configs.Config.Instance.Reset();
                RefreshCriteriaMenu();
            });

            UIHelper.AddButton(_settingMenu.transform, "CloseSettingsMenu", "Close Menu", new Vector2(250, -70), () =>
            {
                _criteriaMenu.SetActive(true);
                _settingMenu.SetActive(false);
            });

            
            #endregion

            float startPosX = -220, startPosY = -35;

            #region Options
            UIHelper.AddCheckbox(_settingMenu.transform, "DisplayBadcut", "Display Badcut", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.DisplayBadcut, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(_settingMenu.transform, "HighlightOffbeat", "Display Offbeat", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.HighlightOffbeat, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(_settingMenu.transform, "HighlightInline", "Display Inline", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.HighlightInline, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            UIHelper.AddTextInput(_settingMenu.transform, "InlineBeatPrecision", "1 / ", new Vector2(startPosX + 20, startPosY + 5), BLMapCheck.Configs.Config.Instance.InlineBeatPrecision.ToString(), (change) =>
            {
                if(Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.InlineBeatPrecision = result;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(_settingMenu.transform, "DisplayFlick", "Display  Flick", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.DisplayFlick, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            UIHelper.AddTextInput(_settingMenu.transform, "FlickBeatPrecision", "1 / ", new Vector2(startPosX + 20, startPosY + 5), BLMapCheck.Configs.Config.Instance.FlickBeatPrecision.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.FlickBeatPrecision = result;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(_settingMenu.transform, "DisplayAngleOffset", "Display Angle Offset", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.DisplayAngleOffset, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(_settingMenu.transform, "ParityInvertedWarning", "Parity Warning", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.ParityInvertedWarning, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            startPosY -= 26;
            UIHelper.AddCheckbox(_settingMenu.transform, "ParityDebug", "Parity Debug", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.ParityDebug, (check) =>
            {
                BLMapCheck.Configs.Config.Instance.DisplayBadcut = check;
            });
            startPosX = -80;
            startPosY = -30;
            UIHelper.AddLabel(_settingMenu.transform, "VBSettings", "Vision Block", new Vector2(startPosX + 70, startPosY), new Vector2(180, 24), TextAlignmentOptions.Left);
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "VBMinBottomNoteTime", "Bottom Row Allowed", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.VBMinBottomNoteTime.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.VBMinBottomNoteTime = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "VBMaxOuterNoteTime", "Outer Lane Allowed", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.VBMaxOuterNoteTime.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.VBMaxOuterNoteTime = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "VBMaxBombTime", "Bomb Closer Allowed", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.VBMaxBombTime.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.VBMaxBombTime = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "VBMinBombTime", "Bomb Closer Denied", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.VBMinBombTime.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.VBMinBombTime = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "VBMinimum", "Minimum Allowed", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.VBMinimum.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.VBMinimum = result;
            });
            startPosX = 30;
            startPosY = -30;
            UIHelper.AddLabel(_settingMenu.transform, "Settings", "Other Settings", new Vector2(startPosX + 70, startPosY), new Vector2(180, 24), TextAlignmentOptions.Left);
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "HotStartDuration", "Hot Start Duration", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.HotStartDuration.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.HotStartDuration = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "ColdEndDuration", "Cold End Duration", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.ColdEndDuration.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.ColdEndDuration = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "MinSongDuration", "Min Song Duration", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.MinSongDuration.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.MinSongDuration = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "FusedDistance", "Fused Distance", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.FusedDistance.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.FusedDistance = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "AverageLightPerBeat", "Avg Light Per Beat", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.AverageLightPerBeat.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.AverageLightPerBeat = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "LightFadeDuration", "Light Fade Duration", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.LightFadeDuration.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.LightFadeDuration = result;
            });
            startPosX = 140;
            startPosY = -30;
            UIHelper.AddTextInput(_settingMenu.transform, "LightBombReactionTime", "Light Bomb RT", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.LightBombReactionTime.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.LightBombReactionTime = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "MinimumWallDuration", "Min Wall Duration", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.MinimumWallDuration.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.MinimumWallDuration = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "ShortWallTrailDuration", "Wall Trail Duration", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.ShortWallTrailDuration.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.ShortWallTrailDuration = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "MaximumDodgeWallPerSecond", "Hard Dodge Wall/s", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.MaximumDodgeWallPerSecond.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.MaximumDodgeWallPerSecond = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "SubjectiveDodgeWallPerSecond", "Soft Dodge Wall/s", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.SubjectiveDodgeWallPerSecond.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.SubjectiveDodgeWallPerSecond = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "MaxChainRotation", "Max Chain Rotation", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.MaxChainRotation.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.MaxChainRotation = result;
            });
            startPosY -= 26;
            UIHelper.AddTextInput(_settingMenu.transform, "ChainLinkVsAir", "Chain Link vs. Air", new Vector2(startPosX, startPosY), BLMapCheck.Configs.Config.Instance.ChainLinkVsAir.ToString(), (change) =>
            {
                if (Double.TryParse(change, out double result)) BLMapCheck.Configs.Config.Instance.ChainLinkVsAir = result;
            });
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

            UIHelper.AddLabel(_ratingsMenu.transform, "BeatmapScannerValues", $"Difficulty: {stats.diff}☆ | Tech: {stats.tech}☆ | eBPM: {stats.ebpm} | SPS: {stats.sps} | Linear: {stats.linear}%", new Vector2(0, -12), new Vector2(392, 24), TextAlignmentOptions.Left);
            UIHelper.AddLabel(_ratingsMenu.transform, "BeatmapScannerValues2", $"Bomb Reset: {stats.reset}% | Slider: {stats.slider}% | Crouch: {stats.crouch} | Left/Right: {stats.handness}%", new Vector2(0, -36), new Vector2(392, 24), TextAlignmentOptions.Left);

            
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

            UIHelper.AttachTransform(_commentMenu, 325, 175, 1, 1, 0, 0, 1, 1);

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
            var difficultyData = plugin.BeatSaberSongContainer.DifficultyData;

            stats = autocheckHelper.RunBeatmapScanner(difficultyData.ParentBeatmapSet.BeatmapCharacteristicName, difficultyData.DifficultyRank, difficultyData.Difficulty);
        }

        private void RunAutoCheckOnThisDiff()
        {
            var difficultyData = plugin.BeatSaberSongContainer.DifficultyData;

            autocheckHelper.RunAutoCheckOnDiff(difficultyData.ParentBeatmapSet.BeatmapCharacteristicName, difficultyData.DifficultyRank, difficultyData.Difficulty);
            plugin.CommentsUpdated.Invoke();
        }

        private void CreateCriteriaStatusElement(CritResult severity, string name, Vector2 pos, Transform parent = null)
        {
            if (parent == null) parent = _criteriaMenu.transform;
            GameObject critStatusObj = GameObject.Find($"Crit_{name}_status");
            if (critStatusObj != null) Object.Destroy(critStatusObj);

            Color color;
            switch (severity)
            {
                case CritResult.Success:
                    color = Color.green;
                    break;
                case CritResult.Warning:
                    color = Color.yellow;
                    break;
                case CritResult.Fail:
                    color = Color.red;
                    break;
                default:
                    color = Color.gray;
                    break;
            }
            UIHelper.AddLabel(parent, $"Crit_{name}_status", "●", pos, new Vector2(25, 24), null, color, 12);
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

            List<Comment> comments = plugin.currentReview.Comments.Where(c => c.Objects.Any(o => o.Beat >= beat.min && o.Beat <= beat.max)).ToList();

            if (comments.Count == 0)
            {
                RemoveCommentMenu();
                RemoveCommentSelectMenu();
            }
            else if (comments.Count == 1)
            {
                // open top right comment UI
                if (currentCommentMenuId != comments.FirstOrDefault().Id)
                {
                    RemoveCommentMenu();
                    CreateCommentMenu(comments.FirstOrDefault());
                    RemoveCommentSelectMenu();
                }
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

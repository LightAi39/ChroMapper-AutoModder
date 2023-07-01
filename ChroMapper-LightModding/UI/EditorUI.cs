using Beatmap.Base;
using Beatmap.Enums;
using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChroMapper_LightModding.BeatmapScanner.Data.Criteria.InfoCrit;
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
        private GameObject _ratingsMenu;

        private Transform _songTimeline;
        private Transform _pauseMenu;
        public bool enabled = false;

        private bool showTimelineMarkers = false;

        private (double diff, double tech, double ebpm, double slider, double reset, double bomb, int crouch, double linear) stats;

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
                    .WithLabel("Edit file information")
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
                    .WithLabel("Show timeline markers (Experimental)")
                    .WithInitialValue(showTimelineMarkers)
                    .OnChanged((bool o) => {
                        if (o != showTimelineMarkers)
                        {
                            showTimelineMarkers = o;
                            ToggleTimelineMarkers();
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
                    read = " - Marked As Suppressed";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Beats: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type} - {comment.Message}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
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
                    read = " - Marked As Suppressed";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Beats: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type} - {comment.Message}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
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
            dialog.AddFooterButton(() => { ShowReviewCommentUI(plugin.HandleCreateComment(type, message, selectedObjects)); }, "Create");

            dialog.Open();
        }

        public void ShowReviewCommentUI(string id)
        {
            Comment comment = plugin.currentReview.Comments.Where(x => x.Id == id).First();
            string message = comment.Response;
            bool read = comment.MarkAsSuppressed;

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
                    read = " - Marked As Suppressed";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Beats: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type} - {comment.Message}{read}")
                    .OnClick(() => { ShowReviewCommentUI(comment.Id); });
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
        public void Enable(Transform songTimeline, Transform pauseMenu)
        {
            if (enabled) { return; }
            enabled = true;
            _songTimeline = songTimeline;
            _pauseMenu = pauseMenu;
            if (plugin.currentReview != null) RunBeatmapScannerOnThisDiff();
            CreateTimelineMarkers();
            CreateCriteriaMenu();
            
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

            UIHelper.AttachTransform(_timelineMarkers, 926, 22, 0.99f, 0.9f, 0, 0, 1, 1);

            //Image image = _timelineMarkers.AddComponent<Image>();
            //image.sprite = PersistentUI.Instance.Sprites.Background;
            //image.type = Image.Type.Sliced;
            //image.color = new Color(0.35f, 0.35f, 0.35f);

            float totalBeats = (plugin.BeatSaberSongContainer.Song.BeatsPerMinute / 60) * plugin.BeatSaberSongContainer.LoadedSongLength;

            foreach (var comment in plugin.currentReview.Comments)
            {
                float? cmbeat = FindOldBeatForSelectedNote(comment.Objects.FirstOrDefault());
                Debug.Log(cmbeat);
                if (cmbeat != null)
                {
                    float position = (float)(cmbeat / totalBeats * 926 - 463);
                    UIHelper.AddLabel(_timelineMarkers.transform, $"CommentMarker-{comment.Id}", "|", new Vector2(position, -14), new Vector2(0, 0), null, outlineHelper.ChooseOutlineColor(comment.Type));
                } 
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
            AddRatingsMenu(_pauseMenu);
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
            });

            UIHelper.AddButton(_criteriaMenu.transform, "RunBeatmapScanner", "Refresh Map Analytics", new Vector2(-126, -18), () =>
            {
                RunBeatmapScannerOnThisDiff();
                RefreshCriteriaMenu();
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
            name = "Outside";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Outside, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Outside = IncrementSeverity(criteria.Outside);
                posY = startPosY - 26 * 3;
                offsetX = -80;
                name = "Outside";
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
            name = "Chain";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Chain, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Chain = IncrementSeverity(criteria.Chain);
                posY = startPosY;
                offsetX = 110;
                name = "Chain";
                CreateCriteriaStatusElement(criteria.Chain, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26;
            name = "Fused Element";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.FusedElement, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.FusedElement = IncrementSeverity(criteria.FusedElement);
                posY = startPosY - 26;
                offsetX = 110;
                name = "Fused Element";
                CreateCriteriaStatusElement(criteria.FusedElement, name, new Vector2(-90 + offsetX, posY));
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
            name = "Swing Path";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.SwingPath, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.SwingPath = IncrementSeverity(criteria.SwingPath);
                posY = startPosY - 26 * 4;
                offsetX = 110;
                name = "Swing Path";
                CreateCriteriaStatusElement(criteria.SwingPath, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26 * 5;
            name = "Hitbox Issue";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Hitbox, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Hitbox = IncrementSeverity(criteria.Hitbox);
                posY = startPosY - 26 * 5;
                offsetX = 110;
                name = "Hitbox Issue";
                CreateCriteriaStatusElement(criteria.Hitbox, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            // next column
            offsetX = 300;
            posY = startPosY;
            name = "Slider issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Slider, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Slider = IncrementSeverity(criteria.Slider);
                posY = startPosY;
                offsetX = 300;
                name = "Slider issues";
                CreateCriteriaStatusElement(criteria.Slider, name, new Vector2(-90 + offsetX, posY));
            }, 50, 20, 10);

            posY = startPosY - 26;
            name = "Wall issues";
            UIHelper.AddLabel(_criteriaMenu.transform, $"Crit_{name}", name, new Vector2(-142 + offsetX, posY), new Vector2(106, 24), TextAlignmentOptions.Left);
            CreateCriteriaStatusElement(criteria.Wall, name, new Vector2(-90 + offsetX, posY));
            UIHelper.AddButton(_criteriaMenu.transform, $"Crit_{name}_change", "Change Status", new Vector2(-50 + offsetX, posY), () =>
            {
                criteria.Wall = IncrementSeverity(criteria.Wall);
                posY = startPosY - 26;
                offsetX = 300;
                name = "Wall issues";
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

        public void AddRatingsMenu(Transform parent)
        {
            _ratingsMenu = new GameObject("Automodder Ratings Menu");
            _ratingsMenu.transform.parent = parent;
            _ratingsMenu.SetActive(false);

            UIHelper.AttachTransform(_ratingsMenu, 400, 50, 0.05f, 0.65f, 0, 0, 0, 1);

            Image image = _ratingsMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.35f, 0.35f);

            UIHelper.AddLabel(_ratingsMenu.transform, "BeatmapScannerValues", $"Difficulty: {stats.diff}☆ | Tech: {stats.tech}☆ | eBPM: {stats.ebpm} | Slider: {stats.slider}%", new Vector2(-44, -12), new Vector2(292, 24), TextAlignmentOptions.Left);
            UIHelper.AddLabel(_ratingsMenu.transform, "BeatmapScannerValues2", $"Resets: {stats.reset}% | Bomb resets: {stats.bomb}% | Crouch: {stats.crouch} | Linear: {stats.linear}%", new Vector2(-44, -36), new Vector2(292, 24), TextAlignmentOptions.Left);

            
        }

        #endregion

        public float? FindOldBeatForSelectedNote(SelectedObject mapObject)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(mapObject.ObjectType);
            BeatSaberSong.DifficultyBeatmap diff = plugin.BeatSaberSongContainer.Song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == plugin.currentReview.DifficultyCharacteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == plugin.currentReview.Difficulty && y.DifficultyRank == plugin.currentReview.DifficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = plugin.BeatSaberSongContainer.Song.GetMapFromDifficultyBeatmap(diff);

            if (mapObject.ObjectType == ObjectType.Note)
            {
                var container = baseDifficulty.Notes.Where((note) =>
                {
                    if (note.JsonTime == mapObject.Beat && note.PosX == mapObject.PosX && note.PosY == mapObject.PosY && note.Color == mapObject.Color)
                    {
                        return true;
                    }
                    return false;
                }).FirstOrDefault();
                if (container != null) return container.SongBpmTime;
            }
            else if (mapObject.ObjectType == ObjectType.Obstacle)
            {
                var container = baseDifficulty.Obstacles.Where((gridItem) =>
                {
                    if (gridItem.JsonTime == mapObject.Beat && gridItem.PosX == mapObject.PosX && gridItem.PosY == mapObject.PosY)
                    {
                        return true;
                    }
                    return false;
                }).FirstOrDefault();
                if (container != null) return container.SongBpmTime;
            }
            else if (mapObject.ObjectType == ObjectType.Arc || mapObject.ObjectType == ObjectType.Chain)
            {
                BaseSlider container = baseDifficulty.Arcs.Where((slider) =>
                {
                    if (slider.JsonTime == mapObject.Beat && slider.PosX == mapObject.PosX && slider.PosY == mapObject.PosY && slider.Color == mapObject.Color)
                    {
                        return true;
                    }
                    return false;
                }).FirstOrDefault();
                if (container == null)
                {
                    container = baseDifficulty.Chains.Where((slider) =>
                    {
                        if (slider.JsonTime == mapObject.Beat && slider.PosX == mapObject.PosX && slider.PosY == mapObject.PosY && slider.Color == mapObject.Color)
                        {
                            return true;
                        }
                        return false;
                    }).FirstOrDefault();
                }
                if (container != null) return container.SongBpmTime;

            }
            else if (mapObject.ObjectType == ObjectType.BpmChange)
            {
                var container = baseDifficulty.BpmEvents.Where((bpmEvent) =>
                {
                    if (bpmEvent.JsonTime == mapObject.Beat)
                    {
                        return true;
                    }
                    return false;
                }).FirstOrDefault();
                if (container != null) return container.SongBpmTime;
            }
            return null;
        }

        private void RunBeatmapScannerOnThisDiff()
        {
            var difficultyData = plugin.BeatSaberSongContainer.DifficultyData;

            stats = autocheckHelper.RunBeatmapScanner(difficultyData.ParentBeatmapSet.BeatmapCharacteristicName, difficultyData.DifficultyRank, difficultyData.Difficulty);
        }

        private void RunAutoCheckOnThisDiff()
        {
            var difficultyData = plugin.BeatSaberSongContainer.DifficultyData;

            autocheckHelper.RunAutoCheckOnDiff(difficultyData.ParentBeatmapSet.BeatmapCharacteristicName, difficultyData.DifficultyRank, difficultyData.Difficulty);
        }

        private void CreateCriteriaStatusElement(Severity severity, string name, Vector2 pos, Transform parent = null)
        {
            if (parent == null) parent = _criteriaMenu.transform;
            GameObject critStatusObj = GameObject.Find($"Crit_{name}_status");
            if (critStatusObj != null) Object.Destroy(critStatusObj);

            Color color;
            switch (severity)
            {
                case Severity.Success:
                    color = Color.green;
                    break;
                case Severity.Warning:
                    color = Color.yellow;
                    break;
                case Severity.Fail:
                    color = Color.red;
                    break;
                default:
                    color = Color.gray;
                    break;
            }
            UIHelper.AddLabel(parent, $"Crit_{name}_status", "●", pos, new Vector2(25, 24), null, color, 12);
        }

        private Severity IncrementSeverity(Severity severity)
        {
            Severity[] enumValues = (Severity[])Enum.GetValues(typeof(Severity));
            int currentIndex = Array.IndexOf(enumValues, severity);
            int nextIndex = (currentIndex + 1) % enumValues.Length;
            return enumValues[nextIndex];
        }
    }
}

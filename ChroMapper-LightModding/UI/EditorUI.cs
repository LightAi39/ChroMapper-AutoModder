using Beatmap.Base;
using Beatmap.Enums;
using ChroMapper_LightModding.Export;
using ChroMapper_LightModding.Helpers;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ChroMapper_LightModding.UI
{
    internal class EditorUI
    {
        private Plugin plugin;
        private Exporter exporter;
        private OutlineHelper outlineHelper;
        private FileHelper fileHelper;

        private GameObject _timelineMarkers;

        private Transform _songTimeline;
        public bool enabled = false;

        public EditorUI(Plugin plugin, OutlineHelper outlineHelper, FileHelper fileHelper, Exporter exporter)
        {
            this.plugin = plugin;
            this.outlineHelper = outlineHelper;
            this.fileHelper = fileHelper;
            this.exporter = exporter;
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
                    read = " - Marked As Read";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type}{read}")
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
                    read = " - Marked As Read";
                }
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type}{read}")
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
                dialog.AddComponent<ButtonComponent>()
                    .WithLabel($"Objects: " + string.Join(", ", comment.Objects.ConvertAll(p => p.ToString())) + $" | {comment.Type}")
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
        public void Enable(Transform songTimeline)
        {
            if (enabled) { return; }
            enabled = true;
            _songTimeline = songTimeline;
            CreateTimelineMarkers();
        }

        public void Disable()
        {
            if (!enabled) { return; }
            enabled = false;
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
                if (cmbeat != null)
                {
                    float position = (float)(cmbeat / totalBeats * 926 - 463);
                    UIHelper.AddLabel(_timelineMarkers.transform, $"CommentMarker-{comment.Id}", "|", new Vector2(position, -14), new Vector2(0, 0), null, outlineHelper.ChooseOutlineColor(comment.Type));
                } 
            }

            
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
    }
}

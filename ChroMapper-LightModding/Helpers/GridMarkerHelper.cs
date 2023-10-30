using Beatmap.Base;
using Beatmap.Base.Customs;
using ChroMapper_LightModding.BeatmapScanner.MapCheck;
using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using UnityEngine;

namespace ChroMapper_LightModding.Helpers
{
    // the code from here is taken literally straight from CM BookmarkRenderingController.cs and just modified slightly but i mean its a cm plugin
    public class GridMarkerHelper : IDisposable
    {
        private Plugin plugin;
        private Transform gridBookmarksParent;

        private List<CachedComment> renderedComments = new List<CachedComment>();

        private List<BaseBpmEvent> bpmChanges = new List<BaseBpmEvent>();
        private BeatPerMinute bpm;

        private class CachedComment
        {
            public readonly Comment Comment;
            public readonly TextMeshProUGUI Text;
            public string Name;
            public Color Color;

            public CachedComment(Comment comment, TextMeshProUGUI text)
            {
                Comment = comment;
                Text = text;
                Name = comment.Message;
                Color = ChooseColor(comment.Type);
            }
        }

        public GridMarkerHelper(Plugin plugin)
        {
            this.plugin = plugin;
            gridBookmarksParent = GameObject.Find("Measure Lines Canvas").transform;
            plugin.CommentsUpdated += UpdateRenderedBookmarks;
            EditorScaleController.EditorScaleChangedEvent += OnEditorScaleChange;
            Settings.NotifyBySettingName(nameof(Settings.DisplayGridBookmarks), DisplayRenderedBookmarks);
            Settings.NotifyBySettingName(nameof(Settings.GridBookmarksHasLine), RefreshBookmarkGridLine);

            UpdateRenderedBookmarks();
        }

        private void DisplayRenderedBookmarks(object _) => UpdateRenderedBookmarks();

        private void UpdateRenderedBookmarks()
        {
            UpdateBpmChanges();
            var currentComments = plugin.currentReview.Comments;

            if (currentComments.Count < renderedComments.Count) // Removed comment
            {
                List<CachedComment> toDelete = new();
                foreach (var renderedComment in renderedComments.ToList())
                {
                    if (currentComments.All(x => x != renderedComment.Comment))
                    {
                        GameObject.Destroy(renderedComment.Text.gameObject);
                        renderedComments.Remove(renderedComment);
                    }
                }
            }

            if (currentComments.Count > renderedComments.Count) // Added comment
            {
                foreach (var comment in currentComments)
                {
                    if (renderedComments.All(x => x.Comment != comment))
                    {
                        TextMeshProUGUI text = CreateGridBookmark(comment);
                        renderedComments.Add(new CachedComment(comment, text));
                    }
                }
            }

            foreach (CachedComment cachedComment in renderedComments) // Covering for edited comment
            {
                string mapCommentName = cachedComment.Comment.Message;
                Color mapCommentColor = ChooseColor(cachedComment.Comment.Type);

                if (cachedComment.Name != mapCommentName || cachedComment.Color != mapCommentColor)
                {
                    SetGridBookmarkNameColor(cachedComment.Text, mapCommentColor, mapCommentName);

                    cachedComment.Name = mapCommentName;
                    cachedComment.Color = mapCommentColor;
                }
            }
        }

        private void OnEditorScaleChange(float newScale)
        {
            foreach (CachedComment commentDisplay in renderedComments)
                SetBookmarkPos(commentDisplay.Text.rectTransform, (float)bpm.ToBeatTime(bpm.ToRealTime(commentDisplay.Comment.StartBeat)));
        }

        private void SetBookmarkPos(RectTransform rect, float time)
        {
            //Need anchoredPosition3D, so Z gets precisely set, otherwise text might get under lighting grid
            rect.anchoredPosition3D = new Vector3(-4.5f, time * EditorScaleController.EditorScale, 0);
        }

        private TextMeshProUGUI CreateGridBookmark(Comment comment)
        {
            GameObject obj = new GameObject("GridBookmark", typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)obj.transform;
            rect.SetParent(gridBookmarksParent);
            SetBookmarkPos(rect, (float)bpm.ToBeatTime(bpm.ToRealTime(comment.StartBeat)));
            rect.sizeDelta = Vector2.one;
            rect.localRotation = Quaternion.identity;

            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            text.alignment = TextAlignmentOptions.Left;
            text.fontSize = 0.4f;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.fontMaterial.renderQueue = 3150; // Above grid and measure numbers - Below grid interface
            SetGridBookmarkNameColor(text, ChooseColor(comment.Type), comment.Message);

            return text;
        }

        private void RefreshBookmarkGridLine(object _)
        {
            foreach (CachedComment cachedBookmark in renderedComments)
                SetGridBookmarkNameColor(cachedBookmark.Text, cachedBookmark.Color, cachedBookmark.Name);
        }


        private void SetGridBookmarkNameColor(TextMeshProUGUI text, Color color, string name)
        {
            string hex = HEXFromColor(color, false);

            SetText();
            text.ForceMeshUpdate();

            //Here making so bookmarks with short name have still long colored rectangle on the right to the grid
            if (text.textBounds.size.x < 2) //2 is distance between notes and lighting grid
            {
                SetText((int)((2 - text.textBounds.size.x) / 0.0642f)); //Divided by 'space' character width for chosen fontSize
            }

            void SetText(int spaceNumber = 0)
            {
                string spaces = spaceNumber <= 0 ? null : new string(' ', spaceNumber);
                //<voffset> to align the bumped up text to grid, <s> to draw a line across the grid, in the end putting transparent dot, so trailing spaces don't get trimmed, 
                text.text = (Settings.Instance.GridBookmarksHasLine)
                    ? $"<mark={hex}50><voffset=0.06><s> <indent=3.92> </s></voffset> {name}{spaces}<color=#00000000>.</color>"
                    : $"<mark={hex}50><voffset=0.06> <indent=3.92> </voffset> {name}{spaces}<color=#00000000>.</color>";
            }
        }

        /// <summary> Returned string starts with # </summary>
        private string HEXFromColor(Color color, bool inclAlpha = true) => inclAlpha
            ? $"#{ColorUtility.ToHtmlStringRGBA(color)}"
            : $"#{ColorUtility.ToHtmlStringRGB(color)}";

        public void RefreshVisibility(float currentBeat, float beatsAhead, float beatsBehind)
        {
            foreach (var bookmarkDisplay in renderedComments)
            {
                var time = bookmarkDisplay.Comment.StartBeat;
                var text = bookmarkDisplay.Text;
                var enabled = time >= currentBeat - beatsBehind && time <= currentBeat + beatsAhead;
                text.gameObject.SetActive(enabled);
            }
        }

        public static Color ChooseColor(CommentTypesEnum type)
        {
            switch (type)
            {
                case CommentTypesEnum.Suggestion:
                    return Color.green;
                case CommentTypesEnum.Unsure:
                    return Color.yellow;
                case CommentTypesEnum.Issue:
                    return Color.red;
                case CommentTypesEnum.Info:
                    return Color.magenta;
                default:
                    return Color.clear;
            }
        }

        private void UpdateBpmChanges()
        {
            bpmChanges = plugin.BPMChangeGridContainer.LoadedObjects.Cast<BaseBpmEvent>().ToList();
            if (bpmChanges.Count == 0) // apparently on intial load we are getting no bpm changes, so doing this for now to try and get them from the saved file anyway
            {
                BeatSaberSong.DifficultyBeatmap diff = plugin.BeatSaberSongContainer.Song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == plugin.currentReview.DifficultyCharacteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == plugin.currentReview.Difficulty && y.DifficultyRank == plugin.currentReview.DifficultyRank).FirstOrDefault();
                BaseDifficulty baseDifficulty = plugin.BeatSaberSongContainer.Song.GetMapFromDifficultyBeatmap(diff);
                bpmChanges = baseDifficulty.BpmEvents;
            }

            bpm = BeatPerMinute.Create(BeatSaberSongContainer.Instance.Song.BeatsPerMinute, bpmChanges, BeatSaberSongContainer.Instance.Song.SongTimeOffset);
        }

        public void Dispose()
        {
            plugin.CommentsUpdated -= UpdateRenderedBookmarks;
            EditorScaleController.EditorScaleChangedEvent -= OnEditorScaleChange;
            Settings.ClearSettingNotifications(nameof(Settings.DisplayGridBookmarks));
            Settings.ClearSettingNotifications(nameof(Settings.GridBookmarksHasLine));

            foreach (var comment in renderedComments)
            {
                try
                {
                    GameObject.Destroy(comment.Text.gameObject);
                }
                catch (NullReferenceException)
                {
                    // yo thats cool bro but if it doesnt exist i just dont care
                }
                
            }
        }
    }
}

using ChroMapper_LightModding.Models;
using System.Linq;
using UnityEngine;

namespace ChroMapper_LightModding.Export
{
    internal class Exporter
    {
        public void ExportToDiscordMDByBeats(MapsetReview review)
        {
            string text = @"";

            text += $"## {review.SongName} {review.SubName} by {review.SongAuthor} - {review.ReviewType}\n \n";

            text += $"**Song Info Comments:**\n\n";

            foreach (var comment in review.Comments)
            {
                text += $"**{comment.Type} - {comment.Message}**";

                if (comment.Response != "")
                {
                    text += $"\n- Response: {comment.Response}";
                }

                if (comment.MarkAsSuppressed)
                {
                    text += " - *Comment was marked as suppressed*";
                }

                text += "\n \n";
            }

            foreach (var diffReview in review.DifficultyReviews)
            {
                text += $"**{diffReview.Difficulty}:**\n\n";

                foreach (var comment in diffReview.Comments)
                {
                    text += $"**Beats: {string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()).Distinct())} | {comment.Type}**\n{comment.Message}";

                    if (comment.Response != "")
                    {
                        text += $"\n- Response: {comment.Response}";
                    }

                    if (comment.MarkAsSuppressed)
                    {
                        text += "\n*Comment was marked as read*";
                    }

                    text += "\n \n";
                }

                if (diffReview.OverallComment != "")
                {
                    text += $"**Overall feedback:**\n{diffReview.OverallComment}";
                }
            }

            CopyToClipboard(text);
        }

        public void ExportToDiscordMDByImportance(MapsetReview _review)
        {
            MapsetReview review = _review;
            string text = @"";

            text += $"## {review.SongName} {review.SubName} by {review.SongAuthor} - {review.ReviewType}\n \n";

            text += $"**Song Info Comments:**\n\n";

            foreach (var comment in review.Comments)
            {
                text += $"**{comment.Type} - {comment.Message}**";

                if (comment.Response != "")
                {
                    text += $"\n- Response: {comment.Response}";
                }

                if (comment.MarkAsSuppressed)
                {
                    text += " - *Comment was marked as Solved*";
                }

                text += "\n \n";
            }

            foreach (var diffReview in review.DifficultyReviews)
            {
                CommentTypesEnum? lastType = null;
                diffReview.Comments = diffReview.Comments.OrderByDescending(x => x.Type).ToList();
                text += $"**{diffReview.Difficulty}:**\n\n";

                foreach (var comment in diffReview.Comments)
                {
                    if (lastType != comment.Type)
                    {
                        text += $"### {comment.Type}:\n";
                        lastType = comment.Type;
                    }

                    text += $"**Beats: {string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()).Distinct())} |**\n{comment.Message}";

                    if (comment.Response != "")
                    {
                        text += $"\n- Response: {comment.Response}";
                    }

                    if (comment.MarkAsSuppressed)
                    {
                        text += "\n*Comment was marked as Solved*";
                    }

                    text += "\n \n";
                }

                if (diffReview.OverallComment != "")
                {
                    text += $"**Overall feedback:**\n{diffReview.OverallComment}";
                }

                diffReview.Comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }

            CopyToClipboard(text);
        }

        public void ExportToBeatLeaderComment(DifficultyReview review)
        {
            string text = @"";

            foreach (var comment in review.Comments)
            {
                text += $"{string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()).Distinct())} | {comment.Type} - {comment.Message}";

                if (comment.Response != "")
                {
                    text += $" -- Response: {comment.Response}";
                }

                if (comment.MarkAsSuppressed)
                {
                    text += " *Solved*";
                }

                text += "\n";
            }

            if (review.OverallComment != "")
            {
                text += $"Overall feedback: {review.OverallComment}";
            }

            CopyToClipboard(text);
        }


        public static void CopyToClipboard(string text)
        {
            GUIUtility.systemCopyBuffer = text;
        }
    }
}

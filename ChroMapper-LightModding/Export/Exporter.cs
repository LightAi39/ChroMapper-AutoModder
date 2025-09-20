using ChroMapper_LightModding.Models;
using System;
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
                if (comment.Type == CommentTypesEnum.Data) continue;

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
                    if (comment.Type == CommentTypesEnum.Data) continue;

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

        public void ExportToDiscordMDShort(MapsetReview review)
        {
            string text = string.Join(", ", Enum.GetValues(typeof(CommentTypesEnum))
                .Cast<CommentTypesEnum>()
                .Where(t => t != CommentTypesEnum.Data && review.DifficultyReviews.Any(r => r.Comments.Any(c => c.Type == t)))
                .Select(t => $"({CommentTypeShortening(t)}) = {Exporter.CommentTypeName(t)}")) + "\n\n";
            foreach (var diffReview in review.DifficultyReviews)
            {
                if (diffReview.Comments.Count > 0 || !string.IsNullOrEmpty(diffReview.OverallComment)) {
                    text += $"{diffReview.Difficulty}:\n";
                }

                foreach (var comment in diffReview.Comments)
                {
                    if (comment.Type == CommentTypesEnum.Data) continue;

                    text += $"({CommentTypeShortening(comment.Type)}) {string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()).Distinct())} - {comment.Message}\n";
                }

                if (diffReview.Comments.Count > 0) {
                    text += "\n";
                }

                if (!string.IsNullOrEmpty(diffReview.OverallComment))
                {
                    text += $"Overall feedback:\n{diffReview.OverallComment}";
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
                if (comment.Type == CommentTypesEnum.Data) continue;

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
                    if (comment.Type == CommentTypesEnum.Data) continue;

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
                if (comment.Type == CommentTypesEnum.Data) continue;

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

        public static string CommentTypeShortening(CommentTypesEnum commentType) {
            switch (commentType)
            {
                case CommentTypesEnum.Suggestion:
                    return "S";
                case CommentTypesEnum.Questionable:
                    return "?";
                case CommentTypesEnum.Unrankable:
                    return "X";
                case CommentTypesEnum.Note:
                    return "i";
                case CommentTypesEnum.Data:
                    return "";
            }

            return "";
        }

        public static string CommentTypeName(CommentTypesEnum commentType) {
            switch (commentType)
            {
                case CommentTypesEnum.Suggestion:
                    return "Suggestion";
                case CommentTypesEnum.Questionable:
                    return "Questionable";
                case CommentTypesEnum.Unrankable:
                    return "Unrankable";
                case CommentTypesEnum.Note:
                    return "Note";
                case CommentTypesEnum.Data:
                    return "Data";
            }

            return "";
        }
    }
}

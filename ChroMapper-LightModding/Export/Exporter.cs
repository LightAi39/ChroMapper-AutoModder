using ChroMapper_LightModding.Models;
using System.Linq;
using UnityEngine;

namespace ChroMapper_LightModding.Export
{
    internal class Exporter
    {
        public void ExportToDiscordMDByBeats(DifficultyReview review)
        {
            string text = @"";

            text += $"## {review.Title} \n*Song: {review.MapName} - Difficulty: {review.Difficulty} - {review.ReviewType} by {review.Author}*\n \n";

            foreach (var comment in review.Comments)
            {
                text += $"**Beats: {string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()).Distinct())} | {comment.Type}**\n{comment.Message}";

                if (comment.Response != "")
                {
                    text += $"\n- Response: {comment.Response}";
                }

                if (comment.MarkAsRead)
                {
                    text += "\n*Comment was marked as read*";
                }

                text += "\n \n";
            }

            if (review.OverallComment != "")
            {
                text += $"**Overall feedback:**\n{review.OverallComment}";
            }

            CopyToClipboard(text);
        }

        public void ExportToDiscordMDByImportance(DifficultyReview _review)
        {
            DifficultyReview review = _review;
            CommentTypesEnum? lastType = null;
            string text = @"";

            text += $"## {review.Title} \n*Song: {review.MapName} - Difficulty: {review.Difficulty} - {review.ReviewType} by {review.Author}*\n \n";

            review.Comments = review.Comments.OrderByDescending(x => x.Type).ToList();

            foreach (var comment in review.Comments)
            {
                if (lastType != comment.Type)
                {
                    text += $"### {comment.Type}:\n";
                    lastType = comment.Type;
                }

                text += $"**Beats: {string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()).Distinct())}**\n{comment.Message}";

                if (comment.Response != "")
                {
                    text += $"\n- Response: {comment.Response}";
                }

                if (comment.MarkAsRead)
                {
                    text += "\n*Comment was marked as read*";
                }

                text += "\n \n";
            }

            if (review.OverallComment != "")
            {
                text += $"**Overall feedback:**\n{review.OverallComment}";
            }

            review.Comments = review.Comments.OrderBy(x => x.StartBeat).ToList();

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

                if (comment.MarkAsRead)
                {
                    text += " *read*";
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

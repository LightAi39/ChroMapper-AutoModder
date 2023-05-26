using ChroMapper_LightModding.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Windows;

namespace ChroMapper_LightModding.Export
{
    internal class Exporter
    {
        public void ExportToDiscordMD(DifficultyReview review)
        {
            string text = @"";

            text += $"## {review.Title} \n *Song: {review.MapName} - Difficulty: {review.Difficulty}* \n{review.ReviewType} Review by {review.Author} \n*{review.FinalizationDate.ToShortDateString()} {review.FinalizationDate.ToShortTimeString()}*\n \n";

            foreach (var comment in review.Comments)
            {
                text += $"**Beats: {string.Join(", ", comment.Objects.ConvertAll(p => p.ToString()))} | {comment.Type}**\n{comment.Message}";

                if (comment.Response != "")
                {
                    text += $"\n**Response:** {comment.Response}";
                }

                if (comment.MarkAsRead)
                {
                    text += "\n*Comment was marked as read*";
                }

                text += "\n \n";
            }

            CopyToClipboard(text);
        }


        public static void CopyToClipboard(string text)
        {
            GUIUtility.systemCopyBuffer = text;
        }
    }
}

using ChroMapper_LightModding.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChroMapper_LightModding.Helpers
{
    internal class FileHelper
    {
        private Plugin plugin;
        private OutlineHelper outlineHelper;

        public FileHelper(Plugin plugin, OutlineHelper outlineHelper)
        {
            this.plugin = plugin;
            this.outlineHelper = outlineHelper;
        }

        public void OldFileLoader()
        {
            // check in the map folder for any existing review files for this difficulty, then load it if it is not a backup
            try
            {
                if (!Directory.Exists($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews"))
                {
                    Debug.Log("No review files folder found in this map file");
                    return;
                }
                List<string> files = Directory.GetFiles($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews", "*.lreview").ToList();
                List<(DifficultyReview, string)> reviews = new();

                if (files.Count == 0)
                {
                    Debug.Log("No review files found in this map file");
                    return;
                }

                foreach (string file in files)
                {
                    if (!file.Contains("AUTOMATIC_BACKUP.lreview"))
                    {
                        reviews.Add((JsonConvert.DeserializeObject<DifficultyReview>(File.ReadAllText(file)), file));
                    }
                }

                reviews = reviews.Where(f => f.Item1.Version == Plugin.fileVersion).ToList();

                if (reviews.Count == 0)
                {
                    Debug.Log("No review files found in this map file with the correct file version");
                    return;
                }

                reviews = reviews.OrderByDescending(f => f.Item1.FinalizationDate).ToList();

                var correctReviewFilePair = reviews.First(x => x.Item1.DifficultyRank == plugin.BeatSaberSongContainer.DifficultyData.DifficultyRank);

                plugin.currentReview = correctReviewFilePair.Item1;
                plugin.currentlyLoadedFilePath = correctReviewFilePair.Item2;
                plugin.SubscribeToEvents();
                outlineHelper.selectionCache = new();
                Debug.Log("Loaded existing review file.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "Sequence contains no matching element")
                {
                    Debug.Log("No review files found in this map file for the current difficulty");
                }

            }
        }
    }
}

using ChroMapper_LightModding.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace ChroMapper_LightModding.Helpers
{
    internal class FileHelper
    {
        private Plugin plugin;
        private static string fileExtension = ".lreview";
        private static string backupText = "BACKUP";

        public FileHelper(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Try to automatically load a present mapset review file.
        /// </summary>
        /// <returns>true if a file was loaded, false if a file was not loaded</returns>
        public bool MapsetReviewLoader()
        {
            if (!Directory.Exists($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews"))
            {
                return false;
            }

            List<string> files = Directory.GetFiles($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews", "*" + fileExtension).ToList();
            if (files.Count == 0) return false;

            List<(MapsetReview, string)> reviews = new();

            foreach (string filepath in files)
            {
                if (!filepath.Contains(backupText + fileExtension))
                {
                    reviews.Add((JsonConvert.DeserializeObject<MapsetReview>(File.ReadAllText(filepath)), filepath));
                }
            }

            reviews = reviews.Where(f => f.Item1.FileVersion == Plugin.fileVersion).ToList();
            if (reviews.Count == 0) return false;

            reviews = reviews.OrderByDescending(f => f.Item1.LastEdited).ToList();

            // Sanity check if the review is more likely than not for the currently selected map.
            // If any of these are true then we can assume this is probably a valid map-review pair without invalidating when any change is made.
            var correctReviewFilePair = reviews.FirstOrDefault(x =>
            {
                return x.Item1.SongName == plugin.BeatSaberSongContainer.Song.SongName || x.Item1.SongAuthor == plugin.BeatSaberSongContainer.Song.SongAuthorName || x.Item1.Creator == plugin.BeatSaberSongContainer.Song.LevelAuthorName || x.Item1.SongLength == plugin.BeatSaberSongContainer.LoadedSongLength;
            });

            if (correctReviewFilePair.Item1 == null) return false;

            plugin.currentMapsetReview = correctReviewFilePair.Item1;
            plugin.currentlyLoadedFilePath = correctReviewFilePair.Item2;
            Debug.Log("Loaded mapset review.");
            return true;
        }

        /// <summary>
        /// Save the review file as it is stored in memory
        /// </summary>
        /// <param name="overrideExisting">Wether to override the existing file (delete it) or keep it.</param>
        public void MapsetReviewSaver(bool overrideExisting = true)
        {
            if (!Directory.Exists($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews"))
            {
                Directory.CreateDirectory($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews");
            }

            if (plugin.currentlyLoadedFilePath == "external") overrideExisting = false;
            var song = plugin.BeatSaberSongContainer.Song;
            var review = plugin.currentMapsetReview;

            // updating song details
            review.SongName = song.SongName;
            review.SubName = song.SongSubName;
            review.SongAuthor = song.SongAuthorName;
            review.Creator = song.LevelAuthorName;
            review.SongLength = plugin.BeatSaberSongContainer.LoadedSongLength;
            review.LastEdited = DateTime.UtcNow;

            string newFilePath = $"{plugin.BeatSaberSongContainer.Song.Directory}/reviews/{CleanFileName($"{review.SongName} {review.ReviewType} {review.LastEdited.Day}-{review.LastEdited.Month}-{review.LastEdited.Year} {review.LastEdited.Hour}.{review.LastEdited.Minute}.{review.LastEdited.Second}")}" + fileExtension;
            File.WriteAllText(newFilePath, JsonConvert.SerializeObject(review, Formatting.Indented));

            if (overrideExisting)
            {
                File.Delete(plugin.currentlyLoadedFilePath);
            }

            plugin.currentlyLoadedFilePath = newFilePath;
        }

        /// <summary>
        /// Create a new MapsetReview and save it.
        /// </summary>
        public void MapsetReviewCreator()
        {
            var song = plugin.BeatSaberSongContainer.Song;
            var difficultyData = plugin.BeatSaberSongContainer.DifficultyData;


            List<DifficultyReview> difficultyReviews = new List<DifficultyReview>();
            foreach (var diffSet in song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffSet.DifficultyBeatmaps)
                {
                    difficultyReviews.Add(new()
                    {
                        DifficultyCharacteristic = diffSet.BeatmapCharacteristicName,
                        Difficulty = diff.Difficulty,
                        DifficultyRank = diff.DifficultyRank
                    });
                }
            }
            difficultyReviews = difficultyReviews.OrderByDescending(x => x.DifficultyRank).ToList();

            MapsetReview review = new()
            {
                SongName = song.SongName,
                SubName = song.SongSubName,
                SongAuthor = song.SongAuthorName,
                Creator = song.LevelAuthorName,
                SongLength = plugin.BeatSaberSongContainer.LoadedSongLength,
                ReviewType = ReviewTypeEnum.Feedback,
                FileVersion = Plugin.fileVersion,
                DifficultyReviews = difficultyReviews
            };

            plugin.currentMapsetReview = review;

            if (!Directory.Exists($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews"))
            {
                Directory.CreateDirectory($"{plugin.BeatSaberSongContainer.Song.Directory}/reviews");
            }

            string newFilePath = $"{plugin.BeatSaberSongContainer.Song.Directory}/reviews/{CleanFileName($"{review.SongName} {review.ReviewType} {review.LastEdited.Day}-{review.LastEdited.Month}-{review.LastEdited.Year} {review.LastEdited.Hour}.{review.LastEdited.Minute}.{review.LastEdited.Second}")}" + fileExtension;
            Debug.Log(newFilePath);
            File.WriteAllText(newFilePath, JsonConvert.SerializeObject(review, Formatting.Indented));
            plugin.currentlyLoadedFilePath = newFilePath;
        }

        /// <summary>
        /// Save the review file as it is stored in memory as a backup
        /// </summary>
        public void MapsetReviewBackupSaver()
        {
            // preparation for the backup limit
            List<string> files = Directory.GetFiles(plugin.BeatSaberSongContainer.Song.Directory + "/reviews", "*" + backupText + fileExtension).ToList();

            List<(MapsetReview, string)> reviews = new();

            foreach (string file in files)
            {
                reviews.Add((JsonConvert.DeserializeObject<MapsetReview>(File.ReadAllText(file)), file));
            }

            reviews = reviews.OrderBy(f => f.Item1.LastEdited).ToList();

            // enforcing the backup limit
            if (reviews.Count >= Plugin.backupLimit)
            {
                File.Delete(reviews[0].Item2);
            }

            var song = plugin.BeatSaberSongContainer.Song;
            var review = plugin.currentMapsetReview;

            // updating song details
            review.SongName = song.SongName;
            review.SubName = song.SongSubName;
            review.SongAuthor = song.SongAuthorName;
            review.Creator = song.LevelAuthorName;
            review.SongLength = plugin.BeatSaberSongContainer.LoadedSongLength;
            review.LastEdited = DateTime.UtcNow;

            string newFilePath = $"{plugin.BeatSaberSongContainer.Song.Directory}/reviews/{CleanFileName($"{review.SongName} {review.ReviewType} {review.LastEdited.Day}-{review.LastEdited.Month}-{review.LastEdited.Year} {review.LastEdited.Hour}.{review.LastEdited.Minute}.{review.LastEdited.Second}")}" + backupText + fileExtension;
            File.WriteAllText(newFilePath, JsonConvert.SerializeObject(review, Formatting.Indented));

            plugin.currentlyLoadedFilePath = newFilePath;
        }

        /// <summary>
        /// Delete the currently loaded mapsetreview
        /// </summary>
        public void MapsetReviewRemover()
        {
            if (plugin.currentlyLoadedFilePath == "external") return;
            File.Delete(plugin.currentlyLoadedFilePath);
            plugin.currentMapsetReview = null;
            plugin.currentlyLoadedFilePath = null;
        }

        /// <summary>
        /// Function to get the file from file panel input
        /// </summary>
        public void OnSelectReviewFile(string[] obj)
        {
            if (obj == null || obj.Length == 0) return;

            var fileLocation = obj[0];

            if (!File.Exists(fileLocation)) return;

            MapsetReview review = JsonConvert.DeserializeObject<MapsetReview>(File.ReadAllText(fileLocation));

            plugin.currentMapsetReview = review;
            plugin.currentlyLoadedFilePath = "external";
            Debug.Log("Loaded mapset review.");
        }

        /// <summary>
        /// Check if theres a review for every difficulty and if there are any reviews for difficulties that dont exist
        /// </summary>
        public void CheckDifficultyReviewsExist()
        {
            var song = plugin.BeatSaberSongContainer.Song;

            List<DifficultyReview> difficultyReviews = plugin.currentMapsetReview.DifficultyReviews;

            // is there a file for every difficulty
            foreach (var diffSet in song.DifficultyBeatmapSets)
            {
                foreach (var diff in diffSet.DifficultyBeatmaps)
                {
                    if (!difficultyReviews.Any(x => x.DifficultyCharacteristic == diffSet.BeatmapCharacteristicName && x.Difficulty == diff.Difficulty && x.DifficultyRank == diff.DifficultyRank))
                    {
                        difficultyReviews.Add(new()
                        {
                            DifficultyCharacteristic = diffSet.BeatmapCharacteristicName,
                            Difficulty = diff.Difficulty,
                            DifficultyRank = diff.DifficultyRank
                        });
                    }
                }
            }

            // are there any review files for difficulties that dont exist
            foreach (var diff in difficultyReviews)
            {
                if (!song.DifficultyBeatmapSets.Any(x => x.BeatmapCharacteristicName == diff.DifficultyCharacteristic))
                {
                    difficultyReviews.Remove(diff);
                    return;
                }
                if (!song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == diff.DifficultyCharacteristic).FirstOrDefault().DifficultyBeatmaps.Any(y => y.Difficulty == diff.Difficulty && y.DifficultyRank == diff.DifficultyRank))
                {
                    difficultyReviews.Remove(diff);
                    return;
                }
            }

            difficultyReviews = difficultyReviews.OrderByDescending(x => x.DifficultyRank).ToList();

            plugin.currentMapsetReview.DifficultyReviews = difficultyReviews;
        }

        private static string CleanFileName(string fileName)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string validFileName = Regex.Replace(fileName, "[" + invalidChars + "]", "");

            validFileName = validFileName.Trim();

            validFileName = Regex.Replace(validFileName, @"\s+", " ");

            return validFileName;
        }

    }
}

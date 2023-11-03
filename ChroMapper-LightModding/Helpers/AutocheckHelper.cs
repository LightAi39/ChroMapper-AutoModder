using Beatmap.Base;
using BLMapCheck;
using JoshaParity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChroMapper_LightModding.Helpers
{
    internal class AutocheckHelper
    {
        private Plugin plugin;
        private BLMapChecker criteriaCheck;
        private FileHelper fileHelper;

        public AutocheckHelper(Plugin plugin, FileHelper fileHelper)
        {
            this.plugin = plugin;
            criteriaCheck = new BLMapChecker();
            this.fileHelper = fileHelper;
        }

        public (double diff, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) RunAutoCheck(bool isAutoCheckOnInfo, bool isAutoCheckOnDiff, bool isForMapCheckStats, string characteristic = "", int difficultyRank = 0, string difficulty = "")
        {
            criteriaCheck.LoadMap(plugin.currentlyLoadedFolderPath, BeatSaberSongContainer.Instance.LoadedSongLength);
            var results = criteriaCheck.CheckAllCriteria();

            if (isAutoCheckOnInfo)
            {
                RemovePastAutoCheckCommentsSongInfo();
                plugin.currentMapsetReview.Criteria = results.InfoCriteriaResult;
            }
            if (isAutoCheckOnDiff)
            {
                fileHelper.CheckDifficultyReviewsExist();
                RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
                plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Critera = results.DifficultyCriteriaResults.Where(x => x.difficulty == difficulty && x.characteristic == characteristic).FirstOrDefault().crit;
            }
            if (isForMapCheckStats)
            {
                var resultData = results.Results.Where(x => x.Name == "BeatmapScanner data").FirstOrDefault().ResultData;
                return (
                    Convert.ToDouble(resultData.Where(x => x.key == "Pass").FirstOrDefault().value),
                    Convert.ToDouble(resultData.Where(x => x.key == "Tech").FirstOrDefault().value),
                    Convert.ToDouble(resultData.Where(x => x.key == "EBPM").FirstOrDefault().value),
                    Convert.ToDouble(resultData.Where(x => x.key == "Slider").FirstOrDefault().value),
                    Convert.ToDouble(resultData.Where(x => x.key == "Reset").FirstOrDefault().value),
                    Convert.ToInt32(resultData.Where(x => x.key == "Crouch").FirstOrDefault().value),
                    Convert.ToDouble(resultData.Where(x => x.key == "Linear").FirstOrDefault().value),
                    Convert.ToDouble(resultData.Where(x => x.key == "SPS").FirstOrDefault().value),
                    resultData.Where(x => x.key == "Handness").FirstOrDefault().value
                    );
            }
            return (0, 0, 0, 0, 0, 0, 0, 0, "");
        }

        public void RunAutoCheckOnInfo()
        {
            //RemovePastAutoCheckCommentsSongInfo();
            //plugin.currentMapsetReview.Criteria = criteriaCheck.AutoInfoCheck();
            RunAutoCheck(true, false, false);
        }

        public void RunAutoCheckOnDiff(string characteristic, int difficultyRank, string difficulty)
        {
            //fileHelper.CheckDifficultyReviewsExist();

            //var result = RunBeatmapScanner(characteristic, difficultyRank, difficulty);

            //if (result != (-1, -1, -1, -1, -1, -1, -1, -1, "-1"))
            //{
            //    RemovePastAutoCheckCommentsOnDiff(characteristic, difficultyRank, difficulty);
            //    plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Critera = criteriaCheck.AutoDiffCheck(characteristic, difficultyRank, difficulty);
            //}
            RunAutoCheck(false, true, false, characteristic, difficultyRank, difficulty);
        }

        public (double diff, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) RunBeatmapScanner(string characteristic, int difficultyRank, string difficulty)
        {
            //var song = plugin.BeatSaberSongContainer.Song;
            //BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();

            //BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);

            //if (baseDifficulty.Notes.Any())
            //{
            //    List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
            //    notes = notes.OrderBy(o => o.JsonTime).ToList();

            //    if (notes.Count > 0)
            //    {
            //        List<BaseSlider> chains = baseDifficulty.Chains.Cast<BaseSlider>().ToList();
            //        chains = chains.OrderBy(o => o.JsonTime).ToList();

            //        List<BaseNote> bombs = baseDifficulty.Notes.Where(n => n.Type == 3).ToList();
            //        bombs = bombs.OrderBy(b => b.JsonTime).ToList();

            //        List<BaseObstacle> obstacles = baseDifficulty.Obstacles.ToList();
            //        obstacles = obstacles.OrderBy(o => o.JsonTime).ToList();

            //        var data = BeatmapScanner.BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatSaberSongContainer.Instance.Song.BeatsPerMinute);
            //        var analysedMap = new MapAnalyser(song.Directory);
            //        var swings = analysedMap.GetSwingData((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());
            //        data.sps = Math.Round(analysedMap.GetSPS((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower()), 2);
            //        var temp = analysedMap.GetHandedness((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());
            //        data.handness = Math.Round(temp.Y, 2).ToString() + "/" + Math.Round(temp.X, 2).ToString();
            //        data.reset = Math.Round((double)analysedMap.GetResetCount((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower(), ResetType.Bomb) / swings.Count() * 100, 2);
            //        data.ebpm = Math.Round(analysedMap.GetAverageEBPM((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower()), 2);
            //        return data;
            //    }
            //}
            //return (-1, -1, -1, -1, -1, -1, -1, -1, "-1");

            return RunAutoCheck(false, false, true, characteristic, difficultyRank, difficulty);
        }

        public void RemovePastAutoCheckCommentsSongInfo()
        {
            plugin.currentMapsetReview.Comments = plugin.currentMapsetReview.Comments.Where(x => x.IsAutogenerated == false || x.Response != "" || x.MarkAsSuppressed).ToList();
        }

        public void RemovePastAutoCheckCommentsOnDiff(string characteristic, int difficultyRank, string difficulty)
        {
            plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().OverallComment = "";
            plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments.Where(x => x.IsAutogenerated == false || x.Response != "" || x.MarkAsSuppressed).ToList();
        }
    }
}

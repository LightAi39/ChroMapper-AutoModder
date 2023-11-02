﻿using BLMapCheck.Classes.Results;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Config.Config;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class Creator
    {
        public static CritResult Check(string LevelAuthorName)
        {
            if (LevelAuthorName.Count() == 0)
            {
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Creator",
                    Severity = Severity.Error,
                    CheckType = "SongInfo",
                    Description = "The creator field is empty.",
                    ResultData = new() { new("CreatorLength", "0") }
                });
                return CritResult.Fail;
            }
            if (LevelAuthorName.Count() > MaxChar)
            {
                //CreateSongInfoComment("R7C - Creator field is too long. Maybe use a group name instead?", CommentTypesEnum.Suggestion); TODO: USE NEW METHOD
                CheckResults.Instance.AddResult(new CheckResult()
                {
                    Name = "Creator",
                    Severity = Severity.Suggestion,
                    CheckType = "SongInfo",
                    Description = "The creator field is very long. Consider using a group name.",
                    ResultData = new() { new("CreatorLength", LevelAuthorName.Count().ToString()) }
                });
                return CritResult.Warning;
            }

            CheckResults.Instance.AddResult(new CheckResult()
            {
                Name = "Creator",
                Severity = Severity.Passed,
                CheckType = "SongInfo",
                Description = "The creator field is not empty and is not too long.",
                ResultData = new() { new("CreatorLength", LevelAuthorName.Count().ToString()) }
            });
            return CritResult.Success;
        }
    }
}

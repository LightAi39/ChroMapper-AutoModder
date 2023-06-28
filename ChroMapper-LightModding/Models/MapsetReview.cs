﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    /*public enum ReviewTypeEnum
    {
        Feedback,
        Quality,
        Rankability,
        Other
    } uncomment when the other one is gone*/

    public class MapsetReview
    {
        #region Review Metadata
        public string SongName { get; set; }
        public string SubName { get; set; }
        public string SongAuthor { get; set; }
        public string Creator { get; set; }
        public float SongLength { get; set; }
        public ReviewTypeEnum ReviewType { get; set; }
        public string FileVersion { get; set; }
        public DateTime LastEdited { get; set; } = DateTime.UtcNow;
        #endregion

        //public InfoCrit Criteria { get; set; } uncomment when we merge

        public List<DifficultyReview> DifficultyReviews { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    public enum ReviewTypeEnum
    {
        Feedback,
        QualityMod,
        RankabilityMod,
        Other
    } // will be moved to mapsetReview

    public class DifficultyReview
    {
        public string Title { get; set; } = null!; // will be deleted
        public string Author { get; set; } = null!; // will be deleted
        public string MapName { get; set; } = null!; // will be deleted

        public string Difficulty { get; set; } = null!;
        public int DifficultyRank { get; set; }
        public ReviewTypeEnum ReviewType { get; set; } // will be deleted
        public DateTime LastEdited { get; set; } = DateTime.UtcNow;

        public string Version { get; set; } = null!; // will be deleted

        public List<Comment> Comments { get; set; } = new List<Comment>();

        //public DiffCrit Critera { get; set; } uncomment when we merge

        public string OverallComment { get; set; } = null!;

    }
}

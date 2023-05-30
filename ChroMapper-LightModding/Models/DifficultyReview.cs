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
    }

    public class DifficultyReview
    {
        public string Title { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string OverallComment { get; set; } = null!;
        public string MapName { get; set; } = null!;
        public string Difficulty { get; set; } = null!;
        public int DifficultyRank { get; set; }
        public ReviewTypeEnum ReviewType { get; set; }
        public DateTime FinalizationDate { get; set; } = DateTime.UtcNow;

        public string Version { get; set; } = null!;

        public List<Comment> Comments { get; set; } = null!;

    }
}

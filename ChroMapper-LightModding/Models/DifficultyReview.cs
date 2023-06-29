using ChroMapper_LightModding.BeatmapScanner.Data.Criteria;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    public class DifficultyReview
    {
        public string DifficultyCharacteristic { get; set; }
        public string Difficulty { get; set; } = null!;
        public int DifficultyRank { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();

        public DiffCrit Critera { get; set; } = new DiffCrit();

        public string OverallComment { get; set; } = null!;

    }
}

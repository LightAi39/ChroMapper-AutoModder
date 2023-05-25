using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    public class DifficultyReview
    {
        public string Author { get; set; } = null!;
        public string MapName { get; set; } = null!;
        public string Difficulty { get; set; } = null!;
        public DateTime FinalizationDate { get; set; } = DateTime.UtcNow;

        public List<Comment> Comments { get; set; } = null!;

    }
}

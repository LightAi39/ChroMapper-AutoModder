using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    public enum CommentTypesEnum
    {
        Note,
        Suggestion,
        Warning,
        Issue
    }

    public class Comment
    {
        public float StartBeat { get; set; }

        public List<SelectedNote> Notes { get; set; }

        public CommentTypesEnum Type { get; set; }

        public string[] Message { get; set; } = null!;
        public string[] Response { get; set; } = null!;

        public bool MarkAsRead { get; set; } = false;
    }
}

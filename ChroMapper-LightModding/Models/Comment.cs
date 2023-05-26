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
        public string Id { get; set; } = null!;
        public float StartBeat { get; set; }

        public List<SelectedObject> Objects { get; set; }

        public CommentTypesEnum Type { get; set; }

        public string Message { get; set; } = null!;
        public string Response { get; set; } = "";

        public bool MarkAsRead { get; set; } = false;
    }
}

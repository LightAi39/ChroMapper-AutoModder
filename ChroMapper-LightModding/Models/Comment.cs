using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    public enum CommentTypesEnum
    {
        note,
        suggestion,
        warning,
        issue
    }

    public class Comment
    {
        public float Beat { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }

        public CommentTypesEnum Type { get; set; }

        public string[] Message { get; set; } = null!;
        public string[] Response { get; set; } = null!;

        public bool MarkAsRead { get; set; } = false;
    }
}

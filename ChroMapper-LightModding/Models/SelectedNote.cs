using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChroMapper_LightModding.Models
{
    public class SelectedNote
    {
        public float Beat { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }

        public override string ToString()
        {
            return Beat.ToString();
        }

        public string ToStringFull()
        {
            return $"{Beat} ({PosX},{PosY})";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.ChroMapper
{
    public class BaseObstacle : BaseGrid
    {
        public float Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}

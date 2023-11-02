using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.ChroMapper
{
    public class BaseSlider : BaseGrid
    {
        public int Color { get; set; }
        public int CutDirection { get; set; }
        public int AngleOffset { get; set; }
        public float TailJsonTime { get; set; }
        public int TailPosX { get; set; }
        public int TailPosY { get; set; }
    }
}

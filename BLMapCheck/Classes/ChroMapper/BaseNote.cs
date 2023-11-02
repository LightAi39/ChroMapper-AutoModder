using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.ChroMapper
{
    public class BaseNote : BaseGrid
    {
        public int Type { get; set; }
        public int Color { get; set; }
        public int CutDirection { get; set; }
        public int AngleOffset { get; set; }
    }
}

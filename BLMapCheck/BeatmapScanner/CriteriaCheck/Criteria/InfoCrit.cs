using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.BeatmapScanner.Data.Criteria
{
    public class InfoCrit
    {
        public CritResult SongName { get; set; } = CritResult.Fail;
        public CritResult SubName { get; set; } = CritResult.Fail;
        public CritResult SongAuthor { get; set; } = CritResult.Fail;
        public CritResult Creator { get; set; } = CritResult.Fail;
        public CritResult Offset { get; set; } = CritResult.Fail;
        public CritResult BPM { get; set; } = CritResult.Fail;
        public CritResult DifficultyOrdering { get; set; } = CritResult.Fail;
        public CritResult Preview { get; set; } = CritResult.Fail;

        public enum CritResult
        {
            Success = 0,
            Warning = 1,
            Fail = 2
        }
    }
}

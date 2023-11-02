using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.BeatmapScanner.Data.Criteria
{
    public class InfoCrit
    {
        public CritSeverity SongName { get; set; } = CritSeverity.Fail;
        public CritSeverity SubName { get; set; } = CritSeverity.Fail;
        public CritSeverity SongAuthor { get; set; } = CritSeverity.Fail;
        public CritSeverity Creator { get; set; } = CritSeverity.Fail;
        public CritSeverity Offset { get; set; } = CritSeverity.Fail;
        public CritSeverity BPM { get; set; } = CritSeverity.Fail;
        public CritSeverity DifficultyOrdering { get; set; } = CritSeverity.Fail;
        public CritSeverity Preview { get; set; } = CritSeverity.Fail;

        public enum CritSeverity
        {
            Success = 0,
            Warning = 1,
            Fail = 2
        }
    }
}

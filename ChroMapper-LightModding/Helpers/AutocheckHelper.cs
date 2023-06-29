using Beatmap.Base;
using ChroMapper_LightModding.BeatmapScanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace ChroMapper_LightModding.Helpers
{
    internal class AutocheckHelper
    {
        private Plugin plugin;

        public AutocheckHelper(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void RunAutoCheckOnInfo()
        {
            plugin.currentMapsetReview.Criteria = CriteriaCheck.AutoInfoCheck();
        }

        public void RunAutoCheckOnDiff()
        {
            
        }
    }
}

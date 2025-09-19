using System.Collections.Generic;
using System.Linq;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Shrado
    {
        public static void Check()
        {
            string characteristic = CriteriaCheckManager.Characteristic;
            string difficulty = CriteriaCheckManager.Difficulty;
            string name = "Shrado Angle";
            string checkType = "Shrado";

            var red = NotesData.Where(n => n.Note.Color == 0 && (n.Head || !n.Pattern)).ToList();
            var blue = NotesData.Where(n => n.Note.Color == 1 && (n.Head || !n.Pattern)).ToList();

            for (int i = 0; i < red.Count - 1; i++)
            {
                if (red[i + 1].Note.Beats - red[i].Note.Beats <= Configs.Config.Instance.ShradoMaxBeat)
                {
                    if (DetectShrado(red[i].Note, red[i + 1].Note))
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Info, checkType, name, new(), new() { red[i + 1].Note });
                    }
                }
            }

            for (int i = 0; i < blue.Count - 1; i++)
            {
                if (blue[i + 1].Note.Beats - blue[i].Note.Beats <= Configs.Config.Instance.ShradoMaxBeat)
                {
                    if (DetectShrado(blue[i].Note, blue[i + 1].Note))
                    {
                        CheckResults.Instance.CreateAndAddResult(characteristic, difficulty,
                            name, Severity.Info, checkType, name, new(), new() { blue[i + 1].Note });
                    }
                }
            }
        }

        public static bool DetectShrado(Note previous, Note next)
        {
            switch (previous.CutDirection)
            {
                case 6:
                    if (next.CutDirection == 0 && previous.x == 1 && next.x == 3 && previous.y == next.y - 1)
                    {
                        return true;
                    }
                    break;
                case 7:
                    if (next.CutDirection == 0 && previous.x == 2 && next.x == 0 && previous.y == next.y - 1)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}

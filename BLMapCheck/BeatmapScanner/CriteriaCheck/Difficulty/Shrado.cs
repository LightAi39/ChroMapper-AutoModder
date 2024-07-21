using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Grid;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal class Shrado
    {
        public static void Check(List<Note> notes)
        {
            if (Configs.Config.Instance.DisplayShrado)
            {
                if (notes.Any())
                {
                    var red = NotesData.Where(n => n.Note.Color == 0 && (n.Head || !n.Pattern)).ToList();
                    var blue = NotesData.Where(n => n.Note.Color == 1 && (n.Head || !n.Pattern)).ToList();

                    for (int i = 0; i < red.Count - 1; i++)
                    {
                        if(red[i + 1].Note.Beats - red[i].Note.Beats <= Configs.Config.Instance.ShradoMaxBeat)
                        {
                            if(DetectShrado(red[i].Note, red[i + 1].Note))
                            {
                                CheckResults.Instance.AddResult(new CheckResult()
                                {
                                    Characteristic = CriteriaCheckManager.Characteristic,
                                    Difficulty = CriteriaCheckManager.Difficulty,
                                    Name = "Shrado Angle",
                                    Severity = Severity.Info,
                                    CheckType = "Shrado",
                                    Description = "Shrado Angle",
                                    ResultData = new() { },
                                    BeatmapObjects = new() { red[i + 1].Note }
                                });
                            }
                        }
                    }

                    for (int i = 0; i < blue.Count - 1; i++)
                    {
                        if (blue[i + 1].Note.Beats - blue[i].Note.Beats <= Configs.Config.Instance.ShradoMaxBeat)
                        {
                            if (DetectShrado(blue[i].Note, blue[i + 1].Note))
                            {
                                CheckResults.Instance.AddResult(new CheckResult()
                                {
                                    Characteristic = CriteriaCheckManager.Characteristic,
                                    Difficulty = CriteriaCheckManager.Difficulty,
                                    Name = "Shrado Angle",
                                    Severity = Severity.Info,
                                    CheckType = "Shrado",
                                    Description = "Shrado Angle",
                                    ResultData = new() { },
                                    BeatmapObjects = new() { blue[i + 1].Note }
                                });
                            }
                        }
                    }
                }
            }       
        }

        public static bool DetectShrado(Note previous, Note next)
        {
            switch (previous.CutDirection)
            {
                case 4:
                    if(next.CutDirection == 1 && previous.x <= next.x - 2 && previous.y == next.y + 1)
                    {
                        return true;
                    }
                    break;
                case 5:
                    if (next.CutDirection == 1 && previous.x >= next.x + 2 && previous.y == next.y + 1)
                    {
                        return true;
                    }
                    break;
                case 6:
                    if (next.CutDirection == 0 && previous.x <= next.x - 2 && previous.y == next.y - 1)
                    {
                        return true;
                    }
                    break;
                case 7:
                    if (next.CutDirection == 0 && previous.x >= next.x + 2 && previous.y == next.y - 1)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}

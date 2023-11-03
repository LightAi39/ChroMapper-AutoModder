using JoshaParity;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    internal static class RollingEBPM
    {
        public class EbpmData
        {
            public SwingData Swing { get; set; } = new();
            public float Average { get; set; } = 0f;
            public bool Flick { get; set; } = false;
        }

        public static void Check(List<SwingData> Swings)
        {
            var windowSize = 4f; // beats
            Queue<SwingData> dataWindowLeft = new();
            Queue<SwingData> dataWindowRight = new();
            List<EbpmData> rollingAverageLeft = new();
            List<EbpmData> rollingAverageRight = new();
            List<EbpmData> ReverseRollingAverageLeft = new();
            List<EbpmData> ReverseRollingAverageRight = new();
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();

            foreach (var swing in Swings)
            {
                EbpmData data = new();
                var clean = true;
                if (!swing.rightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        if (dataWindowLeft.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowLeft.Select(d => d.swingEBPM).Average();
                    rollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.swingEBPM).Average();
                    rollingAverageRight.Add(data);
                }
            }
            dataWindowLeft.Clear();
            dataWindowRight.Clear();
            for (int i = Swings.Count - 1; i >= 0; i--)
            {
                var swing = Swings[i];
                EbpmData data = new();
                var clean = true;
                if (!swing.rightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        if (dataWindowLeft.Peek().swingStartBeat > swing.swingStartBeat + windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowLeft.Select(d => d.swingEBPM).Average();
                    ReverseRollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().swingStartBeat > swing.swingStartBeat + windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.swingEBPM).Average();
                    ReverseRollingAverageRight.Add(data);
                }
            }

            foreach (var data in rollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in rollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            rollingAverageLeft.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageLeft.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);
            rollingAverageRight.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageRight.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);

            foreach (var data in rollingAverageLeft)
            {
                if (data.Flick)
                {
                    var note = data.Swing.notes.FirstOrDefault();
                    var index = cubes.FindIndex(c => c.Time == note.b && c.Type == note.c && note.x == c.Line && note.y == c.Layer);
                    var cube = cubes[index];
                    if (index < cubes.Count - 3)
                    {
                        if (cubes[index + 1].Time - cube.Time != cubes[index + 2].Time - cubes[index + 1].Time) continue;
                        //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                    }
                    else continue; //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                }
            }
            foreach (var data in rollingAverageRight)
            {
                if (data.Flick)
                {
                    var note = data.Swing.notes.FirstOrDefault();
                    var index = cubes.FindIndex(c => c.Time == note.b && c.Type == note.c && note.x == c.Line && note.y == c.Layer);
                    var cube = cubes[index];
                    if (index < cubes.Count - 3)
                    {
                        if (cubes[index + 1].Time - cube.Time != cubes[index + 2].Time - cubes[index + 1].Time) continue;
                        //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                    }
                    else continue; //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                }
            }
        }
    }
}

using Microsoft.ML.OnnxRuntime;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.AccAi
{
    // How a note is scored, matching the scoring types BeatLeader records in replays.
    public enum NoteKind
    {
        Normal = 0,
        ArcHead = 1,
        ArcTail = 2,
        ChainHead = 3,
        Combined = -1, // e.g. arc head + arc tail: no one-hot slot (all zeros), same as in training
    }

    public class MapNote
    {
        public double Time { get; }
        public int X { get; }
        public int Y { get; }
        public int Direction { get; }
        public int Color { get; }
        public double Njs { get; }
        public NoteKind Kind { get; }

        public MapNote(double time, int x, int y, int direction, int color, double njs, NoteKind kind)
        {
            Time = time;
            X = x;
            Y = y;
            Direction = direction;
            Color = color;
            Njs = njs;
            Kind = kind;
        }
    }

    internal class DataProcessing
    {
        public static int preSegmentSize = 12;
        public static int postSegmentSize = 12;
        public static int predictionSize = 8;

        public const int BaseNoteSize = 49;
        public const int NoteKindFeatures = 4;

        // Set from the ONNX model's input size (ConfigureFor):
        // 49 values per note = old model, 53 = model trained with the note-type features.
        public static bool UseNoteKindFeatures = true;

        public static int noteSize => BaseNoteSize + (UseNoteKindFeatures ? NoteKindFeatures : 0);
        public static int segmentSize => preSegmentSize + postSegmentSize + predictionSize;

        // Build inputs in the layout the model was trained with
        public static void ConfigureFor(InferenceSession session)
        {
            var dims = session.InputMetadata["input_1"].Dimensions;
            int size = dims[dims.Length - 1];
            if (size != BaseNoteSize && size != BaseNoteSize + NoteKindFeatures)
            {
                throw new InvalidOperationException($"model_sleep_bl.onnx expects {size} values per note; DataProcessing supports {BaseNoteSize} or {BaseNoteSize + NoteKindFeatures}.");
            }
            UseNoteKindFeatures = size == BaseNoteSize + NoteKindFeatures;
        }

        // Dictionaries for direction to angle and angle to direction
        private static readonly Dictionary<int, int> DirectionToAngle = new Dictionary<int, int>
        {
            {0, 180},
            {1, 0},
            {2, 90},
            {3, 270},
            {4, 135},
            {5, 225},
            {6, 45},
            {7, 315}
        };

        private static readonly Dictionary<int, int> AngleToDirection = new Dictionary<int, int>
        {
            {180, 0},
            {0, 1},
            {90, 2},
            {270, 3},
            {135, 4},
            {225, 5},
            {45, 6},
            {315, 7}
        };

        // Method to get the note direction
        public static int GetNoteDirection(int direction, double angle)
        {
            if (direction == 8)
            {
                return 8;
            }

            int noteAngle = (DirectionToAngle[direction] - (int)Math.Round(angle / 45) * 45) % 360;
            if (noteAngle < 0)
            {
                noteAngle += 360;
            }
            return AngleToDirection[noteAngle];
        }

        // Same matching as the game / Parser.Utils.ScoringExtensions: a note is an arc head or tail when an arc of the
        // same color starts or ends on it, and a chain head when a chain of the same color starts on it.
        public static NoteKind GetNoteKind(Note note, DifficultyV3 mapdata)
        {
            bool arcHead = mapdata.Arcs.Any(a => a.Color == note.Color && a.BpmTime == note.BpmTime && a.x == note.x && a.y == note.y);
            bool arcTail = mapdata.Arcs.Any(a => a.Color == note.Color && a.TailBpmTime == note.BpmTime && a.tx == note.x && a.ty == note.y);
            bool chainHead = mapdata.Chains.Any(c => c.Color == note.Color && c.BpmTime == note.BpmTime && c.x == note.x && c.y == note.y);

            int count = (arcHead ? 1 : 0) + (arcTail ? 1 : 0) + (chainHead ? 1 : 0);
            if (count > 1) return NoteKind.Combined;
            if (chainHead) return NoteKind.ChainHead;
            if (arcHead) return NoteKind.ArcHead;
            if (arcTail) return NoteKind.ArcTail;
            return NoteKind.Normal;
        }

        // Map notes in the order the model was trained on: time, then x, y, direction, color.
        // Chain links are not included (they are counted as free points), bombs are not notes.
        public static List<MapNote> GetMapNotesFromJson(DifficultyV3 mapdata, double bpm)
        {
            bool hasSliders = mapdata.Arcs.Any() || mapdata.Chains.Count > 0;
            return mapdata.Notes
                    .Where(n => n.x < 1000 && n.x >= 0 && n.y < 1000 && n.y >= 0)
                    .Where(n => n.Color == 0 || n.Color == 1)
                    .Select(n => new MapNote(
                        n.Seconds,
                        n.x,
                        n.y,
                        GetNoteDirection(n.CutDirection, n.AngleOffset),
                        n.Color,
                        n.njs,
                        hasSliders ? GetNoteKind(n, mapdata) : NoteKind.Normal
                    ))
                    .OrderBy(n => n.Time).ThenBy(n => n.X).ThenBy(n => n.Y).ThenBy(n => n.Direction).ThenBy(n => n.Color)
                    .ToList();
        }

        // Share of the in-game NJS change the model sees (see PreprocessMapNotes)
        public const double NJS_SPEED_SHARE = 0.75;

        public static List<double> PreprocessNote(double delta, double deltaOther, MapNote note, double njs, double timeScale)
        {
            delta /= timeScale;
            deltaOther /= timeScale;

            double deltaLong = Math.Max(0, 2 - delta) / 2;
            double deltaOtherLong = Math.Max(0, 2 - deltaOther) / 2;
            double deltaShort = Math.Max(0, 0.5 - delta) * 2;
            double deltaOtherShort = Math.Max(0, 0.5 - deltaOther) * 2;

            int colNumber = Math.Clamp(note.X, 0, 3);
            int rowNumber = Math.Clamp(note.Y, 0, 2);
            int directionNumber = Math.Clamp(note.Direction, 0, 9);
            int color = note.Color;

            double[] rowCol = new double[4 * 3];
            double[] direction = new double[10];

            double[] rowCol2 = new double[4 * 3];
            double[] direction2 = new double[10];

            rowCol[colNumber * 3 + rowNumber] = 1;
            direction[directionNumber] = 1;

            List<double> response = new List<double>(noteSize);

            if (color == 0)
            {
                response.AddRange(rowCol);
                response.AddRange(direction);
                response.AddRange(rowCol2);
                response.AddRange(direction2);
                response.Add(deltaShort);
                response.Add(deltaLong);
                response.Add(deltaOtherShort);
                response.Add(deltaOtherLong);
            }
            else
            {
                response.AddRange(rowCol2);
                response.AddRange(direction2);
                response.AddRange(rowCol);
                response.AddRange(direction);
                response.Add(deltaOtherShort);
                response.Add(deltaOtherLong);
                response.Add(deltaShort);
                response.Add(deltaLong);
            }

            response.Add(njs / 30);

            if (UseNoteKindFeatures)
            {
                double[] kind = new double[NoteKindFeatures];
                if (note.Kind != NoteKind.Combined) kind[(int)note.Kind] = 1;
                response.AddRange(kind);
            }

            return response;
        }

        public static Tuple<List<double[]>, List<double>> PreprocessMapNotes(List<MapNote> mapNotes, double timeScale, double njsMult = 1)
        {
            List<double[]> notes = new();
            List<double> noteTimes = new();

            double prevZeroNoteTime = 0;
            double prevOneNoteTime = 0;

            foreach (var note in mapNotes)
            {
                double noteTime = note.Time;
                // The model sees only part of the in-game NJS change (speed, times njsMult for the half-NJS BFS / BSF):
                // 75% (SFS x1.375, FS x1.15, SS x0.8875, BSF x1.1875, BFS x1.075).
                double njs = note.Njs * (1 + NJS_SPEED_SHARE * (njsMult * timeScale - 1));

                double deltaToZero = noteTime - prevZeroNoteTime;
                double deltaToOne = noteTime - prevOneNoteTime;

                if (note.Color == 0)
                {
                    prevZeroNoteTime = noteTime;
                    notes.Add(PreprocessNote(deltaToZero, deltaToOne, note, njs, timeScale).ToArray());
                }
                else
                {
                    prevOneNoteTime = noteTime;
                    notes.Add(PreprocessNote(deltaToOne, deltaToZero, note, njs, timeScale).ToArray());
                }
                noteTimes.Add(noteTime);
            }

            return new Tuple<List<double[]>, List<double>>(notes, noteTimes);
        }

        // Windows of preSegmentSize | predictionSize | postSegmentSize notes, stepping by predictionSize so every note
        // is predicted once. The last window is padded with empty notes (all zeros) instead of dropping the map's
        // last notes; the predictions for those padded slots are skipped.
        public List<List<double[]>> CreateSegments(List<double[]> notes)
        {
            var segments = new List<List<double[]>>();
            int size = noteSize;

            for (int i = 0; i < notes.Count; i += predictionSize)
            {
                var finalSegment = new List<double[]>(segmentSize);
                for (int j = i - preSegmentSize; j < i + predictionSize + postSegmentSize; j++)
                {
                    finalSegment.Add(j >= 0 && j < notes.Count ? notes[j] : new double[size]);
                }
                segments.Add(finalSegment);
            }

            return segments;
        }

        // Chain links score a fixed 20 points each. A chain's SliceCount includes its head, which is a regular note
        // already predicted by the model, so only SliceCount - 1 links are free points.
        public int GetFreePointsForMap(DifficultyV3 mapdata)
        {
            if (mapdata.Chains.Count == 0) return 0;

            int linkCount = 0;
            foreach (var burstSlider in mapdata.Chains)
            {
                linkCount += Math.Max(0, burstSlider.SliceCount - 1);
            }
            return linkCount * 20 * 8;
        }

        public (List<MapNote> mapNotes, int freePoints) GetMapData(DifficultyV3 mapdata, double bpm)
        {
            var mapNotes = GetMapNotesFromJson(mapdata, bpm);
            var freePoints = GetFreePointsForMap(mapdata);
            return (mapNotes, freePoints);
        }

        public (List<List<double[]>> segments, List<double> noteTimes, int freePoints) PreprocessMap(DifficultyV3 mapdata, double bpm, double timescale, double njsMult = 1)
        {
            var emptyResponse = (new List<List<double[]>>(), new List<double>(), 0);
            var (mapNotes, freePoints) = GetMapData(mapdata, bpm);
            if (mapNotes == null || mapNotes.Count == 0)
            {
                return emptyResponse;
            }

            var (notes, noteTimes) = PreprocessMapNotes(mapNotes, timescale, njsMult);
            List<List<double[]>> segments = CreateSegments(notes);
            return (segments, noteTimes, freePoints);
        }
    }
}

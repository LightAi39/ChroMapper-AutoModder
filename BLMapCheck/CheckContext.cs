using beatleader_analyzer;
using Analyzer.BeatmapScanner.Data;
using beatleader_analyzer.BeatmapScanner.Data;
using beatleader_parser.Timescale;
using BLMapCheck.Classes.AccAi;
using BLMapCheck.Classes.Results;
using BLMapCheck.Configs;
using Parser.Map;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck
{
    // Everything a single check run needs. A new context is created for each BLMapChecker call,
    // so nothing is shared between runs or between checker instances.
    internal class CheckContext
    {
        public BeatmapV3 Map { get; }
        public Config Config { get; }
        public Analyze Analyzer { get; }
        public CheckResults Results { get; } = new();
        // Folder the map was loaded from, null if it was loaded from memory
        public string FolderPath { get; set; }

        // Data of the difficulty currently being checked
        public Timescale Timescale { get; private set; }
        public List<NoteData> NotesData { get; set; } = new();
        // Slider precision used for the current difficulty, detected or from the config
        public double SliderPrecision { get; set; }
        // Valid BPM events of the current difficulty, sorted by beat
        private List<BpmEvent> bpmEvents = new();

        // The analyzer is the most expensive step, cache its result so each difficulty is only rated once per run
        private readonly Dictionary<DifficultyV3, Ratings> ratingCache = new();
        private readonly Dictionary<DifficultyV3, RatingResult> accCache = new();
        // The acc AI, shared, it loads the ONNX model once. Null if ONNX Runtime can't load
        private static readonly Lazy<Full> accAi = new(() =>
        {
            try { return new Full(); }
            catch (Exception) { return null; }
        });

        public CheckContext(BeatmapV3 map, Config config, Analyze analyzer)
        {
            Map = map;
            Config = config;
            Analyzer = analyzer;
        }

        // Difficulty that the next difficulty results will be attached to
        public void SetDifficulty(string characteristic, string difficulty, int difficultyRank)
        {
            Results.CurrentCharacteristic = characteristic;
            Results.CurrentDifficulty = difficulty;
            Results.CurrentDifficultyRank = difficultyRank;
        }

        // Timescale is only used for absolute conversions, the BPM at a beat comes from BpmAt
        public void SetBpmEvents(List<BpmEvent> events)
        {
            bpmEvents = events.Where(x => x.Bpm < 10000 && x.Bpm > 0).OrderBy(x => x.Beats).ToList();
            Timescale = Timescale.Create(Map.Info._beatsPerMinute, bpmEvents, Map.Info._songTimeOffset);
        }

        // BPM active at that beat, the song BPM is used until the first BPM event
        public float BpmAt(float beat)
        {
            float bpm = Map.Info._beatsPerMinute;
            foreach (var ev in bpmEvents)
            {
                if (ev.Beats > beat) break;
                bpm = ev.Bpm;
            }
            return bpm;
        }

        // Duration conversions using the BPM active at that beat
        public float SecondsToBeats(double seconds, float beat) => (float)(seconds * BpmAt(beat) / 60);
        public float BeatsToSeconds(double beats, float beat) => (float)(beats * 60 / BpmAt(beat));

        public Ratings GetRating(DifficultySet diffSet)
        {
            if (!ratingCache.TryGetValue(diffSet.Data, out var rating))
            {
                rating = Analyzer.GetRating(diffSet.Data, diffSet.Characteristic, diffSet.Difficulty, Map.Info._beatsPerMinute);
                ratingCache[diffSet.Data] = rating;
            }
            return rating;
        }

        // BeatLeader star and acc rating of a difficulty, as RatingAPI computes them with the acc AI. Null if it can't be rated
        public RatingResult GetAccRating(DifficultySet diffSet)
        {
            if (!accCache.TryGetValue(diffSet.Data, out var result))
            {
                var rating = GetRating(diffSet);
                var ai = accAi.Value;
                if (rating != null && ai != null)
                {
                    try { result = RatingsController.GetBLRatings(rating, ai, diffSet.Data, Map.Info._beatsPerMinute, 1); }
                    catch (Exception) { }
                }
                accCache[diffSet.Data] = result;
            }
            return result;
        }

        public double? GetStars(DifficultySet diffSet) => GetAccRating(diffSet)?.Stars;
    }
}

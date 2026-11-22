using beatleader_analyzer;
using Parser.Map.Difficulty.V3.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.AccAi
{
    // Same as RatingAPI's RatingsController (GetBLRatings and the speed modifier blends), without the web API. Keep in sync
    // with RatingAPI master.

    public class LackMapCalculation
    {
        public double PassRating { get; set; } = 0;
        public double TechRating { get; set; } = 0;
        // Sliders and curved sliders per swing (the per-map curve's slider tilt, CurveSettings.SliderTilt)
        public double SliderPercentage { get; set; } = 0;
        // Analyzer Ratings.HitDistance (used by the acc rating)
        public double HitDistance { get; set; } = 0;
        // Analyzer Ratings.HardSwingRatio (used by the acc rating)
        public double HardSwingRatio { get; set; } = 0;
        // Color notes (the per-map curve's low note nerf, CurveSettings.LowNoteMultiplier; 0 = unknown)
        public int NoteCount { get; set; } = 0;
        // First to last color note in real time (song time / the speed modifier), seconds: the length nerf
        // (CurveSettings.ShortFactor; 0 = unknown)
        public double NoteSpan { get; set; } = 0;
    }

    public class RatingResult
    {
        public double PredictedAcc { get; set; } = 0;
        public double AccRating { get; set; } = 0;
        public LackMapCalculation LackMapCalculation { get; set; } = new();
        public List<Point> PointList { get; set; } = new();
        // What a 96% score is worth, from these ratings and PointList (PpCalculator.ToStars)
        public double Stars { get; set; } = 0;
    }

    public class Point
    {
        public double x { get; set; } = 0;
        public double y { get; set; } = 0;

        public Point()
        {

        }

        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    internal static class RatingsController
    {
        // RatingAPI's modifier names by timescale: SS 0.85, FS 1.2, SFS 1.5. Other timescales count as SS (slower) or as a
        // speed-up (faster), so they get the same blends.
        public static string ModifierName(double timescale) =>
            timescale == 1 ? "none" : timescale < 1 ? "SS" : timescale == 1.2 ? "FS" : "SFS";

        // The ratings of a difficulty at timescale, as RatingAPI's Get returns them for that modifier: with a speed modifier,
        // nomod is rated too for the blends. Also the Analyzer's ratings at timescale (null when it can't rate the map).
        public static (RatingResult result, beatleader_analyzer.BeatmapScanner.Data.Ratings? ratings) Rate(Analyze analyzer, Full ai, DifficultyV3 mapdata, string characteristic, string difficulty, double bpm, double timescale)
        {
            var (result, ratings) = GetBLRatings(analyzer, ai, mapdata, characteristic, difficulty, bpm, timescale);
            string name = ModifierName(timescale);
            if (name == "none" || ratings == null) return (result, ratings);

            var results = new Dictionary<string, RatingResult>
            {
                ["none"] = GetBLRatings(analyzer, ai, mapdata, characteristic, difficulty, bpm, 1).result,
                [name] = result,
            };
            UseNomodHighPass(results);
            BlendLowNoteSpeeds(results);
            BlendLowStarSpeeds(results);
            return (results[name], ratings);
        }

        // Every speed modifier's acc rating again with the nomod pass rating in its high pass correction and buff
        // (AccRating.EffectiveAcc), and its stars, when AccRating.SpeedHighPassFromNomod is on. Needs the nomod ratings
        // ("none"); before BlendLowNoteSpeeds.
        public static void UseNomodHighPass(Dictionary<string, RatingResult> results)
        {
            if (!AccRating.SpeedHighPassFromNomod || !results.TryGetValue("none", out var nomod)) return;
            double nomodPass = nomod.LackMapCalculation.PassRating;
            var ar = new AccRating();
            foreach (var (name, result) in results)
            {
                if (name == "none" || !(result.PredictedAcc > 0)) continue;
                var lack = result.LackMapCalculation;
                result.AccRating = ar.GetRating(result.PredictedAcc, lack.PassRating, lack.TechRating, lack.HitDistance, lack.HardSwingRatio, nomodPass);
                result.Stars = PpCalculator.ToStars((float)result.AccRating, (float)lack.PassRating, (float)lack.TechRating, PpCalculator.ToCurve(result.PointList), (float)result.PredictedAcc);
            }
        }

        // The low note speed blend (CurveSettings.LowNoteSpeedBlend) on every speed modifier's pass, tech and acc ratings, and
        // their stars again. Needs the nomod ratings ("none").
        public static void BlendLowNoteSpeeds(Dictionary<string, RatingResult> results)
        {
            if (!results.TryGetValue("none", out var nomod)) return;
            int notes = nomod.LackMapCalculation.NoteCount;
            var settings = CurveSettings.Current;
            if (notes <= 0 || notes >= settings.LowNoteStart) return;
            foreach (var (name, result) in results)
            {
                if (name == "none") continue;
                var lack = result.LackMapCalculation;
                lack.PassRating = settings.LowNoteSpeedBlend(nomod.LackMapCalculation.PassRating, lack.PassRating, notes);
                lack.TechRating = settings.LowNoteSpeedBlend(nomod.LackMapCalculation.TechRating, lack.TechRating, notes);
                result.AccRating = settings.LowNoteSpeedBlend(nomod.AccRating, result.AccRating, notes);
                result.Stars = PpCalculator.ToStars((float)result.AccRating, (float)lack.PassRating, (float)lack.TechRating, PpCalculator.ToCurve(result.PointList), (float)result.PredictedAcc);
            }
        }

        // Low star speed blend: on low and mid star maps, the speed-up modifiers' (FS, SFS, BFS, BSF) predicted acc, pass and
        // tech are blended toward nomod's, and their acc rating, curve and stars computed again from them, so they pay less
        // at every accuracy. See RatingAPI's RatingsController for where the constants come from.
        public static double LowStarSpeedMidStars { get; set; } = 5.048;
        public static double LowStarSpeedWidth { get; set; } = 1.1218;
        public static double LowStarSpeedWidthAbove { get; set; } = 0.5609;
        public static double LowStarSpeedMinShare { get; set; } = 0.15;
        public static double LowStarSpeedMaxShare { get; set; } = 1;

        // The share of a speed modifier's difference from nomod kept on a map with these nomod stars
        public static double LowStarSpeedShare(double nomodStars)
        {
            double width = nomodStars < LowStarSpeedMidStars ? LowStarSpeedWidth : LowStarSpeedWidthAbove;
            return LowStarSpeedMinShare + (LowStarSpeedMaxShare - LowStarSpeedMinShare) / (1 + Math.Exp(-(nomodStars - LowStarSpeedMidStars) / width));
        }

        // The low star speed blend on every speed-up modifier. Needs the nomod ratings ("none"); after BlendLowNoteSpeeds.
        public static void BlendLowStarSpeeds(Dictionary<string, RatingResult> results)
        {
            if (!results.TryGetValue("none", out var nomod) || !(nomod.PredictedAcc > 0)) return;
            double share = LowStarSpeedShare(nomod.Stars);
            if (share >= 1) return;
            var ar = new AccRating();
            var curve = new Curve();
            var nomodLack = nomod.LackMapCalculation;
            foreach (var (name, result) in results)
            {
                if (name is not ("FS" or "SFS" or "BFS" or "BSF") || !(result.PredictedAcc > 0)) continue;
                var lack = result.LackMapCalculation;
                result.PredictedAcc = nomod.PredictedAcc + share * (result.PredictedAcc - nomod.PredictedAcc);
                lack.PassRating = nomodLack.PassRating + share * (lack.PassRating - nomodLack.PassRating);
                lack.TechRating = nomodLack.TechRating + share * (lack.TechRating - nomodLack.TechRating);
                result.AccRating = ar.GetRating(result.PredictedAcc, lack.PassRating, lack.TechRating, lack.HitDistance, lack.HardSwingRatio,
                    AccRating.SpeedHighPassFromNomod ? nomodLack.PassRating : null);
                result.PointList = curve.GetCurve(lack, result.PredictedAcc);
                result.Stars = PpCalculator.ToStars((float)result.AccRating, (float)lack.PassRating, (float)lack.TechRating, PpCalculator.ToCurve(result.PointList), (float)result.PredictedAcc);
            }
        }

        // First to last color note of a difficulty in real time: song seconds / timescale (LackMapCalculation.NoteSpan)
        public static double NoteSpan(DifficultyV3 mapdata, double timescale = 1)
        {
            if (mapdata.Notes.Count < 2 || timescale <= 0) return 0;
            return (mapdata.Notes.Max(n => n.Seconds) - mapdata.Notes.Min(n => n.Seconds)) / timescale;
        }

        public static (RatingResult result, beatleader_analyzer.BeatmapScanner.Data.Ratings? ratings) GetBLRatings(Analyze analyzer, Full ai, DifficultyV3 mapdata, string characteristic, string difficulty, double bpm, double timescale)
        {
            var ratings = analyzer.GetRating(mapdata, characteristic, difficulty, (float)bpm, (float)timescale);
            if (ratings == null) return (new(), null);
            return (GetBLRatings(ratings, ai, mapdata, bpm, timescale), ratings);
        }

        // From the Analyzer's ratings, already computed at timescale
        public static RatingResult GetBLRatings(beatleader_analyzer.BeatmapScanner.Data.Ratings ratings, Full ai, DifficultyV3 mapdata, double bpm, double timescale)
        {
            var lack = new LackMapCalculation
            {
                PassRating = ratings.PassRating,
                TechRating = ratings.TechRating,
                SliderPercentage = Curve.SliderPercentage(ratings),
                HitDistance = ratings.HitDistance,
                HardSwingRatio = ratings.HardSwingRatio,
                NoteCount = mapdata.Notes.Count,
                NoteSpan = NoteSpan(mapdata, timescale),
            };

            var predictedAcc = ai.GetAIAcc(mapdata, bpm, timescale);
            AccRating ar = new();
            var accRating = ar.GetRating(predictedAcc, ratings.PassRating, ratings.TechRating, ratings.HitDistance, ratings.HardSwingRatio);
            Curve curve = new();
            // Per-map curve (CurveSettings.Table by predicted acc, slider tilt); scores and stars use it
            var pointList = curve.GetCurve(lack, predictedAcc);
            RatingResult result = new()
            {
                Stars = PpCalculator.ToStars((float)accRating, (float)lack.PassRating, (float)lack.TechRating, PpCalculator.ToCurve(pointList), (float)predictedAcc),
                PredictedAcc = predictedAcc,
                AccRating = accRating,
                LackMapCalculation = lack,
                PointList = pointList,
            };
            return result;
        }
    }
}

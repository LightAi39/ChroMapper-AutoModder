using System;
using System.Collections.Generic;
using System.Linq;

namespace BLMapCheck.Classes.AccAi
{
    public class Curve
    {
        // The base acc curve (Pp.PpCalculator.CurveValue): acc pp = curve(acc) * AccWeight * acc rating
        public List<(double x, double y)> baseCurve = BaseTable.ToList();

        // Lifted at low acc (+20% up to 80%, fading to 0 at 92%): maps players score low on were worth less for them than
        // their other maps. Stars (a 96% score) and acc ratings (the curve at the effective acc, 95%+) don't move.
        // 100% at 7.2 (was 7.424): the last step was 1.64x the one before it, a slope jump of x1.33 at 99.9% in every curve.
        // 90-95% steeper ("Dev Acc Steeper 90-95" DBs), with a rising growth per 1% acc (4.5, 5.8, 7.0, 7.6, 8.0%: up to
        // 95-95.5%'s 8.0%) instead of the jump from 4.1% to 6.6% at 92% where the lift ended; 95% stays at 1, everything
        // under 90% x0.966 (the lift's shape kept). Stars and acc ratings don't move; top 1000 players' pp -0.2%. Before:
        //   (0.94, 0.931), (0.93, 0.867), (0.92, 0.813), (0.91, 0.781), (0.9, 0.753), (0.875, 0.699), (0.85, 0.649),
        //   (0.825, 0.605), (0.8, 0.568), (0.75, 0.485), (0.7, 0.414), (0.65, 0.355), (0.6, 0.307)
        public static readonly (double x, double y)[] BaseTable =
        {
                (1.0, 7.2),
                (0.999, 6.241),
                (0.9975, 5.158),
                (0.995, 4.010),
                (0.9925, 3.241),
                (0.99, 2.700),
                (0.9875, 2.303),
                (0.985, 2.007),
                (0.9825, 1.786),
                (0.98, 1.618),
                (0.9775, 1.490),
                (0.975, 1.392),
                (0.9725, 1.315),
                (0.97, 1.256),
                (0.965, 1.167),
                (0.96, 1.094),
                (0.955, 1.039),
                (0.95, 1.000),
                (0.94, 0.926),
                (0.93, 0.861),
                (0.92, 0.804),
                (0.91, 0.760),
                (0.9, 0.727),
                (0.875, 0.675),
                (0.85, 0.627),
                (0.825, 0.584),
                (0.8, 0.549),
                (0.75, 0.469),
                (0.7, 0.400),
                (0.65, 0.343),
                (0.6, 0.297),
                (0.0, 0.000)
        };

        // Base curve for every map (maps without a usable predicted acc)
        public List<Point> GetCurve(LackMapCalculation lackRatings)
        {
            double nerf = CurveSettings.Current.LowNoteMultiplier(lackRatings.NoteCount, lackRatings.NoteSpan);
            return BaseTable.Select(p => new Point(p.x, p.y * nerf)).ToList();
        }

        // Per-map acc curve: ModifiableCurve (from the AccCurvePerMap branch) with all its parameters (A, C, K, W, K2, P2)
        // read from CurveSettings.Table by the map's predicted acc:
        // - K, W shape the middle part (60-95%), K2, P2 the upper part (95% to A);
        // - above A, the high-pass extension L(x) leaves A with the curve's slope there, which then keeps growing (C < 0,
        //   still exponential) or fades (0 < C <= 1; 0 = straight line).
        // The parameters only act relative to the default ModifiableCurve: value = base table(x) * modified(x) / default(x)
        // (the table interpolated smoothly, SmoothBaseValue), so default parameters give exactly the base curve (the default
        // ModifiableCurve itself is 3-6% above the table at 97-99.9%). Above A, table / default is held at its value and
        // slope at A, so the top follows A and C only (PerMapValue). Never above the base curve. Only map data is used.
        public List<Point> GetCurve(LackMapCalculation lackRatings, double predictedAcc)
        {
            var curves = PerMapCurves(predictedAcc);
            double tilt = SliderTilt(lackRatings, predictedAcc);
            var settings = CurveSettings.Current;
            double nerf = settings.LowNoteMultiplier(lackRatings.NoteCount, lackRatings.NoteSpan);
            if (curves == null && tilt == 0)
            {
                return GetCurve(lackRatings);
            }
            double a = curves?.modified.A ?? 1.0;

            // Base table points, 0.5% steps from 60% to 95%, 0.05% steps from 95% to 100%, and A
            var xs = BaseTable.Select(p => p.x)
                .Concat(Enumerable.Range(0, 71).Select(i => 0.6 + i * 0.005))
                .Concat(Enumerable.Range(0, 101).Select(i => 0.95 + i * 0.0005))
                .Append(a)
                .Select(x => Math.Round(x, 6))
                .Where(x => x >= 0 && x <= 1)
                .Distinct()
                .OrderByDescending(x => x);

            double Raw(double x)
            {
                double value = curves == null ? BaseValue(x) : PerMapValue(curves.Value, x);
                if (tilt != 0)
                {
                    // Slider tilt: steeper around the predicted acc (unchanged there), up to SliderTiltRange away from it
                    double distance = Math.Clamp(x - predictedAcc, -settings.SliderTiltRange, settings.SliderTiltRange);
                    value *= Math.Exp(tilt * distance * 100);
                }
                return value;
            }
            return xs.Select(x => new Point(x, Raw(x) * nerf)).ToList();
        }

        // Sliders and curved sliders per swing of a rated map
        public static double SliderPercentage(global::beatleader_analyzer.BeatmapScanner.Data.Ratings ratings) =>
            ratings.SwingData.Count > 0 ? (ratings.Statistics.Sliders + ratings.Statistics.CurvedSliders) / (double)ratings.SwingData.Count : 0;

        // Slider tilt per 1% acc of a map's curve (0: none)
        private static double SliderTilt(LackMapCalculation lackRatings, double predictedAcc)
        {
            var settings = CurveSettings.Current;
            if (!settings.Enabled || settings.SliderTilt == 0 || predictedAcc <= 0.6 || predictedAcc >= 0.9995) return 0;
            return settings.SliderTilt * lackRatings.SliderPercentage;
        }

        // Value of the per-map curve (GetCurve above) at acc, computed directly instead of from the sampled points.
        // Used by the acc rating (AccRating.Curve). forRating: with the rows' RatingC instead of C (see CurveRow.RatingC).
        public static double PerMapValue(double predictedAcc, double acc, bool forRating = false)
        {
            var curves = PerMapCurves(predictedAcc, forRating);
            return curves == null ? BaseValue(acc) : PerMapValue(curves.Value, acc);
        }

        // The modified and default ModifiableCurve of a map, null when it gets the base curve. forRating: see PerMapValue.
        private static (ModifiableCurve modified, ModifiableCurve standard)? PerMapCurves(double predictedAcc, bool forRating = false)
        {
            var settings = CurveSettings.Current;
            if (!settings.Enabled || predictedAcc <= 0.6 || predictedAcc >= 0.9995)
            {
                return null;
            }
            var row = settings.ParamsAt(predictedAcc);
            if (row.IsDefault)
            {
                return null;
            }
            double a = Math.Min(Math.Max(row.A, 0.951), 1.0);
            double c = forRating ? row.RatingC ?? row.C : row.C;
            return (new ModifiableCurve { A = a, C = c, K = row.K, W = row.W, K2 = row.K2, P2 = row.P2 }, new ModifiableCurve());
        }

        // The base it scales is SmoothBaseValue: the linear table's slope jumps at every table point, which made per-map
        // curves bumpy above ~98.5%. Same values at the table points (96% included), up to 1.34% lower between them.
        // value = modified(x) x TableFactor(x), the table over the default ModifiableCurve. Above A, TableFactor is held at
        // its value at A, continued with its slope there (TopTableFactor): the table's last steps (99.75%, 99.9%, 100%) are
        // steeper than the default ModifiableCurve, so TableFactor sags to 0.930 at 99.7% and climbs back to 0.970 at 100%,
        // which made every top (meant as a straight line for C 0, or a smooth taper) flatten up to ~99.75% and bend up to 100%.
        private static double PerMapValue((ModifiableCurve modified, ModifiableCurve standard) curves, double x)
        {
            double baseValue = SmoothBaseValue(x);
            double a = curves.modified.A;
            double factor = a < 0.9995 && x > a ? TopTableFactor(curves.standard, a, x) : TableFactor(curves.standard, x);
            double value = factor > 0 ? curves.modified.GetValue(x) * factor : baseValue;
            return Math.Min(baseValue, value);
        }

        // The table over the default ModifiableCurve at x (0 where the default curve is 0)
        private static double TableFactor(ModifiableCurve standard, double x)
        {
            double reference = standard.GetValue(x);
            return reference > 0 ? SmoothBaseValue(x) / reference : 0;
        }

        // TableFactor above A: its value at A plus its slope there (from just under A) times the distance to A, so the curve
        // keeps the same value and slope at A
        private static double TopTableFactor(ModifiableCurve standard, double a, double x)
        {
            const double h = 1e-5;
            double atA = TableFactor(standard, a);
            double slope = (atA - TableFactor(standard, a - h)) / h;
            return atA + slope * (x - a);
        }

        public static double BaseValue(double acc)
        {
            int i = 0;
            for (; i < BaseTable.Length; i++)
            {
                if (BaseTable[i].x <= acc) break;
            }
            if (i == 0) i = 1;
            if (i >= BaseTable.Length) i = BaseTable.Length - 1;
            double middle_dis = (acc - BaseTable[i - 1].x) / (BaseTable[i].x - BaseTable[i - 1].x);
            return BaseTable[i - 1].y + middle_dis * (BaseTable[i].y - BaseTable[i - 1].y);
        }

        // The base table with monotone cubic interpolation (Fritsch-Carlson) instead of linear: through every table point,
        // with a continuous slope. Per-map curves only (PerMapValue); the base curve stays linear.
        public static double SmoothBaseValue(double acc)
        {
            var (xs, ys, slopes) = Smooth.Value;
            if (acc <= xs[0]) return ys[0];
            if (acc >= xs[^1]) return ys[^1];
            int i = 0;
            while (xs[i + 1] < acc) i++;
            double h = xs[i + 1] - xs[i];
            double t = (acc - xs[i]) / h;
            double t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * ys[i] + (t3 - 2 * t2 + t) * h * slopes[i]
                + (-2 * t3 + 3 * t2) * ys[i + 1] + (t3 - t2) * h * slopes[i + 1];
        }

        // Table points ascending and the slope at each (weighted harmonic mean of the neighbouring segments' slopes)
        private static readonly Lazy<(double[] xs, double[] ys, double[] slopes)> Smooth = new(() =>
        {
            var points = BaseTable.OrderBy(p => p.x).ToArray();
            int n = points.Length;
            double[] xs = points.Select(p => p.x).ToArray(), ys = points.Select(p => p.y).ToArray();
            double[] h = new double[n - 1], d = new double[n - 1], slopes = new double[n];
            for (int i = 0; i < n - 1; i++)
            {
                h[i] = xs[i + 1] - xs[i];
                d[i] = (ys[i + 1] - ys[i]) / h[i];
            }
            slopes[0] = d[0];
            slopes[n - 1] = d[n - 2];
            for (int i = 1; i < n - 1; i++)
            {
                if (d[i - 1] * d[i] <= 0) continue;
                double w1 = 2 * h[i] + h[i - 1], w2 = h[i] + 2 * h[i - 1];
                slopes[i] = (w1 + w2) / (w1 / d[i - 1] + w2 / d[i]);
            }
            return (xs, ys, slopes);
        });

        // From the AccCurvePerMap branch. Default curve parameters from Desmos https://www.desmos.com/calculator/hlhqzmtzon
        // Only GetF2 (to join the base curve at A) and the extension above A (GetL) are used here.
        public class ModifiableCurve
        {
            public double Y0 { get; set; } = 0.256;
            public double K { get; set; } = 4.86;
            public double W { get; set; } = 0.6;
            public double K2 { get; set; } = 2.88;
            public double P2 { get; set; } = 2.46;
            // Taper of the extension above A: 0 = keeps the slope at A (straight line), 1 = the slope fades out (flattens)
            public double C { get; set; } = 1.0;
            // Where the extension starts
            public double A { get; set; } = 1.0;

            public double GetValue(double x)
            {
                if (x <= 0.60) return (Y0 / 0.60) * x;
                if (x <= 0.95) return GetF1(x);
                if (x <= A) return GetF2(x);
                return GetL(x);
            }

            // Slope at x = 0.95
            public double GetM()
            {
                double term1 = (1.0 - Y0) / 0.35;
                double term2 = (1.0 - W) + W * (K * Math.Exp(K)) / (Math.Exp(K) - 1.0);
                return term1 * term2;
            }

            // 0.60 < x <= 0.95
            public double GetF1(double x)
            {
                double u = (x - 0.6) / 0.35;
                double blend = (1.0 - W) * u + W * (Math.Exp(K * u) - 1.0) / (Math.Exp(K) - 1.0);
                return Y0 + (1.0 - Y0) * blend;
            }

            // 0.95 < x <= A
            public double GetF2(double x)
            {
                double m = GetM();
                double u2 = (x - 0.95) / 0.05;
                double expTerm = (Math.Exp(K2 * Math.Pow(u2, P2)) - 1.0) / (Math.Exp(K2) - 1.0);
                return 1.0 + m * (x - 0.95) + (6.424 - 0.05 * m) * expTerm;
            }

            // High-pass extension above A: starts with the slope S at A, which decays towards S * (1 - C)
            public double GetL(double x)
            {
                double s = GetS();
                double s1 = s * (1.0 - C);
                double f2A = GetF2(A);

                if (Math.Abs(A - 1.0) < 1e-9)
                {
                    return f2A + s1 * (x - A);
                }

                double expTerm = 1.0 - Math.Exp(-(x - A) / (1.0 - A));
                return f2A + s1 * (x - A) + (s - s1) * (1.0 - A) * expTerm;
            }

            // Slope at x = A
            public double GetS()
            {
                double m = GetM();
                double uA = (A - 0.95) / 0.05;
                double uAPower = uA > 0 ? Math.Pow(uA, P2) : 0;
                double uAPowerMinus1 = uA > 0 ? Math.Pow(uA, P2 - 1.0) : 0;
                double expTerm = Math.Exp(K2 * uAPower) / (Math.Exp(K2) - 1.0);
                double derivativeFactor = (K2 * P2 / 0.05) * uAPowerMinus1;
                return m + (6.424 - 0.05 * m) * expTerm * derivativeFactor;
            }
        }
    }

    // One row of CurveSettings.Table: ModifiableCurve parameters for a predicted acc
    public class CurveRow
    {
        public double PredictedAcc { get; set; }
        public double A { get; set; } = 1.0;
        public double C { get; set; } = 0;
        public double K { get; set; } = 4.86;
        public double W { get; set; } = 0.6;
        public double K2 { get; set; } = 2.88;
        public double P2 { get; set; } = 2.46;
        // The C the acc rating reads (AccRating.Curve, at the predicted acc), null: C. Lets a row flatten its curve above A
        // for scores without raising the acc rating of maps predicted above A (curve^-0.8222 there).
        public double? RatingC { get; set; }

        // A at 100% and the default shape: the base curve
        public bool IsDefault => A >= 0.9995 && K == 4.86 && W == 0.6 && K2 == 2.88 && P2 == 2.46;
    }

    // Settings of the per-map curves (Curve.GetCurve): ModifiableCurve parameters by predicted acc, linearly interpolated
    // between the rows; the first / last row applies below / above them.
    // 1. 9-12* maps (predicted 96-97.5%) gave too much for scores about +1% above the predicted acc: K, W, K2, P2 reshape
    // the curve around 96%, easing back to the default shape by 97.5-98.5%.
    // 2. True acc (<2*) and acc maps (<5*) gave too much for tiny jumps around 99%: less steep there, hugging the base curve
    // while cutting the extreme tail. The 99.5% row keeps the 99% row from applying above 99%, which would cut the curve
    // at <2* maps' predicted acc far more.
    // 3. The 99% and 99.5% rows' C (0: a straight line above 99%) flattens their curve's top, which PpFormula's acc top
    // compression (x0.8 above 99% on every map) did before. Their RatingC keeps the acc rating on the old top (C -1.2):
    // read at the predicted acc, the flatter top raised the acc rating of maps predicted above 99% by up to x1.10, and the
    // easiest acc maps' 99.5%+ scores with it.
    // 4. The 0.975 and 0.980 rows (7-8 star maps and speed versions rated there) start their top at 98.3% instead of 98.5%,
    // as a straight line (C 0): the old mid star top bonus (PpFormula.MidStarTopBonus x1.04 from 96% acc, since reworked) pushed their 98.7%+ scores past 800pp
    // (Let Mom Sleep Hard SF 834). The acc rating reads the same curve: unchanged up to ~98.1% predicted acc (it reads the
    // curve at the predicted acc + ACC_OFFSET, under A), slightly higher (up to x1.003) at 98.2-98.8%, where it reads the
    // new top. The 0.985 row's C -1.2 -> -0.6 (between its neighbours' 0): at -1.2, maps predicted 98.3-98.5% paid more for
    // 99.75-100% (relative to a score at their predicted acc) than the harder 98% maps next to them.
    public class CurveSettings
    {
        public bool Enabled { get; set; } = true;

        public List<CurveRow> Table { get; set; } = new()
        {
            new CurveRow { PredictedAcc = 0.955, A = 1.0,     C = -5.30, K = 4.86, W = 0.60, K2 = 2.88, P2 = 2.46 },
            new CurveRow { PredictedAcc = 0.960, A = 0.99,    C = -3.00, K = 4.30, W = 0.66, K2 = 4.80, P2 = 1.85 },
            new CurveRow { PredictedAcc = 0.965, A = 0.988,   C = -2.40, K = 4.50, W = 0.64, K2 = 4.50, P2 = 1.95 },
            new CurveRow { PredictedAcc = 0.970, A = 0.986,   C = -2.20, K = 4.70, W = 0.62, K2 = 4.20, P2 = 2.05 },
            new CurveRow { PredictedAcc = 0.975, A = 0.983,   C = 0.00,  K = 4.86, W = 0.60, K2 = 3.80, P2 = 2.15 },
            new CurveRow { PredictedAcc = 0.980, A = 0.983,   C = 0.00,  K = 4.86, W = 0.60, K2 = 3.35, P2 = 2.30 },
            new CurveRow { PredictedAcc = 0.985, A = 0.985,   C = -0.60, K = 4.86, W = 0.60, K2 = 2.88, P2 = 2.46 },
            new CurveRow { PredictedAcc = 0.990, A = 0.99,    C = 0.00,  K = 4.86, W = 0.60, K2 = 2.88, P2 = 2.46, RatingC = -1.20 },
            new CurveRow { PredictedAcc = 0.995, A = 0.99,    C = 0.00,  K = 4.86, W = 0.60, K2 = 2.88, P2 = 2.46, RatingC = -1.20 },
        };

        // C never goes above 1 (the curve would go down)
        public double MaxC { get; set; } = 1;

        // Slider tilt: maps with many sliders (and curved sliders) were worth more than others of their stars, while their
        // top scores sit as close to the predicted acc as on other maps. Their curve is multiplied by
        // e^(SliderTilt x slider share x (acc - predicted acc) in %), up to SliderTiltRange from the predicted acc: higher
        // above it, lower under it, unchanged at it (the acc rating doesn't use it; the stars do, as scores at 96%). 0 = off.
        public double SliderTilt { get; set; } = 0.2;
        public double SliderTiltRange { get; set; } = 0.015;

        // Low note and length nerf: maps with few notes and short maps are worth too much for how easily they're grinded to
        // 100%. The whole curve (so it stays smooth) is multiplied by LowNoteMultiplier = LowNoteFactor x ShortFactor, each 1
        // at most: one of them nerfs a bit, both nerf more, nothing is buffed. Speed modifiers don't raise low note maps'
        // ratings (LowNoteSpeedBlend), and their shorter real time makes the ShortFactor stronger. Pass and tech pp aren't
        // nerfed.
        // Notes: 1 - (1 - LowNoteRefMultiplier) x ln(LowNoteStart / notes) / ln(LowNoteStart / LowNoteRefNotes), at least
        // LowNoteMinMultiplier, under LowNoteStart color notes (LackMapCalculation.NoteCount; 0 = unknown: 1). LowNoteStart
        // 0 = off.
        public double LowNoteStart { get; set; } = 115;
        public double LowNoteRefNotes { get; set; } = 20;
        // 0.9654: Quiet Water Easy (20 notes, 18 s) pays 600pp for a 100% (99%+ rows' C 0 and RatingC -1.2, base 100% at 7.2).
        // 0.8031: 500pp (586 with 0.9654 on StarDivisor 55.26; the map's stars 2.29 -> about 1.95, so its low star bonus rises).
        public double LowNoteRefMultiplier { get; set; } = 0.8031;
        public double LowNoteMinMultiplier { get; set; } = 0.8031;
        // Length: 1 - (1 - ShortRefMultiplier) x ln(ShortStart / span) / ln(ShortStart / ShortRefSpan), at least
        // ShortMinMultiplier, under ShortStart seconds (ShortRefSpan 12 s: SF on an 18 s map, so the speeds stay apart above
        // it). The span is the first to the last color note in real time (song time / the speed modifier's,
        // LackMapCalculation.NoteSpan; 0 = unknown: 1), so FS and SF, shorter grinds, are nerfed more and SS less.
        // ShortStart 0 = off.
        public double ShortStart { get; set; } = 75;
        public double ShortRefSpan { get; set; } = 12;
        public double ShortRefMultiplier { get; set; } = 0.9;
        public double ShortMinMultiplier { get; set; } = 0.9;

        // The whole curve's multiplier (notes: color notes; span: the first to the last of them in real time, in seconds;
        // 0 = unknown)
        public double LowNoteMultiplier(int notes, double span = 0) => LowNoteFactor(notes) * ShortFactor(span);

        public double LowNoteFactor(int notes)
        {
            if (LowNoteStart <= 0 || notes <= 0 || notes >= LowNoteStart || LowNoteRefNotes >= LowNoteStart) return 1;
            double nerf = (1 - LowNoteRefMultiplier) * Math.Log(LowNoteStart / notes) / Math.Log(LowNoteStart / LowNoteRefNotes);
            return Math.Clamp(1 - nerf, LowNoteMinMultiplier, 1);
        }

        public double ShortFactor(double span)
        {
            if (ShortStart <= 0 || span <= 0 || span >= ShortStart || ShortRefSpan >= ShortStart) return 1;
            double nerf = (1 - ShortRefMultiplier) * Math.Log(ShortStart / span) / Math.Log(ShortStart / ShortRefSpan);
            return Math.Clamp(1 - nerf, ShortMinMultiplier, 1);
        }

        // Low note speed blend: under LowNoteStart color notes, a speed modifier's pass, tech and acc ratings keep (notes /
        // LowNoteStart)^2 of their difference to the nomod ones (RatingsController.BlendLowNoteSpeeds, RatingsRefresh.Refresh):
        // speed makes these maps faster to grind, not harder, so it shouldn't raise their ratings, and the ShortFactor then
        // gives the faster modifiers less. Squared so SF doesn't still pay more than nomod. notes: color notes (0 = unknown:
        // unchanged).
        public double LowNoteSpeedBlend(double nomod, double modifier, int notes)
        {
            if (notes <= 0 || LowNoteStart <= 0 || notes >= LowNoteStart) return modifier;
            double kept = notes / LowNoteStart;
            return nomod + (modifier - nomod) * kept * kept;
        }

        public CurveRow ParamsAt(double predictedAcc)
        {
            var rows = Table.OrderBy(r => r.PredictedAcc).ToList();
            if (rows.Count == 0) return new CurveRow { PredictedAcc = predictedAcc };
            CurveRow Clamp(CurveRow r) => new CurveRow { PredictedAcc = predictedAcc, A = r.A, C = Math.Min(r.C, MaxC), K = r.K, W = r.W, K2 = r.K2, P2 = r.P2,
                RatingC = Math.Min(r.RatingC ?? r.C, MaxC) };
            if (predictedAcc <= rows[0].PredictedAcc) return Clamp(rows[0]);
            for (int i = 1; i < rows.Count; i++)
            {
                if (predictedAcc <= rows[i].PredictedAcc)
                {
                    var p = rows[i - 1]; var n = rows[i];
                    double t = (predictedAcc - p.PredictedAcc) / (n.PredictedAcc - p.PredictedAcc);
                    double L(double x, double y) => x + t * (y - x);
                    return Clamp(new CurveRow { A = L(p.A, n.A), C = L(p.C, n.C), K = L(p.K, n.K), W = L(p.W, n.W), K2 = L(p.K2, n.K2), P2 = L(p.P2, n.P2),
                        RatingC = L(p.RatingC ?? p.C, n.RatingC ?? n.C) });
                }
            }
            return Clamp(rows[rows.Count - 1]);
        }

        // A and C only (Leaderboard page)
        public (double a, double c) At(double predictedAcc)
        {
            var row = ParamsAt(predictedAcc);
            return (row.A, row.C);
        }

        public static CurveSettings Current { get; set; } = new();
    }
}

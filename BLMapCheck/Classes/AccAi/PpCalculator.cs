using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BLMapCheck.Classes.AccAi
{
    // Same as RatingAPI's Pp.PpCalculator, the parts the stars need
    public static class PpCalculator
    {
        // Stars are what a score at this accuracy is worth
        public const float StarAccuracy = 0.96f;

        // Value of a map's acc curve at acc (points from 100% down, see Curve.GetCurve). Null or fewer than 2 points: the
        // base curve.
        public static float CurveValue(float acc, IReadOnlyList<Vector2>? curve)
        {
            if (curve == null || curve.Count < 2)
            {
                return CurveValue(acc);
            }

            // The first point at or below acc (the points go down from 100%): binary search
            int i = 0, hi = curve.Count;
            while (i < hi)
            {
                int mid = (i + hi) >> 1;
                if (curve[mid].X <= acc) hi = mid;
                else i = mid + 1;
            }

            if (i == 0) {
                i = 1;
            }
            if (i >= curve.Count) {
                i = curve.Count - 1;
            }

            double middle_dis = (acc - curve[i - 1].X) / (double)(curve[i].X - curve[i - 1].X);
            return (float)(curve[i - 1].Y + middle_dis * (curve[i].Y - curve[i - 1].Y));
        }

        // Value of the base curve (Curve.BaseTable) at acc
        public static float CurveValue(float acc) => (float)Curve.BaseValue(acc);

        // Curve.GetCurve's points as CurveValue reads them
        public static List<Vector2> ToCurve(IEnumerable<Point>? points) =>
            points?.Select(p => new Vector2((float)p.x, (float)p.y)).ToList() ?? new List<Vector2>();

        // Pass, acc and tech pp at accuracy, before PpMultiplier. curve: the map's acc curve (null: the base curve). stars: the
        // map's, for the acc curve power (PpFormula.CurvePowerAt; null: AccCurvePower).
        // predictedAcc: the map's, for the acc relative pass and tech pp (0: off).
        public static (float pass, float acc, float tech) GetPp(float accuracy, float accRating, float passRating, float techRating, IReadOnlyList<Vector2>? curve = null, float? stars = null, float predictedAcc = 0)
        {
            var formula = PpFormula.Current;
            double power = stars is float s ? formula.CurvePowerAt(s) : formula.AccCurvePower;
            return GetPp(accuracy, accRating, passRating, techRating, curve, power, power != 1 ? CurveValue(StarAccuracy, curve) : 0, predictedAcc);
        }

        // GetPp with the acc curve power and the curve's value at StarAccuracy given
        private static (float pass, float acc, float tech) GetPp(float accuracy, float accRating, float passRating, float techRating, IReadOnlyList<Vector2>? curve, double power, float starCurveValue, float predictedAcc)
        {
            var formula = PpFormula.Current;
            float curveValue = CurveValue(accuracy, curve);
            if (power != 1) curveValue = formula.AccCurve(curveValue, starCurveValue, power);
            return (formula.PassPp(passRating) * formula.PassAccMultiplier(accuracy, predictedAcc), formula.AccPp(curveValue, accRating),
                formula.TechPp(accuracy, techRating));
        }

        // Stars: what a StarAccuracy score without modifiers is worth (ScorePp's pp, with the map's curve and the low star
        // bonus), on the star scale (PpFormula.StarsFromPp). With a speed modifier's ratings and curve: that modifier's stars.
        public static float ToStars(float accRating, float passRating, float techRating, IReadOnlyList<Vector2>? curve = null, float predictedAcc = 0) =>
            ToStars(StarAccuracy, accRating, passRating, techRating, curve, predictedAcc);

        // Plugin only: ToStars with a score at accuracy instead of StarAccuracy (the Star Calc setting)
        public static float ToStars(float accuracy, float accRating, float passRating, float techRating, IReadOnlyList<Vector2>? curve, float predictedAcc)
        {
            var formula = PpFormula.Current;
            (float passPP, float accPP, float techPP) = GetPp(accuracy, accRating, passRating, techRating, curve, predictedAcc: predictedAcc);
            return formula.StarsFromPp(passPP + accPP + techPP);
        }
    }
}

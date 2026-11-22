using System;

namespace BLMapCheck.Classes.AccAi
{
    public class AccRating
    {
        // acc rating = ACC_SCALE / Curve(EffectiveAcc(predictedAcc, pass, hit distance) + ACC_OFFSET), linear (no power to look
        // like prod's).
        // portaBLe's balance tuner (Balance Tuning page) suggests ACC_OFFSET; ACC_SCALE is its reference. portaBLe's
        // ReplayUtils.AccRating uses this class, so change them here only.
        // ACC_SCALE 14.8727: the same median acc rating (8.765) as with the old power 0.8222 (13.5382 with it).
        public const double ACC_SCALE = 14.8727;
        public const double ACC_OFFSET = 0.0019;

        // The corrections' constants below are settable for experiments (portaBLe reacc); change their defaults here.
        // Hard map correction. Below HARD_MAP_KNEE predicted acc, top players fall further behind the Acc AI's prediction
        // the harder the map gets, beyond the smooth trend that holds from 96.8% to 99%.
        // The acc rating uses a predicted acc lowered by HARD_MAP_SLOPE times the distance below the knee, capped at
        // HARD_MAP_FLOOR (the edge of the fitted data). Map data only at rating time: the constants were fitted offline on
        // scores (Refresh/Tools/hard_map_offset_fit.py). The stored predicted acc and the per-map curves are unchanged.
        // HARD_MAP_SLOPE 0 = off.
        public static double HARD_MAP_KNEE { get; set; } = 0.9675;
        public static double HARD_MAP_SLOPE { get; set; } = 1.24;
        public static double HARD_MAP_FLOOR { get; set; } = 0.95;

        // Movement and high pass corrections. The Acc AI predicts the accuracy of the notes players cut (its targets leave
        // misses and bad cuts out), and where the hands travel far, or the pass is very high, players lose more to misses and
        // bad cuts. Fitted on the top 300 players' scores after removing each player's own level: HIT_DISTANCE_SLOPE acc per
        // meter of mean hit distance (Analyzer Ratings.HitDistance) above HIT_DISTANCE_KNEE (the median map), and
        // HIGH_PASS_SLOPE per pass point above HIGH_PASS_KNEE.
        // The hit distance one is phased out between 97% and 97.5% predicted acc: the acc rating is much more sensitive to
        // the effective acc on easier maps (steeper curve), so applied everywhere it overpaid easy maps with a lot of
        // movement. Hit distance is capped at 0.8 m, where the shortfall stops growing (extreme spacing got too much).
        // No tech term: tech pp already pays for the acc players lose on tech maps.
        public static double HIT_DISTANCE_KNEE { get; set; } = 0.62;
        public static double HIT_DISTANCE_SLOPE { get; set; } = 0.030;
        public static double HIT_DISTANCE_CAP { get; set; } = 0.80;
        public static double HIT_DISTANCE_FULL_BELOW { get; set; } = 0.97;
        public static double HIT_DISTANCE_NONE_ABOVE { get; set; } = 0.975;
        // Share of the hit distance correction kept above HIT_DISTANCE_NONE_ABOVE (0: phased out entirely). 0.2: on the P75
        // worth, easy maps (97.5%+) over 0.7 m were 1.1% under the others with none (-0.0111 -> -0.0009 with 0.2; 0.4 and 0.8
        // overshot to +0.009 / +0.028)
        public static double HIT_DISTANCE_EASY_SHARE { get; set; } = 0.2;
        public static double HIGH_PASS_KNEE { get; set; } = 14.52;
        public static double HIGH_PASS_SLOPE { get; set; } = 0.00214;

        // Very high pass buff, on top of the high pass correction: HIGH_PASS_BUFF less effective acc from HIGH_PASS_BUFF_FULL
        // pass, ramped in linearly from HIGH_PASS_BUFF_START (no step at the threshold; flat above, so the 17 pass maps don't
        // gain more than the 14.5 ones). Each score's pp relative to the player's top pp was ~4% under maps of the same stars
        // on pass 14+ maps at every rank range (weighted worth hid it); their best scores sit under the predicted acc where
        // other maps' are over. HIGH_PASS_BUFF 0 = off. 0.0025 (was 0.0055): on the P75 worth pass 14+ maps were 1.4-2.3%
        // over others of their stars with 0.0055 (0.003: +0.2-0.9%, 0.001 overshot to -0.7-1.1%).
        public static double HIGH_PASS_BUFF_START { get; set; } = 13.5;
        public static double HIGH_PASS_BUFF_FULL { get; set; } = 14.5;
        // 0.001 with the rotation^1.5 / soft capped strain tech and its tech refit (pass 14.5+ +1.8-1.9% over with 0.0025, +0.9-1.0%)
        // 0 (off) with the parity model and the raw ratings: on the P75 worth pass 13.5+ maps (44) were about +0.6% over others
        // of their stars with 0.001, about +0.2% without (13.5-14.5 +0.9 -> +0.6%, 14.5-15.5 -0.2 -> -1.0%, 15.5+ +1.3 -> +0.6%,
        // small samples).
        public static double HIGH_PASS_BUFF { get; set; } = 0;

        // On: speed modifiers (SS / FS / SF) take the high pass correction and buff from the map's nomod pass rating (both were
        // fitted on nomod scores; FS's ~1.2x pass puts 136 maps into the buff that their nomod pass doesn't reach, FS acc
        // ratings up to x1.26 nomod's where x1.10 is typical). Off: from their own pass rating. Read by RatingsController
        // (UseNomodHighPass), RatingsRefresh.Refresh and the balance tuner.
        public static bool SpeedHighPassFromNomod { get; set; } = false;

        // The predicted acc the acc rating uses (see the corrections above). passRating: the pass rating; hitDistance:
        // Ratings.HitDistance. 0 leaves that correction out. highPassRating: the pass rating of the high pass correction and
        // buff (a speed modifier's: the nomod one; null: passRating).
        public static double EffectiveAcc(double predictedAcc, double passRating = 0, double hitDistance = 0, double? highPassRating = null)
        {
            double highPass = highPassRating ?? passRating;
            return predictedAcc - HARD_MAP_SLOPE * Math.Max(0, HARD_MAP_KNEE - Math.Max(predictedAcc, HARD_MAP_FLOOR))
                - HIT_DISTANCE_SLOPE * Math.Max(0, Math.Min(hitDistance, HIT_DISTANCE_CAP) - HIT_DISTANCE_KNEE)
                    * Math.Max(HIT_DISTANCE_EASY_SHARE, Math.Clamp((HIT_DISTANCE_NONE_ABOVE - predictedAcc) / (HIT_DISTANCE_NONE_ABOVE - HIT_DISTANCE_FULL_BELOW), 0, 1))
                - HIGH_PASS_SLOPE * Math.Max(0, highPass - HIGH_PASS_KNEE)
                - HIGH_PASS_BUFF * Math.Clamp((highPass - HIGH_PASS_BUFF_START) / (HIGH_PASS_BUFF_FULL - HIGH_PASS_BUFF_START), 0, 1);
        }

        // Hard swing correction: maps whose hardest swings are scattered over the map (Analyzer Ratings.HardSwingRatio above 1)
        // were worth less than others of their stars, most on pass 11+ maps: the pass rating averages the hardest windows, so
        // isolated hard swings count little, and the Acc AI predicts the accuracy of cut notes, so the misses and bad cuts on
        // them don't count either. The acc rating is multiplied by 1 + HARD_SWING_SLOPE x the excess of (ratio - 1) x pass
        // over HARD_SWING_KNEE, up to HARD_SWING_CAP. A multiplier, not a lower effective acc: the same acc pp change at
        // every predicted acc (the curve is much steeper on easy maps). A steeper slope over-concentrates top plays on the
        // raised maps. One hand maps (ratio 0) aren't changed. HARD_SWING_SLOPE 0 = off. HARD_SWING_KNEE 1 (was 2): on the P75
        // worth the maps just past the knee were still 0.3-0.7% under (pass 9+: corrected -0.0073 against +0.0052 without); with
        // 1 and the lower TechAccExponent both are about 0, and the 700pp+ scores spread over more maps (447 -> 484, at most 26
        // -> 15 on one map). A slope of 0.03 instead left the maps just past the knee under.
        // Slope and cap x1.2163 (were 0.02 and 0.12 with acc pp following the acc rating to the power 1.2163): the same pp.
        public static double HARD_SWING_KNEE { get; set; } = 1.0;
        public static double HARD_SWING_SLOPE { get; set; } = 0.0243;
        public static double HARD_SWING_CAP { get; set; } = 0.148;

        // The hard swing correction's multiplier of the acc rating. hardSwingRatio: Ratings.HardSwingRatio (0: none)
        public static double HardSwingMultiplier(double passRating, double hardSwingRatio) =>
            hardSwingRatio > 0
                ? 1 + Math.Min(HARD_SWING_SLOPE * Math.Max(0, (hardSwingRatio - 1) * passRating - HARD_SWING_KNEE), HARD_SWING_CAP)
                : 1;

        // highPassRating: see EffectiveAcc (a speed modifier's: the nomod pass rating)
        public double GetRating(double? predictedAcc, double? passRating, double? techRating, double hitDistance = 0, double hardSwingRatio = 0, double? highPassRating = null)
        {
            double difficulty_to_acc;
            if (predictedAcc > 0)
            {
                double effective = EffectiveAcc(predictedAcc ?? 0, passRating ?? 0, hitDistance, highPassRating);
                difficulty_to_acc = ACC_SCALE / Curve(predictedAcc ?? 0, effective + ACC_OFFSET)
                    * HardSwingMultiplier(passRating ?? 0, hardSwingRatio);
            }
            else
            {
                // No predicted acc: prod's old formula, then from its scale (13.5382 x curve^-0.8222) to the linear one
                double tiny_tech = 0.0208f * (techRating ?? 0) + 1.1284f;
                double old = (-Math.Pow(tiny_tech, -(passRating ?? 0)) + 1) * 8 + 2 + 0.01f * (techRating ?? 0) * (passRating ?? 0);
                difficulty_to_acc = old > 0 ? ACC_SCALE * Math.Pow(old / 13.5382, 1 / 0.8222) : old;
            }
            if (double.IsInfinity(difficulty_to_acc) || double.IsNaN(difficulty_to_acc) || double.IsNegativeInfinity(difficulty_to_acc))
            {
                difficulty_to_acc = 0;
            }
            return difficulty_to_acc;
        }

        // The map's own per-map acc curve (ModifiableCurve, see Curve.GetCurve), picked by its predicted acc, at acc; with the
        // curve rows' RatingC (CurveRow.RatingC)
        public static float Curve(double predictedAcc, double acc) => (float)AccAi.Curve.PerMapValue(predictedAcc, acc, forRating: true);
    }
}

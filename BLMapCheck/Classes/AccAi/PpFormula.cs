using System;

namespace BLMapCheck.Classes.AccAi
{
    // The pp formula's parameters (used by PpCalculator.GetPp, ToStars, ScorePp). PpFormula.Current is what PpCalculator
    // uses; portaBLe's formula sweeps (BalanceTuner.RunPpFormula) swap it temporarily to try other values.
    //   pass pp = PassScale * e^(PassInner * pass^(1/PassExponent)) - PassOffset   (0 when negative; above PassTopKnee
    //             each point counts PassTopSlope)
    //   acc pp  = curve(acc) * AccWeight * accRating   (the map's own curve, Curve.GetCurve), x MidStarTopMultiplier in
    //             scores (4-10.5 stars, from the predicted acc - 3%; not in stars)
    //   tech pp = e^(TechAccExponent * acc) * TechWeight * tech   (above TechTopKnee each point counts TechTopSlope)
    //   pp      = (pass + acc + tech) * PpMultiplier   (Scale)
    //   pp      = pp * (1 + LowStarBonus * (LowStarBonusEnd - stars)^LowStarBonusPower)   (only under LowStarBonusEnd stars)
    //   stars   = final pp of a 96% score / PpMultiplier / StarDivisor, low star bonus included (StarsFromPp). PpMultiplier
    //             doesn't move stars. Linear: no power to look like prod's (stars were 1.1653 x (pp / 83.6925)^1.17507; the
    //             star thresholds below were converted from those, new = (old / 1.1653)^(1 / 1.17507) x 83.6925 / StarDivisor,
    //             so pp is about the same).
    public class PpFormula
    {
        public double PassScale { get; set; } = 12.3;
        public double PassExponent { get; set; } = 2.62;
        public double PassOffset { get; set; } = 30;
        // 1.4862^(-1 / 2.62): the pass formula was written for a pass 1.4862 times smaller (before the pass display curve
        // was folded into AnalyzeMap.PASS_CALIBRATION_FACTOR), so the same map gets the same pass pp
        public double PassInner { get; set; } = 0.85966;
        // Acc and tech pp are linear in the acc and tech ratings, which are linear themselves (no power to look like prod's).
        // 34.519 (was 21.584 with the rating to the power 1.2163 and AccRating.ACC_SCALE 13.5382): the same acc pp.
        public double AccWeight { get; set; } = 34.519;
        // How steeply acc pp follows the curve around 96%: curve(acc) is replaced by curve(96%) x (curve(acc) / curve(96%))^
        // AccCurvePower, so stars (read at 96%) don't move. 1 = the curve as it is. Below 1 evens pass-heavy maps with
        // others of their stars.
        public double AccCurvePower { get; set; } = 0.9;
        // The acc curve power on low-star maps (CurvePowerAt): LowStarCurvePower up to LowStarCurveStart stars, then linearly
        // to AccCurvePower at LowStarCurveEnd. Equal to AccCurvePower = off. Top players score far above 96% on easier maps,
        // so AccCurvePower < 1 would cut their best scores there most. Doesn't move stars (read at 96%).
        public double LowStarCurvePower { get; set; } = 1.0;
        // (was 6.7242 and 9.606 on the old prod-scale stars, 1.1653 x (pp / 83.6925)^1.17507)
        public double LowStarCurveStart { get; set; } = 7.1529;
        public double LowStarCurveEnd { get; set; } = 9.6894;
        // The acc curve power on the lowest-star maps (CurvePowerAt), before the ramp above: VeryLowStarCurvePower up to
        // VeryLowStarCurveStart stars, then linearly to LowStarCurvePower at VeryLowStarCurveEnd. Equal to LowStarCurvePower
        // = off. Lets the best plays on the easiest maps reach those of higher stars; the per-map curve rows (CurveSettings.
        // Table) can't do this since they can only lower the curve. Doesn't move stars (read at 96%).
        public double VeryLowStarCurvePower { get; set; } = 1.08;
        // (was 2.8818 and 4.803 on the old prod-scale stars, 1.1653 x (pp / 83.6925)^1.17507)
        public double VeryLowStarCurveStart { get; set; } = 3.478;
        public double VeryLowStarCurveEnd { get; set; } = 5.3718;
        // Tuned with BalanceTuner so tech-heavy maps pay the same as others of their stars (tech share slope near 0).
        // 0.587 with the Analyzer's BALANCED_TECH_SCALER 10 (0.13657 with 43.948, after the power 1.2477 was removed): the
        // balance tuner (TargetTolerance 0.002) from 0.54628, tech share slope -0.0046 -> 0, worth spread 0.03168 -> 0.03156.
        // 0.47 with TechTopKnee 4.5 (see there): about the same tech pp on the median map; 0.455 and 0.5045 moved the P75
        // worth by tech little (stars take most of a TechWeight change), 0.47 had the highest tech maps closest to 0.
        // 0.28 with the linear strain + rotation tech (BALANCED_TECH_SCALER 5.933, median tech 2.15 -> 4.03): 0.25 kept the
        // median map's tech pp, 0.28 has the tech-heavy maps for their stars 0.3-0.5% closer to 0 (0.31 only 0.2% more, with
        // more pp inflation).
        // 0.2417 (0.28 / 1.1584) with BALANCED_TECH_SCALER 6.917 (was 5.971): the same tech pp.
        // 0.2677 (0.2417 / 0.90294) with BALANCED_TECH_SCALER 6.2457 (was 6.917): the same tech pp.
        // 5.7887 (0.2677 x e^(3.2 x 0.96)) with TechAccExponent 0: tech pp flat in the acc, as pass pp, the same tech pp at 96%.
        public double TechWeight { get; set; } = 5.7887;
        // 3.27 (was 3.5): on the P75 worth (each player's better efforts), tech 9-13 maps were 1.3-1.6% over others of their
        // stars and tech pp grew too fast with acc; 3.27 halves it (tech share slope +0.0062 -> about 0), as TechWeight 0.131
        // would, with a slightly lower worth spread. 3.2 with the rotation^1.5 / soft capped strain tech (with TechTopKnee 9).
        // 0 (was 3.2): flat tech pp (TechWeight x tech, the acc dependence left to the acc pp and the acc relative pass pp).
        // On the P75 worth the spread 0.0330 -> 0.0319 (1.6: 0.0326, 5: 0.0336); every tech percentile and tech / stars bin
        // within 0.4% with TechTopSlope 0.7, slow / midspeed tech +0.29 / +0.07% (were +0.09 / -0.17%). The acc relative tech
        // multiplier (tech pp x e^(exponent x (acc - reference)), as the pass one) on top raised the spread (0.0335-0.0345).
        public double TechAccExponent { get; set; } = 0;
        // Stars = pp of a 96% score (before PpMultiplier) / StarDivisor. 52 (was 55.26): the star thresholds here, in
        // RatingsController (speed nerf), the acc reweighter and the worth x 55.26 / 52 (the low star bonus x 1 / 1.06269^1.98), so pp is the same.
        public double StarDivisor { get; set; } = 52;
        // Keeps the top 1000 players' average pp at prod's; retune when the formula changes it. 1.0299 (was 1.0199): the tech
        // refit (TechTopKnee 9, TechAccExponent 3.2) lowered players' pp about 1%.
        // 1.0190 (was 1.0299) with the acc relative pass pp (PassAccExponent 7) and TechTopSlope 1: the top 1000's average pp
        // as before.
        // 1.0173 with the flat tech pp (TechAccExponent 0, TechTopSlope 0.7) and the stronger low note nerf.
        // 1.0238 with the pass pp floor, PassAccExponent 6, no mid star top bonus and LowStarSpeedMaxShare 0.8.
        // 1.0176 (1.0238 x 16447.2 / 16547.0) with the mid star top bonus (x1.035): the top 1000's average pp as before.
        // 1.0151 with the mid star top bonus x1.05 fading 600-700pp (emulated: the top 1000's average pp as with x1.035).
        // 1.0128 with its stars 4 -> 6 ... 9 -> 11.5 (emulated: the top 1000's average pp as with x1.035).
        // Kept at 1.0128 with the mid star top bonus on acc pp only (stars 4 -> 6.4 ... 8 -> 10.5) and the Analyzer's tech
        // scaler 6.0 / rotation 6.0^1.25 (the top 1000's average pp 16393, v3 16441): 1.0158 (v3's average) put Feral E+ and
        // Unwelcome School (14+ stars, 96.9 / 96.3% BE) from 799 to 801-802pp.
        public double PpMultiplier { get; set; } = 1.0128;

        // Low star bonus: maps under LowStarBonusEnd stars pay more. Top players farm the easiest maps to 99%+, but stars are
        // measured at 96%, so pp vs stars fell off steeply at low stars. Stars include the bonus (StarsFromPp), and its
        // input is those stars. The power puts the bonus on the lowest stars without inflating 700 pp+ scores at 2-8 stars.
        // Worth compares each map with others of its stars, so the acc reweighter doesn't undo it. 0 = off.
        // 0.01948 x (5.504 - stars)^1.98: the old 0.00382 x (8.6454 - stars)^2.3 on the old prod-scale stars, refit on the
        // linear stars (RMS 0.006 of a bonus up to 0.545); 0.008181 x (8.5304 - stars)^1.98 on StarDivisor 54 (the same bonus)
        public double LowStarBonus { get; set; } = 0.007593;
        public double LowStarBonusEnd { get; set; } = 8.8584;
        public double LowStarBonusPower { get; set; } = 1.98;

        // Mid star top bonus: a top 1000 player's best effort on a 7-9 star map paid too little to count among their top plays
        // (a 98.5% on ABCD E+, 9.12 stars predicted 97.67%, ~709pp). acc pp x (1 + MidStarTopBonus x bump(stars) x ramp(acc)):
        // bump is 1 from MidStarTopFull to MidStarTopFullEnd stars, easing (smoothstep) to 0 at MidStarTopStart /
        // MidStarTopEnd; ramp eases from 0 at the map's predicted acc - MidStarTopAccBelow to 1 at its predicted acc +
        // MidStarTopAccAbove and stays 1 above. Stars and the acc ratings don't move. stars and predicted acc: those of the
        // version played (a speed modifier's own), like the low star bonus. 0 = off.
        // A ramp from predicted - 0.25% to + 0.75% only paid the top 100 (+0.5-0.85% on their 6-10.5 star plays, ranks
        // 101-1000 +0.02%) and made 31 plays 800+ (4 before); x0.07 from predicted - 1.5% moved players with mostly mid star
        // speed versions ~100 ranks up.
        // 0.035 from predicted - 3%: on 7-9 star plays the top 100 +2.3-2.7%, ranks 301-1000 +0.9-1.3%; 700+ plays on 6-10.5
        // stars 552 -> 923 (top 50 players), 800+ none; the top 100's weighted pp on 7-9 stars 7.1 -> 12.2% (Old Prod 13.8%,
        // New Prod 7.8%). Worth compares maps with others of their stars, so the acc reweighter doesn't undo it: its multipliers
        // on 7-9 star maps moved -0.02% on average (sd 0.4%), and the top 100's 7-9 star share was 12.6% after it.
        // x1.05 fading 600-700pp (was x1.035 fading 700-800): the bonus added the most pp (+24) at 700, so for the top 20 700+
        // plays were as common on 7-9.5 star maps as on 10.5-12 (10.8 / 10.4% of their plays) and 750+ 3.5x more common, while
        // 600+ stayed rarer (54 / 71%). Emulated: 700+ / 750+ back to without the bonus (5.3 / 6.4 / 10.4% on 7-9.5 / 9.5-10.5
        // / 10.5-12 stars), 600+ for ranks 21-100 on 7-9.5 stars 22 -> 28% (9.5-10.5: 49%), the top 1000's weighted pp on
        // 7-9.5 stars 15.4 -> 17.4%.
        // Stars 4 -> 6 ... 9 -> 11.5 (were 6 -> 7 ... 9.5 -> 10.5), emulated. Start: 5-6 star maps paid ranks 101-1000 the
        // least of 4-7 stars (500+ on 5.4% of their plays, 4-5: 8.4%, 6-7: 9.6%); 8.8% now, under 6-7's (4 -> 5 put 4-5 over
        // 5-6). End: with the pp fade already cutting the bonus at 9.5+ stars (par scores over 600pp), the fade-out over one
        // star made 10-10.5 star maps pay less than 9.5-10 (the top 100's pp at the predicted acc 636 -> 634, ranks 101-1000's
        // 600+ 3.5 -> 2.8%); now rising at every half star (629, 635, 641, 650 at 9-10.5).
        // Acc pp only (was the whole pp, fading out from 600 to 700pp before it): the fade flattened the pp curve around 650pp
        // (x1.05 at 600pp, x1 at 700), then it rose again, a bump. Now the bonus stays on up to 100%, so the curve keeps its
        // shape, and the star range ends lower so it makes no new 800pp play: x1.05 (about x1.044 on the whole pp at the
        // predicted acc, acc pp ~88% of it) full on 6.38-8 stars, out at 10.5 (9-9.5+ star maps already have 760-790pp plays).
        // Emulated on the Tech 6 DB (plain / reweighted): no new 800pp play (top bonus map play 793 / 795), the top 100's
        // weighted pp on 7-9.5 stars 10.6 -> 13.1% (no bonus 7.1%), the top 1000's average pp -0.8% (the bonus no longer
        // lifts the pass and tech pp of ranks 101-1000's plays).
        public double MidStarTopBonus { get; set; } = 0.05;
        public double MidStarTopStart { get; set; } = 4.2508;
        public double MidStarTopFull { get; set; } = 6.3762;
        public double MidStarTopFullEnd { get; set; } = 8.0;
        public double MidStarTopEnd { get; set; } = 10.5;
        public double MidStarTopAccBelow { get; set; } = 0.03;
        public double MidStarTopAccAbove { get; set; } = 0;

        public static PpFormula Current { get; set; } = new();

        // The acc pp multiplier
        public float MidStarTopMultiplier(float stars, float accuracy, float predictedAcc)
        {
            if (MidStarTopBonus == 0 || !(predictedAcc > 0)) return 1;
            double from = predictedAcc - MidStarTopAccBelow;
            if (!(accuracy > from)) return 1;
            double bump = MidStarTopBump(stars);
            if (bump == 0) return 1;
            double ramp = Ease((accuracy - from) / (MidStarTopAccBelow + MidStarTopAccAbove));
            return 1 + (float)(MidStarTopBonus * bump * ramp);
        }

        private double MidStarTopBump(float stars)
        {
            if (!(stars > MidStarTopStart && stars < MidStarTopEnd)) return 0;
            return stars < MidStarTopFull ? Ease((stars - MidStarTopStart) / (MidStarTopFull - MidStarTopStart))
                : stars > MidStarTopFullEnd ? Ease((MidStarTopEnd - stars) / (MidStarTopEnd - MidStarTopFullEnd)) : 1;
        }

        private static double Ease(double t) { t = Math.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }

        // Tech pp top compression: above TechTopKnee tech, each point counts TechTopSlope. 1 = off. Tech share was only
        // overpaid on the highest-tech maps (13+), so this trims them without touching tech 11-13. 9 (was 12) with the rotation^1.5
        // / soft capped strain tech, whose tech 9-14 maps were 1.2-2.2% over others of their stars on the P75 worth (0.2-0.9%
        // with 9 and TechAccExponent 3.2; the worth spread 0.0298 -> 0.0295). In tech units, so it follows the Analyzer's
        // BALANCED_TECH_SCALER: 2.207 with 10 (9.7 with 43.948). Fitted on the linear tech from the hardest half of the swings
        // with no strain soft cap: the balance tuner's sweep had the tech share slope -0.010 at 8, +0.0085 at 11.1 and +0.037
        // without the compression (worth spread 0.0314 / 0.0317 / 0.0344), all with the 43.948 scale.
        // 4.5 (was 2.207, about the median tech, so half the maps were compressed): on the P75 worth the top 10% of tech
        // were -0.6 to -0.8% (players ranked ~10000: -1.6 to -2.7%) and the 50-75th percentile +0.6%. With 4.5 (about the 89th
        // percentile) and TechWeight 0.47: top 10% -0.4 to +0.5% (top 100 about 0, ~10000 -0.5% at the 90-97th), top tech
        // for stars about 0; the 75-90th percentile +0.8%, under the median -0.5% (TechWeight doesn't move those); worth
        // RMS 0.0308 -> 0.0312. 3.5 left the top 10% -0.3 to -0.5%; 6 overpaid them (+0.7 to +1.2%, top 100 +0.7%).
        // Knee 6 (about the 89th percentile) and slope 1.6, an expansion, with the linear strain + rotation tech, whose top is
        // compressed: with knee 6.1 / slope 0.33 the 90-97th / 97-99th / 99th+ percentiles were -1.0 / -2.6 / -4.3% on the
        // P75 worth, -0.7 / -1.4 / -2.3% with no knee; slope 1.5 -0.4 / -0.1 / -0.4%, 1.75 -0.2 / +0.2 / +0.3% (TechWeight 0.28).
        // 1.2 with the swing tech's powers back (strain^1.25, rotation^1.5), which spread the top: 1.6 overpaid the 97-99th /
        // 99th+ percentiles +1.0 / +2.2%; 1.2 +0.2 / +0.7% (no expansion -0.2 / -0.2%, but tech-heavy maps for their stars
        // -0.4%), the 90-97th -0.3%.
        // 6.95 (6 x 1.1584) with BALANCED_TECH_SCALER 6.917 (was 5.971), the same tech.
        // 6.2754 (6.95 x 0.90294) with BALANCED_TECH_SCALER 6.2457 (was 6.917), the same tech.
        public double TechTopKnee { get; set; } = 6.2754;
        // 1 (was 1.2) with the acc relative pass pp (PassAccExponent 7): 1.2 overpaid the 97-99th / 99th+ percentiles +0.9 /
        // +1.5% (tech-heavy maps for their stars +1.0%), 1 +0.35 / +0.43% (+0.3%).
        // 0.7 with the flat tech pp (TechAccExponent 0), which raised the 97-99th / 99th+ percentiles to +1.2 / +1.7%: now +0.2 /
        // +0.1% (0.5: -0.5 / -1.1%).
        public double TechTopSlope { get; set; } = 0.7;

        public PpFormula Clone() => (PpFormula)MemberwiseClone();

        // Pass pp top compression: above PassTopKnee pass, each point counts PassTopSlope (1 = off). High-pass maps' pp is
        // uneven between players (their 30pp+ top plays are mostly from players ranked ~1000-3000), so no pass pp value
        // suits all of them; the knee trims the outliers without cutting pass 10.4-13 maps.
        public double PassTopKnee { get; set; } = 13;
        public double PassTopSlope { get; set; } = 0.5;

        public float PassPp(float passRating)
        {
            float pass = passRating;
            if (PassTopSlope != 1 && pass > PassTopKnee) pass = (float)(PassTopKnee + (pass - PassTopKnee) * PassTopSlope);
            float passPP = (float)PassScale * MathF.Exp((float)PassInner * MathF.Pow(pass, 1 / (float)PassExponent)) - (float)PassOffset;
            if (float.IsInfinity(passPP) || float.IsNaN(passPP) || passPP < 0) passPP = 0;
            return passPP;
        }

        // The acc curve power of a map with these stars (AccCurvePower, LowStarCurvePower, VeryLowStarCurvePower)
        public double CurvePowerAt(float stars)
        {
            if (VeryLowStarCurvePower != LowStarCurvePower && stars < VeryLowStarCurveEnd)
            {
                if (stars <= VeryLowStarCurveStart) return VeryLowStarCurvePower;
                return VeryLowStarCurvePower + (LowStarCurvePower - VeryLowStarCurvePower) * (stars - VeryLowStarCurveStart) / (VeryLowStarCurveEnd - VeryLowStarCurveStart);
            }
            if (LowStarCurvePower == AccCurvePower || !(stars < LowStarCurveEnd)) return AccCurvePower;
            if (stars <= LowStarCurveStart) return LowStarCurvePower;
            return LowStarCurvePower + (AccCurvePower - LowStarCurvePower) * (stars - LowStarCurveStart) / (LowStarCurveEnd - LowStarCurveStart);
        }

        // The curve value acc pp uses, from the curve at the score's accuracy and at 96% (power: CurvePowerAt)
        public float AccCurve(float curveValue, float curveAt96, double power)
        {
            if (power == 1 || !(curveValue > 0) || !(curveAt96 > 0)) return curveValue;
            return curveAt96 * MathF.Pow(curveValue / curveAt96, (float)power);
        }

        public float AccPp(float curveValue, float accRating) =>
            accRating > 0 ? curveValue * (float)AccWeight * accRating : 0;

        // Acc relative pass pp: pass pp x e^(PassAccExponent x (acc - the map's predicted acc)), so beating the Acc AI's
        // prediction pays more pass pp and falling short of it less (1 at the predicted acc). 0 = off (pass pp doesn't follow
        // the acc). Maps without a predicted acc: 1.
        // 10 with PassAccErrorOffset 1 (the reference is predicted acc - its error): speed maps +0.8% for the top 100 against
        // their other plays (players ranked 5000+ -3.7 to -5.3%), maps predicted 99%+ -3% for the top 100, FS / SF scores 3%+
        // under the predicted acc -1.7% for the top 1000. It spreads players' pp (top 100 +3%, ranked 5000+ -6%): PpMultiplier.
        // 7 (was 10) once the balance tuner's worth used it too: on the P75 worth the pass / stars tilt -0.15 / -0.05% at the
        // 20-40th / 80%+ (was -0.42 / +0.39% without, -0.04 / -0.26% with 10), maps predicted 98%+ with high pass -0.2% (were
        // +0.9%), slow tech +0.1%; the worth spread 0.0308 -> 0.0330 (0.0336 with 10, 0.0325 with 5, which loses the high pass
        // nerf).
        // 6 with the floor (PassAccMinMultiplier 0.8): the top 100's pass / stars 80%+ maps +1.69 -> +1.64%, 700pp+ plays fewer.
        public double PassAccExponent { get; set; } = 6;
        // Cap of that multiplier, so far-off scores don't run away
        public double PassAccMaxMultiplier { get; set; } = 3;
        // Floor of that multiplier (0 = 1 / PassAccMaxMultiplier), so players below their maps' reference can lose less pass
        // pp than the top gains.
        // 0.8: players far below their maps' reference lose at most 20% of the pass pp. On the P75 worth the spread 0.0329 ->
        // 0.0314; pass-heavy speed maps for players ranked ~10000 -6.0 -> -4.5%, linear speed maps -5.1 -> -3.7%; the top unchanged.
        public double PassAccMinMultiplier { get; set; } = 0.8;

        // The reference acc is lowered by this share of the map's predicted error: predicted acc - PassAccErrorOffset x (1 -
        // predicted acc). Top players fall short of the Acc AI's prediction more on maps that are harder to acc (0.9% at
        // 95-97% predicted, 0.2% at 98-99%), so with 0 the multiplier favors the easy to acc maps.
        public double PassAccErrorOffset { get; set; } = 1;

        public float PassAccMultiplier(float accuracy, float predictedAcc)
        {
            if (PassAccExponent == 0 || !(predictedAcc > 0)) return 1;
            double gap = accuracy - (predictedAcc - PassAccErrorOffset * (1 - predictedAcc));
            double m = Math.Exp(PassAccExponent * gap);
            return (float)Math.Clamp(m, PassAccMinMultiplier > 0 ? PassAccMinMultiplier : 1 / PassAccMaxMultiplier, PassAccMaxMultiplier);
        }

        public float TechPp(float accuracy, float techRating)
        {
            if (!(techRating > 0)) return 0;
            float tech = techRating;
            if (tech > TechTopKnee) tech = (float)(TechTopKnee + (tech - TechTopKnee) * TechTopSlope);
            return MathF.Exp((float)TechAccExponent * accuracy) * (float)TechWeight * tech;
        }

        // Stars from pp at 96% (pass + acc + tech, before PpMultiplier), without the low star bonus
        public float Stars(float ppAt96) => (float)(ppAt96 / StarDivisor);

        // Stars from pp at 96% before the low star bonus (before PpMultiplier): the stars s with
        // s = Stars(ppAt96 * LowStarMultiplier(s)), so they match the final pp of a 96% score, bonus included. The bonus
        // falls as stars rise, so there's exactly one s, between the stars without the bonus and LowStarBonusEnd.
        public float StarsFromPp(float ppAt96)
        {
            float stars = Stars(ppAt96);
            if (!(ppAt96 > 0) || LowStarBonus <= 0 || stars >= LowStarBonusEnd) return stars;

            double lo = stars, hi = LowStarBonusEnd;
            while (hi - lo > 1e-6)
            {
                double mid = (lo + hi) / 2;
                if (mid < Stars(ppAt96 * LowStarMultiplier((float)mid))) lo = mid; else hi = mid;
            }
            return (float)((lo + hi) / 2);
        }

        public float LowStarMultiplier(float stars)
        {
            if (LowStarBonus <= 0 || float.IsNaN(stars) || stars >= LowStarBonusEnd) return 1;
            return 1 + (float)(LowStarBonus * Math.Pow(LowStarBonusEnd - Math.Max(stars, 0), LowStarBonusPower));
        }

        // pp from the sum of pass, acc and tech pp, before the low star bonus
        public float Scale(float pp) => (float)PpMultiplier * pp;

        public override string ToString() => FormattableString.Invariant(
            $"pass {PassScale}*e^({PassInner}*p^(1/{PassExponent}))-{PassOffset} (x{PassTopSlope} above {PassTopKnee}), acc x{AccWeight}*a, tech e^({TechAccExponent}a)*{TechWeight}*t (x{TechTopSlope} above {TechTopKnee}), x{PpMultiplier}, stars pp/{StarDivisor}, low star bonus {LowStarBonus}*({LowStarBonusEnd}-stars)^{LowStarBonusPower}, mid star top bonus {MidStarTopBonus} ({MidStarTopStart}-{MidStarTopFull}-{MidStarTopFullEnd}-{MidStarTopEnd} stars, predicted -{MidStarTopAccBelow} to +{MidStarTopAccAbove} acc, acc pp)");
    }
}

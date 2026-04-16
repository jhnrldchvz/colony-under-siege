using NUnit.Framework;

/// <summary>
/// FuzzyDDATests — EditMode unit tests for the FuzzyDDA inference engine.
///
/// Covers:
///   A1 — Membership functions (whitebox): AccLow, AccMedium, AccHigh
///        at exact boundary and plateau values derived from the implementation.
///   A2 — Evaluate() inference output (whitebox + blackbox):
///        exact scores when one rule fires, blended score in overlap zone,
///        debug snapshot weights, and observable input→output contracts.
/// </summary>
[TestFixture]
public class FuzzyDDATests
{
    private const float Eps = 0.005f;

    // =========================================================================
    // A1 — Membership Functions (Whitebox)
    // =========================================================================
    // Tests verify the shape of each trapezoidal / triangular membership curve
    // at its exact boundary and plateau values as defined in FuzzyDDA.cs.
    // These are whitebox: they break if the curve parameters are re-tuned.

    // ── AccLow : Trapezoid(acc, 0, 0, 0.20, 0.45) ────────────────────────────

    [Test]
    [Category("Whitebox")]
    public void AccLow_AtZero_IsZero()
    {
        // x <= a (a=0) → strict inequality returns 0
        Assert.AreEqual(0f, FuzzyDDA.AccLow(0f), Eps,
            "AccLow(0) must be 0 — left-shoulder trapezoid: x <= a returns 0.");
    }

    [Test]
    [Category("Whitebox")]
    public void AccLow_At0f10_InsidePlateau_IsOne()
    {
        // x = 0.10 is inside plateau [b, c] = [0, 0.20] → μ = 1
        Assert.AreEqual(1f, FuzzyDDA.AccLow(0.10f), Eps,
            "AccLow(0.10) must be 1 — inside the plateau [0, 0.20].");
    }

    [Test]
    [Category("Whitebox")]
    public void AccLow_At0f20_RightPlateauEdge_IsOne()
    {
        // x = 0.20 = c (right edge of plateau) → μ = 1
        Assert.AreEqual(1f, FuzzyDDA.AccLow(0.20f), Eps,
            "AccLow(0.20) must be 1 — right edge of the plateau.");
    }

    [Test]
    [Category("Whitebox")]
    public void AccLow_At0f45_FadeComplete_IsZero()
    {
        // x = 0.45 = d → x >= d returns 0
        Assert.AreEqual(0f, FuzzyDDA.AccLow(0.45f), Eps,
            "AccLow(0.45) must be 0 — at the right fade boundary (d=0.45).");
    }

    [Test]
    [Category("Whitebox")]
    public void AccLow_At0f325_MidFade_IsHalf()
    {
        // Midpoint of fade [c=0.20, d=0.45]: (0.45 - 0.325) / (0.45 - 0.20) = 0.46 ≈ 0.46
        // More precisely: fade slope = (d-x)/(d-c) = (0.45-0.325)/(0.25) = 0.125/0.25 = 0.5
        Assert.AreEqual(0.5f, FuzzyDDA.AccLow(0.325f), 0.01f,
            "AccLow(0.325) must be ~0.5 — midpoint of the fade-out ramp.");
    }

    // ── AccMedium : Triangle(acc, 0.25, 0.475, 0.70) ─────────────────────────

    [Test]
    [Category("Whitebox")]
    public void AccMedium_At0f25_LeftFoot_IsZero()
    {
        // x = 0.25 = a → x <= a returns 0
        Assert.AreEqual(0f, FuzzyDDA.AccMedium(0.25f), Eps,
            "AccMedium(0.25) must be 0 — at the triangle left foot.");
    }

    [Test]
    [Category("Whitebox")]
    public void AccMedium_At0f475_Peak_IsOne()
    {
        // x = 0.475 = b (triangle peak) → μ = 1
        Assert.AreEqual(1f, FuzzyDDA.AccMedium(0.475f), Eps,
            "AccMedium(0.475) must be 1 — at the triangle peak.");
    }

    [Test]
    [Category("Whitebox")]
    public void AccMedium_At0f70_RightFoot_IsZero()
    {
        // x = 0.70 = c → x >= c returns 0
        Assert.AreEqual(0f, FuzzyDDA.AccMedium(0.70f), Eps,
            "AccMedium(0.70) must be 0 — at the triangle right foot.");
    }

    // ── AccHigh : Trapezoid(acc, 0.55, 0.80, 1, 1) ───────────────────────────

    [Test]
    [Category("Whitebox")]
    public void AccHigh_At0f55_LeftEdge_IsZero()
    {
        // x = 0.55 = a → x <= a returns 0
        Assert.AreEqual(0f, FuzzyDDA.AccHigh(0.55f), Eps,
            "AccHigh(0.55) must be 0 — at the left fade boundary (a=0.55).");
    }

    [Test]
    [Category("Whitebox")]
    public void AccHigh_At0f80_PlateauStart_IsOne()
    {
        // x = 0.80 = b → start of plateau → μ = 1
        Assert.AreEqual(1f, FuzzyDDA.AccHigh(0.80f), Eps,
            "AccHigh(0.80) must be 1 — start of the High plateau (b=0.80).");
    }

    [Test]
    [Category("Whitebox")]
    public void AccHigh_At0f90_InsidePlateau_IsOne()
    {
        // x = 0.90 is inside plateau [b, c] = [0.80, 1.0] → μ = 1
        Assert.AreEqual(1f, FuzzyDDA.AccHigh(0.90f), Eps,
            "AccHigh(0.90) must be 1 — inside the High plateau.");
    }

    [Test]
    [Category("Whitebox")]
    public void AccHigh_AtOne_IsZero()
    {
        // x = 1.0 = d → x >= d returns 0 (strict inequality in Trapezoid)
        Assert.AreEqual(0f, FuzzyDDA.AccHigh(1.0f), Eps,
            "AccHigh(1.0) must be 0 — x >= d (d=1) returns 0 per Trapezoid definition.");
    }

    // =========================================================================
    // A2 — Inference Output: Whitebox (exact expected scores)
    // =========================================================================
    // When only ONE rule fires the weighted-average defuzz reduces to that
    // rule's singleton → exact, predictable result.

    [Test]
    [Category("Whitebox")]
    public void Evaluate_LowAccuracy_OnlyR1Fires_ReturnsVeryEasySingleton()
    {
        // acc=0.10 → AccLow=1, AccMedium=0, AccHigh=0 → only R1 fires
        // score = (1.0 × 0.10) / 1.0 = 0.10
        float score = FuzzyDDA.Evaluate(0.10f);
        Assert.AreEqual(FuzzyDDA.C_VeryEasy, score, Eps,
            "Evaluate(0.10) must equal C_VeryEasy (0.10) — only R1 fires.");
    }

    [Test]
    [Category("Whitebox")]
    public void Evaluate_PeakMediumAccuracy_OnlyR2Fires_ReturnsNormalSingleton()
    {
        // acc=0.475 → AccLow=0, AccMedium=1, AccHigh=0 → only R2 fires
        // score = (1.0 × 0.50) / 1.0 = 0.50
        float score = FuzzyDDA.Evaluate(0.475f);
        Assert.AreEqual(FuzzyDDA.C_Normal, score, Eps,
            "Evaluate(0.475) must equal C_Normal (0.50) — only R2 fires at the triangle peak.");
    }

    [Test]
    [Category("Whitebox")]
    public void Evaluate_HighAccuracy_OnlyR3Fires_ReturnsVeryHardSingleton()
    {
        // acc=0.90 → AccLow=0, AccMedium=0, AccHigh=1 → only R3 fires
        // score = (1.0 × 0.90) / 1.0 = 0.90
        float score = FuzzyDDA.Evaluate(0.90f);
        Assert.AreEqual(FuzzyDDA.C_VeryHard, score, Eps,
            "Evaluate(0.90) must equal C_VeryHard (0.90) — only R3 fires.");
    }

    [Test]
    [Category("Whitebox")]
    public void Evaluate_OverlapZone_R1AndR2Blend_ScoreIsInterpolated()
    {
        // acc=0.30 sits in the Low→Medium transition zone.
        // AccLow(0.30)    = (0.45-0.30)/(0.45-0.20) = 0.15/0.25 = 0.60
        // AccMedium(0.30) = (0.30-0.25)/(0.475-0.25) = 0.05/0.225 ≈ 0.222
        // score = (0.60×0.10 + 0.222×0.50) / (0.60 + 0.222)
        //       = (0.060 + 0.111) / 0.822 ≈ 0.208
        float score = FuzzyDDA.Evaluate(0.30f);
        Assert.AreEqual(0.208f, score, 0.005f,
            "Evaluate(0.30) must blend R1 and R2 to ≈ 0.208.");
    }

    [Test]
    [Category("Whitebox")]
    public void Evaluate_DebugSnapshot_WeightsMatchMembershipValues()
    {
        // At acc=0.475 only R2 fires → snapshot weights: wNormal=1, others=0
        FuzzyDDA.Evaluate(0.475f, out FuzzyDebugSnapshot snap);

        Assert.AreEqual(1f, snap.wNormal,   Eps, "wNormal must equal AccMedium at peak (1.0).");
        Assert.AreEqual(0f, snap.wVeryEasy, Eps, "wVeryEasy must be 0 when AccLow=0.");
        Assert.AreEqual(0f, snap.wVeryHard, Eps, "wVeryHard must be 0 when AccHigh=0.");
    }

    // =========================================================================
    // A2 — Inference Output: Blackbox (input→output contracts only)
    // =========================================================================
    // These tests do NOT depend on internal parameter values.
    // They verify only observable ordering and range contracts.

    [Test]
    [Category("Blackbox")]
    public void Evaluate_LowAccuracy_ReturnsBelowHalf()
    {
        float score = FuzzyDDA.Evaluate(0.10f);
        Assert.Less(score, 0.5f,
            "Low accuracy (10%) must produce a score < 0.5 (easier than normal).");
    }

    [Test]
    [Category("Blackbox")]
    public void Evaluate_HighAccuracy_ReturnsAboveHalf()
    {
        float score = FuzzyDDA.Evaluate(0.90f);
        Assert.Greater(score, 0.5f,
            "High accuracy (90%) must produce a score > 0.5 (harder than normal).");
    }

    [Test]
    [Category("Blackbox")]
    public void Evaluate_MidAccuracy_ReturnsNearHalf()
    {
        float score = FuzzyDDA.Evaluate(0.475f);
        Assert.AreEqual(0.5f, score, 0.05f,
            "Mid accuracy (~47.5%) must produce a score near 0.5 (neutral difficulty).");
    }

    [Test]
    [Category("Blackbox")]
    public void Evaluate_ScoreIsMonotonicallyIncreasing()
    {
        // Higher accuracy must always yield a higher (or equal) difficulty score.
        float scoreLow  = FuzzyDDA.Evaluate(0.10f);
        float scoreMid  = FuzzyDDA.Evaluate(0.475f);
        float scoreHigh = FuzzyDDA.Evaluate(0.90f);

        Assert.Less(scoreLow, scoreMid,
            "Score for 10% accuracy must be less than score for 47.5% accuracy.");
        Assert.Less(scoreMid, scoreHigh,
            "Score for 47.5% accuracy must be less than score for 90% accuracy.");
    }

    [Test]
    [Category("Blackbox")]
    public void Evaluate_AlwaysReturnsClamped_Between0And1()
    {
        float[] inputs = { 0.01f, 0.10f, 0.30f, 0.475f, 0.60f, 0.90f };
        foreach (float acc in inputs)
        {
            float score = FuzzyDDA.Evaluate(acc);
            Assert.GreaterOrEqual(score, 0f, $"Score for acc={acc} is below 0.");
            Assert.LessOrEqual   (score, 1f, $"Score for acc={acc} is above 1.");
        }
    }
}

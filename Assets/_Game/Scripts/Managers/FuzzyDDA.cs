using UnityEngine;

/// <summary>
/// FuzzyDDA — Mamdani-style Fuzzy Logic inference engine for Dynamic Difficulty Adjustment.
///
/// ╔══════════════════════════════════════════════════════════════════════════╗
/// ║  INPUT   (crisp, normalised 0–1)                                        ║
/// ║    • accuracy — hits / shots fired in the current evaluation window     ║
/// ║                                                                          ║
/// ║  LINGUISTIC TERMS                                                        ║
/// ║    accuracy : Low | Medium | High                                        ║
/// ║                                                                          ║
/// ║  OUTPUT  (crisp difficulty score 0–1)                                    ║
/// ║    0 = absolute easiest   1 = absolute hardest                           ║
/// ║    Output terms : VeryEasy(0.10)  Normal(0.50)  VeryHard(0.90)          ║
/// ║                                                                          ║
/// ║  INFERENCE                                                               ║
/// ║    Rule strength   : membership value of accuracy term (single input)   ║
/// ║    Aggregation     : max operator per output term                        ║
/// ║    Defuzzification : weighted average (centroid) over singleton terms    ║
/// ╚══════════════════════════════════════════════════════════════════════════╝
///
/// FUZZY RULE BASE (3 rules)
/// ┌─────┬────────────────┬────────────┐
/// │ R#  │ accuracy       │ output     │
/// ├─────┼────────────────┼────────────┤
/// │ R1  │ Low            │ VeryEasy   │
/// │ R2  │ Medium         │ Normal     │
/// │ R3  │ High           │ VeryHard   │
/// └─────┴────────────────┴────────────┘
///
/// Rationale:
///   Accuracy is the sole performance signal.  Low accuracy → reduce challenge;
///   high accuracy → increase challenge.  Smooth transition zones between terms
///   produce continuous interpolated scores at the membership overlap boundaries.
/// </summary>
public static class FuzzyDDA
{
    // ─────────────────────────────────────────────────────────────────────────
    // §1  Membership functions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Trapezoidal membership function.
    /// Returns 0 outside [a, d].  Ramps linearly from 0→1 on [a, b].
    /// Plateau of 1 on [b, c].  Ramps linearly from 1→0 on [c, d].
    /// Degenerate cases: a==b gives a left-shoulder; c==d gives a right-shoulder.
    /// </summary>
    public static float Trapezoid(float x, float a, float b, float c, float d)
    {
        if (x <= a || x >= d) return 0f;
        if (x >= b && x <= c) return 1f;
        if (x < b)            return Mathf.Clamp01((x - a) / (b - a));
        return                       Mathf.Clamp01((d - x) / (d - c));
    }

    /// <summary>
    /// Triangular membership function.
    /// Returns 0 outside [a, c].  Ramps 0→1 on [a, b] and 1→0 on [b, c].
    /// </summary>
    public static float Triangle(float x, float a, float b, float c)
    {
        if (x <= a || x >= c) return 0f;
        if (x <= b)           return Mathf.Clamp01((x - a) / (b - a));
        return                       Mathf.Clamp01((c - x) / (c - b));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §2  Fuzzification — Accuracy
    // ─────────────────────────────────────────────────────────────────────────
    //
    //  μ
    //  1 ┤▓▓▓▓\        /▓▓\        /▓▓▓▓
    //    │     \      /    \      /
    //  0 ┼──────\────/──────\────/──────► accuracy
    //    0     .20 .25     .70 .80      1
    //
    //  Low    : Trapezoid (0,   0,   0.20, 0.45)  — plateau [0, 0.20], fades by 0.45
    //  Medium : Triangle  (0.25, 0.475, 0.70)     — peak at 0.475
    //  High   : Trapezoid (0.55, 0.80,  1,   1)   — rises from 0.55, plateau [0.80, 1]

    public static float AccLow   (float acc) => Trapezoid(acc, 0f,   0f,    0.20f, 0.45f);
    public static float AccMedium(float acc) => Triangle (acc, 0.25f, 0.475f, 0.70f);
    public static float AccHigh  (float acc) => Trapezoid(acc, 0.55f, 0.80f,  1f,   1f  );

    // ─────────────────────────────────────────────────────────────────────────
    // §3  Output singleton centres
    // ─────────────────────────────────────────────────────────────────────────

    public const float C_VeryEasy = 0.10f;
    public const float C_Normal   = 0.50f;
    public const float C_VeryHard = 0.90f;

    // ─────────────────────────────────────────────────────────────────────────
    // §4  Inference — simple overload (score only)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Run the full fuzzy inference pipeline.
    /// </summary>
    /// <param name="accuracy">Player hit accuracy in [0, 1].</param>
    /// <returns>Crisp difficulty score in [0, 1]. 0 = easiest, 1 = hardest.</returns>
    public static float Evaluate(float accuracy)
    {
        FuzzyDebugSnapshot snap;
        return Evaluate(accuracy, out snap);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §5  Inference — full overload (score + debug snapshot)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Run the full fuzzy inference pipeline and expose all intermediate
    /// membership values via <paramref name="snap"/> for debug HUD / research export.
    /// </summary>
    public static float Evaluate(float accuracy, out FuzzyDebugSnapshot snap)
    {
        // ── Step 1: Fuzzify input ────────────────────────────────────────────
        snap.accLow  = AccLow   (accuracy);
        snap.accMed  = AccMedium(accuracy);
        snap.accHigh = AccHigh  (accuracy);

        // ── Step 2 + 3: Fire rules and aggregate per output term ─────────────
        //  Each output term is driven by exactly one rule whose strength equals
        //  the accuracy membership value — no AND operator needed.
        //  R1: Low    → VeryEasy
        //  R2: Medium → Normal
        //  R3: High   → VeryHard
        snap.wVeryEasy = snap.accLow;
        snap.wNormal   = snap.accMed;
        snap.wVeryHard = snap.accHigh;

        // ── Step 4: Defuzzify — weighted average (centroid for singletons) ──
        float numerator   = snap.wVeryEasy * C_VeryEasy
                          + snap.wNormal   * C_Normal
                          + snap.wVeryHard * C_VeryHard;

        float denominator = snap.wVeryEasy + snap.wNormal + snap.wVeryHard;

        // Guard: if no rule fires return neutral 0.5
        snap.crisp = denominator > 0f ? numerator / denominator : 0.5f;
        return snap.crisp;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Debug snapshot — all intermediate values from one inference cycle
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// All intermediate fuzzy membership values captured during a single inference run.
/// Serializable so TestDataExporter can write each field to CSV.
/// </summary>
[System.Serializable]
public struct FuzzyDebugSnapshot
{
    // ── Fuzzified accuracy memberships ──────────────────────────────────────
    [Tooltip("μ(accuracy, Low)")]    public float accLow;
    [Tooltip("μ(accuracy, Medium)")] public float accMed;
    [Tooltip("μ(accuracy, High)")]   public float accHigh;

    // ── Aggregated output weights ────────────────────────────────────────────
    [Tooltip("w(VeryEasy) — rule R1")] public float wVeryEasy;
    [Tooltip("w(Normal)   — rule R2")] public float wNormal;
    [Tooltip("w(VeryHard) — rule R3")] public float wVeryHard;

    // ── Defuzzified result ───────────────────────────────────────────────────
    [Tooltip("Crisp difficulty score [0, 1]")] public float crisp;

    public override string ToString() =>
        $"Acc[Lo={accLow:F2} Me={accMed:F2} Hi={accHigh:F2}] | " +
        $"Out[VE={wVeryEasy:F2} N={wNormal:F2} VH={wVeryHard:F2}] " +
        $"→ score={crisp:F3}";
}

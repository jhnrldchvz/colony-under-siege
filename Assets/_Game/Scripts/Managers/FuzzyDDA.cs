using UnityEngine;

/// <summary>
/// FuzzyDDA — Mamdani-style Fuzzy Logic inference engine for Dynamic Difficulty Adjustment.
///
/// ╔══════════════════════════════════════════════════════════════════════════╗
/// ║  INPUTS  (crisp, normalised 0–1)                                        ║
/// ║    • accuracy    — hits / shots fired in the current evaluation window   ║
/// ║    • healthRatio — player current HP / maxHP                             ║
/// ║                                                                          ║
/// ║  LINGUISTIC TERMS PER INPUT                                              ║
/// ║    accuracy    : Low | Medium | High                                     ║
/// ║    healthRatio : Low | Medium | High                                     ║
/// ║                                                                          ║
/// ║  OUTPUT  (crisp difficulty score 0–1)                                    ║
/// ║    0 = absolute easiest   1 = absolute hardest                           ║
/// ║    Output terms : VeryEasy(0.10) Easy(0.30) Normal(0.50)                ║
/// ║                   Hard(0.70) VeryHard(0.90)                              ║
/// ║                                                                          ║
/// ║  INFERENCE                                                               ║
/// ║    AND-connective  : min operator                                        ║
/// ║    Aggregation     : max operator per output term                        ║
/// ║    Defuzzification : weighted average (centroid) over singleton terms    ║
/// ╚══════════════════════════════════════════════════════════════════════════╝
///
/// FUZZY RULE BASE (9 rules)
/// ┌─────┬────────────────┬──────────────┬────────────┐
/// │ R#  │ accuracy       │ healthRatio  │ output     │
/// ├─────┼────────────────┼──────────────┼────────────┤
/// │ R1  │ Low            │ Low          │ VeryEasy   │
/// │ R2  │ Low            │ Medium       │ Easy       │
/// │ R3  │ Low            │ High         │ Easy       │
/// │ R4  │ Medium         │ Low          │ Easy       │
/// │ R5  │ Medium         │ Medium       │ Normal     │
/// │ R6  │ Medium         │ High         │ Normal     │
/// │ R7  │ High           │ Low          │ Normal     │
/// │ R8  │ High           │ Medium       │ Hard       │
/// │ R9  │ High           │ High         │ VeryHard   │
/// └─────┴────────────────┴──────────────┴────────────┘
///
/// Rationale:
///   Accuracy is the primary performance signal. Player health ratio acts as a
///   "struggling" modifier — even a highly accurate player who is critically hurt
///   is capped at Normal difficulty rather than Hard, reflecting that the combat
///   challenge is already sufficient.
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
    // §2  Fuzzification — Accuracy  (input 1)
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
    // §3  Fuzzification — Health Ratio  (input 2)
    // ─────────────────────────────────────────────────────────────────────────
    //
    //  μ
    //  1 ┤▓▓▓▓\        /▓▓\        /▓▓▓▓
    //    │     \      /    \      /
    //  0 ┼──────\────/──────\────/──────► healthRatio
    //    0     .25 .30     .80 .85      1
    //
    //  Low    : Trapezoid (0,   0,   0.25, 0.50)
    //  Medium : Triangle  (0.30, 0.55, 0.80)
    //  High   : Trapezoid (0.60, 0.85, 1,   1  )

    public static float HpLow   (float hp) => Trapezoid(hp, 0f,   0f,    0.25f, 0.50f);
    public static float HpMedium(float hp) => Triangle (hp, 0.30f, 0.55f, 0.80f);
    public static float HpHigh  (float hp) => Trapezoid(hp, 0.60f, 0.85f, 1f,   1f  );

    // ─────────────────────────────────────────────────────────────────────────
    // §4  Output singleton centres
    // ─────────────────────────────────────────────────────────────────────────

    public const float C_VeryEasy = 0.10f;
    public const float C_Easy     = 0.30f;
    public const float C_Normal   = 0.50f;
    public const float C_Hard     = 0.70f;
    public const float C_VeryHard = 0.90f;

    // ─────────────────────────────────────────────────────────────────────────
    // §5  Inference — simple overload (score only)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Run the full Mamdani fuzzy inference pipeline.
    /// </summary>
    /// <param name="accuracy">Player hit accuracy in [0, 1].</param>
    /// <param name="healthRatio">Player current HP / maxHP in [0, 1].</param>
    /// <returns>Crisp difficulty score in [0, 1]. 0 = easiest, 1 = hardest.</returns>
    public static float Evaluate(float accuracy, float healthRatio)
    {
        FuzzyDebugSnapshot snap;
        return Evaluate(accuracy, healthRatio, out snap);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §6  Inference — full overload (score + debug snapshot)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Run the full Mamdani fuzzy inference pipeline and expose all intermediate
    /// membership values via <paramref name="snap"/> for debug HUD / research export.
    /// </summary>
    public static float Evaluate(float accuracy, float healthRatio,
                                 out FuzzyDebugSnapshot snap)
    {
        // ── Step 1: Fuzzify inputs ───────────────────────────────────────────
        snap.accLow  = AccLow   (accuracy);
        snap.accMed  = AccMedium(accuracy);
        snap.accHigh = AccHigh  (accuracy);

        snap.hpLow   = HpLow   (healthRatio);
        snap.hpMed   = HpMedium(healthRatio);
        snap.hpHigh  = HpHigh  (healthRatio);

        // ── Step 2: Fire rules  (AND = min operator) ────────────────────────
        float r1 = Mathf.Min(snap.accLow,  snap.hpLow );   // VeryEasy
        float r2 = Mathf.Min(snap.accLow,  snap.hpMed );   // Easy
        float r3 = Mathf.Min(snap.accLow,  snap.hpHigh);   // Easy
        float r4 = Mathf.Min(snap.accMed,  snap.hpLow );   // Easy
        float r5 = Mathf.Min(snap.accMed,  snap.hpMed );   // Normal
        float r6 = Mathf.Min(snap.accMed,  snap.hpHigh);   // Normal
        float r7 = Mathf.Min(snap.accHigh, snap.hpLow );   // Normal
        float r8 = Mathf.Min(snap.accHigh, snap.hpMed );   // Hard
        float r9 = Mathf.Min(snap.accHigh, snap.hpHigh);   // VeryHard

        // ── Step 3: Aggregate per output term (max operator) ────────────────
        snap.wVeryEasy = r1;
        snap.wEasy     = Mathf.Max(r2, r3, r4);
        snap.wNormal   = Mathf.Max(r5, r6, r7);
        snap.wHard     = r8;
        snap.wVeryHard = r9;

        // ── Step 4: Defuzzify — weighted average (centroid for singletons) ──
        float numerator = snap.wVeryEasy * C_VeryEasy
                        + snap.wEasy     * C_Easy
                        + snap.wNormal   * C_Normal
                        + snap.wHard     * C_Hard
                        + snap.wVeryHard * C_VeryHard;

        float denominator = snap.wVeryEasy + snap.wEasy + snap.wNormal
                          + snap.wHard     + snap.wVeryHard;

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

    // ── Fuzzified health memberships ────────────────────────────────────────
    [Tooltip("μ(hp, Low)")]          public float hpLow;
    [Tooltip("μ(hp, Medium)")]       public float hpMed;
    [Tooltip("μ(hp, High)")]         public float hpHigh;

    // ── Aggregated output weights ────────────────────────────────────────────
    [Tooltip("w(VeryEasy) — rule R1")]              public float wVeryEasy;
    [Tooltip("w(Easy)     — max(R2, R3, R4)")]      public float wEasy;
    [Tooltip("w(Normal)   — max(R5, R6, R7)")]      public float wNormal;
    [Tooltip("w(Hard)     — rule R8")]              public float wHard;
    [Tooltip("w(VeryHard) — rule R9")]              public float wVeryHard;

    // ── Defuzzified result ───────────────────────────────────────────────────
    [Tooltip("Crisp difficulty score [0, 1]")]      public float crisp;

    public override string ToString() =>
        $"Acc[Lo={accLow:F2} Me={accMed:F2} Hi={accHigh:F2}] " +
        $"HP[Lo={hpLow:F2} Me={hpMed:F2} Hi={hpHigh:F2}] | " +
        $"Out[VE={wVeryEasy:F2} E={wEasy:F2} N={wNormal:F2} H={wHard:F2} VH={wVeryHard:F2}] " +
        $"→ score={crisp:F3}";
}

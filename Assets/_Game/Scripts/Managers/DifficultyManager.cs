using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// DifficultyManager — Fuzzy Logic Dynamic Difficulty Adjustment (FL-DDA).
///
/// ╔═══════════════════════════════════════════════════════════════════════════╗
/// ║  HOW IT WORKS                                                             ║
/// ║                                                                           ║
/// ║  Every <evaluationInterval> seconds the manager:                          ║
/// ║    1. Samples window accuracy  (hits / shots in the window)               ║
/// ║    2. Samples player health ratio  (currentHP / maxHP)                    ║
/// ║    3. Feeds both into FuzzyDDA.Evaluate() → crisp score in [0, 1]        ║
/// ║    4. Linearly interpolates all enemy stat multipliers between the        ║
/// ║       Easy anchor (score=0) and Hard anchor (score=1) via Normal (0.5)   ║
/// ║    5. Applies the interpolated settings to every living enemy via         ║
/// ║       EnemyAI.ApplyDifficultySettings()                                  ║
/// ║                                                                           ║
/// ║  The DifficultyTier enum is kept for UI display only.  It is derived      ║
/// ║  from the continuous score rather than driving it:                        ║
/// ║    score < 0.35 → Easy   |   0.35–0.65 → Normal   |   > 0.65 → Hard     ║
/// ║                                                                           ║
/// ║  RESULT: Smooth, continuous enemy scaling with no abrupt stat jumps.      ║
/// ╚═══════════════════════════════════════════════════════════════════════════╝
///
/// Setup:
///   1. Create an empty GameObject named "DifficultyManager".
///   2. Attach this script.
///   3. Tune anchor multipliers and evaluation parameters in the Inspector.
///   4. WeaponController already calls ReportShot() — no extra wiring needed.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Singleton
    // ─────────────────────────────────────────────────────────────────────────

    public static DifficultyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Difficulty tier (display only — derived from fuzzy score)
    // ─────────────────────────────────────────────────────────────────────────

    public enum DifficultyTier { Easy, Normal, Hard }

    /// <summary>Display tier derived from CurrentFuzzyScore. Triggers OnTierChanged.</summary>
    public DifficultyTier CurrentTier { get; private set; } = DifficultyTier.Normal;

    /// <summary>
    /// Raw fuzzy difficulty score in [0, 1].
    /// 0 = absolute easiest   1 = absolute hardest.
    /// Updated every evaluation interval.
    /// </summary>
    public float CurrentFuzzyScore { get; private set; } = 0.5f;

    /// <summary>Most recent intermediate fuzzy values — for debug HUD and CSV export.</summary>
    public FuzzyDebugSnapshot LastSnapshot { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Evaluation parameters
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Evaluation Parameters")]
    [Tooltip("Seconds between each fuzzy evaluation cycle")]
    public float evaluationInterval = 5f;

    [Tooltip("Minimum shots fired in the window before DDA activates")]
    public int minShotsBeforeEval = 5;

    [Tooltip("Score band boundaries for display tier derivation")]
    [Range(0f, 0.5f)] public float easyBandMax  = 0.35f;   // score < this  → Easy
    [Range(0.5f, 1f)] public float hardBandMin  = 0.65f;   // score > this  → Hard

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — EASY anchor multipliers   (score = 0.0)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Easy Anchor Multipliers  (fuzzy score = 0.0)")]
    [Tooltip("< 1 = weaker enemy")]
    public float easyHealthMult      = 0.70f;
    public float easyPatrolSpeedMult = 0.80f;
    public float easyChaseSpeedMult  = 0.70f;
    public float easyDamageMult      = 0.60f;
    public float easyDetectionMult   = 0.70f;
    public float easyAttackRangeMult = 0.90f;
    [Tooltip("> 1 = slower attacks")]
    public float easyCooldownMult    = 1.50f;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — NORMAL anchor multipliers (score = 0.5)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Normal Anchor Multipliers  (fuzzy score = 0.5)")]
    public float normalHealthMult      = 1.00f;
    public float normalPatrolSpeedMult = 1.00f;
    public float normalChaseSpeedMult  = 1.00f;
    public float normalDamageMult      = 1.00f;
    public float normalDetectionMult   = 1.00f;
    public float normalAttackRangeMult = 1.00f;
    public float normalCooldownMult    = 1.00f;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — HARD anchor multipliers  (score = 1.0)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Hard Anchor Multipliers  (fuzzy score = 1.0)")]
    [Tooltip("> 1 = stronger enemy")]
    public float hardHealthMult      = 1.50f;
    public float hardPatrolSpeedMult = 1.50f;
    public float hardChaseSpeedMult  = 1.60f;
    public float hardDamageMult      = 2.00f;
    public float hardDetectionMult   = 1.40f;
    public float hardAttackRangeMult = 1.20f;
    [Tooltip("< 1 = faster attacks")]
    public float hardCooldownMult    = 0.60f;

    // ─────────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when the display tier (Easy/Normal/Hard) changes.
    /// DifficultyHUD subscribes to update its label.
    /// Note: The underlying fuzzy score changes continuously — this event only
    /// fires when the score crosses a band boundary.
    /// </summary>
    public event Action<DifficultyTier> OnTierChanged;

    /// <summary>
    /// Fired every evaluation cycle regardless of tier change.
    /// Carries the new fuzzy score and snapshot for research logging.
    /// </summary>
    public event Action<float, FuzzyDebugSnapshot> OnFuzzyScoreUpdated;

    // ─────────────────────────────────────────────────────────────────────────
    // Private — shot window
    // ─────────────────────────────────────────────────────────────────────────

    private int   _shotsInWindow = 0;
    private int   _hitsInWindow  = 0;
    private float _evalTimer     = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    // Public — lifetime accuracy stats
    // ─────────────────────────────────────────────────────────────────────────

    public int   TotalShotsFired { get; private set; } = 0;
    public int   TotalShotsHit   { get; private set; } = 0;
    public float LifetimeAccuracy =>
        TotalShotsFired > 0 ? (float)TotalShotsHit / TotalShotsFired : 0f;
    public float WindowAccuracy =>
        _shotsInWindow > 0 ? (float)_hitsInWindow / _shotsInWindow : 0f;

    // ─────────────────────────────────────────────────────────────────────────
    // Player reference (for health ratio)
    // ─────────────────────────────────────────────────────────────────────────

    private PlayerController _player;

    private float HealthRatio
    {
        get
        {
            if (_player == null) return 1f;   // Default to full health if no reference
            return _player.maxHealth > 0
                ? Mathf.Clamp01((float)_player.CurrentHealth / _player.maxHealth)
                : 1f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(InitAfterEnemiesRegister());
    }

    private IEnumerator InitAfterEnemiesRegister()
    {
        yield return null;   // frame 1 — enemies call Start() and register
        yield return null;   // frame 2 — safety buffer

        CachePlayer();
        ApplyFuzzyScoreToAllEnemies(CurrentFuzzyScore);
        Debug.Log("[DifficultyManager] FL-DDA initialised. " +
                  $"Starting score: {CurrentFuzzyScore:F3} | tier: {CurrentTier}");
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        _evalTimer += Time.deltaTime;
        if (_evalTimer >= evaluationInterval)
        {
            _evalTimer = 0f;
            EvaluateDifficulty();
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene _,
                               UnityEngine.SceneManagement.LoadSceneMode __)
    {
        StartCoroutine(ReapplyAfterLoad());
    }

    private IEnumerator ReapplyAfterLoad()
    {
        yield return null;
        yield return null;

        ResetWindow();
        _evalTimer = 0f;
        CachePlayer();
        ApplyFuzzyScoreToAllEnemies(CurrentFuzzyScore);

        Debug.Log($"[DifficultyManager] Scene loaded — persisted score {CurrentFuzzyScore:F3} " +
                  $"({CurrentTier}) applied. Window reset.");
    }

    private void CachePlayer()
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player == null)
            Debug.LogWarning("[DifficultyManager] PlayerController not found — " +
                             "health ratio will default to 1.0 (full health).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — called by WeaponController
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by WeaponController once per shot.
    /// <paramref name="hitEnemy"/> = true if the ray hit an EnemyAI.
    /// </summary>
    public void ReportShot(bool hitEnemy)
    {
        _shotsInWindow++;
        TotalShotsFired++;

        if (hitEnemy)
        {
            _hitsInWindow++;
            TotalShotsHit++;
        }

        TestMetricsCollector.Instance?.RecordShot(hitEnemy);

        Debug.Log($"[DifficultyManager] Shot: window {_hitsInWindow}/{_shotsInWindow} " +
                  $"({WindowAccuracy * 100f:F0}%) | lifetime {LifetimeAccuracy * 100f:F0}%");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fuzzy evaluation cycle
    // ─────────────────────────────────────────────────────────────────────────

    private void EvaluateDifficulty()
    {
        if (_shotsInWindow < minShotsBeforeEval)
        {
            Debug.Log($"[DifficultyManager] Skipping — only {_shotsInWindow}/" +
                      $"{minShotsBeforeEval} shots in window.");
            ResetWindow();
            return;
        }

        float accuracy    = WindowAccuracy;
        float healthRatio = HealthRatio;

        // ── Fuzzy inference ─────────────────────────────────────────────────
        float newScore = FuzzyDDA.Evaluate(accuracy, healthRatio, out FuzzyDebugSnapshot snap);

        ResetWindow();

        // ── Update state ────────────────────────────────────────────────────
        CurrentFuzzyScore = newScore;
        LastSnapshot      = snap;

        Debug.Log($"[DifficultyManager] FL-DDA eval | acc={accuracy * 100f:F0}% " +
                  $"hp={healthRatio * 100f:F0}% | {snap}");

        // ── Apply to enemies ────────────────────────────────────────────────
        ApplyFuzzyScoreToAllEnemies(newScore);

        // ── Derive display tier ─────────────────────────────────────────────
        DifficultyTier newTier = ScoreToTier(newScore);
        if (newTier != CurrentTier)
        {
            DifficultyTier prev = CurrentTier;
            CurrentTier = newTier;
            OnTierChanged?.Invoke(newTier);

            TestMetricsCollector.Instance?.RecordDDAChange(
                prev.ToString(), newTier.ToString(), accuracy);

            Debug.Log($"[DifficultyManager] Display tier: {prev} → {newTier} " +
                      $"(score={newScore:F3})");
        }

        // Always fire the continuous score event for research HUD/export
        OnFuzzyScoreUpdated?.Invoke(newScore, snap);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tier derivation from score
    // ─────────────────────────────────────────────────────────────────────────

    private DifficultyTier ScoreToTier(float score)
    {
        if (score < easyBandMax) return DifficultyTier.Easy;
        if (score > hardBandMin) return DifficultyTier.Hard;
        return DifficultyTier.Normal;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Apply settings to all living enemies
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyFuzzyScoreToAllEnemies(float score)
    {
        if (EnemyManager.Instance == null) return;

        EnemyDifficultySettings settings = BuildSettingsFromScore(score);
        var enemies = EnemyManager.Instance.GetLiveEnemies();

        foreach (IEnemy iEnemy in enemies)
        {
            if (iEnemy == null || !iEnemy.IsAlive) continue;
            if (iEnemy is not EnemyAI ai) continue;
            ai.ApplyDifficultySettings(settings);
        }

        Debug.Log($"[DifficultyManager] Applied score={score:F3} settings " +
                  $"to {enemies.Count} living enemies.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Settings builder — continuous interpolation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build an <see cref="EnemyDifficultySettings"/> by linearly interpolating
    /// between the Easy→Normal→Hard anchor multipliers using the fuzzy score.
    ///
    /// score ∈ [0.0, 0.5] : lerp from Easy anchor to Normal anchor
    /// score ∈ [0.5, 1.0] : lerp from Normal anchor to Hard anchor
    ///
    /// This produces smooth, continuous enemy scaling with no stat jumps.
    /// </summary>
    public EnemyDifficultySettings BuildSettingsFromScore(float score)
    {
        score = Mathf.Clamp01(score);

        EnemyDifficultySettings easy   = AnchorEasy();
        EnemyDifficultySettings normal = AnchorNormal();
        EnemyDifficultySettings hard   = AnchorHard();

        if (score <= 0.5f)
        {
            float t = score / 0.5f;   // 0 at Easy anchor, 1 at Normal anchor
            return Lerp(easy, normal, t);
        }
        else
        {
            float t = (score - 0.5f) / 0.5f;   // 0 at Normal anchor, 1 at Hard anchor
            return Lerp(normal, hard, t);
        }
    }

    /// <summary>
    /// Backward-compatible method called by EnemyAI.Start() when it initialises
    /// before the first fuzzy evaluation has run.  Maps tier to a score then
    /// delegates to <see cref="BuildSettingsFromScore"/>.
    /// </summary>
    public EnemyDifficultySettings BuildSettings(DifficultyTier tier)
    {
        float score = tier switch
        {
            DifficultyTier.Easy => 0.0f,
            DifficultyTier.Hard => 1.0f,
            _                   => 0.5f
        };
        return BuildSettingsFromScore(score);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Anchor constructors
    // ─────────────────────────────────────────────────────────────────────────

    private EnemyDifficultySettings AnchorEasy() => new()
    {
        healthMult      = easyHealthMult,
        patrolSpeedMult = easyPatrolSpeedMult,
        chaseSpeedMult  = easyChaseSpeedMult,
        damageMult      = easyDamageMult,
        detectionMult   = easyDetectionMult,
        attackRangeMult = easyAttackRangeMult,
        cooldownMult    = easyCooldownMult
    };

    private EnemyDifficultySettings AnchorNormal() => new()
    {
        healthMult      = normalHealthMult,
        patrolSpeedMult = normalPatrolSpeedMult,
        chaseSpeedMult  = normalChaseSpeedMult,
        damageMult      = normalDamageMult,
        detectionMult   = normalDetectionMult,
        attackRangeMult = normalAttackRangeMult,
        cooldownMult    = normalCooldownMult
    };

    private EnemyDifficultySettings AnchorHard() => new()
    {
        healthMult      = hardHealthMult,
        patrolSpeedMult = hardPatrolSpeedMult,
        chaseSpeedMult  = hardChaseSpeedMult,
        damageMult      = hardDamageMult,
        detectionMult   = hardDetectionMult,
        attackRangeMult = hardAttackRangeMult,
        cooldownMult    = hardCooldownMult
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Linear interpolation between two EnemyDifficultySettings
    // ─────────────────────────────────────────────────────────────────────────

    private static EnemyDifficultySettings Lerp(EnemyDifficultySettings a,
                                                EnemyDifficultySettings b,
                                                float t)
    {
        return new EnemyDifficultySettings
        {
            healthMult      = Mathf.Lerp(a.healthMult,      b.healthMult,      t),
            patrolSpeedMult = Mathf.Lerp(a.patrolSpeedMult, b.patrolSpeedMult, t),
            chaseSpeedMult  = Mathf.Lerp(a.chaseSpeedMult,  b.chaseSpeedMult,  t),
            damageMult      = Mathf.Lerp(a.damageMult,      b.damageMult,      t),
            detectionMult   = Mathf.Lerp(a.detectionMult,   b.detectionMult,   t),
            attackRangeMult = Mathf.Lerp(a.attackRangeMult, b.attackRangeMult, t),
            cooldownMult    = Mathf.Lerp(a.cooldownMult,    b.cooldownMult,    t)
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────────────────

    private void ResetWindow()
    {
        _shotsInWindow = 0;
        _hitsInWindow  = 0;
    }

    /// <summary>One-line summary for debug HUD.</summary>
    public string GetDebugSummary()
    {
        return $"FL-DDA | Score: {CurrentFuzzyScore:F3} | Tier: {CurrentTier} | " +
               $"Window: {WindowAccuracy * 100f:F0}% ({_hitsInWindow}/{_shotsInWindow}) | " +
               $"HP ratio: {HealthRatio * 100f:F0}% | " +
               $"Lifetime acc: {LifetimeAccuracy * 100f:F0}%";
    }

    /// <summary>Multi-line fuzzy membership breakdown for debug overlay.</summary>
    public string GetFuzzyBreakdown()
    {
        FuzzyDebugSnapshot s = LastSnapshot;
        return
            $"── Accuracy memberships ──\n" +
            $"  Low:    {s.accLow  :F3}\n" +
            $"  Medium: {s.accMed  :F3}\n" +
            $"  High:   {s.accHigh :F3}\n" +
            $"── HP memberships ─────────\n" +
            $"  Low:    {s.hpLow   :F3}\n" +
            $"  Medium: {s.hpMed   :F3}\n" +
            $"  High:   {s.hpHigh  :F3}\n" +
            $"── Output weights ─────────\n" +
            $"  VeryEasy: {s.wVeryEasy:F3}\n" +
            $"  Easy:     {s.wEasy    :F3}\n" +
            $"  Normal:   {s.wNormal  :F3}\n" +
            $"  Hard:     {s.wHard    :F3}\n" +
            $"  VeryHard: {s.wVeryHard:F3}\n" +
            $"── Result ─────────────────\n" +
            $"  Score: {s.crisp:F4}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test hooks
    // ─────────────────────────────────────────────────────────────────────────

    public int ShotsInWindow => _shotsInWindow;
    public int HitsInWindow  => _hitsInWindow;

    /// <summary>
    /// Pure static classifier — maps raw accuracy to a display tier.
    /// Thresholds match Chapter 3 design: below 30% = Easy, above 65% = Hard.
    /// Used by unit tests only; the live system uses FuzzyDDA.Evaluate().
    /// </summary>
    public static DifficultyTier ClassifyAccuracy(float accuracy)
    {
        if (accuracy < 0.30f) return DifficultyTier.Easy;
        if (accuracy > 0.65f) return DifficultyTier.Hard;
        return DifficultyTier.Normal;
    }

    /// <summary>
    /// Forces a fuzzy score and immediately applies it to all living enemies.
    /// Use only in PlayMode integration tests — never called during gameplay.
    /// </summary>
    public void ForceApplyFuzzyScore(float score)
    {
        CurrentFuzzyScore = Mathf.Clamp01(score);
        DifficultyTier newTier = ScoreToTier(CurrentFuzzyScore);
        if (newTier != CurrentTier)
        {
            CurrentTier = newTier;
            OnTierChanged?.Invoke(CurrentTier);
        }
        ApplyFuzzyScoreToAllEnemies(CurrentFuzzyScore);
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Data container — passed from DifficultyManager to EnemyAI (unchanged API)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Multiplier-based difficulty settings.
/// Built by DifficultyManager.BuildSettingsFromScore() via linear interpolation.
/// Read by EnemyAI.ApplyDifficultySettings().
///
/// All multipliers are applied against each enemy's stored base stats, so
/// roster balance is preserved at every point on the continuous difficulty curve.
/// cooldownMult is inverted — higher value = slower attack rate.
/// </summary>
[Serializable]
public struct EnemyDifficultySettings
{
    public float healthMult;
    public float patrolSpeedMult;
    public float chaseSpeedMult;
    public float damageMult;
    public float detectionMult;
    public float attackRangeMult;
    public float cooldownMult;
}

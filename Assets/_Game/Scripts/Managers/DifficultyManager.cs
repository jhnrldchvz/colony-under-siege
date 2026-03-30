using System;
using UnityEngine;

/// <summary>
/// DifficultyManager — Dynamic Difficulty Adjustment (DDA) system.
///
/// Tracks player accuracy over a rolling evaluation window.
/// When accuracy crosses a threshold, all living enemies are
/// upgraded or downgraded to match the new difficulty tier.
///
/// Tiers:
///   Easy   — accuracy below easyThreshold   (default 30%)
///   Normal — accuracy between easy and hard  (default 30–65%)
///   Hard   — accuracy above hardThreshold    (default 65%)
///
/// Setup:
///   1. Create an empty GameObject named "DifficultyManager".
///   2. Attach this script.
///   3. Tune thresholds and stat multipliers in the Inspector.
///   4. WeaponController already calls ReportShot() — no extra wiring needed.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static DifficultyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ---------------------------------------------------------------
    // Difficulty tiers
    // ---------------------------------------------------------------

    public enum DifficultyTier { Easy, Normal, Hard }

    public DifficultyTier CurrentTier { get; private set; } = DifficultyTier.Normal;

    // ---------------------------------------------------------------
    // Inspector — Accuracy thresholds
    // ---------------------------------------------------------------

    [Header("Accuracy Thresholds")]
    [Tooltip("Accuracy below this = Easy tier")]
    [Range(0f, 1f)] public float easyThreshold   = 0.30f;

    [Tooltip("Accuracy above this = Hard tier")]
    [Range(0f, 1f)] public float hardThreshold    = 0.65f;

    [Tooltip("Seconds between each difficulty evaluation")]
    public float evaluationInterval = 5f;

    [Tooltip("Minimum shots fired before DDA kicks in — prevents 1-shot samples")]
    public int minShotsBeforeEval   = 5;

    // ---------------------------------------------------------------
    // Inspector — Easy tier multipliers (applied on top of each enemy's base stats)
    // ---------------------------------------------------------------

    [Header("Easy Tier — Multipliers (< 1 = weaker)")]
    public float easyHealthMult       = 0.7f;
    public float easyPatrolSpeedMult  = 0.8f;
    public float easyChaseSpeedMult   = 0.7f;
    public float easyDamageMult       = 0.6f;
    public float easyDetectionMult    = 0.7f;
    public float easyAttackRangeMult  = 0.9f;
    public float easyCooldownMult     = 1.5f;  // Higher = slower attacks

    // ---------------------------------------------------------------
    // Inspector — Normal tier multipliers (1.0 = base stats unchanged)
    // ---------------------------------------------------------------

    [Header("Normal Tier — Multipliers (1.0 = base stats)")]
    public float normalHealthMult       = 1.0f;
    public float normalPatrolSpeedMult  = 1.0f;
    public float normalChaseSpeedMult   = 1.0f;
    public float normalDamageMult       = 1.0f;
    public float normalDetectionMult    = 1.0f;
    public float normalAttackRangeMult  = 1.0f;
    public float normalCooldownMult     = 1.0f;

    // ---------------------------------------------------------------
    // Inspector — Hard tier multipliers (> 1 = stronger)
    // ---------------------------------------------------------------

    [Header("Hard Tier — Multipliers (> 1 = stronger)")]
    public float hardHealthMult       = 1.5f;
    public float hardPatrolSpeedMult  = 1.5f;
    public float hardChaseSpeedMult   = 1.6f;
    public float hardDamageMult       = 2.0f;
    public float hardDetectionMult    = 1.4f;
    public float hardAttackRangeMult  = 1.2f;
    public float hardCooldownMult     = 0.6f;  // Lower = faster attacks

    // ---------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------

    /// <summary>Fired when the difficulty tier changes. Passes the new tier.</summary>
    public event Action<DifficultyTier> OnTierChanged;

    // ---------------------------------------------------------------
    // Private — accuracy tracking
    // ---------------------------------------------------------------

    private int   _shotsFiredinWindow = 0;
    private int   _shotsHitInWindow   = 0;
    private float _evalTimer          = 0f;

    // Lifetime stats for display
    public int   TotalShotsFired { get; private set; } = 0;
    public int   TotalShotsHit   { get; private set; } = 0;
    public float LifetimeAccuracy =>
        TotalShotsFired > 0 ? (float)TotalShotsHit / TotalShotsFired : 0f;
    public float WindowAccuracy =>
        _shotsFiredinWindow > 0
            ? (float)_shotsHitInWindow / _shotsFiredinWindow
            : 0f;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Wait one frame so all EnemyAI.Start() calls finish registering first
        StartCoroutine(InitializeAfterEnemiesRegister());
    }

    private System.Collections.IEnumerator InitializeAfterEnemiesRegister()
    {
        yield return null; // Wait one frame

        ApplyTierToAllEnemies(DifficultyTier.Normal);
        Debug.Log("[DifficultyManager] Initialized. Starting tier: Normal.");
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

    // ---------------------------------------------------------------
    // Public API — called by WeaponController
    // ---------------------------------------------------------------

    /// <summary>
    /// Called by WeaponController once per shot.
    /// hitEnemy = true if the ray hit an EnemyAI, false if it missed.
    /// </summary>
    public void ReportShot(bool hitEnemy)
    {
        _shotsFiredinWindow++;
        TotalShotsFired++;

        if (hitEnemy)
        {
            _shotsHitInWindow++;
            TotalShotsHit++;
        }

        // Report to test metrics collector if present in scene
        TestMetricsCollector.Instance?.RecordShot(hitEnemy);

        Debug.Log($"[DifficultyManager] Shot reported. Window: " +
                  $"{_shotsHitInWindow}/{_shotsFiredinWindow} " +
                  $"({WindowAccuracy * 100f:F0}%) | " +
                  $"Lifetime: {LifetimeAccuracy * 100f:F0}%");
    }

    // ---------------------------------------------------------------
    // Evaluation
    // ---------------------------------------------------------------

    private void EvaluateDifficulty()
    {
        // Not enough shots in this window to make a fair judgment
        if (_shotsFiredinWindow < minShotsBeforeEval)
        {
            Debug.Log($"[DifficultyManager] Skipping eval — only " +
                      $"{_shotsFiredinWindow}/{minShotsBeforeEval} shots in window.");
            ResetWindow();
            return;
        }

        float accuracy = WindowAccuracy;
        DifficultyTier newTier;

        if (accuracy < easyThreshold)
            newTier = DifficultyTier.Easy;
        else if (accuracy > hardThreshold)
            newTier = DifficultyTier.Hard;
        else
            newTier = DifficultyTier.Normal;

        Debug.Log($"[DifficultyManager] Eval: accuracy={accuracy * 100f:F0}% → tier={newTier}");

        ResetWindow();

        if (newTier != CurrentTier)
            SetTier(newTier);
    }

    private void SetTier(DifficultyTier newTier)
    {
        DifficultyTier previous = CurrentTier;
        CurrentTier = newTier;

        ApplyTierToAllEnemies(newTier);
        OnTierChanged?.Invoke(newTier);

        // Report to test metrics collector if present in scene
        TestMetricsCollector.Instance?.RecordDDAChange(
            previous.ToString(), newTier.ToString(), WindowAccuracy);

        Debug.Log($"[DifficultyManager] Tier changed: {previous} → {newTier}");
    }

    // ---------------------------------------------------------------
    // Apply stats to enemies
    // ---------------------------------------------------------------

    private void ApplyTierToAllEnemies(DifficultyTier tier)
    {
        if (EnemyManager.Instance == null) return;

        var enemies = EnemyManager.Instance.GetLiveEnemies();

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            enemy.ApplyDifficultySettings(BuildSettings(tier));
        }

        Debug.Log($"[DifficultyManager] Applied {tier} settings to " +
                  $"{enemies.Count} living enemies.");
    }

    // ---------------------------------------------------------------
    // Settings builder
    // ---------------------------------------------------------------

    public EnemyDifficultySettings BuildSettings(DifficultyTier tier)
    {
        switch (tier)
        {
            case DifficultyTier.Easy:
                return new EnemyDifficultySettings
                {
                    healthMult       = easyHealthMult,
                    patrolSpeedMult  = easyPatrolSpeedMult,
                    chaseSpeedMult   = easyChaseSpeedMult,
                    damageMult       = easyDamageMult,
                    detectionMult    = easyDetectionMult,
                    attackRangeMult  = easyAttackRangeMult,
                    cooldownMult     = easyCooldownMult
                };

            case DifficultyTier.Hard:
                return new EnemyDifficultySettings
                {
                    healthMult       = hardHealthMult,
                    patrolSpeedMult  = hardPatrolSpeedMult,
                    chaseSpeedMult   = hardChaseSpeedMult,
                    damageMult       = hardDamageMult,
                    detectionMult    = hardDetectionMult,
                    attackRangeMult  = hardAttackRangeMult,
                    cooldownMult     = hardCooldownMult
                };

            default: // Normal
                return new EnemyDifficultySettings
                {
                    healthMult       = normalHealthMult,
                    patrolSpeedMult  = normalPatrolSpeedMult,
                    chaseSpeedMult   = normalChaseSpeedMult,
                    damageMult       = normalDamageMult,
                    detectionMult    = normalDetectionMult,
                    attackRangeMult  = normalAttackRangeMult,
                    cooldownMult     = normalCooldownMult
                };
        }
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    private void ResetWindow()
    {
        _shotsFiredinWindow = 0;
        _shotsHitInWindow   = 0;
    }

    /// <summary>Returns a readable summary for a debug HUD.</summary>
    public string GetDebugSummary()
    {
        return $"Tier: {CurrentTier} | " +
               $"Window: {WindowAccuracy * 100f:F0}% ({_shotsHitInWindow}/{_shotsFiredinWindow}) | " +
               $"Lifetime: {LifetimeAccuracy * 100f:F0}%";
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

// ---------------------------------------------------------------
// Data container — passed from DifficultyManager to EnemyAI
// ---------------------------------------------------------------

/// <summary>
/// Plain data struct carrying all stat values for one difficulty tier.
/// DifficultyManager builds it, EnemyAI.ApplyDifficultySettings() reads it.
/// </summary>
/// <summary>
/// Multiplier-based difficulty settings.
/// Each value is multiplied against the enemy's own base stats
/// so every enemy type scales proportionally — roster balance
/// is preserved at all difficulty tiers.
///
/// Normal tier = all 1.0 (no change from base).
/// Easy tier   = values below 1.0 (weaker enemies).
/// Hard tier   = values above 1.0 (stronger enemies).
/// cooldownMult is inverted — higher value = slower attacks.
/// </summary>
[System.Serializable]
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
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
    // Inspector — Easy tier stats
    // ---------------------------------------------------------------

    [Header("Easy Tier — Enemy Stats")]
    public float easyPatrolSpeed    = 2f;
    public float easyChaseSpeed     = 3.5f;
    public int   easyMaxHealth      = 35;
    public int   easyAttackDamage   = 7;
    public float easyDetectionRange = 7f;
    public float easyAttackRange    = 2f;
    public float easyAttackCooldown = 2f;

    // ---------------------------------------------------------------
    // Inspector — Normal tier stats
    // ---------------------------------------------------------------

    [Header("Normal Tier — Enemy Stats")]
    public float normalPatrolSpeed    = 2.5f;
    public float normalChaseSpeed     = 5f;
    public int   normalMaxHealth      = 50;
    public int   normalAttackDamage   = 10;
    public float normalDetectionRange = 10f;
    public float normalAttackRange    = 2f;
    public float normalAttackCooldown = 1.5f;

    // ---------------------------------------------------------------
    // Inspector — Hard tier stats
    // ---------------------------------------------------------------

    [Header("Hard Tier — Enemy Stats")]
    public float hardPatrolSpeed    = 4f;
    public float hardChaseSpeed     = 8f;
    public int   hardMaxHealth      = 80;
    public int   hardAttackDamage   = 20;
    public float hardDetectionRange = 14f;
    public float hardAttackRange    = 3f;
    public float hardAttackCooldown = 0.8f;

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
        // Apply normal difficulty on start
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
                    patrolSpeed    = easyPatrolSpeed,
                    chaseSpeed     = easyChaseSpeed,
                    maxHealth      = easyMaxHealth,
                    attackDamage   = easyAttackDamage,
                    detectionRange = easyDetectionRange,
                    attackRange    = easyAttackRange,
                    attackCooldown = easyAttackCooldown
                };

            case DifficultyTier.Hard:
                return new EnemyDifficultySettings
                {
                    patrolSpeed    = hardPatrolSpeed,
                    chaseSpeed     = hardChaseSpeed,
                    maxHealth      = hardMaxHealth,
                    attackDamage   = hardAttackDamage,
                    detectionRange = hardDetectionRange,
                    attackRange    = hardAttackRange,
                    attackCooldown = hardAttackCooldown
                };

            default: // Normal
                return new EnemyDifficultySettings
                {
                    patrolSpeed    = normalPatrolSpeed,
                    chaseSpeed     = normalChaseSpeed,
                    maxHealth      = normalMaxHealth,
                    attackDamage   = normalAttackDamage,
                    detectionRange = normalDetectionRange,
                    attackRange    = normalAttackRange,
                    attackCooldown = normalAttackCooldown
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
[System.Serializable]
public struct EnemyDifficultySettings
{
    public float patrolSpeed;
    public float chaseSpeed;
    public int   maxHealth;
    public int   attackDamage;
    public float detectionRange;
    public float attackRange;
    public float attackCooldown;
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyManager — tracks all living enemies in the current scene.
///
/// Responsibilities:
///   - Maintains a live List of EnemyAI instances
///   - Tracks total spawned vs killed for objective checking
///   - Fires OnAllEnemiesDefeated when the list empties
///   - Provides kill count to UIManager and ObjectiveManager
///
/// Setup:
///   Create an empty GameObject named "EnemyManager" in the scene.
///   Attach this script to it.
///   Enemies self-register on Start() and self-remove on death —
///   no manual wiring needed.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static EnemyManager Instance { get; private set; }

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
    // Events
    // ---------------------------------------------------------------

    /// <summary>
    /// Fired when every enemy in the scene has been killed.
    /// ObjectiveManager subscribes to this to check kill objectives.
    /// </summary>
    public event Action OnAllEnemiesDefeated;

    /// <summary>
    /// Fired each time an enemy dies. Passes current kill count.
    /// UIManager can subscribe to show a kill counter if needed.
    /// </summary>
    public event Action<int> OnEnemyKilled;

    // ---------------------------------------------------------------
    // Public read-only stats
    // ---------------------------------------------------------------

    public int TotalEnemies { get; private set; } = 0;
    public int KillCount    { get; private set; } = 0;
    public int AliveCount   => _liveEnemies.Count;

    // ---------------------------------------------------------------
    // Private — HashSet for O(1) Contains / Remove
    // ---------------------------------------------------------------

    private readonly HashSet<EnemyAI> _liveEnemies = new HashSet<EnemyAI>();

    // ---------------------------------------------------------------
    // Registration — called by EnemyAI.Start() and EnemyAI.Die()
    // ---------------------------------------------------------------

    /// <summary>
    /// Called by EnemyAI.Start() — adds the enemy to the live set.
    /// </summary>
    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy == null || !_liveEnemies.Add(enemy)) return;

        TotalEnemies++;
        Debug.Log($"[EnemyManager] Registered: {enemy.gameObject.name}. " +
                  $"Total: {TotalEnemies}, Alive: {AliveCount}");
    }

    /// <summary>
    /// Called by EnemyAI.Die() — removes the enemy from the live set
    /// and checks if all enemies are defeated.
    /// </summary>
    public void DeregisterEnemy(EnemyAI enemy)
    {
        if (enemy == null || !_liveEnemies.Remove(enemy)) return;

        KillCount++;
        Debug.Log($"[EnemyManager] Killed: {enemy.gameObject.name}. " +
                  $"Kills: {KillCount}/{TotalEnemies}, Alive: {AliveCount}");

        OnEnemyKilled?.Invoke(KillCount);

        if (_liveEnemies.Count == 0)
        {
            Debug.Log("[EnemyManager] All enemies defeated!");
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    // ---------------------------------------------------------------
    // Queries — used by ObjectiveManager
    // ---------------------------------------------------------------

    /// <summary>Returns true if all registered enemies are now dead.</summary>
    public bool AreAllEnemiesDefeated()
    {
        return TotalEnemies > 0 && _liveEnemies.Count == 0;
    }

    /// <summary>Returns true if at least the required number of enemies are killed.</summary>
    public bool HasReachedKillTarget(int target)
    {
        return KillCount >= target;
    }

    /// <summary>Returns a read-only snapshot of all currently living enemies.</summary>
    public IReadOnlyCollection<EnemyAI> GetLiveEnemies()
    {
        return _liveEnemies;
    }

    // ---------------------------------------------------------------
    // Reset — called by LevelManager on scene restart
    // ---------------------------------------------------------------

    /// <summary>
    /// Clears all tracking data. Called when restarting the current stage.
    /// Enemies re-register themselves when the scene reloads.
    /// </summary>
    public void ResetEnemyData()
    {
        _liveEnemies.Clear();
        TotalEnemies = 0;
        KillCount    = 0;

        Debug.Log("[EnemyManager] Enemy data reset.");
    }

    // ---------------------------------------------------------------
    // Cleanup
    // ---------------------------------------------------------------

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AICoreManager — heals all alive enemies periodically.
/// Active at scene start. Deactivated when all 4 terminals are switched off
/// via TerminalGroupController.onAllDeactivated.
///
/// Setup:
///   1. Create empty GameObject "AICoreManager" in Stage [4]
///   2. Attach this script
///   3. Wire TerminalGroupController.onAllDeactivated → AICoreManager.Deactivate()
/// </summary>
public class AICoreManager : MonoBehaviour
{
    [Header("Healing")]
    [Tooltip("Seconds between heal pulses")]
    public float healInterval  = 8f;

    [Tooltip("HP restored to each enemy per pulse")]
    public int   healAmount    = 15;

    [Header("Events")]
    public UnityEvent onDeactivated;  // Wire to: AccessKey GO SetActive(true)

    // ---------------------------------------------------------------
    public bool IsActive { get; private set; } = true;

    private float _healTimer = 0f;

    private void Update()
    {
        if (!IsActive) return;

        _healTimer += Time.deltaTime;
        if (_healTimer >= healInterval)
        {
            _healTimer = 0f;
            HealAllEnemies();
        }
    }

    private void HealAllEnemies()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        int count = 0;

        foreach (EnemyAI enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            enemy.Heal(healAmount);
            count++;
        }

        Debug.Log($"[AICoreManager] Healed {count} enemies by {healAmount} HP.");
    }

    /// <summary>
    /// Called by TerminalGroupController.onAllDeactivated UnityEvent.
    /// Stops healing and fires onDeactivated for access key reveal.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive   = false;
        _healTimer = 0f;

        Debug.Log("[AICoreManager] AI Core deactivated — healing stopped.");
        onDeactivated?.Invoke();
    }
}
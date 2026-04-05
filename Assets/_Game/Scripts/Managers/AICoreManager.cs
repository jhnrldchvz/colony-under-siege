using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AICoreManager — heals all alive enemies every second.
/// Active at scene start. Deactivated when all 4 terminals are switched off.
///
/// Setup:
///   1. Create empty GameObject "AICoreManager" in Stage [4]
///   2. Attach this script
///   3. Wire healingWarningBanner — drag the warning panel GO directly
///   4. Wire TerminalGroupController.onAllDeactivated → AICoreManager.Deactivate()
///   5. Wire onDeactivated → AccessKey GO SetActive(true)
/// </summary>
public class AICoreManager : MonoBehaviour
{
    [Header("Healing")]
    public float healInterval = 1f;
    public int   healAmount   = 5;

    [Header("Warning Banner")]
    [Tooltip("Drag the HUD warning panel here — shown on start, hidden on deactivate")]
    public GameObject healingWarningBanner;

    [Tooltip("Enable blinking effect on the banner")]
    public bool  blinkBanner    = true;
    [Tooltip("Blinks per second")]
    public float blinkSpeed     = 2f;

    [Header("Events")]
    public UnityEvent onDeactivated;

    // ---------------------------------------------------------------
    public bool IsActive { get; private set; } = true;

    private float _healTimer  = 0f;
    private float _blinkTimer = 0f;

    private void Start()
    {
        if (healingWarningBanner != null)
            healingWarningBanner.SetActive(true);
    }

    private void Update()
    {
        if (!IsActive) return;

        _healTimer += Time.deltaTime;

        if (_healTimer >= healInterval)
        {
            _healTimer = 0f;
            HealAllEnemies();
        }

        // Blink the banner
        if (blinkBanner && healingWarningBanner != null)
        {
            _blinkTimer += Time.deltaTime * blinkSpeed;
            healingWarningBanner.SetActive(Mathf.Sin(_blinkTimer * Mathf.PI) > 0f);
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

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive   = false;
        _healTimer = 0f;

        if (healingWarningBanner != null)
            healingWarningBanner.SetActive(false);

        Debug.Log("[AICoreManager] AI Core deactivated — healing stopped.");
        onDeactivated?.Invoke();
    }
}
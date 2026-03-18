using UnityEngine;

/// <summary>
/// Objective — ScriptableObject that defines a single stage objective.
///
/// Create one asset per objective via:
///   Right-click in Project window →
///   Create → Colony Under Siege → Objective
///
/// Examples:
///   Type: KillAll        → kill every enemy in the scene
///   Type: KillCount      → kill at least N enemies
///   Type: CollectItem    → pick up a specific key item
///   Type: ActivateSwitch → interact with a specific switch
/// </summary>
[CreateAssetMenu(fileName = "Objective_New",
                 menuName  = "Colony Under Siege/Objective")]
public class Objective : ScriptableObject
{
    // ---------------------------------------------------------------
    // Objective type enum
    // ---------------------------------------------------------------

    public enum ObjectiveType
    {
        KillAll,         // Defeat every enemy registered in EnemyManager
        KillCount,       // Defeat at least 'requiredCount' enemies
        CollectItem,     // Add item with 'requiredItemId' to inventory
        ActivateSwitch   // Interact with a switch that has 'requiredItemId' as its ID
    }

    // ---------------------------------------------------------------
    // Inspector fields — fill these in the ScriptableObject asset
    // ---------------------------------------------------------------

    [Header("Identity")]
    [Tooltip("Short description shown in the HUD, e.g. 'Defeat all enemies'")]
    public string objectiveText = "Complete the objective";

    [Tooltip("Unique ID used to reference this objective in code")]
    public string objectiveId = "objective_01";

    [Header("Type")]
    public ObjectiveType type = ObjectiveType.KillAll;

    [Header("Requirements")]
    [Tooltip("For KillCount: number of kills needed. Ignored for other types.")]
    public int requiredCount = 0;

    [Tooltip("For CollectItem / ActivateSwitch: the item/switch ID to match.")]
    public string requiredItemId = "";

    // ---------------------------------------------------------------
    // Runtime state — tracked by ObjectiveManager at runtime
    // NOT serialized — resets every play session automatically
    // ---------------------------------------------------------------

    [System.NonSerialized] public int  CurrentCount  = 0;
    [System.NonSerialized] public bool IsComplete     = false;

    // ---------------------------------------------------------------
    // Runtime helpers
    // ---------------------------------------------------------------

    /// <summary>Resets runtime state. Called by ObjectiveManager on scene load.</summary>
    public void ResetRuntime()
    {
        CurrentCount = 0;
        IsComplete   = false;
    }

    /// <summary>
    /// Checks if this objective is satisfied given the current game state.
    /// ObjectiveManager calls this after every relevant event.
    /// </summary>
    public bool Evaluate()
    {
        if (IsComplete) return true;

        switch (type)
        {
            case ObjectiveType.KillAll:
                IsComplete = EnemyManager.Instance != null &&
                             EnemyManager.Instance.AreAllEnemiesDefeated();
                break;

            case ObjectiveType.KillCount:
                CurrentCount = EnemyManager.Instance != null
                    ? EnemyManager.Instance.KillCount
                    : 0;
                IsComplete = CurrentCount >= requiredCount;
                break;

            case ObjectiveType.CollectItem:
                IsComplete = InventoryManager.Instance != null &&
                             InventoryManager.Instance.HasKeyItem(requiredItemId);
                break;

            case ObjectiveType.ActivateSwitch:
                // SwitchInteractable calls ObjectiveManager.NotifySwitchActivated(id)
                // which sets IsComplete directly — nothing to evaluate here
                break;
        }

        return IsComplete;
    }

    /// <summary>
    /// Builds the HUD display string for this objective.
    /// e.g. "Defeat all enemies  [3/5]"
    /// </summary>
    public string GetDisplayText()
    {
        if (IsComplete)
            return $"✓ {objectiveText}";

        switch (type)
        {
            case ObjectiveType.KillCount:
                int kc = EnemyManager.Instance != null
                    ? EnemyManager.Instance.KillCount : CurrentCount;
                return $"• {objectiveText}  [{kc}/{requiredCount}]";

            case ObjectiveType.KillAll:
                int alive = EnemyManager.Instance != null
                    ? EnemyManager.Instance.AliveCount : 0;
                int total = EnemyManager.Instance != null
                    ? EnemyManager.Instance.TotalEnemies : 0;
                int killed = total - alive;
                // Show 0/0 as 0/? until enemies register
                string countStr = total > 0 ? $"{killed}/{total}" : "0/?";
                return $"• {objectiveText}  [{countStr}]";

            case ObjectiveType.CollectItem:
                bool hasItem = InventoryManager.Instance != null &&
                               InventoryManager.Instance.HasKeyItem(requiredItemId);
                return $"• {objectiveText}  [{(hasItem ? "1/1" : "0/1")}]";

            case ObjectiveType.ActivateSwitch:
                return $"• {objectiveText}";

            default:
                return $"• {objectiveText}";
        }
    }
}
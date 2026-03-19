using UnityEngine;

/// <summary>
/// Objective — ScriptableObject that defines a single stage objective.
/// Pure data only — no runtime state lives here.
/// ObjectiveManager wraps each Objective in an ObjectiveRuntime instance
/// that owns CurrentCount, IsComplete, etc. per session.
///
/// Create via: right-click Project → Create → Colony Under Siege → Objective
/// </summary>
[CreateAssetMenu(fileName = "Objective_New",
                 menuName  = "Colony Under Siege/Objective")]
public class Objective : ScriptableObject
{
    public enum ObjectiveType
    {
        KillAll,
        KillCount,
        CollectItem,
        ActivateSwitch
    }

    [Header("Identity")]
    public string objectiveText = "Complete the objective";
    public string objectiveId   = "objective_01";

    [Header("Type")]
    public ObjectiveType type = ObjectiveType.KillAll;

    [Header("Requirements")]
    [Tooltip("For KillCount: kills needed.")]
    public int    requiredCount  = 0;

    [Tooltip("For CollectItem / ActivateSwitch: item or switch ID.")]
    public string requiredItemId = "";
}


/// <summary>
/// ObjectiveRuntime — mutable session state for one Objective.
/// ObjectiveManager creates one per Objective per scene load.
/// ScriptableObject assets are never mutated.
/// </summary>
public class ObjectiveRuntime
{
    public Objective Data         { get; }
    public int       CurrentCount { get; set; } = 0;
    public bool      IsComplete   { get; set; } = false;

    public ObjectiveRuntime(Objective data) { Data = data; }

    /// <summary>Evaluates completion against current game state.</summary>
    public bool Evaluate()
    {
        if (IsComplete) return true;

        switch (Data.type)
        {
            case Objective.ObjectiveType.KillAll:
                IsComplete = EnemyManager.Instance != null &&
                             EnemyManager.Instance.AreAllEnemiesDefeated();
                break;

            case Objective.ObjectiveType.KillCount:
                CurrentCount = EnemyManager.Instance != null
                    ? EnemyManager.Instance.KillCount : 0;
                IsComplete = CurrentCount >= Data.requiredCount;
                break;

            case Objective.ObjectiveType.CollectItem:
                IsComplete = InventoryManager.Instance != null &&
                             InventoryManager.Instance.HasKeyItem(Data.requiredItemId);
                break;

            case Objective.ObjectiveType.ActivateSwitch:
                // Set directly by ObjectiveManager.NotifySwitchActivated()
                break;
        }

        return IsComplete;
    }

    /// <summary>Builds HUD display string for this objective.</summary>
    public string GetDisplayText()
    {
        if (IsComplete)
            return $"✓ {Data.objectiveText}";

        switch (Data.type)
        {
            case Objective.ObjectiveType.KillCount:
                int kc = EnemyManager.Instance != null
                    ? EnemyManager.Instance.KillCount : CurrentCount;
                return $"• {Data.objectiveText}  [{kc}/{Data.requiredCount}]";

            case Objective.ObjectiveType.KillAll:
                int alive  = EnemyManager.Instance?.AliveCount    ?? 0;
                int total  = EnemyManager.Instance?.TotalEnemies  ?? 0;
                int killed = total - alive;
                string countStr = total > 0 ? $"{killed}/{total}" : "0/?";
                return $"• {Data.objectiveText}  [{countStr}]";

            case Objective.ObjectiveType.CollectItem:
                bool has = InventoryManager.Instance != null &&
                           InventoryManager.Instance.HasKeyItem(Data.requiredItemId);
                return $"• {Data.objectiveText}  [{(has ? "1/1" : "0/1")}]";

            default:
                return $"• {Data.objectiveText}";
        }
    }
}
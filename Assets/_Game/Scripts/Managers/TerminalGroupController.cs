using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TerminalGroupController — tracks N SwitchInteractables.
/// When all are activated (switched off), fires onAllDeactivated.
/// Reports count to ObjectiveManager for HUD display [X/4].
///
/// Setup:
///   1. Create empty GameObject "TerminalGroupController" in Stage [4]
///   2. Attach this script
///   3. Drag all 4 terminal SwitchInteractable GameObjects into terminals[]
///   4. Set switchId = "terminals_group"
///   5. Wire onAllDeactivated → AICoreManager.Deactivate()
/// </summary>
public class TerminalGroupController : MonoBehaviour
{
    [Header("Terminals")]
    [Tooltip("Drag all 4 SwitchInteractable GameObjects here")]
    public SwitchInteractable[] terminals;

    [Header("Objective")]
    [Tooltip("Must match Obj_L4_Terminals.requiredItemId")]
    public string switchId = "terminals_group";

    [Header("Events")]
    public UnityEvent onAllDeactivated;  // Wire → AICoreManager.Deactivate()

    // ---------------------------------------------------------------
    private bool _complete        = false;
    private int  _lastActiveCount = -1;

    private void Update()
    {
        if (_complete || terminals == null || terminals.Length == 0) return;

        int activeCount = CountActivated();

        // Update HUD count when changed
        if (activeCount != _lastActiveCount)
        {
            _lastActiveCount = activeCount;
            ObjectiveManager.Instance?.UpdatePlateCount(switchId, activeCount);
        }

        // All terminals deactivated
        if (activeCount >= terminals.Length)
        {
            _complete = true;
            ObjectiveManager.Instance?.NotifySwitchActivated(switchId);
            onAllDeactivated?.Invoke();
            Debug.Log("[TerminalGroupController] All terminals deactivated.");
        }
    }

    private int CountActivated()
    {
        int count = 0;
        foreach (SwitchInteractable t in terminals)
            if (t != null && t.IsOn) count++;
        return count;
    }

    private void OnDrawGizmosSelected()
    {
        if (terminals == null) return;
        Gizmos.color = Color.cyan;
        foreach (SwitchInteractable t in terminals)
            if (t != null)
                Gizmos.DrawLine(transform.position, t.transform.position);
    }
}
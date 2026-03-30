using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PlateDoorController — opens a door only when ALL assigned pressure
/// plates are active simultaneously. Works with any number of plates.
///
/// Setup:
///   1. Create an empty GameObject → rename "PlateDoorController"
///   2. Attach this script
///   3. Drag all PressurePlate GameObjects into the plates array
///   4. Wire onAllActivated → door SetActive(false) or Animator
///   5. Wire onAnyDeactivated → door SetActive(true) or reverse
///   6. Optionally wire a switchId to complete an objective on all-active
/// </summary>
public class PlateDoorController : MonoBehaviour
{
    [Header("Plates — all must be active to open")]
    public PressurePlate[] plates;

    [Header("Events")]
    public UnityEvent onAllActivated;
    public UnityEvent onAnyDeactivated;

    [Header("Objective")]
    [Tooltip("Notifies ObjectiveManager when all plates are active")]
    public string switchId = "";

    // ---------------------------------------------------------------
    private bool _doorOpen = false;

    private int _lastActiveCount = -1;

    private void Update()
    {
        if (plates == null || plates.Length == 0) return;

        bool allActive   = AllPlatesActive();
        int  activeCount = CountActivePlates();

        // Update plate count on HUD whenever it changes
        if (activeCount != _lastActiveCount)
        {
            _lastActiveCount = activeCount;
            if (!string.IsNullOrEmpty(switchId) && ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.UpdatePlateCount(switchId, activeCount);
        }

        if (allActive && !_doorOpen)
        {
            _doorOpen = true;
            onAllActivated?.Invoke();

            if (!string.IsNullOrEmpty(switchId) && ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.NotifySwitchActivated(switchId);

            Debug.Log($"[PlateDoorController] All {plates.Length} plates active — door open.");
        }
        else if (!allActive && _doorOpen)
        {
            _doorOpen = false;
            onAnyDeactivated?.Invoke();

            if (!string.IsNullOrEmpty(switchId) && ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.ResetSwitchObjective(switchId);

            Debug.Log("[PlateDoorController] A plate deactivated — door closed.");
        }
    }

    private int CountActivePlates()
    {
        int count = 0;
        foreach (PressurePlate plate in plates)
            if (plate != null && plate.IsActive) count++;
        return count;
    }

    private bool AllPlatesActive()
    {
        foreach (PressurePlate plate in plates)
        {
            if (plate == null || !plate.IsActive) return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (plates == null) return;
        Gizmos.color = Color.yellow;
        foreach (PressurePlate plate in plates)
        {
            if (plate != null)
                Gizmos.DrawLine(transform.position, plate.transform.position);
        }
    }
}
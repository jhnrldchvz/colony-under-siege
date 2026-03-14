using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// SwitchInteractable — a toggle switch or lever the player can activate.
///
/// When the player presses E while looking at this object:
///   - Toggles its own on/off state
///   - Fires a UnityEvent (wire doors, bridges, gates in the Inspector)
///   - Notifies ObjectiveManager if this switch is an objective target
///
/// Setup:
///   1. Add this script to any GameObject with a Collider.
///   2. Set the GameObject's layer to "Interactable".
///   3. Assign a unique switchId matching the Objective asset's requiredItemId.
///   4. Wire onSwitchActivated to the door/bridge Animator or SetActive.
/// </summary>
public class SwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip("Must match the requiredItemId in the Objective asset for ActivateSwitch objectives")]
    public string switchId = "switch_01";

    [Header("State")]
    [Tooltip("Can this switch be toggled on and off, or only activated once?")]
    public bool isToggleable = false;

    public bool IsOn { get; private set; } = false;

    [Header("Events")]
    [Tooltip("Fired when the switch turns ON — wire your door/bridge/gate here")]
    public UnityEvent onSwitchActivated;

    [Tooltip("Fired when a toggleable switch turns OFF")]
    public UnityEvent onSwitchDeactivated;

    [Header("Visual (optional)")]
    [Tooltip("Drag a child mesh here to rotate or move it as a visual indicator")]
    public Transform switchVisual;

    [Tooltip("Local rotation applied to switchVisual when ON")]
    public Vector3 activatedRotation = new Vector3(45f, 0f, 0f);

    private Quaternion _offRotation;
    private Quaternion _onRotation;

    private void Start()
    {
        if (switchVisual != null)
        {
            _offRotation = switchVisual.localRotation;
            _onRotation  = Quaternion.Euler(activatedRotation);
        }
    }

    public void Interact(PlayerController player)
    {
        if (!isToggleable && IsOn) return; // One-time switch already used

        IsOn = !IsOn;

        // Rotate the visual handle
        if (switchVisual != null)
            switchVisual.localRotation = IsOn ? _onRotation : _offRotation;

        if (IsOn)
        {
            onSwitchActivated?.Invoke();

            // Notify ObjectiveManager so ActivateSwitch objectives progress
            if (ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.NotifySwitchActivated(switchId);

            Debug.Log($"[Switch] '{switchId}' activated.");
        }
        else
        {
            onSwitchDeactivated?.Invoke();
            Debug.Log($"[Switch] '{switchId}' deactivated.");
        }
    }
}
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PressurePlate — a floor trigger that activates when a heavy enough
/// object is placed on it. Releases when the object is removed.
///
/// Use case in Bio-Lab:
///   Player picks up a heavy box, carefully places it on the plate,
///   the door opens. If the box is removed the door closes again.
///   Player must find a way to keep the plate pressed permanently
///   (e.g. shoot a crate onto it, or find a second box).
///
/// Setup:
///   1. Create a flat cube → scale (1.5, 0.1, 1.5) → rename PressurePlate
///   2. Add Box Collider — Is Trigger: ON
///   3. Attach this script
///   4. Wire onActivated → door SetActive(false) or Animator
///   5. Wire onDeactivated → door SetActive(true) or Animator reverse
/// </summary>
public class PressurePlate : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Minimum mass required to activate the plate")]
    public float minMass           = 3f;

    [Tooltip("If true — once activated it stays on even if object removed")]
    public bool  lockOnActivation  = false;

    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    [Header("Visual")]
    [Tooltip("Renderer on the plate — changes color when active")]
    public Renderer plateRenderer;
    public Color    inactiveColor  = new Color(0.3f, 0.3f, 0.3f);
    public Color    activeColor    = new Color(0.2f, 0.8f, 0.2f);

    [Header("Objective")]
    [Tooltip("Optional — notifies ObjectiveManager when activated")]
    public string switchId         = "";

    // ---------------------------------------------------------------
    public bool IsActive { get; private set; } = false;

    private int _objectsOnPlate = 0;

    private void Start()
    {
        SetVisual(false);
    }

    // ---------------------------------------------------------------
    // Trigger detection
    // ---------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (lockOnActivation && IsActive) return;

        // Only react to rigidbodies with enough mass
        Rigidbody rb = other.GetComponent<Rigidbody>() ??
                       other.GetComponentInParent<Rigidbody>();

        if (rb == null || rb.mass < minMass) return;
        if (other.isTrigger) return;

        _objectsOnPlate++;
        Debug.Log($"[PressurePlate] Object entered: {other.name} " +
                  $"mass={rb.mass} count={_objectsOnPlate}");

        if (!IsActive)
            Activate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (lockOnActivation && IsActive) return;

        Rigidbody rb = other.GetComponent<Rigidbody>() ??
                       other.GetComponentInParent<Rigidbody>();

        if (rb == null || rb.mass < minMass) return;
        if (other.isTrigger) return;

        _objectsOnPlate = Mathf.Max(0, _objectsOnPlate - 1);
        Debug.Log($"[PressurePlate] Object left: {other.name} " +
                  $"count={_objectsOnPlate}");

        if (_objectsOnPlate == 0 && IsActive)
            Deactivate();
    }

    // ---------------------------------------------------------------
    // Activate / Deactivate
    // ---------------------------------------------------------------

    private void Activate()
    {
        IsActive = true;
        SetVisual(true);
        onActivated?.Invoke();

        if (!string.IsNullOrEmpty(switchId) && ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.NotifySwitchActivated(switchId);

        Debug.Log("[PressurePlate] Activated.");
    }

    private void Deactivate()
    {
        IsActive = false;
        SetVisual(false);
        onDeactivated?.Invoke();

        Debug.Log("[PressurePlate] Deactivated — object removed.");
    }

    // ---------------------------------------------------------------
    // Visual feedback
    // ---------------------------------------------------------------

    private void SetVisual(bool active)
    {
        if (plateRenderer == null) return;
        plateRenderer.material.color = active ? activeColor : inactiveColor;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsActive
            ? new Color(0.2f, 0.8f, 0.2f, 0.3f)
            : new Color(0.8f, 0.8f, 0.2f, 0.2f);

        Gizmos.matrix = transform.localToWorldMatrix;
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
            Gizmos.DrawCube(bc.center, bc.size);
    }
}
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

    [Header("Highlight — shows when player holds a heavy box")]
    [Tooltip("Quick Outline component on this plate — pulses when player is carrying")]
    public Outline  plateOutline;
    public Color    highlightColor = new Color(1f, 0.85f, 0.1f);
    public Color    activeOutlineColor = new Color(0.2f, 1f, 0.2f);

    [Header("Objective")]
    [Tooltip("Optional — notifies ObjectiveManager when activated")]
    public string switchId         = "";

    // ---------------------------------------------------------------
    public bool IsActive { get; private set; } = false;

    private System.Collections.Generic.HashSet<Collider> _objectsOnPlate
        = new System.Collections.Generic.HashSet<Collider>();
    private bool  _playerHolding  = false;
    private float _pulseTimer     = 0f;

    private void Start()
    {
        SetVisual(false);
        if (plateOutline != null) plateOutline.enabled = false;
    }

    private void Update()
    {
        if (_playerHolding && !IsActive && plateOutline != null)
        {
            // Pulse outline to draw attention
            _pulseTimer += Time.deltaTime * 3f;
            plateOutline.OutlineWidth = 4f + Mathf.Sin(_pulseTimer) * 2f;
        }
    }

    /// <summary>Called by PressurePlateHeavyBox when player picks up the box.</summary>
    public void ShowHighlight()
    {
        _playerHolding = true;
        if (plateOutline != null)
        {
            plateOutline.OutlineColor = highlightColor;
            plateOutline.enabled      = true;
        }
    }

    /// <summary>Called by PressurePlateHeavyBox when player drops/throws the box.</summary>
    public void HideHighlight()
    {
        _playerHolding = false;
        _pulseTimer    = 0f;
        if (plateOutline != null && !IsActive)
            plateOutline.enabled = false;
    }

    // ---------------------------------------------------------------
    // Trigger detection
    // ---------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (lockOnActivation && IsActive) return;
        if (other.isTrigger) return;

        Rigidbody rb = other.GetComponent<Rigidbody>() ??
                       other.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.mass < minMass) return;

        _objectsOnPlate.Add(other);
        Debug.Log($"[PressurePlate] Object entered: {other.name} " +
                  $"mass={rb.mass} count={_objectsOnPlate.Count}");

        if (!IsActive) Activate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (lockOnActivation && IsActive) return;
        if (other.isTrigger) return;

        Rigidbody rb = other.GetComponent<Rigidbody>() ??
                       other.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.mass < minMass) return;

        _objectsOnPlate.Remove(other);
        Debug.Log($"[PressurePlate] Object left: {other.name} " +
                  $"count={_objectsOnPlate.Count}");

        if (_objectsOnPlate.Count == 0 && IsActive) Deactivate();
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

    /// <summary>Resets plate so it can fire objective again after removal.</summary>
    public void ResetPlate()
    {
        _objectsOnPlate.Clear();
        IsActive = false;
        SetVisual(false);
    }

    // ---------------------------------------------------------------
    // Visual feedback
    // ---------------------------------------------------------------

    private void SetVisual(bool active)
    {
        if (plateRenderer != null)
            plateRenderer.material.color = active ? activeColor : inactiveColor;

        if (plateOutline != null)
        {
            if (active)
            {
                plateOutline.OutlineColor = activeOutlineColor;
                plateOutline.OutlineWidth = 6f;
                plateOutline.enabled      = true;
            }
            else if (!_playerHolding)
            {
                plateOutline.enabled = false;
            }
        }
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
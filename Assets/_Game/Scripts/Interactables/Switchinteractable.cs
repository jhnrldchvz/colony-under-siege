using UnityEngine;
using UnityEngine.Events;
using TMPro;

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

    [Header("Requirements")]
    [Tooltip("Item IDs that must be in inventory before this switch can be activated. Leave empty for no requirement.")]
    public string[] requiredItemIds = new string[0];

    [Tooltip("Message shown when required items are missing")]
    public string missingItemsMessage = "Required items missing";

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

    [Header("Proximity Label")]
    public Canvas            labelCanvas;
    public TextMeshProUGUI   labelText;
    public float             labelRange   = 4f;
    public float             fadeSpeed    = 8f;
    public string            interactPrompt    = "[E] Activate terminal";
    public string            activatedPrompt   = "Terminal active";

    [Header("Outline")]
    public Outline outline;

    private Quaternion _offRotation;
    private Quaternion _onRotation;
    private Transform  _player;
    private Camera     _cam;
    private CanvasGroup _cg;

    private void Start()
    {
        if (switchVisual != null)
        {
            _offRotation = switchVisual.localRotation;
            _onRotation  = Quaternion.Euler(activatedRotation);
        }

        _cam = Camera.main;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        if (labelCanvas != null)
        {
            _cg       = labelCanvas.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = labelCanvas.gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            labelCanvas.gameObject.SetActive(false);
        }

        if (outline != null) outline.enabled = false;
    }

    private void Update()
    {
        if (_player == null) return;

        float dist        = Vector3.Distance(transform.position, _player.position);
        bool  nearPlayer  = dist <= labelRange;

        if (outline != null) outline.enabled = nearPlayer;

        if (nearPlayer && labelText != null)
        {
            if (IsOn)
                labelText.text = activatedPrompt;
            else if (!HasRequiredItems())
                labelText.text = missingItemsMessage;
            else
                labelText.text = interactPrompt;
        }

        if (_cg != null)
        {
            _cg.alpha = Mathf.Lerp(_cg.alpha, nearPlayer ? 1f : 0f,
                                   Time.deltaTime * fadeSpeed);

            bool active = _cg.alpha > 0.01f;
            if (labelCanvas.gameObject.activeSelf != active)
                labelCanvas.gameObject.SetActive(active);

            if (_cam != null && active)
                labelCanvas.transform.LookAt(
                    labelCanvas.transform.position + _cam.transform.forward);
        }
    }

    // ---------------------------------------------------------------
    // Requirements check
    // ---------------------------------------------------------------

    private bool HasRequiredItems()
    {
        if (requiredItemIds == null || requiredItemIds.Length == 0) return true;
        if (InventoryManager.Instance == null) return false;

        foreach (string id in requiredItemIds)
        {
            if (!string.IsNullOrEmpty(id) &&
                !InventoryManager.Instance.HasKeyItem(id))
                return false;
        }
        return true;
    }

    public void Interact(PlayerController player)
    {
        if (!isToggleable && IsOn) return; // One-time switch already used

        // Check required items before allowing activation
        if (!HasRequiredItems())
        {
            Debug.Log($"[Switch] '{switchId}' — required items not collected.");
            StartCoroutine(FlashLabel());
            return;
        }

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
    private System.Collections.IEnumerator FlashLabel()
    {
        if (labelText == null) yield break;
        Color original = labelText.color;
        labelText.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        if (labelText != null) labelText.color = original;
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// DoorInteractable — a locked door that requires a key item to open.
///
/// Features:
///   - Quick Outline appears when player is nearby
///   - World-space label shows prompt or locked message
///   - Checks InventoryManager for the required key item
///   - Loads next scene or plays locked feedback
///
/// Setup:
///   1. Attach to your door prefab root GameObject.
///   2. Attach Quick Outline component — Color white, Width 5.
///   3. Create a World Space Canvas child → add TMP text → wire slots.
///   4. Set requiredKeyId to match your ObjectivePickup itemId.
///   5. Set nextSceneBuildIndex to the scene to load on open.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Key Requirement")]
    [Tooltip("Must match the itemId on your ObjectivePickup")]
    public string requiredKeyId      = "access_key_01";

    [Header("Objectives")]
    [Tooltip("If true, all objectives must be complete before door can be opened")]
    public bool requireAllObjectives = true;

    [Header("Scene")]
    [Tooltip("Build index of scene to load when door opens. -1 = trigger win screen")]
    public int nextSceneBuildIndex   = -1;

    [Header("Proximity Label")]
    public Canvas            labelCanvas;
    public TextMeshProUGUI   labelText;
    public float             labelRange  = 4f;
    public float             fadeSpeed   = 8f;

    [Header("Messages")]
    public string unlockedMessage        = "[E] Enter";
    public string lockedMessage          = "Access Key Required";
    public string objectivesNotComplete  = "Complete all objectives first";

    [Header("Quick Outline")]
    public Outline outline;

    // ---------------------------------------------------------------
    private Transform    _player;
    private Camera       _cam;
    private CanvasGroup  _cg;
    private bool         _playerNearby = false;

    private void Start()
    {
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

        float dist = Vector3.Distance(transform.position, _player.position);
        _playerNearby = dist <= labelRange;

        // Outline
        if (outline != null) outline.enabled = _playerNearby;

        // Update label text based on key and objective state
        if (_playerNearby && labelText != null)
        {
            bool hasKey    = InventoryManager.Instance != null &&
                             InventoryManager.Instance.HasKeyItem(requiredKeyId);
            bool objsDone  = !requireAllObjectives ||
                             (ObjectiveManager.Instance != null &&
                              ObjectiveManager.Instance.AreAllComplete());

            if (!hasKey)
                labelText.text = lockedMessage;
            else if (!objsDone)
                labelText.text = objectivesNotComplete;
            else
                labelText.text = unlockedMessage;
        }

        // Fade label
        if (_cg != null)
        {
            _cg.alpha = Mathf.Lerp(
                _cg.alpha,
                _playerNearby ? 1f : 0f,
                Time.deltaTime * fadeSpeed);

            bool active = _cg.alpha > 0.01f;
            if (labelCanvas.gameObject.activeSelf != active)
                labelCanvas.gameObject.SetActive(active);

            // Billboard
            if (_cam != null && active)
                labelCanvas.transform.LookAt(
                    labelCanvas.transform.position + _cam.transform.forward);
        }
    }

    // ---------------------------------------------------------------
    // IInteractable — called by PlayerController on E press
    // ---------------------------------------------------------------

    public void Interact(PlayerController player)
    {
        bool hasKey   = InventoryManager.Instance != null &&
                        InventoryManager.Instance.HasKeyItem(requiredKeyId);
        bool objsDone = !requireAllObjectives ||
                        (ObjectiveManager.Instance != null &&
                         ObjectiveManager.Instance.AreAllComplete());

        if (!hasKey)
        {
            Debug.Log($"[Door] Locked — need key: '{requiredKeyId}'");
            StartCoroutine(FlashLocked());
            return;
        }

        if (!objsDone)
        {
            Debug.Log("[Door] Objectives not complete yet.");
            StartCoroutine(FlashLocked());
            return;
        }

        Debug.Log("[Door] All objectives done + key found. Stage complete!");
        OpenDoor();
    }

    private void OpenDoor()
    {
        // Trigger win screen — door is the win condition
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerStageWin();
            return;
        }

        // Fallback — load next scene directly
        if (nextSceneBuildIndex >= 0)
            GameManager.Instance?.LoadScene(nextSceneBuildIndex);
        else
            LevelManager.Instance?.LoadNextLevel();
    }

    private System.Collections.IEnumerator FlashLocked()
    {
        if (labelText == null) yield break;

        // Flash red for 0.5s then restore
        Color original = labelText.color;
        labelText.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        labelText.color = original;
    }
}
using UnityEngine;
using TMPro;

/// <summary>
/// ScreenPodInteractable — a terminal/screen pod in the tutorial that shows
/// a room-specific instructions panel when the player presses E.
///
/// Setup:
///   1. Attach to any terminal/screen mesh GameObject.
///   2. Add a Collider (Box or Mesh) — NOT a trigger.
///   3. Set the GameObject's Layer to "Interactable".
///   4. Fill in podTitle and instructions in the Inspector.
///   5. (Optional) Assign labelCanvas, labelText, and outline for proximity UI.
/// </summary>
public class ScreenPodInteractable : MonoBehaviour, IInteractable
{
    [Header("Content")]
    [Tooltip("Heading shown at the top of the instruction panel.")]
    public string podTitle = "TRAINING TERMINAL";

    [Tooltip("The instruction text displayed in the panel body.")]
    [TextArea(4, 12)]
    public string instructions = "Instructions go here.";

    [Tooltip("Optional illustration shown in the panel (e.g. control diagram). Leave empty to hide the image area.")]
    public Sprite illustration;

    [Header("Proximity Label")]
    public Canvas           labelCanvas;
    public TextMeshProUGUI  labelText;
    public float            labelRange  = 3.5f;
    public float            fadeSpeed   = 8f;
    public string           interactPrompt = "[E] Access Terminal";

    [Header("Outline")]
    public Outline outline;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private Transform   _player;
    private Camera      _cam;
    private CanvasGroup _cg;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        _cam = Camera.main;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        if (labelCanvas != null)
        {
            _cg = labelCanvas.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = labelCanvas.gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            labelCanvas.gameObject.SetActive(false);
        }

        if (labelText != null)
            labelText.text = interactPrompt;

        if (outline != null) outline.enabled = false;
    }

    private void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool  near = dist <= labelRange;

        if (outline != null) outline.enabled = near;

        if (_cg != null)
        {
            _cg.alpha = Mathf.Lerp(_cg.alpha, near ? 1f : 0f, Time.deltaTime * fadeSpeed);

            bool active = _cg.alpha > 0.01f;
            if (labelCanvas.gameObject.activeSelf != active)
                labelCanvas.gameObject.SetActive(active);

            if (_cam != null && active)
                labelCanvas.transform.LookAt(
                    labelCanvas.transform.position + _cam.transform.forward);
        }
    }

    // ---------------------------------------------------------------
    // IInteractable
    // ---------------------------------------------------------------

    public void Interact(PlayerController player)
    {
        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[ScreenPod] TutorialManager not found in scene.");
            return;
        }

        TutorialManager.Instance.ShowInstructionPanel(podTitle, instructions, illustration);
        Debug.Log($"[ScreenPod] '{podTitle}' accessed.");
    }
}

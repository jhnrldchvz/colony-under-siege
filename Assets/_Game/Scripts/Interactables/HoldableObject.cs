using UnityEngine;
using TMPro;

/// <summary>
/// HoldableObject — attach to any physics box the player can grab.
///
/// Setup:
///   1. Add Rigidbody to the GameObject — Use Gravity ON, Is Kinematic OFF.
///   2. Add Box Collider fitted to the mesh.
///   3. Add this script.
///   4. Add Quick Outline component — will be toggled by GrabController.
///   5. Set the GameObject layer to "Holdable" (create this layer).
///   6. In GrabController Inspector → Grab Layer → select "Holdable".
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HoldableObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How heavy the object feels when thrown — affects physics collisions")]
    public float mass = 5f;

    [Tooltip("Object can only be picked up once — becomes regular physics object after first throw")]
    public bool oneTimeUse = false;

    [Header("Outline")]
    public Outline outline;

    [Header("Proximity Label")]
    public Canvas            labelCanvas;
    public TextMeshProUGUI   labelText;
    public float             labelRange  = 4f;
    public float             fadeSpeed   = 8f;
    public float             labelHeight = 0.8f;

    [Header("Label Text")]
    public string hoverText = "[E] Pick up";
    public string holdText  = "[E] Drop    [RMB] Throw";

    [Header("Hint Label")]
    [Tooltip("Separate TMP text for a contextual hint below the main label")]
    public TextMeshProUGUI hintText;
    [Tooltip("Hint shown when player is near — e.g. 'Place on pressure plate'")]
    public string          hintMessage = "";

    // ---------------------------------------------------------------
    private Rigidbody   _rb;
    private bool        _used = false;
    private Camera      _cam;
    private Transform   _player;
    private CanvasGroup _cg;

    private void Awake()
    {
        _rb      = GetComponent<Rigidbody>();
        _rb.mass = mass;

        if (outline == null) outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

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

        if (labelText != null) labelText.text = hoverText;
        if (hintText  != null) hintText.text  = hintMessage;
    }

    private bool _isHeld = false;
    public  bool IsHeld  => _isHeld;

    private void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool  near = dist <= labelRange || _isHeld;

        if (_cg != null)
        {
            _cg.alpha = Mathf.Lerp(_cg.alpha, near ? 1f : 0f,
                                   Time.deltaTime * fadeSpeed);
            bool active = _cg.alpha > 0.01f;
            if (labelCanvas.gameObject.activeSelf != active)
                labelCanvas.gameObject.SetActive(active);

            // Show hint ONLY while being held
            if (hintText != null)
                hintText.gameObject.SetActive(_isHeld);
        }

        // Always keep label upright and facing camera
        // Use world-space rotation so box rotation never affects the label
        if (labelCanvas != null && _cam != null)
        {
            // Keep label above the box in world space
            labelCanvas.transform.position = transform.position +
                Vector3.up * labelHeight;

            // Face camera — Y axis only so label stays upright
            Vector3 forward = _cam.transform.forward;
            forward.y = 0f;
            if (forward != Vector3.zero)
                labelCanvas.transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    // ---------------------------------------------------------------
    // Called by GrabController
    // ---------------------------------------------------------------
    public void OnPickedUp()
    {
        _isHeld = true;
        if (labelText != null) labelText.text = holdText;
        if (_cg != null) _cg.alpha = 1f;
        if (labelCanvas != null) labelCanvas.gameObject.SetActive(true);
        // Show hint when picked up
        if (hintText != null)
        {
            hintText.text = hintMessage;
            hintText.gameObject.SetActive(true);
        }
    }

    public void OnDropped()
    {
        _isHeld = false;
        if (labelText != null) labelText.text = hoverText;
        if (hintText  != null) hintText.text  = hintMessage;
        if (_cg != null) _cg.alpha = 0f;
        if (labelCanvas != null) labelCanvas.gameObject.SetActive(false);
        if (oneTimeUse) _used = true;
    }

    public void OnThrown()
    {
        _isHeld = false;
        if (labelCanvas != null) labelCanvas.gameObject.SetActive(false);
        if (oneTimeUse) _used = true;
    }

    public void SetOutline(bool show, Color color)
    {
        if (outline == null) return;
        outline.enabled      = show;
        if (show) outline.OutlineColor = color;
    }

    public bool CanBeGrabbed => !_used;
}
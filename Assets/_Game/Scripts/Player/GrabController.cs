using UnityEngine;

/// <summary>
/// GrabController — pick up, rotate, and throw physics objects.
///
/// Controls:
///   E              — pick up / drop
///   Mouse X/Y      — rotate held object (only when holding, decoupled from camera)
///   Scroll wheel   — roll held object on Z axis
///   Right Click    — throw
/// </summary>
public class GrabController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;

    [Header("Grab Settings")]
    public float grabRange        = 4f;
    public float holdDistance     = 2.5f;
    public LayerMask grabLayer;

    [Header("Rotation")]
    [Tooltip("Mouse X/Y rotation speed while holding")]
    public float rotateSpeed      = 150f;

    [Tooltip("Scroll wheel roll speed")]
    public float scrollRollSpeed  = 120f;

    [Tooltip("Hold this key + move mouse to rotate the object. Default: Left Alt")]
    public KeyCode rotateModifier = KeyCode.LeftAlt;

    [Header("Throw")]
    public float throwForce       = 18f;

    [Header("Outline")]
    public Color hoverOutlineColor = Color.yellow;
    public Color holdOutlineColor  = Color.cyan;

    // ---------------------------------------------------------------
    public bool IsHolding => _heldRb != null;

    private Rigidbody      _heldRb;
    private HoldableObject _heldObj;
    private HoldableObject _hoveredObj;

    // World-space rotation of the held object — fully independent of camera
    private Quaternion _holdRotation = Quaternion.identity;

    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (cameraHolder == null && _playerController != null)
            cameraHolder = _playerController.cameraHolder;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        UpdateHover();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_heldRb != null) Drop();
            else                  TryGrab();
        }

        if (Input.GetMouseButtonDown(1) && _heldRb != null)
            Throw();

        if (_heldRb != null)
        {
            UpdateHoldPosition();
            UpdateHoldRotation();
        }
    }

    // ---------------------------------------------------------------
    // Hover
    // ---------------------------------------------------------------
    private void UpdateHover()
    {
        if (_heldRb != null) { ClearHover(); return; }
        if (cameraHolder == null) return;

        Ray ray = Camera.main != null
            ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(cameraHolder.position, cameraHolder.forward);

        HoldableObject newHover = null;
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabLayer))
            newHover = hit.collider.GetComponent<HoldableObject>() ??
                       hit.collider.GetComponentInParent<HoldableObject>();

        if (newHover != _hoveredObj)
        {
            ClearHover();
            _hoveredObj = newHover;
            if (_hoveredObj != null)
                _hoveredObj.SetOutline(true, hoverOutlineColor);
        }
    }

    private void ClearHover()
    {
        if (_hoveredObj == null) return;
        _hoveredObj.SetOutline(false, Color.white);
        _hoveredObj = null;
    }

    // ---------------------------------------------------------------
    // Grab
    // ---------------------------------------------------------------
    private void TryGrab()
    {
        if (cameraHolder == null) return;

        Ray ray = Camera.main != null
            ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(cameraHolder.position, cameraHolder.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, grabRange, grabLayer)) return;

        HoldableObject holdable = hit.collider.GetComponent<HoldableObject>() ??
                                  hit.collider.GetComponentInParent<HoldableObject>();
        if (holdable == null) return;

        Rigidbody rb = holdable.GetComponent<Rigidbody>();
        if (rb == null) return;

        _heldRb  = rb;
        _heldObj = holdable;

        _heldRb.useGravity  = false;
        _heldRb.isKinematic = true;

        // Start with a clean upright rotation — fully independent of camera
        _holdRotation = Quaternion.identity;
        _heldRb.transform.rotation = _holdRotation;

        ClearHover();
        _heldObj.SetOutline(true, holdOutlineColor);
        _heldObj.OnPickedUp();

        Debug.Log($"[Grab] Picked up: {holdable.gameObject.name}");
    }

    // ---------------------------------------------------------------
    // Drop
    // ---------------------------------------------------------------
    private void Drop()
    {
        if (_heldRb == null) return;

        _heldRb.isKinematic = false;
        _heldRb.useGravity  = true;

        _heldObj?.SetOutline(false, Color.white);
        _heldObj?.OnDropped();

        Debug.Log($"[Grab] Dropped: {_heldObj?.gameObject.name}");
        _heldRb  = null;
        _heldObj = null;
    }

    // ---------------------------------------------------------------
    // Throw
    // ---------------------------------------------------------------
    private void Throw()
    {
        if (_heldRb == null) return;

        _heldRb.isKinematic = false;
        _heldRb.useGravity  = true;

        Vector3 dir = cameraHolder != null ? cameraHolder.forward : transform.forward;
        _heldRb.linearVelocity = dir * throwForce;

        _heldObj?.SetOutline(false, Color.white);
        _heldObj?.OnThrown();

        Debug.Log($"[Grab] Threw: {_heldObj?.gameObject.name}");
        _heldRb  = null;
        _heldObj = null;
    }

    // ---------------------------------------------------------------
    // Hold position — always in front of camera
    // ---------------------------------------------------------------
    private void UpdateHoldPosition()
    {
        if (_heldRb == null || cameraHolder == null) return;

        Vector3 target = cameraHolder.position + cameraHolder.forward * holdDistance;
        _heldRb.transform.position = Vector3.Lerp(
            _heldRb.transform.position, target, Time.deltaTime * 20f);
    }

    // ---------------------------------------------------------------
    // Hold rotation — independent world-space quaternion
    // Mouse X/Y rotates around world Up/Right axes
    // Scroll rotates around world Forward axis (roll)
    // Camera movement does NOT affect this rotation
    // ---------------------------------------------------------------
    private void UpdateHoldRotation()
    {
        if (_heldRb == null) return;

        // Scroll wheel rotates object horizontally (Y axis)
        float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollRollSpeed;
        if (Mathf.Abs(scroll) > 0.001f)
            _holdRotation = Quaternion.AngleAxis(-scroll, Vector3.up) * _holdRotation;

        _heldRb.transform.rotation = Quaternion.Slerp(
            _heldRb.transform.rotation, _holdRotation, Time.deltaTime * 15f);
    }

        // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (cameraHolder == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * grabRange);
        Gizmos.DrawWireSphere(
            cameraHolder.position + cameraHolder.forward * holdDistance, 0.15f);
    }
}
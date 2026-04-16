using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float walkSpeed        = 4f;
    public float runSpeed         = 8f;
    public float jumpForce        = 5f;
    public float gravityMultiplier = 2.5f;

    [Header("Camera Look")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float maxLookUp        = 80f;
    public float maxLookDown      = 80f;

    [Header("Health")]
    public int maxHealth = 100;

    [Header("Interaction")]
    public float     interactRange     = 3.5f;
    public LayerMask interactableLayer;
    public KeyCode   interactKey       = KeyCode.E;

    [Header("NavMesh Boundary")]
    [Tooltip("Clamp player movement to NavMesh surface")]
    public bool  clampToNavMesh    = true;
    [Tooltip("How far to sample NavMesh from player position — keep small (0.3–0.5) to prevent passing through walls")]
    public float navMeshSampleRange = 0.35f;

    public int  CurrentHealth { get; private set; }
    public bool IsAlive       { get; private set; } = true;

    /// <summary>Fired when health changes. UIManager subscribes to this.</summary>
    public event Action<int> OnHealthChanged;

    /// <summary>Fired once on Start with maxHealth. UIManager subscribes to this.</summary>
    public event Action<int> OnMaxHealthSet;

    private CharacterController _cc;
    private Vector3             _velocity;
    private float               _verticalLook;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        CurrentHealth = maxHealth;

        OnMaxHealthSet?.Invoke(maxHealth);

        // Wait 2 frames before locking cursor — fixes post-restart input focus
        StartCoroutine(InitCursorAfterLoad());
    }

    private IEnumerator InitCursorAfterLoad()
    {
        yield return null;
        yield return null;

        // Don't lock cursor if UIManager is showing pre-game instructions
        // Instructions need visible cursor for button clicks
        UIManager ui = FindFirstObjectByType<UIManager>();
        bool hasInstructions = ui != null
            && ui.instructionSlides != null
            && ui.instructionSlides.Length > 0
            && ui.instructionPanel != null;

        if (!hasInstructions)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        HandleMovement();
        HandleLook();
        HandleInteraction();
    }

    public bool IsInputBlocked =>
        GameManager.Instance != null && !GameManager.Instance.IsPlaying();

    private void HandleMovement()
    {
        float h     = Input.GetAxis("Horizontal");
        float v     = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        Vector3 move = (transform.right * h + transform.forward * v).normalized * speed;

        if (_cc.isGrounded)
        {
            if (_velocity.y < 0f) _velocity.y = -2f;
            if (Input.GetButtonDown("Jump")) _velocity.y = jumpForce;
        }

        _velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

        // Calculate where the player would move to
        Vector3 nextPos = transform.position + (move + Vector3.up * _velocity.y) * Time.deltaTime;

        // Clamp to NavMesh — block movement if next position is off NavMesh
        if (clampToNavMesh && move.magnitude > 0f)
        {
            NavMeshHit hit;
            bool onNavMesh = NavMesh.SamplePosition(nextPos, out hit, navMeshSampleRange, NavMesh.AllAreas);
            // Also confirm the sampled point is close to nextPos horizontally —
            // a large lateral gap means the player would cross into a wall or off the walkable area.
            bool laterallyValid = onNavMesh &&
                Vector2.Distance(new Vector2(hit.position.x, hit.position.z),
                                 new Vector2(nextPos.x, nextPos.z)) < navMeshSampleRange * 0.5f;

            if (laterallyValid)
            {
                // Next position is on NavMesh — allow movement
                _cc.Move((move + Vector3.up * _velocity.y) * Time.deltaTime);
            }
            else
            {
                // Next position is off NavMesh or blocked by an obstacle — keep gravity only
                _cc.Move(Vector3.up * _velocity.y * Time.deltaTime);
            }
        }
        else
        {
            _cc.Move((move + Vector3.up * _velocity.y) * Time.deltaTime);
        }
    }

    /// <summary>Set to false to freeze camera look (e.g. while a UI panel is open).</summary>
    public bool LookEnabled = true;

    private void HandleLook()
    {
        if (cameraHolder == null) return;
        if (!LookEnabled) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _verticalLook -= mouseY;
        _verticalLook  = Mathf.Clamp(_verticalLook, -maxLookUp, maxLookDown);
        cameraHolder.localRotation = Quaternion.Euler(_verticalLook, 0f, 0f);
    }

    private void HandleInteraction()
    {
        if (!Input.GetKeyDown(interactKey)) return;
        if (cameraHolder == null) return;

        // Skip door/switch interaction while holding a physics object
        // GrabController handles E key pickup/drop instead
        GrabController grab = GetComponent<GrabController>();
        if (grab != null && grab.IsHolding) return;

        Vector3 origin    = cameraHolder.position;
        Vector3 direction = cameraHolder.forward;

        Debug.DrawRay(origin, direction * interactRange, Color.green, 2f);

        // -------------------------------------------------------
        // Step 1: log everything the ray hits regardless of layer
        // -------------------------------------------------------
        RaycastHit[] allHits = Physics.RaycastAll(
            origin, direction, interactRange,
            ~0,                                    // all layers
            QueryTriggerInteraction.Collide);      // include triggers

        if (allHits.Length == 0)
        {
            Debug.Log("[Interact] Ray hit NOTHING at all — aim directly at the pickup");
            return;
        }

        // Sort by distance — closest first
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in allHits)
        {
            Debug.Log($"[Interact] Hit: '{h.collider.name}' " +
                      $"layer={LayerMask.LayerToName(h.collider.gameObject.layer)} " +
                      $"trigger={h.collider.isTrigger} " +
                      $"dist={h.distance:F2}");

            // Try to get IInteractable on this or its parent
            IInteractable interactable =
                h.collider.GetComponent<IInteractable>() ??
                h.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                Debug.Log($"[Interact] Found IInteractable on '{h.collider.name}' — calling Interact()");
                interactable.Interact(this);
                return;
            }
        }

        Debug.Log("[Interact] Ray hit objects but NONE had IInteractable component");
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth);

        // Player hurt sound
        SFXManager.Instance?.PlayPlayerHurt();

        // Trigger visual hit feedback
        HitEffects fx = GetComponent<HitEffects>();
        if (fx != null)
        {
            float intensity = Mathf.Clamp01((float)amount / maxHealth);
            fx.TriggerHit(intensity);
        }

        Debug.Log($"[Player] Took {amount} dmg. HP:{CurrentHealth}/{maxHealth}");
        if (CurrentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth);
        Debug.Log($"[Player] Healed {amount}. HP:{CurrentHealth}/{maxHealth}");
    }

    private void Die()
    {
        if (!IsAlive) return;
        IsAlive          = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
        enabled = false;
    }

    public Vector3 GetAimDirection()   => cameraHolder != null ? cameraHolder.forward   : transform.forward;
    public Vector3 GetCameraPosition() => cameraHolder != null ? cameraHolder.position  : transform.position;

    private void OnDrawGizmosSelected()
    {
        if (cameraHolder == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * interactRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            cameraHolder.position + cameraHolder.forward * interactRange, 0.1f);
    }
}
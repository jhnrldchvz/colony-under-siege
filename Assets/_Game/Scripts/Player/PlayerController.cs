using UnityEngine;

/// <summary>
/// PlayerController — handles all player-side logic:
/// movement, camera look, jumping, health, damage,
/// interaction raycasting, and death.
///
/// Setup:
///   1. Create an empty GameObject named "Player".
///   2. Attach a CharacterController component to it.
///   3. Create a child GameObject named "CameraHolder" — attach the
///      Main Camera to it. Zero out its local position and rotation.
///   4. Attach this script to the Player root GameObject.
///   5. Drag CameraHolder into the cameraHolder Inspector slot.
///   6. Set the Interactable layer in the Inspector (create a Layer
///      called "Interactable" in Edit → Project Settings → Tags and Layers).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector — Movement
    // ---------------------------------------------------------------

    [Header("Movement")]
    [Tooltip("Normal walking speed in m/s")]
    public float walkSpeed = 4f;

    [Tooltip("Sprint speed in m/s — hold Left Shift to sprint")]
    public float runSpeed = 8f;

    [Tooltip("Upward force applied when jumping")]
    public float jumpForce = 5f;

    [Tooltip("How fast the player falls — multiplied with Physics.gravity")]
    public float gravityMultiplier = 2.5f;

    // ---------------------------------------------------------------
    // Inspector — Camera Look
    // ---------------------------------------------------------------

    [Header("Camera Look")]
    [Tooltip("The child GameObject holding the Main Camera")]
    public Transform cameraHolder;

    [Tooltip("Mouse sensitivity — horizontal and vertical")]
    public float mouseSensitivity = 2f;

    [Tooltip("Maximum degrees the camera can look up")]
    public float maxLookUp = 80f;

    [Tooltip("Maximum degrees the camera can look down")]
    public float maxLookDown = 80f;

    // ---------------------------------------------------------------
    // Inspector — Health
    // ---------------------------------------------------------------

    [Header("Health")]
    [Tooltip("Starting and maximum health")]
    public int maxHealth = 100;

    // ---------------------------------------------------------------
    // Inspector — Interaction
    // ---------------------------------------------------------------

    [Header("Interaction")]
    [Tooltip("How far the player can reach to interact with objects")]
    public float interactRange = 2.5f;

    [Tooltip("Layer mask for interactable objects — set to 'Interactable' layer")]
    public LayerMask interactableLayer;

    [Tooltip("Key used to interact with objects in the world")]
    public KeyCode interactKey = KeyCode.E;

    // ---------------------------------------------------------------
    // Public state — read by other systems
    // ---------------------------------------------------------------

    public int  CurrentHealth { get; private set; }
    public bool IsAlive       { get; private set; } = true;

    // ---------------------------------------------------------------
    // Private — components
    // ---------------------------------------------------------------

    private CharacterController _cc;

    // ---------------------------------------------------------------
    // Private — movement
    // ---------------------------------------------------------------

    private Vector3 _velocity;     // Tracks full 3D velocity including gravity
    private float   _verticalLook; // Accumulated vertical camera rotation

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraHolder == null)
            Debug.LogError("[PlayerController] cameraHolder is not assigned. " +
                           "Drag the CameraHolder child into the Inspector slot.");
    }

    private void Start()
    {
        CurrentHealth = maxHealth;

        // Lock and hide cursor for FPS play
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Tell UIManager the max health so the bar scales correctly
        if (UIManager.Instance != null)
            UIManager.Instance.SetMaxHealth(maxHealth);

        Debug.Log("[PlayerController] Initialized.");
    }

    private void Update()
    {
        // Block all input while the game is not in Playing state
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        HandleMovement();
        HandleLook();
        HandleInteraction();
    }

    // ---------------------------------------------------------------
    // Movement
    // ---------------------------------------------------------------

    private void HandleMovement()
    {
        // --- Horizontal input ---
        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float v = Input.GetAxis("Vertical");   // W/S or Up/Down arrows

        bool  isSprinting = Input.GetKey(KeyCode.LeftShift);
        float speed       = isSprinting ? runSpeed : walkSpeed;

        // Move relative to where the player is facing
        Vector3 move = (transform.right * h + transform.forward * v).normalized * speed;

        // --- Jumping & gravity ---
        if (_cc.isGrounded)
        {
            // Prevent gravity from accumulating while grounded
            if (_velocity.y < 0f)
                _velocity.y = -2f;

            if (Input.GetButtonDown("Jump"))
                _velocity.y = jumpForce;
        }

        // Apply gravity every frame (scaled for snappier feel)
        _velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

        // Combine horizontal movement with vertical velocity and move
        Vector3 finalMove = move + Vector3.up * _velocity.y;
        _cc.Move(finalMove * Time.deltaTime);
    }

    // ---------------------------------------------------------------
    // Camera look — FPS mouselook
    // ---------------------------------------------------------------

    private void HandleLook()
    {
        if (cameraHolder == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the Player body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Accumulate and clamp vertical look angle
        _verticalLook -= mouseY;
        _verticalLook  = Mathf.Clamp(_verticalLook, -maxLookUp, maxLookDown);

        // Apply vertical rotation only to the camera, not the whole body
        cameraHolder.localRotation = Quaternion.Euler(_verticalLook, 0f, 0f);
    }

    // ---------------------------------------------------------------
    // Interaction — raycast from camera, call IInteractable.Interact()
    // ---------------------------------------------------------------

    private void HandleInteraction()
    {
        if (!Input.GetKeyDown(interactKey)) return;
        if (cameraHolder == null) return;

        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                Debug.Log($"[PlayerController] Interacted with: {hit.collider.gameObject.name}");
            }
        }
    }

    // ---------------------------------------------------------------
    // Health — damage & healing
    // ---------------------------------------------------------------

    /// <summary>
    /// Called by EnemyAI when an attack lands on the player.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        // Sync HUD health bar
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealth(CurrentHealth);

        Debug.Log($"[PlayerController] Took {amount} damage. " +
                  $"Health: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Called by FoodPickup when the player walks over or collects food.
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

        // Sync HUD health bar
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealth(CurrentHealth);

        Debug.Log($"[PlayerController] Healed {amount}. " +
                  $"Health: {CurrentHealth}/{maxHealth}");
    }

    // ---------------------------------------------------------------
    // Death
    // ---------------------------------------------------------------

    private void Die()
    {
        if (!IsAlive) return;

        IsAlive = false;

        Debug.Log("[PlayerController] Player died.");

        // Release cursor — GameManager and UIManager take it from here
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Tell GameManager — triggers GameOver state and fires UI events
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();

        // Stop this script's Update() loop
        enabled = false;
    }

    // ---------------------------------------------------------------
    // Utility — called by WeaponController and other systems
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns the direction the camera is aiming.
    /// WeaponController uses this as the bullet raycast direction.
    /// </summary>
    public Vector3 GetAimDirection()
    {
        return cameraHolder != null ? cameraHolder.forward : transform.forward;
    }

    /// <summary>
    /// Returns the camera's world position.
    /// WeaponController uses this as the bullet raycast origin.
    /// </summary>
    public Vector3 GetCameraPosition()
    {
        return cameraHolder != null ? cameraHolder.position : transform.position;
    }

    // ---------------------------------------------------------------
    // Debug — visualise interaction ray in the Scene view
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (cameraHolder == null) return;

        // Cyan ray shows interact reach
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * interactRange);

        // Yellow sphere shows the endpoint
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            cameraHolder.position + cameraHolder.forward * interactRange, 0.05f);
    }
}
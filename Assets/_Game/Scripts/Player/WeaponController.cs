using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon")]
    public InventoryManager.WeaponType currentWeaponType = InventoryManager.WeaponType.Rifle;
    public int   damagePerShot = 25;
    public float range         = 50f;
    public float fireRate      = 0.1f;
    public float reloadTime    = 1.8f;

    [Header("References")]
    public PlayerController playerController;

    [Tooltip("Drag CameraHolder here — the same Transform used in PlayerController")]
    public Transform cameraHolder;

    [Header("Effects (optional)")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public float      muzzleFlashDuration = 0.05f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private float  _nextFireTime = 0f;
    private bool   _isReloading  = false;
    private bool   _fireHeld     = false;

    private InputSystem_Actions _inputs;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _inputs = new InputSystem_Actions();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        // Use PlayerController.cameraHolder as the authoritative aiming pivot —
        // this is the same Transform that HandleLook() rotates, so it always
        // matches where the crosshair is pointing.
        if (cameraHolder == null && playerController != null)
            cameraHolder = playerController.cameraHolder;

        // Last-resort fallback: Camera.main (works when there is no local offset on the camera)
        if (cameraHolder == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraHolder = cam.transform;
        }

        if (cameraHolder == null)
            Debug.LogError("[WeaponController] No cameraHolder found! Drag PlayerController.cameraHolder into the slot.");
        else
            Debug.Log($"[WeaponController] Using camera: {cameraHolder.name}");
    }

    private void OnEnable()
    {
        _inputs.Player.Enable();
        _inputs.Player.Fire.performed   += ctx => _fireHeld = true;
        _inputs.Player.Fire.canceled    += ctx => _fireHeld = false;
        _inputs.Player.Reload.performed += ctx => TryReload();
    }

    private void OnDisable()
    {
        _inputs.Player.Disable();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            if (InventoryManager.Instance.GetCurrentAmmo(currentWeaponType) == 0)
                InventoryManager.Instance.GiveWeaponAmmo(currentWeaponType, 90);
        }

        Debug.Log($"[WeaponController] Ready. Weapon: {currentWeaponType}");
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;
        if (playerController != null && !playerController.IsAlive) return;
        if (_isReloading) return;

        if (_fireHeld && Time.time >= _nextFireTime)
            TryFire();

        // Auto-reload when empty
        if (InventoryManager.Instance != null &&
            !InventoryManager.Instance.HasAmmo(currentWeaponType) &&
            InventoryManager.Instance.CanReload(currentWeaponType))
        {
            TryReload();
        }
    }

    // ---------------------------------------------------------------
    // Shooting
    // ---------------------------------------------------------------

    private void TryFire()
    {
        if (InventoryManager.Instance == null) return;

        if (!InventoryManager.Instance.UseAmmo(currentWeaponType))
        {
            TryReload();
            return;
        }

        _nextFireTime = Time.time + fireRate;

        PerformRaycast();
        SpawnMuzzleFlash();

        Debug.Log($"[WeaponController] Fired. Ammo: {InventoryManager.Instance.GetCurrentAmmo(currentWeaponType)}");
    }

    private void PerformRaycast()
    {
        if (cameraHolder == null) return;

        Vector3 origin    = cameraHolder.position;
        Vector3 direction = cameraHolder.forward;

        Debug.DrawRay(origin, direction * range, Color.red, 1f);

        bool hitEnemy = false;

        // Single raycast — stops at the FIRST object hit
        // Prevents multi-enemy damage when enemies are close together
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            Debug.Log($"[WeaponController] Hit: {hit.collider.gameObject.name}");

            // Check the hit collider AND its parent (for multi-collider setups)
            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>() ??
                            hit.collider.GetComponentInParent<EnemyAI>();

            if (enemy != null && enemy.IsAlive)
            {
                enemy.TakeDamage(damagePerShot);
                SpawnHitEffect(hit.point, hit.normal);
                hitEnemy = true;
                Debug.Log($"[WeaponController] Damaged: {enemy.gameObject.name} " +
                          $"for {damagePerShot} dmg.");
            }
        }

        // Report accuracy to DifficultyManager
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ReportShot(hitEnemy);
    }

    // ---------------------------------------------------------------
    // Reload
    // ---------------------------------------------------------------

    private void TryReload()
    {
        if (_isReloading) return;
        if (InventoryManager.Instance == null) return;

        if (!InventoryManager.Instance.CanReload(currentWeaponType))
        {
            Debug.Log("[WeaponController] Cannot reload.");
            return;
        }

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        Debug.Log($"[WeaponController] Reloading...");

        yield return new WaitForSeconds(reloadTime);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.Reload(currentWeaponType);

        _isReloading = false;
        Debug.Log("[WeaponController] Reload complete.");
    }

    // ---------------------------------------------------------------
    // Weapon switching
    // ---------------------------------------------------------------

    public void SwitchWeapon(InventoryManager.WeaponType newType)
    {
        if (_isReloading) StopAllCoroutines();
        _isReloading      = false;
        currentWeaponType = newType;
        Debug.Log($"[WeaponController] Switched to: {newType}");
    }

    // ---------------------------------------------------------------
    // Effects
    // ---------------------------------------------------------------

    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null || cameraHolder == null) return;

        Vector3    pos   = cameraHolder.position + cameraHolder.forward * 0.5f;
        GameObject flash = Instantiate(muzzleFlashPrefab, pos, cameraHolder.rotation);
        Destroy(flash, muzzleFlashDuration);
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab == null) return;

        GameObject effect = Instantiate(hitEffectPrefab,
                                        position,
                                        Quaternion.LookRotation(normal));
        Destroy(effect, 1f);
    }

    // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (cameraHolder == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * range);
    }
}
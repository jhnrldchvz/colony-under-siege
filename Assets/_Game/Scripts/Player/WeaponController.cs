using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Weapon config — stats per weapon type
    // ---------------------------------------------------------------

    [System.Serializable]
    public class WeaponConfig
    {
        public InventoryManager.WeaponType type;
        public string     displayName  = "Rifle";
        public Sprite     icon;
        public GameObject modelObject;  // Drag weapon model here (child of CameraHolder)
        public Transform  barrelTip;     // Empty GameObject at barrel tip (optional)
        public Color      muzzleColor   = new Color(1f, 0.85f, 0.4f); // Flash color
        public float      muzzleIntensity = 8f;                        // Flash brightness
        public float      muzzleRange    = 3f;                         // Flash radius
        public int        damage       = 5;
        public float   range        = 50f;
        public float   fireRate     = 0.1f;   // Seconds between shots
        public float   reloadTime   = 1.8f;
        public int     startingAmmo = 90;      // Reserve ammo given at start
    }

    [Header("Weapon Configs")]
    public WeaponConfig[] weapons = new WeaponConfig[]
    {
        new WeaponConfig
        {
            type        = InventoryManager.WeaponType.Rifle,
            displayName = "Rifle",
            damage      = 5,
            range       = 50f,
            fireRate    = 0.1f,
            reloadTime  = 1.8f,
            startingAmmo = 90
        },
        new WeaponConfig
        {
            type        = InventoryManager.WeaponType.Pistol,
            displayName = "Pistol",
            damage      = 2,
            range       = 30f,
            fireRate    = 0.25f,
            reloadTime  = 1.2f,
            startingAmmo = 36
        }
    };

    [Header("References")]
    public PlayerController playerController;

    [Tooltip("Drag CameraHolder here — the same Transform used in PlayerController")]
    public Transform cameraHolder;

    [Header("Effects (optional)")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitEffectPrefab;
    public float      muzzleFlashDuration = 0.05f;

    // ---------------------------------------------------------------
    // Private — runtime state
    // ---------------------------------------------------------------

    private int    _currentWeaponIndex = 0;      // Index into weapons[]
    private float  _nextFireTime       = 0f;
    private bool   _isReloading        = false;
    private bool   _fireHeld           = false;

    private InputSystem_Actions _inputs;

    // ---------------------------------------------------------------
    // Convenience accessors
    // ---------------------------------------------------------------

    private WeaponConfig Current => weapons[_currentWeaponIndex];
    public  InventoryManager.WeaponType currentWeaponType => Current.type;

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
            // Seed starting ammo only if the weapon has NO ammo at all
            // Checks both magazine AND reserve — prevents re-seeding after reload
            foreach (WeaponConfig cfg in weapons)
            {
                bool magEmpty     = InventoryManager.Instance.GetCurrentAmmo(cfg.type) == 0;
                bool reserveEmpty = InventoryManager.Instance.GetReserveAmmo(cfg.type) == 0;

                if (magEmpty && reserveEmpty)
                    InventoryManager.Instance.GiveWeaponAmmo(cfg.type, cfg.startingAmmo);
            }
        }

        RefreshHUD();
        ShowActiveModel();
        Debug.Log($"[WeaponController] Ready. Weapon: {Current.displayName}");
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;
        if (playerController != null && !playerController.IsAlive) return;

        // Weapon switching — keys 1/2
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToIndex(1);

        // Scroll wheel switching
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)  SwitchToIndex((_currentWeaponIndex + 1) % weapons.Length);
        if (scroll < 0f)  SwitchToIndex((_currentWeaponIndex - 1 + weapons.Length) % weapons.Length);

        if (_isReloading) return;

        if (_fireHeld && Time.time >= _nextFireTime)
            TryFire();

        // Auto-reload when empty
        if (InventoryManager.Instance != null &&
            !InventoryManager.Instance.HasAmmo(Current.type) &&
            InventoryManager.Instance.CanReload(Current.type))
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

        if (!InventoryManager.Instance.UseAmmo(Current.type))
        {
            TryReload();
            return;
        }

        _nextFireTime = Time.time + Current.fireRate;

        PerformRaycast();
        SpawnMuzzleFlash();
        RefreshHUD();

        Debug.Log($"[WeaponController] Fired {Current.displayName}. " +
                  $"Ammo: {InventoryManager.Instance.GetCurrentAmmo(Current.type)}");
    }

    private void PerformRaycast()
    {
        if (cameraHolder == null) return;

        Vector3 origin    = cameraHolder.position;
        Vector3 direction = cameraHolder.forward;

        Debug.DrawRay(origin, direction * Current.range, Color.red, 1f);

        bool hitEnemy = false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Current.range))
        {
            Debug.Log($"[WeaponController] Hit: {hit.collider.gameObject.name}");

            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>() ??
                            hit.collider.GetComponentInParent<EnemyAI>();

            if (enemy != null && enemy.IsAlive)
            {
                enemy.TakeDamage(Current.damage);
                SpawnHitEffect(hit.point, hit.normal);
                hitEnemy = true;
                Debug.Log($"[WeaponController] {Current.displayName} hit: {enemy.gameObject.name} " +
                          $"for {Current.damage} dmg.");
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

        if (!InventoryManager.Instance.CanReload(Current.type))
        {
            Debug.Log("[WeaponController] Cannot reload.");
            return;
        }

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        Debug.Log($"[WeaponController] Reloading {Current.displayName}...");

        yield return new WaitForSeconds(Current.reloadTime);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.Reload(Current.type);

        _isReloading = false;
        RefreshHUD();
        Debug.Log($"[WeaponController] {Current.displayName} reload complete.");
    }

    // ---------------------------------------------------------------
    // Weapon switching
    // ---------------------------------------------------------------

    private void SwitchToIndex(int index)
    {
        if (index == _currentWeaponIndex) return;
        if (index < 0 || index >= weapons.Length) return;

        // Cancel any active reload
        if (_isReloading)
        {
            StopAllCoroutines();
            _isReloading = false;
        }

        _currentWeaponIndex = index;
        RefreshHUD();
        ShowActiveModel();
        Debug.Log($"[WeaponController] Switched to: {Current.displayName}");
    }

    /// <summary>External switch by weapon type — used by pickup scripts.</summary>
    public void SwitchWeapon(InventoryManager.WeaponType newType)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == newType)
            {
                SwitchToIndex(i);
                return;
            }
        }
    }

    // ---------------------------------------------------------------
    // Model visibility
    // ---------------------------------------------------------------

    private void ShowActiveModel()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].modelObject != null)
                weapons[i].modelObject.SetActive(i == _currentWeaponIndex);
        }
    }

    // ---------------------------------------------------------------
    // HUD refresh
    // ---------------------------------------------------------------

    private void RefreshHUD()
    {
        if (UIManager.Instance == null || InventoryManager.Instance == null) return;

        int current = InventoryManager.Instance.GetCurrentAmmo(Current.type);
        int reserve = InventoryManager.Instance.GetReserveAmmo(Current.type);
        UIManager.Instance.UpdateAmmo(current, reserve);
        UIManager.Instance.UpdateWeaponDisplay(Current.displayName, Current.icon);
    }

    // ---------------------------------------------------------------
    // Effects
    // ---------------------------------------------------------------

    private void SpawnMuzzleFlash()
    {
        if (cameraHolder == null) return;
        StartCoroutine(FlashLight());
    }

    private IEnumerator FlashLight()
    {
        // Spawn position — barrel tip if set, otherwise in front of camera
        Vector3 pos = Current.barrelTip != null
            ? Current.barrelTip.position
            : cameraHolder.position + cameraHolder.forward * 0.5f;

        // Create light GameObject
        GameObject lightObj  = new GameObject("MuzzleFlashLight");
        lightObj.transform.position = pos;

        Light lt         = lightObj.AddComponent<Light>();
        lt.type          = LightType.Point;
        lt.color         = Current.muzzleColor;
        lt.range         = Current.muzzleRange;
        lt.shadows       = LightShadows.None;

        // 3-frame brightness fade: full → half → off
        lt.intensity = Current.muzzleIntensity;
        yield return null;
        lt.intensity = Current.muzzleIntensity * 0.4f;
        yield return null;
        lt.intensity = 0f;
        yield return null;

        Destroy(lightObj);
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
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * Current.range);
    }
}
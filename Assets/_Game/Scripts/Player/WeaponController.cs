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
        public Transform  barrelTip;       // Empty at barrel tip
        public GameObject muzzleFlashFX;   // Drag War FX muzzle prefab here per weapon
        // Legacy point light fallback (used only if muzzleFlashFX is null)
        public Color      muzzleColor     = new Color(1f, 0.85f, 0.4f);
        public float      muzzleIntensity = 8f;
        public float      muzzleRange     = 3f;
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

    private int    _currentWeaponIndex = 0;
    private float  _nextFireTime       = 0f;
    private bool   _isReloading        = false;
    private bool   _fireHeld           = false;
    private bool   _weaponHidden       = false;

    private GrabController      _grab;
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

        _grab = GetComponent<GrabController>();
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

        // Scroll wheel is reserved for GrabController rotation when holding an object

        // Hide weapon model while holding an object
        bool isHolding = _grab != null && _grab.IsHolding;
        if (isHolding != _weaponHidden)
        {
            _weaponHidden = isHolding;
            SetWeaponVisible(!isHolding);
        }

        // Block firing while holding object
        if (isHolding) return;

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
            // Empty click sound
            if (Current.type == InventoryManager.WeaponType.Rifle)
                SFXManager.Instance?.PlayRifleEmpty();
            else
                SFXManager.Instance?.PlayPistolEmpty();
            TryReload();
            return;
        }

        _nextFireTime = Time.time + Current.fireRate;

        // Gunshot sound
        if (Current.type == InventoryManager.WeaponType.Rifle)
            SFXManager.Instance?.PlayRifleShot();
        else
            SFXManager.Instance?.PlayPistolShot();

        PerformRaycast();
        SpawnMuzzleFlash();
        RefreshHUD();

        Debug.Log($"[WeaponController] Fired {Current.displayName}. " +
                  $"Ammo: {InventoryManager.Instance.GetCurrentAmmo(Current.type)}");
    }

    private void PerformRaycast()
    {
        if (cameraHolder == null) return;

        // Fire ray from exact screen center — guarantees crosshair alignment
        Camera cam = Camera.main;
        Ray ray = cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(cameraHolder.position, cameraHolder.forward);

        Debug.DrawRay(ray.origin, ray.direction * Current.range, Color.red, 1f);

        bool hitEnemy = false;

        if (Physics.Raycast(ray, out RaycastHit hit, Current.range))
        {
            Debug.Log($"[WeaponController] Hit: {hit.collider.gameObject.name}");

            // IDamageable — works on enemies, crates, bosses, anything
            IDamageable target = hit.collider.GetComponent<IDamageable>() ??
                                 hit.collider.GetComponentInParent<IDamageable>();

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(Current.damage);
                SpawnHitEffect(hit.point, hit.normal);
                hitEnemy = true;
                Debug.Log($"[WeaponController] {Current.displayName} hit: " +
                          $"{hit.collider.gameObject.name} for {Current.damage} dmg.");
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
        SFXManager.Instance?.PlayReload();
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

    private void SetWeaponVisible(bool visible)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].modelObject != null)
            {
                // Only show the active weapon when visible
                weapons[i].modelObject.SetActive(visible && i == _currentWeaponIndex);
            }
        }
    }

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

        // Spawn position — barrel tip if set, otherwise in front of camera
        Vector3   pos = Current.barrelTip != null
            ? Current.barrelTip.position
            : cameraHolder.position + cameraHolder.forward * 0.5f;

        Quaternion rot = cameraHolder.rotation;

        // Use War FX prefab if assigned — otherwise fall back to point light
        if (Current.muzzleFlashFX != null)
        {
            GameObject fx = Instantiate(Current.muzzleFlashFX, pos, rot);
            // War FX prefabs self-destruct via CFX_AutoDestructShuriken
            // Safety fallback destroy in case auto-destruct is removed
            Destroy(fx, 0.5f);
        }
        else
        {
            StartCoroutine(FlashLight(pos));
        }
    }

    private IEnumerator FlashLight(Vector3 pos)
    {
        GameObject lightObj = new GameObject("MuzzleFlashLight");
        lightObj.transform.position = pos;

        Light lt     = lightObj.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = Current.muzzleColor;
        lt.range     = Current.muzzleRange;
        lt.shadows   = LightShadows.None;

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
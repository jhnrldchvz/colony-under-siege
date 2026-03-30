using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Weapon Configuration
    // ---------------------------------------------------------------
    [System.Serializable]
    public class WeaponConfig
    {
        public InventoryManager.WeaponType type;
        public string displayName = "Weapon";
        public Sprite icon;

        [Header("Model")]
        public GameObject modelObject;           // Weapon model (child of CameraHolder)
        public Transform barrelTip;              // Empty GameObject at the exact muzzle tip

        [Header("Muzzle Flash")]
        public GameObject muzzleFlashFX;         // War FX muzzle prefab
        public Vector3 muzzleFlashOffset = Vector3.zero; // Offset from barrelTip (fixes pistol distance issue)

        [Header("Legacy Light Fallback (if no FX)")]
        public Color muzzleColor = new Color(1f, 0.85f, 0.4f);
        public float muzzleIntensity = 8f;
        public float muzzleRange = 3f;

        [Header("Stats")]
        public int damage = 5;
        public float range = 50f;
        public float fireRate = 0.1f;
        public float reloadTime = 1.8f;
        public int startingAmmo = 90;

        [Header("Decoy Launcher (leave empty for normal weapons)")]
        [Tooltip("If set — this weapon fires decoy grenades instead of raycasting")]
        public GameObject decoyPrefab;
        public float      decoyThrowForce  = 15f;
        public float      decoyUpAngle     = 20f;
        public bool       isDecoyLauncher  => decoyPrefab != null;
    }

    [Header("Weapon Configs")]
    public WeaponConfig[] weapons = new WeaponConfig[]
    {
        new WeaponConfig
        {
            type = InventoryManager.WeaponType.Rifle,
            displayName = "Rifle",
            damage = 5,
            range = 50f,
            fireRate = 0.1f,
            reloadTime = 1.8f,
            startingAmmo = 90,
            muzzleFlashOffset = new Vector3(0f, 0f, 0.05f)   // Adjust if needed
        },
        new WeaponConfig
        {
            type = InventoryManager.WeaponType.Pistol,
            displayName = "Pistol",
            damage = 2,
            range = 30f,
            fireRate = 0.25f,
            reloadTime = 1.2f,
            startingAmmo = 36,
            muzzleFlashOffset = new Vector3(0f, 0f, 0.08f)   // ← Change this Z value to fix pistol muzzle flash distance
        }
    };

    [Header("References")]
    public PlayerController playerController;
    [Tooltip("Drag the same CameraHolder used in PlayerController")]
    public Transform cameraHolder;

    [Header("Shared Effects")]
    public GameObject hitEffectPrefab;
    public float muzzleFlashDuration = 0.05f;

    // ---------------------------------------------------------------
    // Private runtime state
    // ---------------------------------------------------------------
    private int _currentWeaponIndex = 0;
    private float _nextFireTime = 0f;
    private bool _isReloading = false;
    private bool _fireHeld = false;
    private bool _weaponHidden = false;

    private GrabController _grab;
    private InputSystem_Actions _inputs;

    // ---------------------------------------------------------------
    // Convenience accessors
    // ---------------------------------------------------------------
    private WeaponConfig Current => weapons[_currentWeaponIndex];
    public InventoryManager.WeaponType currentWeaponType => Current.type;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------
    private void Awake()
    {
        _inputs = new InputSystem_Actions();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (cameraHolder == null && playerController != null)
            cameraHolder = playerController.cameraHolder;

        if (cameraHolder == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraHolder = cam.transform;
        }

        if (cameraHolder == null)
            Debug.LogError("[WeaponController] No cameraHolder found!");
        else
            Debug.Log($"[WeaponController] Using cameraHolder: {cameraHolder.name}");

        _grab = GetComponent<GrabController>();
    }

    private void OnEnable()
    {
        _inputs.Player.Enable();
        _inputs.Player.Fire.performed += ctx => _fireHeld = true;
        _inputs.Player.Fire.canceled += ctx => _fireHeld = false;
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
            foreach (WeaponConfig cfg in weapons)
            {
                bool magEmpty = InventoryManager.Instance.GetCurrentAmmo(cfg.type) == 0;
                bool reserveEmpty = InventoryManager.Instance.GetReserveAmmo(cfg.type) == 0;

                if (magEmpty && reserveEmpty)
                    InventoryManager.Instance.GiveWeaponAmmo(cfg.type, cfg.startingAmmo);
            }
        }

        RefreshHUD();
        ShowActiveModel();
        Debug.Log($"[WeaponController] Ready. Current weapon: {Current.displayName}");
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;
        if (playerController != null && !playerController.IsAlive) return;

        // Weapon switching — supports any number of weapons via number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToIndex(2);

        // Hide weapon while holding an object
        bool isHolding = _grab != null && _grab.IsHolding;
        if (isHolding != _weaponHidden)
        {
            _weaponHidden = isHolding;
            SetWeaponVisible(!isHolding);
        }

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
            if (Current.type == InventoryManager.WeaponType.Rifle)
                SFXManager.Instance?.PlayRifleEmpty();
            else if (Current.type != InventoryManager.WeaponType.Decoy)
                SFXManager.Instance?.PlayPistolEmpty();
            TryReload();
            return;
        }

        _nextFireTime = Time.time + Current.fireRate;

        // Decoy launcher fires a projectile instead of raycasting
        if (Current.isDecoyLauncher)
        {
            FireDecoy();
        }
        else
        {
            if (Current.type == InventoryManager.WeaponType.Rifle)
                SFXManager.Instance?.PlayRifleShot();
            else
                SFXManager.Instance?.PlayPistolShot();

            PerformRaycast();
            SpawnMuzzleFlash();
        }

        RefreshHUD();
        Debug.Log($"[WeaponController] Fired {Current.displayName}. " +
                  $"Ammo: {InventoryManager.Instance.GetCurrentAmmo(Current.type)}");
    }

    private void FireDecoy()
    {
        if (Current.decoyPrefab == null || cameraHolder == null) return;

        Vector3 spawnPos = cameraHolder.position + cameraHolder.forward * 0.8f;
        Vector3 throwDir = Quaternion.AngleAxis(-Current.decoyUpAngle, cameraHolder.right)
                           * cameraHolder.forward;

        GameObject decoy = Instantiate(Current.decoyPrefab, spawnPos, Quaternion.identity);
        Rigidbody  rb    = decoy.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.AddForce(throwDir * Current.decoyThrowForce, ForceMode.VelocityChange);
        }

        Debug.Log($"[WeaponController] Decoy fired.");
    }

    private void PerformRaycast()
    {
        if (cameraHolder == null) return;

        Camera cam = Camera.main;
        Ray ray = cam != null
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(cameraHolder.position, cameraHolder.forward);

        Debug.DrawRay(ray.origin, ray.direction * Current.range, Color.red, 1f);

        bool hitEnemy = false;

        if (Physics.Raycast(ray, out RaycastHit hit, Current.range))
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>() ??
                                 hit.collider.GetComponentInParent<IDamageable>();

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(Current.damage);
                SpawnHitEffect(hit.point, hit.normal);
                hitEnemy = true;
            }
        }

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ReportShot(hitEnemy);
    }

    // ---------------------------------------------------------------
    // Muzzle Flash (Now Modular)
    // ---------------------------------------------------------------
    private void SpawnMuzzleFlash()
    {
        if (cameraHolder == null) return;

        Transform spawnPoint = Current.barrelTip != null ? Current.barrelTip : cameraHolder;

        Vector3 spawnPos = spawnPoint.position + spawnPoint.TransformDirection(Current.muzzleFlashOffset);
        Quaternion spawnRot = spawnPoint.rotation;

        if (Current.muzzleFlashFX != null)
        {
            GameObject fx = Instantiate(Current.muzzleFlashFX, spawnPos, spawnRot);

            // Parent to barrel so it follows movement and rotation
            fx.transform.SetParent(spawnPoint, true);

            // Optional: force local offset if prefab has its own positioning
            // fx.transform.localPosition = Current.muzzleFlashOffset;

            Destroy(fx, 0.15f); // Safety destroy for War FX
        }
        else
        {
            StartCoroutine(FlashLight(spawnPos));
        }
    }

    private IEnumerator FlashLight(Vector3 pos)
    {
        GameObject lightObj = new GameObject("MuzzleFlashLight");
        lightObj.transform.position = pos;

        Light lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = Current.muzzleColor;
        lt.range = Current.muzzleRange;
        lt.shadows = LightShadows.None;
        lt.intensity = Current.muzzleIntensity;

        yield return null;
        lt.intensity *= 0.4f;
        yield return null;
        lt.intensity = 0f;

        Destroy(lightObj);
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab == null) return;

        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
        Destroy(effect, 1f);
    }

    // ---------------------------------------------------------------
    // Reload
    // ---------------------------------------------------------------
    private void TryReload()
    {
        if (_isReloading || InventoryManager.Instance == null) return;
        if (!InventoryManager.Instance.CanReload(Current.type)) return;

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
    // Weapon Switching
    // ---------------------------------------------------------------
    private void SwitchToIndex(int index)
    {
        if (index == _currentWeaponIndex || index < 0 || index >= weapons.Length) return;

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
    // Model Visibility
    // ---------------------------------------------------------------

    private void SetWeaponVisible(bool visible)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].modelObject != null)
                weapons[i].modelObject.SetActive(visible && i == _currentWeaponIndex);
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
    // HUD
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
    // Gizmos
    // ---------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (cameraHolder == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * Current.range);
    }
}
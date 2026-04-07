using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Weapon Configuration
    // ---------------------------------------------------------------
    public enum WeaponMode { Raycast, Launcher }

    [System.Serializable]
    public class WeaponConfig
    {
        public InventoryManager.WeaponType type;
        public string displayName = "Weapon";
        public Sprite icon;

        [Header("Model")]
        public GameObject modelObject;
        public Transform barrelTip;

        [Header("Mode")]
        [Tooltip("Raycast = hitscan (Rifle/Pistol). Launcher = fires a physics projectile (Decoy).")]
        public WeaponMode mode = WeaponMode.Raycast;

        [Header("Stats — shared")]
        public int   startingAmmo = 90;
        public float fireRate     = 0.1f;
        public float reloadTime   = 1.8f;

        [Header("Stats — Raycast only")]
        public int   damage = 5;
        public float range  = 50f;

        [Header("Muzzle Flash — Raycast only")]
        public GameObject muzzleFlashFX;
        public Vector3    muzzleFlashOffset  = Vector3.zero;
        public Color      muzzleColor        = new Color(1f, 0.85f, 0.4f);
        public float      muzzleIntensity    = 8f;
        public float      muzzleRange        = 3f;

        [Header("Launcher only")]
        [Tooltip("Prefab to throw (e.g. DecoyDevice)")]
        public GameObject projectilePrefab;
        public float      throwForce   = 15f;
        public float      throwUpAngle = 20f;

        [Header("Stage Lock")]
        [Tooltip("This weapon is hidden and unselectable below this build index. " +
                 "0 = always available. Stages: 1=Landing, 2=Engineering, 3=Bio-Lab, 4=AI Core, 5=Reactor.")]
        public int unlockedFromBuildIndex = 0;
    }

    [Header("Weapon Configs")]
    public WeaponConfig[] weapons = new WeaponConfig[]
    {
        new WeaponConfig
        {
            type = InventoryManager.WeaponType.Rifle,
            displayName = "Rifle",
            mode = WeaponMode.Raycast,
            damage = 5,
            range = 50f,
            fireRate = 0.1f,
            reloadTime = 1.8f,
            startingAmmo = 90,
            muzzleFlashOffset = new Vector3(0f, 0f, 0.05f)
        },
        new WeaponConfig
        {
            type = InventoryManager.WeaponType.Pistol,
            displayName = "Pistol",
            mode = WeaponMode.Raycast,
            damage = 2,
            range = 30f,
            fireRate = 0.25f,
            reloadTime = 1.2f,
            startingAmmo = 36,
            muzzleFlashOffset = new Vector3(0f, 0f, 0.08f)
        }
    };

    [Header("References")]
    public PlayerController playerController;
    [Tooltip("Drag the same CameraHolder used in PlayerController")]
    public Transform cameraHolder;

    [Header("Shared Effects")]
    public GameObject hitEffectPrefab;

    // ---------------------------------------------------------------
    // Private runtime state
    // ---------------------------------------------------------------
    private int  _currentWeaponIndex = 0;
    private float _nextFireTime      = 0f;
    private bool  _isReloading       = false;
    private bool  _fireHeld          = false;
    private bool  _weaponHidden      = false;

    private GrabController      _grab;
    private InputSystem_Actions _inputs;

    // ---------------------------------------------------------------
    // Convenience accessors
    // ---------------------------------------------------------------
    private WeaponConfig Current => weapons[_currentWeaponIndex];
    public InventoryManager.WeaponType currentWeaponType => Current.type;

    /// <summary>
    /// Returns true if the weapon at <paramref name="index"/> is unlocked in the current scene.
    /// Compares the weapon's <c>unlockedFromBuildIndex</c> against the active scene build index.
    /// </summary>
    private bool IsWeaponAvailable(int index)
    {
        if (index < 0 || index >= weapons.Length) return false;
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
               >= weapons[index].unlockedFromBuildIndex;
    }

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

        _grab = GetComponent<GrabController>();
    }

    private void OnEnable()
    {
        _inputs.Player.Enable();
        _inputs.Player.Fire.performed  += ctx =>
        {
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying())
                _fireHeld = true;
        };
        _inputs.Player.Fire.canceled   += ctx => _fireHeld = false;
        _inputs.Player.Reload.performed += ctx =>
        {
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying())
                TryReload();
        };
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
                bool magEmpty     = InventoryManager.Instance.GetCurrentAmmo(cfg.type) == 0;
                bool reserveEmpty = InventoryManager.Instance.GetReserveAmmo(cfg.type) == 0;

                if (magEmpty && reserveEmpty)
                    InventoryManager.Instance.GiveWeaponAmmo(cfg.type, cfg.startingAmmo);
            }
        }

        // Hide models for weapons locked in this stage, and ensure we start on an available one
        for (int i = 0; i < weapons.Length; i++)
        {
            if (!IsWeaponAvailable(i) && weapons[i].modelObject != null)
            {
                weapons[i].modelObject.SetActive(false);
                Debug.Log($"[WeaponController] '{weapons[i].displayName}' locked in this stage (requires build index {weapons[i].unlockedFromBuildIndex}).");
            }
        }

        if (!IsWeaponAvailable(_currentWeaponIndex))
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                if (IsWeaponAvailable(i)) { _currentWeaponIndex = i; break; }
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

        // Clear fire held if game is not playing (safety net for Input System)
        if (_fireHeld && (GameManager.Instance == null || !GameManager.Instance.IsPlaying()))
        {
            _fireHeld = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && IsWeaponAvailable(0)) SwitchToIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && IsWeaponAvailable(1)) SwitchToIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && IsWeaponAvailable(2)) SwitchToIndex(2);

        bool isHolding = _grab != null && _grab.IsHolding;
        if (isHolding != _weaponHidden)
        {
            _weaponHidden = isHolding;
            SetWeaponVisible(!isHolding);
        }

        if (isHolding)   return;
        if (_isReloading) return;

        if (_fireHeld && Time.time >= _nextFireTime)
            TryFire();

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

        if (Current.mode == WeaponMode.Launcher)
        {
            FireLauncher();
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

    private void FireLauncher()
    {
        if (Current.projectilePrefab == null || cameraHolder == null) return;

        Transform spawnPoint = Current.barrelTip != null ? Current.barrelTip : cameraHolder;
        Vector3   spawnPos   = spawnPoint.position;

        Vector3 throwDir = Quaternion.AngleAxis(-Current.throwUpAngle, cameraHolder.right)
                           * cameraHolder.forward;

        GameObject proj = Instantiate(Current.projectilePrefab, spawnPos, Quaternion.identity);
        Rigidbody  rb   = proj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.AddForce(throwDir * Current.throwForce, ForceMode.VelocityChange);
        }

        Debug.Log($"[WeaponController] Launched {Current.projectilePrefab.name} from {spawnPoint.name}.");
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

        ScoreManager.Instance?.ReportShot(hitEnemy);
    }

    // ---------------------------------------------------------------
    // Muzzle Flash
    // ---------------------------------------------------------------
    private void SpawnMuzzleFlash()
    {
        if (cameraHolder == null) return;

        Transform spawnPoint = Current.barrelTip != null ? Current.barrelTip : cameraHolder;
        Vector3    spawnPos  = spawnPoint.position + spawnPoint.TransformDirection(Current.muzzleFlashOffset);

        if (Current.muzzleFlashFX != null)
        {
            GameObject fx = Instantiate(Current.muzzleFlashFX, spawnPos, spawnPoint.rotation);
            fx.transform.SetParent(spawnPoint, true);
            Destroy(fx, 0.15f);
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

        Light lt        = lightObj.AddComponent<Light>();
        lt.type         = LightType.Point;
        lt.color        = Current.muzzleColor;
        lt.range        = Current.muzzleRange;
        lt.shadows      = LightShadows.None;
        lt.intensity    = Current.muzzleIntensity;

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
        UIManager.Instance?.SetReloading(true);
        SFXManager.Instance?.PlayReload();
        Debug.Log($"[WeaponController] Reloading {Current.displayName}...");

        yield return new WaitForSeconds(Current.reloadTime);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.Reload(Current.type);

        _isReloading = false;
        UIManager.Instance?.SetReloading(false);
        RefreshHUD();
        Debug.Log($"[WeaponController] {Current.displayName} reload complete.");
    }

    // ---------------------------------------------------------------
    // Weapon Switching
    // ---------------------------------------------------------------
    private void SwitchToIndex(int index)
    {
        if (index == _currentWeaponIndex || index < 0 || index >= weapons.Length) return;
        if (!IsWeaponAvailable(index)) return;

        if (_isReloading)
        {
            StopAllCoroutines();
            _isReloading = false;
            UIManager.Instance?.SetReloading(false);
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
        if (cameraHolder == null || Current.mode == WeaponMode.Launcher) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(cameraHolder.position, cameraHolder.forward * Current.range);
    }
}
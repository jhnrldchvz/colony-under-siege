using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// InventoryManager — tracks everything the player is carrying.
///
/// Responsibilities:
///   - Ammo per weapon type (Dictionary)
///   - Key items required to complete objectives (HashSet)
///   - Fires events so UIManager updates the HUD automatically
///
/// Setup:
///   Attach to an empty GameObject named "InventoryManager" in the scene.
///   It self-registers with GameManager on Awake.
///   No Inspector slots required — all data is managed at runtime.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);


        InitializeWeaponDefaults();          
        Debug.Log("[InventoryManager] Initialized.");
    }

    // ---------------------------------------------------------------
    // Weapon type enum
    // Extend this as you add more weapons to the game
    // ---------------------------------------------------------------

    public enum WeaponType
    {
        Pistol,
        Rifle,
        Shotgun,
        Grenade,
        Decoy       // Decoy launcher charges
    }

    // ---------------------------------------------------------------
    // Events — UIManager and other systems subscribe to these
    // ---------------------------------------------------------------

    /// <summary>Fired when any weapon's ammo count changes. Passes type, current, reserve.</summary>
    public event Action<WeaponType, int, int> OnAmmoChanged;

    /// <summary>Fired when a key item is added to the inventory.</summary>
    public event Action<string> OnKeyItemAdded;

    // ---------------------------------------------------------------
    // Private data
    // ---------------------------------------------------------------

    // Current ammo in the magazine per weapon type
    private Dictionary<WeaponType, int> _currentAmmo  = new Dictionary<WeaponType, int>();

    // Reserve ammo (total carried outside the magazine) per weapon type
    private Dictionary<WeaponType, int> _reserveAmmo  = new Dictionary<WeaponType, int>();

    // Max reserve ammo the player can carry per weapon type
    private Dictionary<WeaponType, int> _maxReserve    = new Dictionary<WeaponType, int>();

    // Magazine size per weapon type
    private Dictionary<WeaponType, int> _magazineSize  = new Dictionary<WeaponType, int>();

    // Objective key items — HashSet prevents duplicates automatically
    private HashSet<string> _keyItems = new HashSet<string>();

    // ---------------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------------

    /// <summary>
    /// Sets default magazine sizes and max reserve values for every weapon type.
    /// Tune these values to match your game's balance.
    /// </summary>
    private void InitializeWeaponDefaults()
    {
        // Format: WeaponType, magazineSize, maxReserve
        RegisterWeapon(WeaponType.Pistol,   magSize: 12, maxRes: 60);
        RegisterWeapon(WeaponType.Rifle,    magSize: 30, maxRes: 120);
        RegisterWeapon(WeaponType.Shotgun,  magSize: 6,  maxRes: 30);
        RegisterWeapon(WeaponType.Grenade,  magSize: 1,  maxRes: 4);
        RegisterWeapon(WeaponType.Decoy,    magSize: 3,  maxRes: 3);
    }

    /// <summary>
    /// Registers a weapon type with its magazine size and reserve cap.
    /// All ammo starts at zero — weapons are given ammo via WeaponPickup.
    /// </summary>
    private void RegisterWeapon(WeaponType type, int magSize, int maxRes)
    {
        _magazineSize[type] = magSize;
        _maxReserve[type]   = maxRes;
        _currentAmmo[type]  = 0;
        _reserveAmmo[type]  = 0;
    }

    // ---------------------------------------------------------------
    // Ammo — add (from pickups)
    // ---------------------------------------------------------------

    /// <summary>
    /// Adds ammo to reserve for a weapon type.
    /// Called by AmmoPickup and WeaponPickup when collected.
    /// Excess beyond max reserve is discarded.
    /// </summary>
    public void AddAmmo(WeaponType type, int amount)
    {
        if (!_reserveAmmo.ContainsKey(type))
        {
            Debug.LogWarning($"[InventoryManager] Unknown weapon type: {type}");
            return;
        }

        int before = _reserveAmmo[type];
        _reserveAmmo[type] = Mathf.Min(_reserveAmmo[type] + amount, _maxReserve[type]);

        int added = _reserveAmmo[type] - before;
        Debug.Log($"[InventoryManager] +{added} {type} ammo. " +
                  $"Reserve: {_reserveAmmo[type]}/{_maxReserve[type]}");

        NotifyAmmoChanged(type);
    }

    /// <summary>
    /// Seeds the magazine when a weapon is first picked up.
    /// Called by WeaponPickup — fills the mag to full, adds reserve ammo.
    /// </summary>
    public void GiveWeaponAmmo(WeaponType type, int reserveToGive)
    {
        if (!_currentAmmo.ContainsKey(type)) return;

        // Fill magazine to its defined size
        _currentAmmo[type] = _magazineSize[type];

        // Add reserve up to the cap
        _reserveAmmo[type] = Mathf.Min(reserveToGive, _maxReserve[type]);

        Debug.Log($"[InventoryManager] Weapon picked up: {type}. " +
                  $"Mag: {_currentAmmo[type]}, Reserve: {_reserveAmmo[type]}");

        NotifyAmmoChanged(type);
    }

    // ---------------------------------------------------------------
    // Ammo — use (from WeaponController when firing)
    // ---------------------------------------------------------------

    /// <summary>
    /// Consumes one round from the current magazine.
    /// Returns true if a round was available, false if the magazine is empty.
    /// WeaponController calls this before firing — if false, play empty click.
    /// </summary>
    public bool UseAmmo(WeaponType type)
    {
        if (!_currentAmmo.ContainsKey(type)) return false;

        if (_currentAmmo[type] <= 0)
        {
            Debug.Log($"[InventoryManager] {type} magazine empty.");
            return false;
        }

        _currentAmmo[type]--;
        NotifyAmmoChanged(type);
        return true;
    }

    /// <summary>
    /// Reloads the magazine from reserve ammo.
    /// Returns true if a reload happened, false if reserve is empty.
    /// WeaponController calls this when the player presses R.
    /// </summary>
    public bool Reload(WeaponType type)
    {
        if (!_currentAmmo.ContainsKey(type)) return false;

        int magSize    = _magazineSize[type];
        int current    = _currentAmmo[type];
        int reserve    = _reserveAmmo[type];

        // Already full — no reload needed
        if (current >= magSize)
        {
            Debug.Log($"[InventoryManager] {type} magazine already full.");
            return false;
        }

        // No reserve to reload from
        if (reserve <= 0)
        {
            Debug.Log($"[InventoryManager] No {type} reserve ammo to reload.");
            return false;
        }

        int needed    = magSize - current;
        int taken     = Mathf.Min(needed, reserve);

        _currentAmmo[type]  += taken;
        _reserveAmmo[type]  -= taken;

        Debug.Log($"[InventoryManager] Reloaded {type}. " +
                  $"Mag: {_currentAmmo[type]}/{magSize}, " +
                  $"Reserve: {_reserveAmmo[type]}");

        NotifyAmmoChanged(type);
        return true;
    }

    // ---------------------------------------------------------------
    // Ammo — queries
    // ---------------------------------------------------------------

    /// <summary>Returns current magazine ammo for a weapon type.</summary>
    public int GetCurrentAmmo(WeaponType type)
    {
        return _currentAmmo.TryGetValue(type, out int val) ? val : 0;
    }

    /// <summary>Returns reserve ammo for a weapon type.</summary>
    public int GetReserveAmmo(WeaponType type)
    {
        return _reserveAmmo.TryGetValue(type, out int val) ? val : 0;
    }

    /// <summary>Returns true if the magazine has at least one round.</summary>
    public bool HasAmmo(WeaponType type)
    {
        return GetCurrentAmmo(type) > 0;
    }

    /// <summary>Returns true if there is any reserve ammo to reload from.</summary>
    public bool CanReload(WeaponType type)
    {
        return GetReserveAmmo(type) > 0 &&
               GetCurrentAmmo(type) < _magazineSize[type];
    }

    // ---------------------------------------------------------------
    // Key items (objective collectibles)
    // ---------------------------------------------------------------

    /// <summary>
    /// Adds a key item to the inventory by its unique string ID.
    /// Called by ObjectivePickup when the player collects it.
    /// Duplicate IDs are silently ignored — HashSet handles it.
    /// </summary>
    public void AddKeyItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[InventoryManager] AddKeyItem called with null/empty ID.");
            return;
        }

        bool added = _keyItems.Add(itemId); // Returns false if already present

        if (added)
        {
            Debug.Log($"[InventoryManager] Key item collected: '{itemId}'. " +
                      $"Total: {_keyItems.Count}");
            OnKeyItemAdded?.Invoke(itemId);
            // UIManager.OnKeyItemCollected subscribes to this event
            // and routes to the correct indicator — no direct call needed
        }
        else
        {
            Debug.Log($"[InventoryManager] Key item '{itemId}' already in inventory.");
        }
    }

    /// <summary>
    /// Returns true if the player is carrying the specified key item.
    /// ObjectiveManager calls this to check objective completion.
    /// </summary>
    public bool HasKeyItem(string itemId)
    {
        return _keyItems.Contains(itemId);
    }

    /// <summary>Returns how many unique key items the player is carrying.</summary>
    public int GetKeyItemCount()
    {
        return _keyItems.Count;
    }

    // ---------------------------------------------------------------
    // Reset — called on scene reload
    // ---------------------------------------------------------------

    /// <summary>
    /// Clears only key items — ammo and weapons are preserved.
    /// LevelManager calls this on new level load so previous level
    /// keys do not carry over into the next scene.
    /// </summary>
    public void ClearKeyItems()
    {
        _keyItems.Clear();
        Debug.Log("[InventoryManager] Key items cleared.");
    }

    /// <summary>
    /// Clears all ammo and key items.
    /// LevelManager calls this when restarting a stage from scratch.
    /// Not needed between levels if the player carries inventory forward.
    /// </summary>
    public void ResetInventory()
    {
        foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
        {
            _currentAmmo[type] = 0;
            _reserveAmmo[type] = 0;
        }

        _keyItems.Clear();

        Debug.Log("[InventoryManager] Inventory reset.");
    }

    // ---------------------------------------------------------------
    // Internal helpers
    // ---------------------------------------------------------------

    /// <summary>
    /// Fires OnAmmoChanged and syncs the HUD in one call.
    /// Every ammo-modifying method routes through here.
    /// </summary>
    private void NotifyAmmoChanged(WeaponType type)
    {
        int current = GetCurrentAmmo(type);
        int reserve = GetReserveAmmo(type);

        OnAmmoChanged?.Invoke(type, current, reserve);

        // Push directly to UIManager so the HUD updates without extra wiring
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAmmo(current, reserve);
    }

    // ---------------------------------------------------------------
    // Cleanup
    // ---------------------------------------------------------------

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
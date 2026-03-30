using UnityEngine;

/// <summary>
/// DecoyLauncher — no longer needed as a separate equip system.
/// The decoy launcher is now a full weapon slot in WeaponController.
/// Press 3 to switch to it, Left Mouse to fire.
///
/// This script is kept for AddAmmo compatibility with AmmoPickup.
/// Attach to Player alongside WeaponController.
/// </summary>
public class DecoyLauncher : MonoBehaviour
{
    public bool IsEquipped => false; // Legacy — no longer used

    /// <summary>Adds decoy charges via InventoryManager.</summary>
    public void AddAmmo(int amount)
    {
        InventoryManager.Instance?.AddAmmo(InventoryManager.WeaponType.Decoy, amount);
    }
}
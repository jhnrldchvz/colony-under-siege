using UnityEngine;

/// <summary>
/// WeaponPickup — gives the player a weapon with a full magazine and reserve ammo.
///
/// When the player presses E while looking at this object:
///   - Calls InventoryManager.GiveWeaponAmmo() — fills mag and seeds reserve
///   - Destroys itself (weapon is "picked up")
///
/// Setup:
///   1. Add this script to any GameObject with a Collider.
///   2. Set the GameObject's layer to "Interactable".
///   3. Set weaponType and reserveAmmoToGive in the Inspector.
///   4. Place in the level where you want the player to find this weapon.
///
/// Note:
///   WeaponPickup only handles ammo — it doesn't swap the player's mesh or
///   animations. That belongs in WeaponController (a future script).
///   For now it ensures the player has ammo ready when WeaponController is built.
/// </summary>
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [Header("Weapon")]
    [Tooltip("Which weapon type this pickup represents")]
    public InventoryManager.WeaponType weaponType = InventoryManager.WeaponType.Rifle;

    [Tooltip("Reserve ammo given alongside the full magazine")]
    public int reserveAmmoToGive = 90;

    [Header("Display")]
    public string displayName = "Assault Rifle";

    [Header("One-time pickup")]
    [Tooltip("If true, this pickup disappears after being collected once")]
    public bool destroyOnPickup = true;

    public void Interact(PlayerController player)
    {
        if (InventoryManager.Instance == null) return;

        // Fill magazine + seed reserve
        InventoryManager.Instance.GiveWeaponAmmo(weaponType, reserveAmmoToGive);

        Debug.Log($"[WeaponPickup] Player picked up '{displayName}'. " +
                  $"Type: {weaponType}, Reserve given: {reserveAmmoToGive}");

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
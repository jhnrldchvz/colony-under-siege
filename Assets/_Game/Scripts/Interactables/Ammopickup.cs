using UnityEngine;

/// <summary>
/// AmmoPickup — adds ammo to the player's reserve when collected.
///
/// Can be triggered two ways:
///   - Player presses E while looking at it (IInteractable)
///   - Player walks over it (OnTriggerEnter — set Collider to Is Trigger)
///
/// Setup:
///   1. Add this script to any GameObject with a Collider.
///   2. For walk-over pickup: check "Is Trigger" on the Collider.
///      For press-E pickup: set layer to "Interactable", uncheck Is Trigger.
///   3. Set weaponType and ammoAmount in the Inspector.
///   4. Dropped by EnemyAI — assign this prefab to the enemy's Loot Drop Prefab slot.
/// </summary>
public class AmmoPickup : MonoBehaviour, IInteractable
{
    [Header("Ammo")]
    [Tooltip("Which weapon type this ammo refills")]
    public InventoryManager.WeaponType weaponType = InventoryManager.WeaponType.Rifle;

    [Tooltip("How many rounds added to the reserve")]
    public int ammoAmount = 30;

    [Header("Display")]
    public string displayName = "Rifle Ammo";

    // Called when player presses E while looking at this object
    public void Interact(PlayerController player)
    {
        Collect(player);
    }

    // Called when player walks over this object (Collider must be Is Trigger)
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            Collect(player);
    }

    private void Collect(PlayerController player)
    {
        if (InventoryManager.Instance == null) return;

        InventoryManager.Instance.AddAmmo(weaponType, ammoAmount);

        Debug.Log($"[AmmoPickup] Player collected '{displayName}' — +{ammoAmount} {weaponType} ammo.");

        Destroy(gameObject);
    }
}
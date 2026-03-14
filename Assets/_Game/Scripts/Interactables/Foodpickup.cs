using UnityEngine;

/// <summary>
/// FoodPickup — restores player health when collected.
///
/// Can be triggered two ways:
///   - Player presses E while looking at it (IInteractable)
///   - Player walks over it (OnTriggerEnter — set Collider to Is Trigger)
///
/// Setup:
///   1. Add this script to any GameObject with a Collider.
///   2. For walk-over pickup: check "Is Trigger" on the Collider.
///      For press-E pickup: set layer to "Interactable", uncheck Is Trigger.
///   3. Set healAmount in the Inspector.
///   4. Dropped by EnemyAI — assign this prefab to the enemy's Loot Drop Prefab slot.
/// </summary>
public class FoodPickup : MonoBehaviour, IInteractable
{
    [Header("Healing")]
    [Tooltip("How much health this food restores")]
    public int healAmount = 25;

    [Header("Display")]
    public string displayName = "Ration Pack";

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
        if (player == null || !player.IsAlive) return;

        player.Heal(healAmount);

        Debug.Log($"[FoodPickup] Player collected '{displayName}' — healed {healAmount} HP.");

        Destroy(gameObject);
    }
}
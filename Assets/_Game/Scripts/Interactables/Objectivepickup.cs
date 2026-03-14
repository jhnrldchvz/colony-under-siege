using UnityEngine;

/// <summary>
/// ObjectivePickup — a key item the player must collect to complete an objective.
///
/// Examples: data core, access card, colony beacon, artifact fragment.
///
/// When the player presses E while looking at this object:
///   - Adds the itemId to InventoryManager
///   - ObjectiveManager evaluates CollectItem objectives automatically
///     via the OnKeyItemAdded event
///   - Destroys itself
///
/// Setup:
///   1. Add this script to any GameObject with a Collider.
///   2. Set the GameObject's layer to "Interactable".
///   3. Set itemId to match the requiredItemId in the Objective asset.
/// </summary>
public class ObjectivePickup : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip("Must match the requiredItemId in the CollectItem Objective asset")]
    public string itemId = "data_core_01";

    [Header("Display")]
    [Tooltip("Name shown in the debug log — use for HUD tooltip later")]
    public string displayName = "Data Core";

    public void Interact(PlayerController player)
    {
        // Add to inventory — this fires OnKeyItemAdded which ObjectiveManager hears
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddKeyItem(itemId);

        Debug.Log($"[ObjectivePickup] Collected: '{displayName}' (id: {itemId})");

        // Remove from the world
        Destroy(gameObject);
    }
}
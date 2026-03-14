/// <summary>
/// IInteractable — implement this on any GameObject the player can interact with.
///
/// PlayerController fires a raycast on the Interactable layer each frame.
/// When the player presses E within range, it calls Interact() on whatever
/// this interface is attached to — no switch statements, no tags.
///
/// Implementations:
///   SwitchInteractable   — toggles a door, bridge, or gate
///   LeverInteractable    — same as switch but with animation state
///   ObjectivePickup      — adds a key item to InventoryManager
///   WeaponPickup         — adds a weapon to the player's loadout
///   FoodPickup           — heals the player
///   AmmoPickup           — adds ammo to InventoryManager
///
/// Usage:
///   public class SwitchInteractable : MonoBehaviour, IInteractable
///   {
///       public void Interact(PlayerController player)
///       {
///           // toggle the door, fire a UnityEvent, etc.
///       }
///   }
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called by PlayerController when the player presses the interact key
    /// while looking at this object within interactRange.
    /// </summary>
    /// <param name="player">
    /// Reference to the PlayerController — lets pickups call
    /// player.Heal(), player.TakeDamage(), etc. directly.
    /// </param>
    void Interact(PlayerController player);
}
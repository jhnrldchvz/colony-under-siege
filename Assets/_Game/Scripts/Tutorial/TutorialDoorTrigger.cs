using UnityEngine;

/// <summary>
/// TutorialDoorTrigger — place this on an invisible trigger zone.
/// When the player walks into it, the assigned door GameObject is hidden
/// (simulating the door opening). Optionally waits for all objectives first.
///
/// Setup:
///   1. Create an empty GameObject where you want the trigger (e.g. at the doorway).
///   2. Add a Box Collider → tick "Is Trigger".
///   3. Attach this script.
///   4. Drag the wall/door mesh GameObject into "Door To Open".
///   5. Tick "Require Objectives" on rooms that have an objective to finish first.
/// </summary>
public class TutorialDoorTrigger : MonoBehaviour
{
    [Tooltip("The door wall or gate GameObject to hide when triggered.")]
    public GameObject doorToOpen;

    [Tooltip("If ticked, the door only opens after all objectives are complete.")]
    public bool requireObjectives = false;

    private bool _opened = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_opened) return;
        if (!other.CompareTag("Player")) return;

        if (requireObjectives)
        {
            bool done = ObjectiveManager.Instance != null &&
                        ObjectiveManager.Instance.AreAllComplete();
            if (!done)
            {
                Debug.Log("[TutorialDoorTrigger] Objectives not done yet.");
                return;
            }
        }

        OpenDoor();
    }

    /// <summary>
    /// Call this from a SwitchInteractable UnityEvent to open the door directly.
    /// </summary>
    public void OpenDoor()
    {
        if (_opened) return;
        _opened = true;

        if (doorToOpen != null)
            doorToOpen.SetActive(false);

        Debug.Log("[TutorialDoorTrigger] Door opened.");
    }
}

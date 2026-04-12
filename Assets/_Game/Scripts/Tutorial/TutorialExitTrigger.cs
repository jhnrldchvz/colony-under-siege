using UnityEngine;

/// <summary>
/// TutorialExitTrigger — place at the Room 6 exit.
/// When the player walks through after all objectives are done,
/// triggers the win screen which then loads the Main Menu.
///
/// Setup:
///   1. Create an empty GameObject at the exit point.
///   2. Add Box Collider → tick "Is Trigger".
///   3. Attach this script. Done.
/// </summary>
public class TutorialExitTrigger : MonoBehaviour
{
    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        bool done = ObjectiveManager.Instance == null ||
                    ObjectiveManager.Instance.AreAllComplete();

        if (!done)
        {
            Debug.Log("[TutorialExit] Objectives not complete yet.");
            return;
        }

        _triggered = true;
        Debug.Log("[TutorialExit] Tutorial complete — showing win screen.");
        GameManager.Instance?.TriggerStageWin();
    }
}

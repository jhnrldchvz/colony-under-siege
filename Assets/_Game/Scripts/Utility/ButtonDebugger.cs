using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ButtonDebugger — attach to any GameObject in the scene.
/// Press B in Play mode to print a full diagnosis of why
/// buttons might not be responding.
/// Remove this script after fixing.
/// </summary>
public class ButtonDebugger : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.B)) return;

        Debug.Log("=== BUTTON DEBUGGER ===");

        // 1 — Cursor state
        Debug.Log($"[Cursor] lockState={Cursor.lockState} visible={Cursor.visible}");

        // 2 — EventSystem
        EventSystem es = EventSystem.current;
        if (es == null)
            Debug.LogError("[EventSystem] MISSING — no EventSystem in scene. Add one via UI → Event System.");
        else
            Debug.Log($"[EventSystem] Found: {es.gameObject.name} active={es.gameObject.activeInHierarchy}");

        // 3 — All Canvases
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"[Canvas] Found {canvases.Length} canvas(es)");
        foreach (Canvas c in canvases)
        {
            GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
            Debug.Log($"[Canvas] '{c.gameObject.name}' " +
                      $"active={c.gameObject.activeInHierarchy} " +
                      $"renderMode={c.renderMode} " +
                      $"GraphicRaycaster={gr != null}");
        }

        // 4 — All active Buttons
        Button[] buttons = FindObjectsOfType<Button>();
        Debug.Log($"[Buttons] Found {buttons.Length} active button(s)");
        foreach (Button b in buttons)
        {
            bool hasListener = b.onClick.GetPersistentEventCount() > 0;
            Debug.Log($"[Button] '{b.gameObject.name}' " +
                      $"interactable={b.interactable} " +
                      $"OnClick listeners={b.onClick.GetPersistentEventCount()} " +
                      $"wired={hasListener}");
        }

        // 5 — Images with Raycast Target that might block clicks
        Image[] images = FindObjectsOfType<Image>();
        int blockers = 0;
        foreach (Image img in images)
        {
            if (img.raycastTarget && img.GetComponent<Button>() == null)
            {
                Debug.Log($"[Blocker?] '{img.gameObject.name}' has Raycast Target ON " +
                          $"but is not a Button — may block clicks");
                blockers++;
            }
        }
        if (blockers == 0)
            Debug.Log("[Blockers] No non-button raycast blockers found.");

        Debug.Log("=== END DEBUG ===");
    }
}
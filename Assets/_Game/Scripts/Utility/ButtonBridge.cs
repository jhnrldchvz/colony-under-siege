using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ButtonBridge — routes UI button clicks to singletons safely.
/// Always restores Time.timeScale before any scene operation.
/// </summary>
public class ButtonBridge : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Pause panel
    // ---------------------------------------------------------------

    public void OnResumePressed()
    {
        Debug.Log("[ButtonBridge] Resume");
        GameManager.Instance?.OnResumePressed();
    }

    public void OnRestartPressed()
    {
        Debug.Log("[ButtonBridge] Restart");

        // Restore time FIRST — cursor lock handled by PlayerController after load
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnQuitPressed()
    {
        Debug.Log("[ButtonBridge] Quit");

        // Force restore time FIRST
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Go to main menu (index 0) directly
        SceneManager.LoadScene(0);
    }

    // ---------------------------------------------------------------
    // Win panel
    // ---------------------------------------------------------------

    public void OnNextLevelPressed()
    {
        Debug.Log("[ButtonBridge] Next Level");
        Time.timeScale = 1f;
        LevelManager lm = FindObjectOfType<LevelManager>();
        lm?.LoadNextLevel();
    }

    public void OnMainMenuPressed()
    {
        Debug.Log("[ButtonBridge] Main Menu");
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        SceneManager.LoadScene(0);
    }
}
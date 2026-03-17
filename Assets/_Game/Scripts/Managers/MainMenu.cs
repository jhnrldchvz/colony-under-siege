using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MainMenuManager — controls the main menu scene.
///
/// Setup:
///   1. Create a new scene named MainMenu.
///   2. Add it to Build Settings as index 0.
///   3. Create a Canvas with Screen Space Overlay.
///   4. Build the panels listed below and wire Inspector slots.
///   5. Attach this script to an empty GameObject named "MainMenuManager".
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;       // Title, Play, Quit buttons
    public GameObject creditsPanel;    // Optional credits screen

    [Header("Main Panel Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI versionText;
    public TextMeshProUGUI subtitleText;

    [Header("Settings")]
    [Tooltip("Build index of the first game level")]
    public int gameSceneIndex = 1;

    [Tooltip("Animate the title text on start")]
    public bool animateTitle = true;

    private float _titleTimer = 0f;

    private void Start()
    {
        // Make sure cursor is visible on menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Make sure time is running (in case restarted from game)
        Time.timeScale = 1f;

        // Set version text
        if (versionText != null)
            versionText.text = $"v{Application.version}";

        // Set title
        if (titleText != null)
            titleText.text = "Colony Under Siege";

        // Set subtitle
        if (subtitleText != null)
            subtitleText.text = "The colony needs you.";

        // Show main panel, hide others
        SetPanel(mainPanel,   true);
        SetPanel(creditsPanel, false);

        Debug.Log("[MainMenuManager] Main menu loaded.");
    }

    private void Update()
    {
        if (!animateTitle || titleText == null) return;

        // Subtle pulse on the title
        _titleTimer += Time.deltaTime;
        float scale = 1f + Mathf.Sin(_titleTimer * 1.5f) * 0.02f;
        titleText.transform.localScale = Vector3.one * scale;
    }

    // ---------------------------------------------------------------
    // Button methods — wire these to OnClick() in Inspector
    // ---------------------------------------------------------------

    /// <summary>Play button — loads the first game level.</summary>
    public void OnPlayPressed()
    {
        Debug.Log("[MainMenuManager] Loading game...");
        SceneManager.LoadScene(gameSceneIndex);
    }

    /// <summary>Quit button — exits the application.</summary>
    public void OnQuitPressed()
    {
        Debug.Log("[MainMenuManager] Quitting...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>Credits button — shows the credits panel.</summary>
    public void OnCreditsPressed()
    {
        SetPanel(mainPanel,    false);
        SetPanel(creditsPanel, true);
    }

    /// <summary>Back button on credits — returns to main panel.</summary>
    public void OnBackPressed()
    {
        SetPanel(mainPanel,    true);
        SetPanel(creditsPanel, false);
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
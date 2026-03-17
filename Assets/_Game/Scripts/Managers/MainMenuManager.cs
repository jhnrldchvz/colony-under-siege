using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MainMenuManager — controls the main menu scene.
///
/// Setup:
///   1. Create a new scene named MainMenu — build index 0.
///   2. Create a Canvas (Screen Space Overlay) with a GraphicRaycaster.
///   3. Build the panels listed below.
///   4. Drag each Button into the matching Inspector slot — NO OnClick() wiring needed.
///   5. Attach this script to an empty GameObject named "MainMenuManager".
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject creditsPanel;

    [Header("Main Panel Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI versionText;
    public TextMeshProUGUI subtitleText;

    [Header("Buttons — drag Button components, not GameObjects")]
    public Button playButton;
    public Button quitButton;
    public Button creditsButton;
    public Button backButton;

    [Header("Settings")]
    [Tooltip("Build index of the first game level")]
    public int gameSceneIndex = 1;

    [Tooltip("Animate the title text on start")]
    public bool animateTitle = true;

    private float _titleTimer = 0f;

    private void Start()
    {
        Time.timeScale = 1f;

        // Wire buttons in code — can never silently break like Inspector OnClick()
        playButton?    .onClick.AddListener(OnPlayPressed);
        quitButton?    .onClick.AddListener(OnQuitPressed);
        creditsButton? .onClick.AddListener(OnCreditsPressed);
        backButton?    .onClick.AddListener(OnBackPressed);

        if (versionText  != null) versionText.text  = $"v{Application.version}";
        if (titleText    != null) titleText.text     = "Colony Under Siege";
        if (subtitleText != null) subtitleText.text  = "The colony needs you.";

        SetPanel(mainPanel,    true);
        SetPanel(creditsPanel, false);

        // Unity can re-lock the cursor on the first frame (editor focus, scene transition).
        // Use a coroutine so the unlock happens after Unity's own cursor handling settles.
        StartCoroutine(UnlockCursorDelayed());

        Debug.Log("[MainMenuManager] Main menu loaded.");
    }

    private System.Collections.IEnumerator UnlockCursorDelayed()
    {
        // Wait 3 frames — enough for Unity to finish its startup cursor handling
        yield return null;
        yield return null;
        yield return null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Debug.Log("[MainMenuManager] Cursor unlocked.");
    }

    private void Update()
    {
        // Keep enforcing cursor unlock every frame — GameManager or Unity itself
        // may re-lock it during scene transitions or on application focus events.
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        if (!animateTitle || titleText == null) return;

        _titleTimer += Time.deltaTime;
        float scale = 1f + Mathf.Sin(_titleTimer * 1.5f) * 0.02f;
        titleText.transform.localScale = Vector3.one * scale;
    }

    // ---------------------------------------------------------------
    // Button actions
    // ---------------------------------------------------------------

    private void OnPlayPressed()
    {
        Debug.Log("[MainMenuManager] Loading game...");
        SceneManager.LoadScene(gameSceneIndex);
    }

    private void OnQuitPressed()
    {
        Debug.Log("[MainMenuManager] Quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnCreditsPressed()
    {
        SetPanel(mainPanel,    false);
        SetPanel(creditsPanel, true);
    }

    private void OnBackPressed()
    {
        SetPanel(mainPanel,    true);
        SetPanel(creditsPanel, false);
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    private void SetPanel(GameObject panel, bool active) => panel?.SetActive(active);
}

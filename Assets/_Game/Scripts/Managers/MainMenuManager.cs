using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MainMenuManager — controls the main menu scene (build index 0).
///
/// Button flow:
///   New Game  → deletes any existing save, loads scene 1
///   Continue  → loads saved scene index (hidden when no save exists)
///   Credits   → shows credits panel
///   Quit      → exits application
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
    public Button newGameButton;
    public Button continueButton;
    public Button quitButton;
    public Button creditsButton;
    public Button backButton;

    [Header("Settings")]
    [Tooltip("Build index of the first game level — used by New Game")]
    public int firstSceneIndex = 1;

    [Tooltip("Animate the title text on start")]
    public bool animateTitle = true;

    private float _titleTimer = 0f;

    private void Start()
    {
        Time.timeScale = 1f;

        newGameButton? .onClick.AddListener(OnNewGamePressed);
        continueButton?.onClick.AddListener(OnContinuePressed);
        quitButton?    .onClick.AddListener(OnQuitPressed);
        creditsButton? .onClick.AddListener(OnCreditsPressed);
        backButton?    .onClick.AddListener(OnBackPressed);

        if (versionText  != null) versionText.text  = $"v{Application.version}";
        if (titleText    != null) titleText.text     = "Colony Under Siege";
        if (subtitleText != null) subtitleText.text  = "The colony needs you.";

        // Show Continue only when save data exists
        RefreshContinueButton();

        SetPanel(mainPanel,    true);
        SetPanel(creditsPanel, false);

        StartCoroutine(UnlockCursorDelayed());

        Debug.Log("[MainMenuManager] Main menu loaded.");
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(
                SaveManager.Instance != null && SaveManager.Instance.HasSaveData());
    }

    private System.Collections.IEnumerator UnlockCursorDelayed()
    {
        yield return null;
        yield return null;
        yield return null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Update()
    {
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

    private void OnNewGamePressed()
    {
        Debug.Log("[MainMenuManager] New Game — clearing save and loading scene 1.");
        SaveManager.Instance?.DeleteSave();
        SceneManager.LoadScene(firstSceneIndex);
    }

    private void OnContinuePressed()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSaveData())
        {
            Debug.LogWarning("[MainMenuManager] Continue pressed but no save data found.");
            return;
        }

        Debug.Log("[MainMenuManager] Continue — loading saved game.");
        SaveManager.Instance.LoadSavedGame();
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

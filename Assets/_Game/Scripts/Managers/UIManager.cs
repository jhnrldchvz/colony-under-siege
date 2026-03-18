using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UIManager — owns every Canvas panel in the game.
/// Listens to GameManager state events and shows/hides
/// the correct panel. Never polls — pure event-driven.
///
/// Setup:
///   1. Attach to your Canvas GameObject.
///   2. Build the 4 panels below the Canvas (see diagram).
///   3. Drag each panel/element into the Inspector slots.
///   4. Wire Pause/Resume/Restart/Quit buttons via Inspector
///      OnClick() → GameManager.Instance → the matching method.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Self-register so GameManager holds a typed reference
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterUIManager(this);
    }

    // ---------------------------------------------------------------
    // Inspector slots — Panels
    // ---------------------------------------------------------------

    [Header("Panels")]
    [Tooltip("Always-visible HUD (health, ammo, crosshair)")]
    public GameObject hudPanel;

    [Tooltip("Shown when ESC is pressed")]
    public GameObject pausePanel;

    [Tooltip("Shown when player health reaches zero")]
    public GameObject gameOverPanel;

    [Tooltip("Shown when all objectives are complete")]
    public GameObject winPanel;

    // ---------------------------------------------------------------
    // Inspector slots — HUD elements
    // ---------------------------------------------------------------

    [Header("HUD — Health")]
    [Tooltip("Image component used as health bar fill (Image Type: Filled)")]
    public Image healthBarFill;

    [Tooltip("Text showing numeric health, e.g. '80 / 100'")]
    public TextMeshProUGUI healthText;

    [Header("HUD — Ammo")]
    [Tooltip("Text showing current / max ammo, e.g. '24 / 90'")]
    public TextMeshProUGUI ammoText;

    [Tooltip("Image displaying the current weapon icon — swap sprite on switch")]
    public UnityEngine.UI.Image weaponIcon;

    [Header("HUD — Objectives")]
    [Tooltip("Small text panel in the corner listing active objectives")]
    public TextMeshProUGUI objectiveText;

    [Header("HUD — Crosshair")]
    [Tooltip("Crosshair image — hidden during cursor-visible states")]
    public GameObject crosshair;

    [Header("HUD — Key Item")]
    [Tooltip("Image shown when player has collected the key — hidden by default")]
    public GameObject keyItemIndicator;

    [Tooltip("Optional icon image inside the key indicator")]
    public UnityEngine.UI.Image keyItemIcon;

    [Tooltip("Optional text label e.g. 'Access Key'")]
    public TextMeshProUGUI keyItemText;

    // ---------------------------------------------------------------
    // Inspector slots — Pause panel
    // ---------------------------------------------------------------

    [Header("Pause Panel")]
    [Tooltip("Optional: dim overlay behind the pause card")]
    public GameObject pauseOverlay;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button quitButton;

    // ---------------------------------------------------------------
    // Inspector slots — Game Over panel
    // ---------------------------------------------------------------

    [Header("Game Over Panel")]
    [Tooltip("'You died' or custom flavour text")]
    public TextMeshProUGUI gameOverMessageText;
    public Button gameOverRestartButton;

    // ---------------------------------------------------------------
    // Inspector slots — Win panel
    // ---------------------------------------------------------------

    [Header("Win Panel")]
    [Tooltip("'Stage Complete' or custom flavour text")]
    public TextMeshProUGUI winMessageText;
    public Button winRestartButton;
    public Button nextLevelButton;
    public Button mainMenuButton;

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private int _maxHealth = 100; // Updated via SetMaxHealth()

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Subscribe to GameManager state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameOver     += ShowGameOverScreen;
            GameManager.Instance.OnStageWin     += ShowWinScreen;
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager.Instance not found. " +
                             "Make sure GameManager is in the scene.");
        }

        // Wire buttons in code — no Inspector OnClick() needed
        resumeButton?         .onClick.AddListener(() => GameManager.Instance?.ResumeGame());
        pauseRestartButton?   .onClick.AddListener(RestartScene);
        quitButton?           .onClick.AddListener(GoToMainMenu);
        gameOverRestartButton?.onClick.AddListener(RestartScene);
        nextLevelButton?      .onClick.AddListener(() => { Time.timeScale = 1f; LevelManager.Instance?.LoadNextLevel(); });
        mainMenuButton?       .onClick.AddListener(GoToMainMenu);

        // Start with a clean slate
        ShowHUDOnly();
    }

    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks after scene reload
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameOver     -= ShowGameOverScreen;
            GameManager.Instance.OnStageWin     -= ShowWinScreen;
        }
    }

    // ---------------------------------------------------------------
    // State handler — drives all panel visibility
    // ---------------------------------------------------------------

    private void HandleStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Playing:
                ShowHUDOnly();
                break;

            case GameManager.GameState.Paused:
                ShowPauseMenu();
                break;

            case GameManager.GameState.GameOver:
                // ShowGameOverScreen() is called directly from the event
                break;

            case GameManager.GameState.Win:
                // ShowWinScreen() is called directly from the event
                break;
        }
    }

    // ---------------------------------------------------------------
    // Panel visibility helpers
    // ---------------------------------------------------------------

    /// <summary>Shows only the HUD. Call this on resume or scene start.</summary>
    private void ShowHUDOnly()
    {
        SetPanel(hudPanel,      true);
        SetPanel(pausePanel,    false);
        SetPanel(gameOverPanel, false);
        SetPanel(winPanel,      false);

        SetCrosshair(true);

        if (pauseOverlay != null) pauseOverlay.SetActive(false);

        // Key indicator hidden by default — shown when key is collected
        if (keyItemIndicator != null) keyItemIndicator.SetActive(false);
    }

    /// <summary>Shows pause panel on top of the HUD.</summary>
    private void ShowPauseMenu()
    {
        // Keep HUD visible underneath
        SetPanel(hudPanel,   true);
        SetPanel(pausePanel, true);

        SetCrosshair(false);

        if (pauseOverlay != null) pauseOverlay.SetActive(true);

        Debug.Log("[UIManager] Pause menu shown.");
    }

    /// <summary>Called by GameManager.OnGameOver event.</summary>
    private void ShowGameOverScreen()
    {
        SetPanel(hudPanel,      false);
        SetPanel(pausePanel,    false);
        SetPanel(gameOverPanel, true);
        SetPanel(winPanel,      false);

        SetCrosshair(false);

        if (gameOverMessageText != null)
            gameOverMessageText.text = "You died.\nThe colony needs you.";

        Debug.Log("[UIManager] Game Over screen shown.");
    }

    /// <summary>Called by GameManager.OnStageWin event.</summary>
    private void ShowWinScreen()
    {
        SetPanel(hudPanel,      false);
        SetPanel(pausePanel,    false);
        SetPanel(gameOverPanel, false);
        SetPanel(winPanel,      true);

        SetCrosshair(false);

        if (winMessageText != null)
            winMessageText.text = "Stage complete.\nThe colony survives — for now.";

        // Hide Next Level button if no next scene is configured
        // LevelManager will call ShowNextLevelButton() if one exists
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);

        Debug.Log("[UIManager] Win screen shown.");
    }

    // ---------------------------------------------------------------
    // HUD update methods — called by PlayerController & InventoryManager
    // ---------------------------------------------------------------

    /// <summary>
    /// Call this once when the player is initialised to set the bar's max.
    /// PlayerController calls this in its Start().
    /// </summary>
    public void SetMaxHealth(int maxHealth)
    {
        _maxHealth = Mathf.Max(1, maxHealth);
        // Trigger a refresh at full health
        UpdateHealth(maxHealth);
    }

    /// <summary>
    /// Updates the health bar fill and numeric label.
    /// PlayerController calls this every time the player takes damage or heals.
    /// </summary>
    public void UpdateHealth(int currentHealth)
    {
        float fraction = Mathf.Clamp01((float)currentHealth / _maxHealth);

        if (healthBarFill != null)
            healthBarFill.fillAmount = fraction;

        if (healthText != null)
            healthText.text = $"{currentHealth} / {_maxHealth}";
    }

    /// <summary>
    /// Updates the ammo counter in the HUD.
    /// InventoryManager calls this whenever ammo changes.
    /// </summary>
    public void UpdateAmmo(int current, int reserve)
    {
        if (ammoText != null)
            ammoText.text = $"{current} / {reserve}";
    }

    /// <summary>
    /// Updates the weapon icon image in the HUD.
    /// WeaponController calls this on switch and on start.
    /// </summary>
    public void UpdateWeaponDisplay(string weaponName, Sprite icon)
    {
        if (weaponIcon == null) return;
        if (icon != null)
        {
            weaponIcon.sprite  = icon;
            weaponIcon.enabled = true;
        }
    }

    /// <summary>
    /// Updates the objective tracker text.
    /// ObjectiveManager calls this when any objective's progress changes.
    /// </summary>
    public void UpdateObjectiveText(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    /// <summary>
    /// Shows the Next Level button on the win screen.
    /// LevelManager calls this if a next scene exists in Build Settings.
    /// </summary>
    public void ShowNextLevelButton()
    {
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(true);
    }

    // ---------------------------------------------------------------
    // Key item indicator
    // ---------------------------------------------------------------

    /// <summary>
    /// Shows the key item indicator in the HUD.
    /// Call this when player collects the key.
    /// InventoryManager.OnKeyItemAdded triggers this via ObjectiveManager
    /// or wire it directly from InventoryManager.
    /// </summary>
    public void ShowKeyItem(string itemName = "Access Key", Sprite icon = null)
    {
        if (keyItemIndicator != null)
            keyItemIndicator.SetActive(true);

        if (keyItemText != null)
            keyItemText.text = itemName;

        if (keyItemIcon != null && icon != null)
            keyItemIcon.sprite = icon;

        Debug.Log($"[UIManager] Key item shown: {itemName}");
    }

    /// <summary>Hides the key item indicator — call on scene restart.</summary>
    public void HideKeyItem()
    {
        if (keyItemIndicator != null)
            keyItemIndicator.SetActive(false);
    }

    // ---------------------------------------------------------------
    // Button action helpers
    // ---------------------------------------------------------------

    private void RestartScene()
    {
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        SceneManager.LoadScene(0);
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void SetCrosshair(bool visible)
    {
        if (crosshair != null) crosshair.SetActive(visible);
    }
}
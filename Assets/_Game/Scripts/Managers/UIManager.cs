using System.Collections;
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
    [Tooltip("Image shown when player has collected the access key — hidden by default")]
    public GameObject keyItemIndicator;

    [Tooltip("Optional icon image inside the key indicator")]
    public UnityEngine.UI.Image keyItemIcon;

    [Tooltip("Optional text label e.g. 'Access Key'")]
    public TextMeshProUGUI keyItemText;

    [Header("HUD — Power Cell 1")]
    [Tooltip("Shown when power_cell_01 is collected — hidden by default")]
    public GameObject powerCell1Indicator;

    [Tooltip("Icon image for power cell 1")]
    public UnityEngine.UI.Image powerCell1Icon;

    [Tooltip("Text label for power cell 1")]
    public TextMeshProUGUI powerCell1Text;

    [Header("HUD — Power Cell 2")]
    [Tooltip("Shown when power_cell_02 is collected — hidden by default")]
    public GameObject powerCell2Indicator;

    [Tooltip("Icon image for power cell 2")]
    public UnityEngine.UI.Image powerCell2Icon;

    [Tooltip("Text label for power cell 2")]
    public TextMeshProUGUI powerCell2Text;

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
    // Inspector slots — Settings panel (inside pause menu)
    // ---------------------------------------------------------------

    [Header("Settings — Mouse")]
    [Tooltip("Slider controlling mouse sensitivity — wire to PlayerController")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    [Header("Settings — Audio")]
    [Tooltip("Slider for master volume (0-1)")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeValueText;

    [Tooltip("Slider for music volume (0-1)")]
    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicVolumeValueText;

    [Tooltip("Slider for SFX volume (0-1)")]
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeValueText;

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

    private int              _maxHealth = 100;
    private PlayerController _player;

    // Tracks which item indicators have been shown so they survive pause/resume
    private bool _keyItemCollected;
    private bool _powerCell1Collected;
    private bool _powerCell2Collected;

    // PlayerPrefs keys
    private const string KEY_SENSITIVITY    = "MouseSensitivity";
    private const string KEY_MASTER_VOLUME  = "MasterVolume";
    private const string KEY_MUSIC_VOLUME   = "MusicVolume";
    private const string KEY_SFX_VOLUME     = "SFXVolume";

    // Key item IDs — must match InventoryManager.AddKeyItem() calls
    private const string KEY_POWER_CELL_01 = "power_cell_01";
    private const string KEY_POWER_CELL_02 = "power_cell_02";

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
            Debug.LogWarning("[UIManager] GameManager.Instance not found.");
        }

        // Subscribe to InventoryManager events — UIManager reacts, nothing calls it directly
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnAmmoChanged  += OnAmmoChanged;
            InventoryManager.Instance.OnKeyItemAdded += OnKeyItemCollected;
        }

        // Subscribe to ObjectiveManager — drives objective HUD text
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnObjectiveTextChanged += UpdateObjectiveText;

        // Cache PlayerController — used for sensitivity and health events
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null)
        {
            _player.OnHealthChanged += UpdateHealth;
            _player.OnMaxHealthSet  += SetMaxHealth;
        }

        // Init settings sliders (loads PlayerPrefs and wires listeners)
        InitSettings();

        // Delay HUD init by one frame so InventoryManager events settle first
        StartCoroutine(DelayedHUDInit());

        // Wire buttons in code — no Inspector OnClick() needed
        resumeButton?         .onClick.AddListener(() => GameManager.Instance?.ResumeGame());
        pauseRestartButton?   .onClick.AddListener(RestartScene);
        quitButton?           .onClick.AddListener(GoToMainMenu);
        gameOverRestartButton?.onClick.AddListener(RestartScene);
        nextLevelButton?      .onClick.AddListener(() => { Time.timeScale = 1f; LevelManager.Instance?.LoadNextLevel(); });
        mainMenuButton?       .onClick.AddListener(GoToMainMenu);

    }

    private IEnumerator DelayedHUDInit()
    {
        yield return null; // wait one frame for inventory to settle
        ShowHUDOnly();     // hide all indicators cleanly
    }

    // ---------------------------------------------------------------
    // Settings — init, load, save
    // ---------------------------------------------------------------

    private void InitSettings()
    {
        // Temporarily activate pause panel so slider components can initialize
        bool wasActive = false;
        if (pausePanel != null)
        {
            wasActive = pausePanel.activeSelf;
            pausePanel.SetActive(true);
        }

        // Load saved values — defaults: sensitivity 2, all volumes 1
        float sens        = PlayerPrefs.GetFloat(KEY_SENSITIVITY,   2f);
        float masterVol   = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1f);
        float musicVol    = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME,  1f);
        float sfxVol      = PlayerPrefs.GetFloat(KEY_SFX_VOLUME,    1f);

        // Wire sliders — panel must be active for listeners to register
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.5f;
            sensitivitySlider.maxValue = 10f;
            sensitivitySlider.value    = sens;
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            UpdateSensitivityText(sens);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value    = masterVol;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            UpdateVolumeText(masterVolumeValueText, masterVol);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value    = musicVol;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            UpdateVolumeText(musicVolumeValueText, musicVol);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value    = sfxVol;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            UpdateVolumeText(sfxVolumeValueText, sfxVol);
        }

        // Restore pause panel state
        if (pausePanel != null && !wasActive)
            pausePanel.SetActive(false);

        // Apply loaded values immediately
        ApplySensitivity(sens);
        ApplyMasterVolume(masterVol);
        ApplyMusicVolume(musicVol);
        ApplySFXVolume(sfxVol);

        ShowHUDOnly();
    }

    // ---------------------------------------------------------------
    // Slider callbacks
    // ---------------------------------------------------------------

    private void OnSensitivityChanged(float val)
    {
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, val);
        UpdateSensitivityText(val);
        ApplySensitivity(val);
    }

    private void OnMasterVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, val);
        UpdateVolumeText(masterVolumeValueText, val);
        ApplyMasterVolume(val);
    }

    private void OnMusicVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, val);
        UpdateVolumeText(musicVolumeValueText, val);
        ApplyMusicVolume(val);
    }

    private void OnSFXVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, val);
        UpdateVolumeText(sfxVolumeValueText, val);
        ApplySFXVolume(val);
    }

    // ---------------------------------------------------------------
    // Apply values to game systems
    // ---------------------------------------------------------------

    private void ApplySensitivity(float val)
    {
        if (_player != null) _player.mouseSensitivity = val;
    }

    private void ApplyMasterVolume(float val)
    {
        AudioListener.volume = val;
    }

    private void ApplyMusicVolume(float val)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(val);
    }

    private void ApplySFXVolume(float val)
    {
        // SFXManager reads this when playing sounds
        if (SFXManager.Instance != null)
            SFXManager.Instance.masterVolume = val;
    }

    // ---------------------------------------------------------------
    // Label helpers
    // ---------------------------------------------------------------

    private void UpdateSensitivityText(float val)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = val.ToString("F1");
    }

    private void UpdateVolumeText(TextMeshProUGUI label, float val)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(val * 100f) + "%";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameOver     -= ShowGameOverScreen;
            GameManager.Instance.OnStageWin     -= ShowWinScreen;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnAmmoChanged  -= OnAmmoChanged;
            InventoryManager.Instance.OnKeyItemAdded -= OnKeyItemCollected;
        }

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnObjectiveTextChanged -= UpdateObjectiveText;
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

    /// <summary>Shows only the HUD. Restores any item indicators the player has collected.</summary>
    private void ShowHUDOnly()
    {
        SetPanel(hudPanel,      true);
        SetPanel(pausePanel,    false);
        SetPanel(gameOverPanel, false);
        SetPanel(winPanel,      false);

        SetCrosshair(true);

        if (pauseOverlay != null) pauseOverlay.SetActive(false);

        // Restore collected-item indicators — never reset them on resume
        if (keyItemIndicator    != null) keyItemIndicator.SetActive(_keyItemCollected);
        if (powerCell1Indicator != null) powerCell1Indicator.SetActive(_powerCell1Collected);
        if (powerCell2Indicator != null) powerCell2Indicator.SetActive(_powerCell2Collected);
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
    // Private event handlers — UIManager listens, nothing calls it directly
    // ---------------------------------------------------------------

    /// <summary>Fires when InventoryManager.OnAmmoChanged fires.</summary>
    private void OnAmmoChanged(InventoryManager.WeaponType type, int current, int reserve)
    {
        UpdateAmmo(current, reserve);
    }

    /// <summary>Fires when InventoryManager.OnKeyItemAdded fires.</summary>
    private void OnKeyItemCollected(string itemId)
    {
        if (itemId == KEY_POWER_CELL_01)       ShowPowerCell1();
        else if (itemId == KEY_POWER_CELL_02)  ShowPowerCell2();
        else                                   ShowKeyItem(itemId);
    }

    // ---------------------------------------------------------------
    // HUD update methods — still public for direct calls where needed
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
        _keyItemCollected = true;
        if (keyItemIndicator != null) keyItemIndicator.SetActive(true);
        if (keyItemText != null) keyItemText.text = itemName;
        if (keyItemIcon != null && icon != null) keyItemIcon.sprite = icon;
        Debug.Log($"[UIManager] Key item shown: {itemName}");
    }

    /// <summary>Hides the key item indicator and clears collected flag — call on scene restart.</summary>
    public void HideKeyItem()
    {
        _keyItemCollected = false;
        if (keyItemIndicator != null) keyItemIndicator.SetActive(false);
    }

    /// <summary>Shows power cell 1 indicator when collected.</summary>
    public void ShowPowerCell1(Sprite icon = null)
    {
        _powerCell1Collected = true;
        if (powerCell1Indicator != null) powerCell1Indicator.SetActive(true);
        if (powerCell1Text != null) powerCell1Text.text = "Power Cell 1";
        if (powerCell1Icon != null && icon != null) powerCell1Icon.sprite = icon;
        Debug.Log("[UIManager] Power Cell 1 collected.");
    }

    /// <summary>Shows power cell 2 indicator when collected.</summary>
    public void ShowPowerCell2(Sprite icon = null)
    {
        _powerCell2Collected = true;
        if (powerCell2Indicator != null) powerCell2Indicator.SetActive(true);
        if (powerCell2Text != null) powerCell2Text.text = "Power Cell 2";
        if (powerCell2Icon != null && icon != null) powerCell2Icon.sprite = icon;
        Debug.Log("[UIManager] Power Cell 2 collected.");
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
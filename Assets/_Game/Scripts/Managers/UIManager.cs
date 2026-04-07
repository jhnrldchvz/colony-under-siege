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

    [Header("HUD — Reload Indicator")]
    [Tooltip("TMP or Image shown in place of crosshair while reloading")]
    public GameObject reloadIndicator;
    [Tooltip("Blink speed — times per second")]
    public float      reloadBlinkSpeed = 4f;

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

    [Header("HUD — Deactivation Device Indicator")]
    [Tooltip("Shown when deactivation_device_01 is collected")]
    public GameObject            deactivationDeviceIndicator;
    public UnityEngine.UI.Image  deactivationDeviceIcon;
    public TextMeshProUGUI       deactivationDeviceText;

    [Header("HUD — AI Core Healing Warning")]
    [Tooltip("Banner panel shown while AI Core is healing enemies")]
    public GameObject            healingWarningBanner;
    [Tooltip("TMP text on the healing warning banner")]
    public TMPro.TextMeshProUGUI healingWarningText;

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

    [Header("Pre-Game Instructions")]
    [Tooltip("Root panel shown before gameplay starts — hidden after player clicks Start")]
    public GameObject instructionPanel;

    [Tooltip("Each entry = one slide. Add images and text per slide.")]
    public InstructionSlide[] instructionSlides;

    [Tooltip("Image component that displays the current slide image")]
    public UnityEngine.UI.Image slideImage;

    [Tooltip("TMP text shown below the slide image")]
    public TMPro.TextMeshProUGUI slideBodyText;

    [Tooltip("TMP showing current slide number e.g. '1 / 2'")]
    public TMPro.TextMeshProUGUI slideCounterText;

    public UnityEngine.UI.Button slideNextButton;
    public UnityEngine.UI.Button slidePrevButton;
    public UnityEngine.UI.Button slideStartButton;

    [Header("Win Panel — Score Display")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI killPointsText;
    public TextMeshProUGUI accuracyBonusText;
    public TextMeshProUGUI timeBonusText;

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private int              _maxHealth   = 100;
    private int              _slideIndex  = 0;
    private PlayerController _player;
    private bool             _isReloading = false;
    private float            _blinkTimer  = 0f;

    // Tracks which item indicators have been shown so they survive pause/resume
    private bool _keyItemCollected;
    private bool _powerCell1Collected;
    private bool _powerCell2Collected;
    private bool _deactivationDeviceCollected;

    // PlayerPrefs keys
    private const string KEY_SENSITIVITY    = "MouseSensitivity";
    private const string KEY_MASTER_VOLUME  = "MasterVolume";
    private const string KEY_MUSIC_VOLUME   = "MusicVolume";
    private const string KEY_SFX_VOLUME     = "SFXVolume";

    // Key item IDs — must match InventoryManager.AddKeyItem() calls
    private const string KEY_POWER_CELL_01     = "power_cell_01";
    private const string KEY_POWER_CELL_02     = "power_cell_02";
    private const string KEY_DEACTIVATION_TOOL = "deactivation_device_01";

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Update()
    {
        if (!_isReloading || reloadIndicator == null) return;

        _blinkTimer += Time.deltaTime * reloadBlinkSpeed;
        reloadIndicator.SetActive(Mathf.Sin(_blinkTimer * Mathf.PI) > 0f);
    }

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
        winRestartButton?     .onClick.AddListener(RestartScene);
        nextLevelButton?      .onClick.AddListener(() => { Time.timeScale = 1f; LevelManager.Instance?.LoadNextLevel(); });
        mainMenuButton?       .onClick.AddListener(GoToMainMenu);

    }

    private IEnumerator DelayedHUDInit()
    {
        yield return null; // wait one frame for inventory to settle

        if (instructionSlides != null && instructionSlides.Length > 0 && instructionPanel != null)
        {
            ShowInstructions();
        }
        else
        {
            ShowHUDOnly();
        }
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

        // Ensure reload indicator starts hidden
        if (reloadIndicator != null) reloadIndicator.SetActive(false);
        if (crosshair       != null) crosshair.SetActive(true);

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

    // ---------------------------------------------------------------
    // AI Core healing warning
    // ---------------------------------------------------------------

    /// <summary>Shows the healing warning banner with optional message.</summary>
    public void ShowHealingWarning(string message = "⚠ AI Core healing enemies — deactivate terminals!")
    {
        if (healingWarningBanner != null) healingWarningBanner.SetActive(true);
        if (healingWarningText   != null) healingWarningText.text = message;
    }

    /// <summary>Updates the warning text — called each heal tick with countdown.</summary>
    public void UpdateHealingWarning(string message)
    {
        if (healingWarningText != null) healingWarningText.text = message;
    }

    /// <summary>Hides the healing warning banner when AI Core is deactivated.</summary>
    public void HideHealingWarning()
    {
        if (healingWarningBanner != null) healingWarningBanner.SetActive(false);
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

    // ---------------------------------------------------------------
    // Pre-Game Instructions
    // ---------------------------------------------------------------

    private void ShowInstructions()
    {
        // Enter Instructions state — freezes time, unlocks cursor, blocks all gameplay input.
        // ESC has no effect in this state (GameManager.Update ignores it).
        GameManager.Instance?.StartInstructions();

        // Hide all panels — show only instruction panel
        if (hudPanel          != null) hudPanel.SetActive(false);
        if (pausePanel        != null) pausePanel.SetActive(false);
        if (gameOverPanel     != null) gameOverPanel.SetActive(false);
        if (winPanel          != null) winPanel.SetActive(false);
        if (instructionPanel  != null) instructionPanel.SetActive(true);

        // Wire buttons
        slideNextButton?  .onClick.RemoveAllListeners();
        slidePrevButton?  .onClick.RemoveAllListeners();
        slideStartButton? .onClick.RemoveAllListeners();

        slideNextButton?  .onClick.AddListener(SlideNext);
        slidePrevButton?  .onClick.AddListener(SlidePrev);
        slideStartButton? .onClick.AddListener(SlideStart);

        _slideIndex = 0;
        RefreshSlide();
    }

    private void RefreshSlide()
    {
        if (instructionSlides == null || instructionSlides.Length == 0) return;

        _slideIndex = Mathf.Clamp(_slideIndex, 0, instructionSlides.Length - 1);
        InstructionSlide slide = instructionSlides[_slideIndex];

        if (slideImage      != null) slideImage.sprite  = slide.image;
        if (slideBodyText   != null) slideBodyText.text  = slide.bodyText;
        if (slideCounterText != null)
            slideCounterText.text = $"{_slideIndex + 1} / {instructionSlides.Length}";

        // Navigation button visibility
        bool isFirst = _slideIndex == 0;
        bool isLast  = _slideIndex == instructionSlides.Length - 1;

        // Prev only shows when not on first slide
        if (slidePrevButton  != null) slidePrevButton.gameObject.SetActive(!isFirst);

        // Next hides on last slide, Start shows on last slide
        if (slideNextButton  != null) slideNextButton.gameObject.SetActive(!isLast);
        if (slideStartButton != null) slideStartButton.gameObject.SetActive(isLast);
    }

    private void SlideNext()
    {
        _slideIndex++;
        RefreshSlide();
    }

    private void SlidePrev()
    {
        _slideIndex--;
        RefreshSlide();
    }

    private void SlideStart()
    {
        if (instructionPanel != null) instructionPanel.SetActive(false);

        // Transition from Instructions → Playing: restores timeScale, locks cursor, fires OnStateChanged
        GameManager.Instance?.EndInstructions();
        ShowHUDOnly();
    }

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
        if (powerCell2Indicator          != null) powerCell2Indicator.SetActive(_powerCell2Collected);
        if (deactivationDeviceIndicator  != null) deactivationDeviceIndicator.SetActive(_deactivationDeviceCollected);
        // Healing warning managed by AICoreManager — don't reset here
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
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Stop score timer and populate score display
        ScoreManager.Instance?.StopTimer();
        PopulateScoreDisplay();

        if (winMessageText != null)
            winMessageText.text = "Stage complete.\nThe colony survives — for now.";

        // Hide Next Level button if no next scene is configured
        // LevelManager will call ShowNextLevelButton() if one exists
        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);

        Debug.Log("[UIManager] Win screen shown.");
    }

    // ---------------------------------------------------------------
    // Score display
    // ---------------------------------------------------------------

    private void PopulateScoreDisplay()
    {
        if (ScoreManager.Instance == null) return;

        ScoreManager.ScoreData s = ScoreManager.Instance.Calculate();

        if (scoreText         != null) scoreText.text         = $"{s.totalScore:N0}";
        if (gradeText         != null) gradeText.text         = s.grade;
        if (killsText         != null) killsText.text         = $"Kills: {s.kills}";
        if (accuracyText      != null) accuracyText.text      = $"Accuracy: {s.accuracyPct:F0}%";
        if (timeText          != null) timeText.text          = $"Time: {FormatTime(s.elapsedSeconds)}";
        if (killPointsText    != null) killPointsText.text    = $"+{s.killPoints}";
        if (accuracyBonusText != null) accuracyBonusText.text = $"+{s.accuracyBonus}";
        if (timeBonusText     != null) timeBonusText.text     = $"+{s.timeBonus}";
    }

    private string FormatTime(float seconds)
    {
        int m  = (int)(seconds / 60);
        int s2 = (int)(seconds % 60);
        return $"{m:00}:{s2:00}";
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
        if (itemId == KEY_POWER_CELL_01)           ShowPowerCell1();
        else if (itemId == KEY_POWER_CELL_02)      ShowPowerCell2();
        else if (itemId == KEY_DEACTIVATION_TOOL)  ShowDeactivationDevice();
        else                                       ShowKeyItem(itemId);
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
    /// <summary>
    /// Called by ObjectivePickup — routes to the correct indicator by itemId.
    /// Falls back to generic ShowKeyItem with display name for unknown items.
    /// </summary>
    public void ShowKeyItemByIdAndName(string itemId, string displayName)
    {
        if (itemId == KEY_POWER_CELL_01)          ShowPowerCell1();
        else if (itemId == KEY_POWER_CELL_02)     ShowPowerCell2();
        else if (itemId == KEY_DEACTIVATION_TOOL) ShowDeactivationDevice();
        else                                      ShowKeyItem(displayName);
    }

    public void ShowDeactivationDevice()
    {
        _deactivationDeviceCollected = true;
        if (deactivationDeviceIndicator != null) deactivationDeviceIndicator.SetActive(true);
        if (deactivationDeviceText      != null) deactivationDeviceText.text = "Deactivation Device";
        Debug.Log("[UIManager] Deactivation Device indicator shown.");
    }

    public void HideDeactivationDevice()
    {
        _deactivationDeviceCollected = false;
        if (deactivationDeviceIndicator != null) deactivationDeviceIndicator.SetActive(false);
    }

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

    /// <summary>Called by WeaponController when reload starts/ends.</summary>
    public void SetReloading(bool reloading)
    {
        _isReloading = reloading;
        _blinkTimer  = 0f;

        if (crosshair       != null) crosshair.SetActive(!reloading);
        if (reloadIndicator != null) reloadIndicator.SetActive(reloading);
    }

    private void SetCrosshair(bool visible)
    {
        if (crosshair != null) crosshair.SetActive(visible);
        if (_isReloading && visible)
        {
            // Don't show crosshair if still reloading
            if (crosshair != null) crosshair.SetActive(false);
        }
    }
}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MainMenuManager — controls the main menu scene (build index 0).
///
/// Panel flow:
///   Main → Stage Select → highlights selected card, shows description + PLAY button
///   Main → Settings     → volume and sensitivity sliders
///   Main → Credits      → credits text
///
/// Stage Select behaviour:
///   • Panel opens with Stage 1 pre-selected (index 0 in the Stages array).
///   • Selected card = full alpha.  Unselected cards = dimAlpha (default 0.45).
///   • Clicking a card updates the description area and the PLAY button target.
///   • PLAY button loads the selected stage's scene.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Panels
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject creditsPanel;
    public GameObject stageSelectPanel;
    public GameObject settingsPanel;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Main panel text
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Main Panel Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI versionText;
    public TextMeshProUGUI subtitleText;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Main menu buttons
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Main Menu Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button tutorialButton;
    public Button stageLevelsButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Stage Select panel
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Stage Select — Cards")]
    [Tooltip("One entry per stage. Fill stageName, description, button, sceneIndex.")]
    public StageEntry[] stages;

    [Tooltip("Alpha of unselected stage cards. Selected card is always 1.0.")]
    [Range(0.1f, 0.9f)] public float dimAlpha = 0.45f;

    [Tooltip("When true, only stages the player has already reached are interactable.")]
    public bool requireProgressToUnlock = true;

    [Header("Stage Select — Description Area")]
    [Tooltip("TMP text that shows the selected stage's name.")]
    public TextMeshProUGUI stageNameText;

    [Tooltip("TMP text that shows the selected stage's description.")]
    public TextMeshProUGUI stageDescriptionText;

    [Tooltip("Image that shows the selected stage's preview sprite (optional).")]
    public Image stagePreviewImage;

    [Tooltip("The single PLAY button that loads the currently selected stage.")]
    public Button playButton;

    [Header("Stage Select — Navigation")]
    public Button backFromStagesButton;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Credits panel
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Credits Panel")]
    public Button backFromCreditsButton;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Settings panel
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Settings Panel — Mouse")]
    [Tooltip("Range 0.5–10, same as in-game settings")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    [Header("Settings Panel — Audio")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeValueText;

    public Slider musicVolumeSlider;
    public TextMeshProUGUI musicVolumeValueText;

    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeValueText;

    public Button backFromSettingsButton;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Scene indices
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Scene Indices")]
    public int firstSceneIndex   = 1;
    public int tutorialSceneIndex = 6;

    // ─────────────────────────────────────────────────────────────────────────
    // Inspector — Misc
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Misc")]
    public bool animateTitle = true;

    // ─────────────────────────────────────────────────────────────────────────
    // PlayerPrefs keys
    // ─────────────────────────────────────────────────────────────────────────

    // Same keys as UIManager — shared across main menu and in-game settings
    private const string KEY_SENSITIVITY   = "MouseSensitivity";
    private const string KEY_MASTER_VOLUME = "MasterVolume";
    private const string KEY_MUSIC_VOLUME  = "MusicVolume";
    private const string KEY_SFX_VOLUME    = "SFXVolume";
    private const string KEY_SAVED_SCENE = "SavedSceneBuildIndex";

    // ─────────────────────────────────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────────────────────────────────

    private int   _selectedStageIndex = 0;
    private float _titleTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        Time.timeScale = 1f;

        WireMainButtons();
        WireStageCards();
        WireSettingsSliders();
        WireBackButtons();

        if (versionText  != null) versionText.text  = $"v{Application.version}";
        if (titleText    != null) titleText.text     = "Colony Under Siege";
        if (subtitleText != null) subtitleText.text  = "The colony needs you.";

        RefreshContinueButton();
        ShowPanel(mainPanel);
        StartCoroutine(UnlockCursorDelayed());
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
        titleText.transform.localScale =
            Vector3.one * (1f + Mathf.Sin(_titleTimer * 1.5f) * 0.02f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Wiring
    // ─────────────────────────────────────────────────────────────────────────

    private void WireMainButtons()
    {
        newGameButton?    .onClick.AddListener(OnNewGamePressed);
        continueButton?   .onClick.AddListener(OnContinuePressed);
        tutorialButton?   .onClick.AddListener(OnTutorialPressed);
        stageLevelsButton?.onClick.AddListener(OnStageLevelsPressed);
        settingsButton?   .onClick.AddListener(OnSettingsPressed);
        creditsButton?    .onClick.AddListener(OnCreditsPressed);
        quitButton?       .onClick.AddListener(OnQuitPressed);
    }

    private void WireStageCards()
    {
        if (stages == null || stages.Length == 0) return;

        int savedScene = SaveManager.Instance != null && SaveManager.Instance.HasSaveData()
            ? PlayerPrefs.GetInt(KEY_SAVED_SCENE, firstSceneIndex)
            : firstSceneIndex;

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].button == null) continue;

            int captured = i;   // closure capture
            stages[i].button.onClick.AddListener(() => SelectStage(captured));

            bool unlocked = !requireProgressToUnlock ||
                            stages[i].sceneIndex <= savedScene;

            stages[i].button.interactable = unlocked;

            // Use explicit Unity != null (not ?.) — catches fake-null/missing references
            if (stages[i].lockedOverlay != null)
                stages[i].lockedOverlay.SetActive(!unlocked);
        }

        // Wire the single PLAY button
        playButton?.onClick.AddListener(OnPlayPressed);
    }

    private void WireSettingsSliders()
    {
        float sens        = PlayerPrefs.GetFloat(KEY_SENSITIVITY,   2f);
        float masterVol   = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1f);
        float musicVol    = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME,  1f);
        float sfxVol      = PlayerPrefs.GetFloat(KEY_SFX_VOLUME,    1f);

        // ── Mouse Sensitivity ────────────────────────────────────────────────
        InitSlider(sensitivitySlider, 0.5f, 10f, sens, v =>
        {
            PlayerPrefs.SetFloat(KEY_SENSITIVITY, v);
            PlayerPrefs.Save();
            if (sensitivityValueText != null) sensitivityValueText.text = v.ToString("F1");
        });
        if (sensitivityValueText != null) sensitivityValueText.text = sens.ToString("F1");

        // ── Master Volume ────────────────────────────────────────────────────
        InitSlider(masterVolumeSlider, 0f, 1f, masterVol, v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, v);
            PlayerPrefs.Save();
            SetVolumeLabel(masterVolumeValueText, v);
        });
        AudioListener.volume = masterVol;
        SetVolumeLabel(masterVolumeValueText, masterVol);

        // ── Music Volume ─────────────────────────────────────────────────────
        InitSlider(musicVolumeSlider, 0f, 1f, musicVol, v =>
        {
            AudioManager.Instance?.SetMusicVolume(v);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, v);
            PlayerPrefs.Save();
            SetVolumeLabel(musicVolumeValueText, v);
        });
        AudioManager.Instance?.SetMusicVolume(musicVol);
        SetVolumeLabel(musicVolumeValueText, musicVol);

        // ── SFX Volume ───────────────────────────────────────────────────────
        InitSlider(sfxVolumeSlider, 0f, 1f, sfxVol, v =>
        {
            if (SFXManager.Instance != null) SFXManager.Instance.masterVolume = v;
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, v);
            PlayerPrefs.Save();
            SetVolumeLabel(sfxVolumeValueText, v);
        });
        if (SFXManager.Instance != null) SFXManager.Instance.masterVolume = sfxVol;
        SetVolumeLabel(sfxVolumeValueText, sfxVol);
    }

    private static void InitSlider(Slider slider, float min, float max, float value,
                                   UnityEngine.Events.UnityAction<float> onChange)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = value;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(onChange);
    }

    private void WireBackButtons()
    {
        backFromCreditsButton? .onClick.AddListener(() => ShowPanel(mainPanel));
        backFromStagesButton?  .onClick.AddListener(() => ShowPanel(mainPanel));
        backFromSettingsButton?.onClick.AddListener(() => ShowPanel(mainPanel));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stage selection
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Highlights the card at <paramref name="index"/>, dims all others,
    /// and updates the description area + PLAY button.
    /// </summary>
    private void SelectStage(int index)
    {
        if (stages == null || index < 0 || index >= stages.Length) return;

        _selectedStageIndex = index;

        // ── Card alpha ───────────────────────────────────────────────────────
        for (int i = 0; i < stages.Length; i++)
        {
            float alpha = (i == index) ? 1f : dimAlpha;
            SetCardAlpha(i, alpha);
        }

        // ── Description area ─────────────────────────────────────────────────
        StageEntry selected = stages[index];

        if (stageNameText != null)
            stageNameText.text = selected.stageName;

        if (stageDescriptionText != null)
            stageDescriptionText.text = selected.description;

        if (stagePreviewImage != null && selected.previewSprite != null)
        {
            stagePreviewImage.sprite = selected.previewSprite;
            stagePreviewImage.gameObject.SetActive(true);
        }

        // ── PLAY button ──────────────────────────────────────────────────────
        if (playButton != null)
            playButton.interactable = selected.button != null && selected.button.interactable;

        Debug.Log($"[MainMenuManager] Stage selected: {selected.stageName} " +
                  $"(scene {selected.sceneIndex})");
    }

    /// <summary>
    /// Sets the visual alpha of stage card at <paramref name="index"/>.
    /// Uses the card's <see cref="CanvasGroup"/> if assigned; otherwise
    /// falls back to the Button's Image alpha.
    /// </summary>
    private void SetCardAlpha(int index, float alpha)
    {
        if (stages[index].cardGroup != null)
        {
            stages[index].cardGroup.alpha = alpha;
            return;
        }

        if (stages[index].button == null) return;

        // Fallback: tint the button image
        Image img = stages[index].button.image;
        if (img != null)
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnNewGamePressed()
    {
        SaveManager.Instance?.DeleteSave();
        SceneManager.LoadScene(firstSceneIndex);
    }

    private void OnContinuePressed()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSaveData()) return;
        SaveManager.Instance.LoadSavedGame();
    }

    private void OnTutorialPressed()
    {
        SceneManager.LoadScene(tutorialSceneIndex);
    }

    private void OnStageLevelsPressed()
    {
        ShowPanel(stageSelectPanel);
        SelectStage(0);   // always open with Stage 1 highlighted
    }

    private void OnSettingsPressed()   => ShowPanel(settingsPanel);
    private void OnCreditsPressed()    => ShowPanel(creditsPanel);

    private void OnPlayPressed()
    {
        if (stages == null || _selectedStageIndex >= stages.Length) return;
        int scene = stages[_selectedStageIndex].sceneIndex;
        Debug.Log($"[MainMenuManager] PLAY — loading scene {scene}.");
        SceneManager.LoadScene(scene);
    }

    private void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Panel switching
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowPanel(GameObject target)
    {
        mainPanel?       .SetActive(target == mainPanel);
        creditsPanel?    .SetActive(target == creditsPanel);
        stageSelectPanel?.SetActive(target == stageSelectPanel);
        settingsPanel?   .SetActive(target == settingsPanel);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void RefreshContinueButton()
    {
        continueButton?.gameObject.SetActive(
            SaveManager.Instance != null && SaveManager.Instance.HasSaveData());
    }

    private static void SetVolumeLabel(TextMeshProUGUI label, float v)
    {
        if (label != null) label.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    private static IEnumerator UnlockCursorDelayed()
    {
        yield return null;
        yield return null;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StageEntry — one card in the Stage Select panel
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public struct StageEntry
{
    [Tooltip("Name shown in the description area when this stage is selected.")]
    public string stageName;

    [Tooltip("Short description shown below the cards when this stage is selected.")]
    [TextArea(2, 5)] public string description;

    [Tooltip("Optional preview sprite shown in the description area.")]
    public Sprite previewSprite;

    [Tooltip("The Button component on this stage card.")]
    public Button button;

    [Tooltip("Build index of the scene this stage loads.")]
    public int sceneIndex;

    [Tooltip("Optional CanvasGroup on the card root — used for full-card alpha dimming.\n" +
             "If empty, the Button's Image alpha is used as fallback.")]
    public CanvasGroup cardGroup;

    [Tooltip("Optional overlay shown when this stage is locked (lock icon, grey tint, etc.).")]
    public GameObject lockedOverlay;
}

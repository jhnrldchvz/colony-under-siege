using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TutorialManager — singleton that drives the tutorial scene UI.
///
/// Responsibilities:
///   - Instruction Panel: shown when the player interacts with a ScreenPodInteractable.
///     Time pauses while the panel is open; "Close" button or CloseInstructionPanel()
///     resumes time.
///   - Complete Panel: shown when ObjectiveManager fires OnAllObjectivesComplete.
///     "Restart Tutorial" reloads the current scene.
///     "Back to Main Menu" loads scene 0.
///
/// Setup:
///   1. Create an empty GameObject "TutorialManager" in the scene.
///   2. Attach this script.
///   3. Wire the Inspector references (instruction panel, complete panel, buttons, texts).
///   4. This script subscribes to ObjectiveManager.OnAllObjectivesComplete automatically.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static TutorialManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ---------------------------------------------------------------
    // Inspector — Instruction Panel
    // ---------------------------------------------------------------

    [Header("Instruction Panel")]
    [Tooltip("Root GameObject of the instruction panel (starts hidden).")]
    public GameObject instructionPanel;

    [Tooltip("Title text at the top of the panel.")]
    public TextMeshProUGUI instructionTitleText;

    [Tooltip("Body text showing the room instructions.")]
    public TextMeshProUGUI instructionBodyText;

    [Tooltip("Image shown alongside the instructions (e.g. control diagram). Hidden when no sprite is provided.")]
    public Image instructionImage;

    [Tooltip("The 'Close' button on the instruction panel.")]
    public Button closeInstructionButton;

    [Tooltip("Pause time while instruction panel is open.")]
    public bool pauseWhileReading = true;

    // ---------------------------------------------------------------
    // Inspector — Tutorial Complete Panel
    // ---------------------------------------------------------------

    [Header("Tutorial Complete Panel")]
    [Tooltip("Root GameObject of the complete panel (starts hidden).")]
    public GameObject completePanel;

    [Tooltip("'Restart Tutorial' button — reloads the current scene.")]
    public Button restartButton;

    [Tooltip("'Back to Main Menu' button — loads scene 0.")]
    public Button mainMenuButton;

    [Tooltip("Main menu scene build index (default 0).")]
    public int mainMenuSceneIndex = 0;

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private bool _instructionOpen = false;
    private bool _completeShown   = false;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Hide panels at scene start
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (completePanel    != null) completePanel.SetActive(false);

        // Wire buttons
        if (closeInstructionButton != null)
            closeInstructionButton.onClick.AddListener(CloseInstructionPanel);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartTutorial);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        // Subscribe to objective completion — one-frame delay so ObjectiveManager.Start() runs first
        StartCoroutine(SubscribeNextFrame());
    }

    private IEnumerator SubscribeNextFrame()
    {
        yield return null;

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnAllObjectivesComplete += OnAllObjectivesComplete;
            Debug.Log("[TutorialManager] Subscribed to ObjectiveManager.OnAllObjectivesComplete.");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] ObjectiveManager not found — complete panel won't auto-show.");
        }
    }

    private void OnDestroy()
    {
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnAllObjectivesComplete -= OnAllObjectivesComplete;

        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------
    // Instruction Panel
    // ---------------------------------------------------------------

    private void Update()
    {
        // Close instruction panel with Escape while it is open
        if (_instructionOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseInstructionPanel();
    }

    /// <summary>
    /// Called by ScreenPodInteractable when the player presses E on a terminal.
    /// Pass a null sprite to hide the image area.
    /// </summary>
    public void ShowInstructionPanel(string title, string body, Sprite illustration = null)
    {
        if (instructionPanel == null) return;

        if (instructionTitleText != null) instructionTitleText.text = title;
        if (instructionBodyText  != null) instructionBodyText.text  = body;

        if (instructionImage != null)
        {
            if (illustration != null)
            {
                instructionImage.sprite  = illustration;
                instructionImage.enabled = true;
                instructionImage.gameObject.SetActive(true);
            }
            else
            {
                instructionImage.gameObject.SetActive(false);
            }
        }

        instructionPanel.SetActive(true);
        _instructionOpen = true;

        if (pauseWhileReading)
            Time.timeScale = 0f;

        // Freeze camera look but show cursor so player can read / click Close
        SetPlayerLook(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log($"[TutorialManager] Instruction panel opened: '{title}'");
    }

    /// <summary>
    /// Called by the Close button or Escape key.
    /// </summary>
    public void CloseInstructionPanel()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        _instructionOpen = false;

        if (pauseWhileReading)
            Time.timeScale = 1f;

        // Restore camera look and re-lock cursor for gameplay
        SetPlayerLook(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        Debug.Log("[TutorialManager] Instruction panel closed.");
    }

    private void SetPlayerLook(bool enabled)
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.LookEnabled = enabled;
    }

    // ---------------------------------------------------------------
    // Complete Panel
    // ---------------------------------------------------------------

    private void OnAllObjectivesComplete()
    {
        if (_completeShown) return;
        _completeShown = true;

        // Close instruction panel if it was open
        if (_instructionOpen) CloseInstructionPanel();

        ShowCompletePanel();
    }

    private void ShowCompletePanel()
    {
        if (completePanel == null)
        {
            Debug.LogWarning("[TutorialManager] completePanel not assigned.");
            return;
        }

        completePanel.SetActive(true);

        // Pause game and unlock cursor for buttons
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[TutorialManager] Tutorial complete panel shown.");
    }

    // ---------------------------------------------------------------
    // Button callbacks
    // ---------------------------------------------------------------

    private void RestartTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}

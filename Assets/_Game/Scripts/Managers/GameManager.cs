using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager — Singleton orchestrator for Colony Under Siege.
/// Owns the GameState machine, pause logic, and broadcasts state
/// change events that all other managers subscribe to.
///
/// Setup: Attach to an empty GameObject named "GameManager" in your
/// first scene. It will persist across all scenes automatically.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Enforce single instance across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads
        InitializeGame();
    }

    // ---------------------------------------------------------------
    // Game State
    // ---------------------------------------------------------------

    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        Win
    }

    // Read-only from outside — only GameManager changes state
    public GameState CurrentState { get; private set; }

    // ---------------------------------------------------------------
    // Events — other managers subscribe, never poll
    // ---------------------------------------------------------------

    /// <summary>Fired whenever GameState changes. Passes the new state.</summary>
    public event Action<GameState> OnStateChanged;

    /// <summary>Fired when the player dies.</summary>
    public event Action OnGameOver;

    /// <summary>Fired when all stage objectives are complete.</summary>
    public event Action OnStageWin;

    // Unity Inspector-visible events (for wiring up without code)
    [Header("Inspector Events")]
    [Tooltip("Drag UI panels or objects here to react to pause")]
    public UnityEvent onGamePaused;

    [Tooltip("Drag UI panels or objects here to react to resume")]
    public UnityEvent onGameResumed;

    // ---------------------------------------------------------------
    // Manager References
    // Assign via Inspector or let each manager register itself
    // ---------------------------------------------------------------

    [Header("Manager References")]
    [Tooltip("Assign in Inspector, or leave blank — managers self-register")]
    public UIManager uiManager;
    public LevelManager levelManager;
    // Additional managers added here as they are built

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private bool _isPaused = false;

    // ---------------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------------

    private void InitializeGame()
    {
        // Start in Playing state
        SetState(GameState.Playing);

        // Make sure time is running (important after scene reloads)
        Time.timeScale = 1f;

        Debug.Log("[GameManager] Initialized.");
    }

    // ---------------------------------------------------------------
    // Update — ESC key handling
    // ---------------------------------------------------------------

    private void Update()
    {
        // Only allow pause toggle during active play or paused state
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
    }

    // ---------------------------------------------------------------
    // State Machine
    // ---------------------------------------------------------------

    /// <summary>
    /// Central method for all state transitions.
    /// Always use this — never set CurrentState directly.
    /// </summary>
    private void SetState(GameState newState)
    {
        if (CurrentState == newState) return; // No redundant transitions

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);

        Debug.Log($"[GameManager] State → {newState}");
    }

    // ---------------------------------------------------------------
    // Pause System
    // ---------------------------------------------------------------

    /// <summary>Toggles between Playing and Paused.</summary>
    public void TogglePause()
    {
        if (_isPaused) ResumeGame();
        else PauseGame();
    }

    /// <summary>Pauses the game. Freezes time and shows pause menu.</summary>
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        _isPaused = true;
        Time.timeScale = 0f;           // Freeze physics & animations
        Cursor.lockState = CursorLockMode.None;  // Unlock cursor for UI
        Cursor.visible = true;

        SetState(GameState.Paused);
        onGamePaused?.Invoke();        // Tell UIManager to show pause panel

        Debug.Log("[GameManager] Game Paused.");
    }

    /// <summary>Resumes the game. Restores time and hides pause menu.</summary>
    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        _isPaused = false;
        Time.timeScale = 1f;           // Restore time
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor for FPS
        Cursor.visible = false;

        SetState(GameState.Playing);
        onGameResumed?.Invoke();       // Tell UIManager to hide pause panel

        Debug.Log("[GameManager] Game Resumed.");
    }

    // ---------------------------------------------------------------
    // Game Over & Win
    // ---------------------------------------------------------------

    /// <summary>
    /// Called by PlayerController when the player's health reaches zero.
    /// </summary>
    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return; // Prevent double trigger

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetState(GameState.GameOver);
        OnGameOver?.Invoke();

        Debug.Log("[GameManager] Game Over.");
    }

    /// <summary>
    /// Called by ObjectiveManager when all stage objectives are complete.
    /// </summary>
    public void TriggerStageWin()
    {
        if (CurrentState == GameState.Win) return;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetState(GameState.Win);
        OnStageWin?.Invoke();

        Debug.Log("[GameManager] Stage Complete — Win!");
    }

    // ---------------------------------------------------------------
    // Pause Menu Actions (bound to UI buttons)
    // ---------------------------------------------------------------

    /// <summary>
    /// Resume button — called by UIManager pause menu.
    /// </summary>
    public void OnResumePressed()
    {
        ResumeGame();
    }

    /// <summary>
    /// Restart button — reloads the current active scene.
    /// Works for both Game Over and mid-game restart.
    /// </summary>
    public void OnRestartPressed()
    {
        Time.timeScale = 1f; // Must restore time BEFORE loading
        _isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);

        Debug.Log($"[GameManager] Restarting scene: {currentScene.name}");
    }

    /// <summary>
    /// Quit button — exits play mode in Editor, quits the built application.
    /// </summary>
    public void OnQuitPressed()
    {
        Debug.Log("[GameManager] Quitting...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------
    // Scene Loading (used by LevelManager)
    // ---------------------------------------------------------------

    /// <summary>
    /// Load a scene by its build index.
    /// LevelManager calls this after stage win sequence completes.
    /// </summary>
    public void LoadScene(int buildIndex)
    {
        Time.timeScale = 1f;
        _isPaused = false;
        SceneManager.LoadScene(buildIndex);
    }

    /// <summary>
    /// Load a scene by name.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        _isPaused = false;
        SceneManager.LoadScene(sceneName);
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    /// <summary>Returns true only when the game is actively running.</summary>
    public bool IsPlaying() => CurrentState == GameState.Playing;

    /// <summary>Returns true when the game is paused.</summary>
    public bool IsPaused() => CurrentState == GameState.Paused;

    /// <summary>
    /// Register a manager reference at runtime.
    /// Each manager calls this in its own Awake() so GameManager
    /// doesn't need every reference pre-assigned in the Inspector.
    /// </summary>
    public void RegisterUIManager(UIManager manager)    => uiManager    = manager;
    public void RegisterLevelManager(LevelManager manager) => levelManager = manager;

    // ---------------------------------------------------------------
    // Cleanup
    // ---------------------------------------------------------------

    private void OnDestroy()
    {
        // Clear static reference if this instance is destroyed
        if (Instance == this) Instance = null;
    }
}
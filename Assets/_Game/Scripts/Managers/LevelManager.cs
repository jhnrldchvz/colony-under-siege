using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// LevelManager — owns the current stage lifecycle.
///
/// Responsibilities:
///   - Registers this scene's objectives with ObjectiveManager on load
///   - Listens for the stage win event and starts the next-level sequence
///   - Shows the Next Level button on the win screen if a next scene exists
///   - Handles delayed scene transitions (lets the win screen breathe)
///   - Provides Restart and Quit to GameManager
///
/// Setup:
///   1. Create an empty GameObject named "LevelManager" in each scene.
///   2. Attach this script to it.
///   3. Set nextSceneBuildIndex in the Inspector to the build index of
///      the next level. Set to -1 if this is the last level.
///   4. Add all your scenes to File → Build Settings → Scenes In Build.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton — per-scene, not DontDestroyOnLoad
    // ---------------------------------------------------------------

    public static LevelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Register with GameManager so it holds a typed reference
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterLevelManager(this);
    }

    // ---------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------

    [Header("Scene Flow")]
    [Tooltip("Build index of the next level. Set -1 if this is the last level.")]
    public int nextSceneBuildIndex = -1;

    [Tooltip("Seconds to wait after win before auto-loading the next scene. " +
             "Set 0 to require the player to press Next Level manually.")]
    public float autoLoadDelay = 0f;

    [Header("Level Info")]
    [Tooltip("Display name shown on the win screen, e.g. 'Sector 1 — Outer Perimeter'")]
    public string levelName = "Level 1";

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private bool _levelComplete = false;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Clear key items from previous level — ammo and weapons carry over
        InventoryManager.Instance?.ClearKeyItems();

        // Subscribe to the win event from ObjectiveManager
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnAllObjectivesComplete += OnStageComplete;

        // Subscribe to GameManager.OnStageWin — fires when door is entered
        if (GameManager.Instance != null)
            GameManager.Instance.OnStageWin += OnStageComplete;

        Debug.Log($"[LevelManager] '{levelName}' started. " +
                  $"Next scene index: {nextSceneBuildIndex}");
    }

    private void OnDestroy()
    {
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnAllObjectivesComplete -= OnStageComplete;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStageWin -= OnStageComplete;

        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------
    // Stage complete
    // ---------------------------------------------------------------

    private void OnStageComplete()
    {
        if (_levelComplete) return;
        _levelComplete = true;

        Debug.Log($"[LevelManager] Stage complete: '{levelName}'");

        // Tell UIManager to show the Next Level button if a next scene exists
        bool hasNextLevel = nextSceneBuildIndex >= 0 &&
                            nextSceneBuildIndex < SceneManager.sceneCountInBuildSettings;

        if (hasNextLevel && UIManager.Instance != null)
            UIManager.Instance.ShowNextLevelButton();

        // Auto-load next scene after delay if configured
        if (hasNextLevel && autoLoadDelay > 0f)
            StartCoroutine(AutoLoadNextScene(autoLoadDelay));
    }

    // ---------------------------------------------------------------
    // Scene loading — called by buttons and GameManager
    // ---------------------------------------------------------------

    /// <summary>
    /// Loads the next scene. Called by the "Next Level" button's OnClick().
    /// Wire: Next Level button → LevelManager → LoadNextLevel()
    /// </summary>
    public void LoadNextLevel()
    {
        if (nextSceneBuildIndex < 0)
        {
            Debug.Log("[LevelManager] No next level configured — returning to main menu.");
            LoadMainMenu();
            return;
        }

        Debug.Log($"[LevelManager] Loading next scene: index {nextSceneBuildIndex}");
        GameManager.Instance?.LoadScene(nextSceneBuildIndex);
    }

    /// <summary>
    /// Reloads the current scene from scratch.
    /// Called by GameManager.OnRestartPressed() — already wired.
    /// Can also be called directly from a restart button.
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log($"[LevelManager] Restarting '{levelName}'.");

        // Reset per-session data before reload
        InventoryManager.Instance?.ResetInventory();
        EnemyManager.Instance?.ResetEnemyData();

        GameManager.Instance?.OnRestartPressed();
    }

    /// <summary>
    /// Loads scene 0 (main menu). Extend when you add a menu scene.
    /// </summary>
    public void LoadMainMenu()
    {
        Debug.Log("[LevelManager] Loading main menu (scene 0).");
        GameManager.Instance?.LoadScene(0);
    }

    // ---------------------------------------------------------------
    // Coroutine — auto-load with delay
    // ---------------------------------------------------------------

    private IEnumerator AutoLoadNextScene(float delay)
    {
        Debug.Log($"[LevelManager] Auto-loading next scene in {delay}s...");
        yield return new WaitForSecondsRealtime(delay); // Realtime — unaffected by timeScale
        LoadNextLevel();
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    /// <summary>Returns true if this is the last level in the build.</summary>
    public bool IsLastLevel()
    {
        return nextSceneBuildIndex < 0 ||
               nextSceneBuildIndex >= SceneManager.sceneCountInBuildSettings;
    }
}
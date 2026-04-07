using UnityEngine;

/// <summary>
/// SaveManager — persists the player's progress across sessions using PlayerPrefs.
///
/// What is saved:
///   - The build index of the last scene the player should continue from.
///
/// When saves happen:
///   1. Pause menu → "Main Menu" — saves current scene (player replays from stage start).
///   2. Stage win — saves the NEXT scene index before loading it (progress advances).
///      Call SaveProgress(nextIndex) from LevelManager.LoadNextLevel().
///
/// When saves load:
///   - Main menu "Continue" button → loads the saved scene index.
///   - New Game deletes the save before loading scene 1.
///
/// Setup:
///   Attach to a persistent GameObject in the MainMenu scene (build index 0).
///   GameManager.DontDestroyOnLoad keeps it alive across all scenes.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // ---------------------------------------------------------------
    // PlayerPrefs keys
    // ---------------------------------------------------------------

    private const string KEY_SCENE   = "SavedSceneBuildIndex";
    private const string KEY_EXISTS  = "HasSaveData";

    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    /// <summary>Returns true if a save file exists.</summary>
    public bool HasSaveData() => PlayerPrefs.GetInt(KEY_EXISTS, 0) == 1;

    /// <summary>
    /// Saves progress at the given scene build index.
    /// Call with the CURRENT scene index when pausing to main menu,
    /// or with the NEXT scene index when completing a stage.
    /// </summary>
    public void SaveProgress(int sceneBuildIndex)
    {
        PlayerPrefs.SetInt(KEY_SCENE,  sceneBuildIndex);
        PlayerPrefs.SetInt(KEY_EXISTS, 1);
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] Progress saved — scene index {sceneBuildIndex}.");
    }

    /// <summary>
    /// Loads the saved scene. Call from the "Continue" button in the main menu.
    /// </summary>
    public void LoadSavedGame()
    {
        if (!HasSaveData())
        {
            Debug.LogWarning("[SaveManager] LoadSavedGame() called but no save exists.");
            return;
        }

        int index = PlayerPrefs.GetInt(KEY_SCENE, 1);
        Debug.Log($"[SaveManager] Loading saved game — scene index {index}.");
        GameManager.Instance?.LoadScene(index);
    }

    /// <summary>
    /// Deletes all save data. Call when starting a New Game.
    /// </summary>
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(KEY_SCENE);
        PlayerPrefs.DeleteKey(KEY_EXISTS);
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] Save data deleted.");
    }

    // ---------------------------------------------------------------
    // Convenience
    // ---------------------------------------------------------------

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager — handles background music with smooth crossfade transitions.
///
/// Setup:
///   1. Create an empty GameObject named "AudioManager" in the scene.
///   2. Attach this script.
///   3. Drag your music clips into the Inspector slots.
///   4. AudioManager will auto-play on Start.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ---------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------

    [Header("Music Clips")]
    [Tooltip("Plays during normal exploration")]
    public AudioClip explorationMusic;

    [Tooltip("Plays when enemies are nearby or combat starts")]
    public AudioClip combatMusic;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float masterVolume    = 0.4f;

    [Tooltip("Seconds to crossfade between tracks")]
    public float crossfadeDuration = 2f;

    [Tooltip("Seconds of silence after detecting no enemies before switching back")]
    public float combatCooldown  = 5f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _activeSouce;

    private bool   _inCombat        = false;
    private float  _combatTimer     = 0f;
    private bool   _crossfading     = false;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Create two audio sources — only once
        if (_sourceA == null)
        {
            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceB = gameObject.AddComponent<AudioSource>();

            foreach (AudioSource src in new[] { _sourceA, _sourceB })
            {
                src.loop         = true;
                src.playOnAwake  = false;
                src.volume       = 0f;
                src.spatialBlend = 0f;
            }

            _activeSouce = _sourceA;
        }

        HandleSceneMusic(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        EnsureSingleAudioListener();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                               UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        _inCombat    = false;
        _combatTimer = 0f;
        StopAllCoroutines();
        _crossfading = false;
        HandleSceneMusic(scene.buildIndex);
        EnsureSingleAudioListener();
    }

    /// <summary>
    /// Guarantees exactly one AudioListener is active in the scene.
    /// Keeps the listener on the MainCamera-tagged camera (the player's FPS
    /// camera in a game scene, or the menu camera). Disables all others.
    /// </summary>
    private void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (listeners.Length <= 1) return;

        // Prefer the listener whose GameObject has the MainCamera tag
        AudioListener preferred = null;
        foreach (AudioListener al in listeners)
        {
            if (al.gameObject.CompareTag("MainCamera"))
            {
                preferred = al;
                break;
            }
        }

        // Fall back to the first enabled one
        if (preferred == null)
        {
            foreach (AudioListener al in listeners)
            {
                if (al.enabled) { preferred = al; break; }
            }
        }

        // Disable every listener that isn't the chosen one
        foreach (AudioListener al in listeners)
        {
            bool keep = al == preferred;
            if (al.enabled != keep)
            {
                al.enabled = keep;
                Debug.Log(keep
                    ? $"[AudioManager] Kept AudioListener on '{al.gameObject.name}'"
                    : $"[AudioManager] Disabled duplicate AudioListener on '{al.gameObject.name}'");
            }
        }
    }

    private void HandleSceneMusic(int sceneIndex)
    {
        StopAllMusicImmediate();

        // Main menu — stay silent
        if (sceneIndex == 0) return;

        // Game scene — start exploration music
        if (explorationMusic != null)
            PlayImmediate(explorationMusic);
    }

    private void StopAllMusicImmediate()
    {
        if (_sourceA != null) { _sourceA.Stop(); _sourceA.volume = 0f; _sourceA.clip = null; }
        if (_sourceB != null) { _sourceB.Stop(); _sourceB.volume = 0f; _sourceB.clip = null; }
        if (_sourceA != null) _activeSouce = _sourceA;
    }

    private void Update()
    {
        // No music logic on the main menu
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0) return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        // Auto-detect combat based on EnemyManager
        bool enemiesNearby = EnemyManager.Instance != null &&
                             EnemyManager.Instance.AliveCount > 0;

        if (enemiesNearby)
        {
            _combatTimer = combatCooldown;

            if (!_inCombat)
            {
                _inCombat = true;
                PlayCombatMusic();
            }
        }
        else if (_inCombat)
        {
            _combatTimer -= Time.deltaTime;

            if (_combatTimer <= 0f)
            {
                _inCombat = false;
                PlayExplorationMusic();
            }
        }
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    public void PlayExplorationMusic()
    {
        if (explorationMusic != null && !IsPlaying(explorationMusic))
            CrossfadeTo(explorationMusic);
    }

    public void PlayCombatMusic()
    {
        if (combatMusic != null && !IsPlaying(combatMusic))
            CrossfadeTo(combatMusic);
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (!_crossfading && _activeSouce != null)
            _activeSouce.volume = masterVolume;
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        StartCoroutine(FadeOut(_activeSouce, fadeDuration));
    }

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    private void PlayImmediate(AudioClip clip)
    {
        _activeSouce.clip   = clip;
        _activeSouce.volume = masterVolume;
        _activeSouce.Play();
    }

    private bool IsPlaying(AudioClip clip)
    {
        return (_activeSouce.clip == clip && _activeSouce.isPlaying);
    }

    private void CrossfadeTo(AudioClip newClip)
    {
        if (_crossfading) return;
        StartCoroutine(CrossfadeCoroutine(newClip));
    }

    private IEnumerator CrossfadeCoroutine(AudioClip newClip)
    {
        _crossfading = true;

        // Inactive source plays new clip at volume 0
        AudioSource incoming = (_activeSouce == _sourceA) ? _sourceB : _sourceA;
        AudioSource outgoing = _activeSouce;

        incoming.clip   = newClip;
        incoming.volume = 0f;
        incoming.Play();

        float timer = 0f;

        while (timer < crossfadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / crossfadeDuration;

            incoming.volume = Mathf.Lerp(0f, masterVolume, t);
            outgoing.volume = Mathf.Lerp(masterVolume, 0f, t);

            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
        incoming.volume = masterVolume;

        _activeSouce = incoming;
        _crossfading = false;

        Debug.Log($"[AudioManager] Now playing: {newClip.name}");
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
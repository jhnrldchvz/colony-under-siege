using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioManager — handles all SFX and music playback.
///
/// Responsibilities:
///   - Maintains an AudioSource pool for SFX (prevents audio cut-off on rapid fire)
///   - Plays 3D spatial audio for enemy sounds and world events
///   - Crossfades music between exploration and combat states
///   - Reacts to GameManager state changes (mute on pause, resume on play)
///
/// Setup:
///   1. Create an empty GameObject named "AudioManager". Attach this script.
///   2. Set poolSize (8 is enough for most FPS scenarios).
///   3. Assign explorationMusic and combatMusic AudioClips in the Inspector.
///   4. Adjust musicVolume and sfxVolume to taste.
///   5. Call AudioManager.Instance.PlaySFX(clip) from any script.
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

        BuildSFXPool();
    }

    // ---------------------------------------------------------------
    // Inspector — Pool
    // ---------------------------------------------------------------

    [Header("SFX Pool")]
    [Tooltip("Number of pooled AudioSources. 8 handles rapid gunfire without cutting out.")]
    public int poolSize = 8;

    // ---------------------------------------------------------------
    // Inspector — Volume
    // ---------------------------------------------------------------

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [Range(0f, 1f)] public float musicVolume  = 0.5f;

    // ---------------------------------------------------------------
    // Inspector — Music
    // ---------------------------------------------------------------

    [Header("Music")]
    [Tooltip("Plays during exploration / no active enemies nearby")]
    public AudioClip explorationMusic;

    [Tooltip("Plays when the player is in combat (enemy in Chase or Attack state)")]
    public AudioClip combatMusic;

    [Tooltip("Seconds for a music crossfade to complete")]
    public float crossfadeDuration = 1.5f;

    // ---------------------------------------------------------------
    // Private — SFX pool
    // ---------------------------------------------------------------

    private List<AudioSource> _sfxPool  = new List<AudioSource>();
    private int               _poolIndex = 0; // Round-robin index

    // ---------------------------------------------------------------
    // Private — Music
    // ---------------------------------------------------------------

    private AudioSource _musicSourceA; // Active music layer
    private AudioSource _musicSourceB; // Crossfade target layer
    private Coroutine   _crossfadeRoutine;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        BuildMusicSources();

        // Subscribe to GameManager state changes
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;

        // Start with exploration music
        PlayMusic(explorationMusic, fadein: false);

        Debug.Log("[AudioManager] Initialized.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;

        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------
    // Pool construction
    // ---------------------------------------------------------------

    private void BuildSFXPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D by default — overridden in PlaySpatial
            _sfxPool.Add(src);
        }
    }

    private void BuildMusicSources()
    {
        _musicSourceA = gameObject.AddComponent<AudioSource>();
        _musicSourceB = gameObject.AddComponent<AudioSource>();

        foreach (AudioSource src in new[] { _musicSourceA, _musicSourceB })
        {
            src.loop        = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // Music is always 2D
            src.volume      = 0f;
        }
    }

    // ---------------------------------------------------------------
    // SFX — 2D (UI clicks, pickups, player sounds)
    // ---------------------------------------------------------------

    /// <summary>
    /// Plays a 2D sound effect from the pool.
    /// Safe to call every frame — pool prevents audio source exhaustion.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource src = GetNextPooledSource();
        src.spatialBlend = 0f;
        src.volume       = sfxVolume;
        src.clip         = clip;
        src.Play();
    }

    /// <summary>
    /// Plays a 3D spatial sound at a world position.
    /// Use for enemy footsteps, gunshots, explosions — anything with a location.
    /// </summary>
    public void PlaySpatial(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        // AudioSource.PlayClipAtPoint is simpler but creates a new GameObject each call.
        // Using the pool + a quick position trick instead:
        AudioSource src = GetNextPooledSource();
        src.transform.position = position;
        src.spatialBlend       = 1f;   // Full 3D
        src.rolloffMode        = AudioRolloffMode.Linear;
        src.maxDistance        = 30f;
        src.volume             = sfxVolume * volume;
        src.clip               = clip;
        src.Play();
    }

    // ---------------------------------------------------------------
    // Music
    // ---------------------------------------------------------------

    /// <summary>
    /// Switches to a music clip with an optional crossfade.
    /// Called internally and by EnemyManager when combat starts/ends.
    /// </summary>
    public void PlayMusic(AudioClip clip, bool fadein = true)
    {
        if (clip == null) return;

        // Already playing this clip — skip
        if (_musicSourceA.clip == clip && _musicSourceA.isPlaying) return;

        if (_crossfadeRoutine != null)
            StopCoroutine(_crossfadeRoutine);

        if (fadein)
            _crossfadeRoutine = StartCoroutine(CrossfadeTo(clip));
        else
        {
            _musicSourceA.clip   = clip;
            _musicSourceA.volume = musicVolume;
            _musicSourceA.Play();
        }
    }

    /// <summary>Switch to combat music — call when an enemy enters Chase state.</summary>
    public void SwitchToCombatMusic()
    {
        PlayMusic(combatMusic);
    }

    /// <summary>Switch back to exploration music — call when all enemies return to Patrol.</summary>
    public void SwitchToExplorationMusic()
    {
        PlayMusic(explorationMusic);
    }

    // ---------------------------------------------------------------
    // Crossfade coroutine
    // ---------------------------------------------------------------

    private IEnumerator CrossfadeTo(AudioClip newClip)
    {
        // Set up the incoming layer (B)
        _musicSourceB.clip   = newClip;
        _musicSourceB.volume = 0f;
        _musicSourceB.Play();

        float elapsed  = 0f;
        float startVol = _musicSourceA.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Unscaled — works during pause
            float t  = elapsed / crossfadeDuration;

            _musicSourceA.volume = Mathf.Lerp(startVol,   0f,           t);
            _musicSourceB.volume = Mathf.Lerp(0f,         musicVolume,  t);

            yield return null;
        }

        // Swap A and B so A is always the active layer
        _musicSourceA.Stop();
        (_musicSourceA, _musicSourceB) = (_musicSourceB, _musicSourceA);

        _crossfadeRoutine = null;
    }

    // ---------------------------------------------------------------
    // State handler — pause/resume audio
    // ---------------------------------------------------------------

    private void HandleStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Paused:
                // Lower music during pause — don't mute entirely
                _musicSourceA.volume = musicVolume * 0.3f;
                break;

            case GameManager.GameState.Playing:
                _musicSourceA.volume = musicVolume;
                break;

            case GameManager.GameState.GameOver:
            case GameManager.GameState.Win:
                // Fade out music on end-states
                if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
                StartCoroutine(FadeOutMusic());
                break;
        }
    }

    private IEnumerator FadeOutMusic()
    {
        float startVol = _musicSourceA.volume;
        float elapsed  = 0f;
        float duration = 1.5f;

        while (elapsed < duration)
        {
            elapsed              += Time.unscaledDeltaTime;
            _musicSourceA.volume  = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        _musicSourceA.Stop();
    }

    // ---------------------------------------------------------------
    // Volume control — exposed for a settings menu later
    // ---------------------------------------------------------------

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume            = Mathf.Clamp01(vol);
        _musicSourceA.volume   = musicVolume;
    }

    // ---------------------------------------------------------------
    // Pool utility
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns the next available AudioSource from the pool (round-robin).
    /// If all sources are busy the oldest one is reused — acceptable for SFX.
    /// </summary>
    private AudioSource GetNextPooledSource()
    {
        AudioSource src = _sfxPool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _sfxPool.Count;
        return src;
    }
}
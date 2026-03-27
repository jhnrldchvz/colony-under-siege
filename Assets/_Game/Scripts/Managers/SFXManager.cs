using UnityEngine;

/// <summary>
/// SFXManager — plays all combat and gameplay sound effects.
///
/// Assign these clips in Inspector:
///   Rifle Shot      — sci-fi rifle gunshot clip
///   Pistol Shot     — pistol gunshot clip
///   Rifle Empty     — empty gun click
///   Pistol Empty    — empty gun click (can be same clip)
///   Reload Sound    — magazine reload clip
///   Enemy Death     — robot explode clip
///   Player Hurt     — player pain grunt clip
///   Pickup Item     — item collect sound
///   Throw Object    — whoosh sound
///
/// Setup:
///   1. Create empty GameObject "SFXManager" in each game scene.
///   2. Attach this script.
///   3. Assign clips in Inspector.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---------------------------------------------------------------
    // Inspector — assign clips
    // ---------------------------------------------------------------

    [Header("Rifle")]
    public AudioClip[] rifleShots;
    public AudioClip   rifleEmpty;

    [Header("Pistol")]
    public AudioClip[] pistolShots;
    public AudioClip   pistolEmpty;

    [Header("Shared Weapon")]
    public AudioClip   reloadSound;

    [Header("Enemy")]
    [Tooltip("Robot explode clip — plays on enemy death")]
    public AudioClip[] enemyDeath;
    [Tooltip("Optional enemy hurt sound — can leave empty")]
    public AudioClip[] enemyHurt;

    [Header("Player")]
    [Tooltip("Player pain grunt clip")]
    public AudioClip[] playerHurt;

    [Header("Interaction")]
    public AudioClip   pickupItem;
    public AudioClip   throwObject;

    [Header("Volume")]
    [Range(0f, 1f)] public float weaponVolume  = 0.8f;
    [Range(0f, 1f)] public float enemyVolume   = 0.7f;
    [Range(0f, 1f)] public float playerVolume  = 0.7f;
    [Range(0f, 1f)] public float pickupVolume  = 0.5f;

    [HideInInspector]
    public float masterVolume = 1f; // Controlled by UIManager settings

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    public void PlayRifleShot()   => PlayRandom(rifleShots,  weaponVolume);
    public void PlayPistolShot()  => PlayRandom(pistolShots, weaponVolume);
    public void PlayRifleEmpty()  => Play2D(rifleEmpty,  weaponVolume * 0.5f);
    public void PlayPistolEmpty() => Play2D(pistolEmpty, weaponVolume * 0.5f);
    public void PlayReload()      => Play2D(reloadSound, weaponVolume * 0.7f);
    public void PlayPlayerHurt()  => PlayRandom(playerHurt, playerVolume);
    public void PlayPickup()      => Play2D(pickupItem, pickupVolume);
    public void PlayThrow()       => Play2D(throwObject, pickupVolume);

    // Positional — 3D sound at enemy location
    public void PlayEnemyDeathAt(Vector3 pos)
        => Play3D(RandomClip(enemyDeath), pos, enemyVolume);

    public void PlayEnemyHurtAt(Vector3 pos)
        => Play3D(RandomClip(enemyHurt), pos, enemyVolume * 0.8f);

    // ---------------------------------------------------------------
    // Internal
    // ---------------------------------------------------------------

    private void PlayRandom(AudioClip[] clips, float vol) =>
        Play2D(RandomClip(clips), vol);

    private void Play2D(AudioClip clip, float vol)
    {
        if (clip == null) return;
        GameObject go = new GameObject("SFX_2D");
        go.transform.position = Camera.main != null
            ? Camera.main.transform.position : Vector3.zero;

        AudioSource src  = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.volume       = vol * masterVolume;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    private void Play3D(AudioClip clip, Vector3 pos, float vol)
    {
        if (clip == null) return;
        GameObject go = new GameObject("SFX_3D");
        go.transform.position = pos;

        AudioSource src  = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.volume       = vol * masterVolume;
        src.spatialBlend = 1f;
        src.minDistance  = 2f;
        src.maxDistance  = 25f;
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    private AudioClip RandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
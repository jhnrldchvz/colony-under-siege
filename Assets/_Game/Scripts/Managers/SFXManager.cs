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
[DefaultExecutionOrder(-50)]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    // Persistent 2D source — reused for every non-spatial sound via PlayOneShot
    // (PlayOneShot overlaps clips on the same source, no GameObject spawning needed)
    private AudioSource _2dSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _2dSource              = gameObject.AddComponent<AudioSource>();
        _2dSource.spatialBlend = 0f;
        _2dSource.playOnAwake  = false;
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

    [Header("Explosion")]
    [Tooltip("3D positional clip played at the barrel's world position")]
    public AudioClip[] explosionSounds;

    [Header("Boss")]
    [Tooltip("Heavy slam clip played when boss lands from a jump attack")]
    public AudioClip[] bossSlamSounds;

    [Header("Volume")]
    [Range(0f, 1f)] public float weaponVolume    = 0.8f;
    [Range(0f, 1f)] public float enemyVolume     = 0.7f;
    [Range(0f, 1f)] public float playerVolume    = 0.7f;
    [Range(0f, 1f)] public float pickupVolume    = 0.5f;
    [Range(0f, 1f)] public float explosionVolume = 1.0f;

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

    // Positional — 3D explosion sound at world position
    public void PlayExplosionAt(Vector3 pos)
        => Play3D(RandomClip(explosionSounds), pos, explosionVolume);

    public void PlayBossSlamAt(Vector3 pos)
        => Play3D(RandomClip(bossSlamSounds), pos, explosionVolume);

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

    // Plays non-spatial audio via PlayOneShot — no GameObject allocation
    private void Play2D(AudioClip clip, float vol)
    {
        if (clip == null || _2dSource == null) return;
        _2dSource.PlayOneShot(clip, vol * masterVolume);
    }

    // Plays positional 3D audio — Unity handles the temporary source internally
    private void Play3D(AudioClip clip, Vector3 pos, float vol)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, pos, vol * masterVolume);
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
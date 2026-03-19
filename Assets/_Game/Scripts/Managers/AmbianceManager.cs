using System.Collections;
using UnityEngine;

/// <summary>
/// AmbianceManager — controls scene ambiance:
///   - Ambient wind/environment sound loop
///   - Subtle sky color shifts over time (day cycle feel)
///   - Dynamic fog density based on distance
///
/// Setup:
///   1. Create empty GameObject named "AmbianceManager" in scene.
///   2. Attach this script.
///   3. Drag audio clips and assign colors in Inspector.
/// </summary>
public class AmbianceManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static AmbianceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---------------------------------------------------------------
    // Inspector — Ambient Sound
    // ---------------------------------------------------------------

    [Header("Ambient Sound")]
    [Tooltip("Looping wind or environment sound")]
    public AudioClip ambienceClip;

    [Range(0f, 1f)]
    public float ambienceVolume = 0.2f;

    // ---------------------------------------------------------------
    // Inspector — Fog
    // ---------------------------------------------------------------

    [Header("Fog")]
    public bool  enableFog      = true;
    public Color fogColor       = new Color(0.5f, 0.55f, 0.6f, 1f);
    public float fogStartDist   = 30f;
    public float fogEndDist     = 150f;

    // ---------------------------------------------------------------
    // Inspector — Ambient Light
    // ---------------------------------------------------------------

    [Header("Ambient Light")]
    public Color ambientSkyColor     = new Color(0.3f, 0.35f, 0.45f);
    public Color ambientGroundColor  = new Color(0.15f, 0.12f, 0.1f);

    // ---------------------------------------------------------------
    // Inspector — Sky Pulse (subtle color breathing)
    // ---------------------------------------------------------------

    [Header("Sky Pulse")]
    [Tooltip("Subtle ambient light pulse — simulates cloud shadows")]
    public bool  enableSkyPulse  = true;
    public float pulseSpeed      = 0.08f;
    public float pulseIntensity  = 0.04f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private AudioSource _ambienceSource;
    private float       _pulseTimer;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        SetupAmbience();
        SetupFog();
        SetupAmbientLight();
    }

    private void Update()
    {
        if (!enableSkyPulse) return;

        // Subtle ambient light pulse — simulates moving clouds
        _pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = Mathf.Sin(_pulseTimer) * pulseIntensity;

        RenderSettings.ambientSkyColor = new Color(
            Mathf.Clamp01(ambientSkyColor.r + pulse),
            Mathf.Clamp01(ambientSkyColor.g + pulse),
            Mathf.Clamp01(ambientSkyColor.b + pulse));
    }

    // ---------------------------------------------------------------
    // Setup helpers
    // ---------------------------------------------------------------

    private void SetupAmbience()
    {
        if (ambienceClip == null) return;

        _ambienceSource              = gameObject.AddComponent<AudioSource>();
        _ambienceSource.clip         = ambienceClip;
        _ambienceSource.loop         = true;
        _ambienceSource.volume       = ambienceVolume;
        _ambienceSource.spatialBlend = 0f; // 2D — everywhere in scene
        _ambienceSource.playOnAwake  = false;

        // Fade in ambience sound
        StartCoroutine(FadeInAmbience());
    }

    private void SetupFog()
    {
        RenderSettings.fog          = enableFog;
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogColor     = fogColor;
        RenderSettings.fogStartDistance = fogStartDist;
        RenderSettings.fogEndDistance   = fogEndDist;
    }

    private void SetupAmbientLight()
    {
        RenderSettings.ambientMode       = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor   = ambientSkyColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientEquatorColor = Color.Lerp(ambientSkyColor, ambientGroundColor, 0.5f);
    }

    private IEnumerator FadeInAmbience()
    {
        // Wait for scene to fully load
        yield return new WaitForSeconds(0.5f);

        _ambienceSource.volume = 0f;
        _ambienceSource.Play();

        float timer = 0f;
        float fadeDuration = 3f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            _ambienceSource.volume = Mathf.Lerp(0f, ambienceVolume, timer / fadeDuration);
            yield return null;
        }

        _ambienceSource.volume = ambienceVolume;
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    public void SetFogDensity(float start, float end)
    {
        RenderSettings.fogStartDistance = start;
        RenderSettings.fogEndDistance   = end;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
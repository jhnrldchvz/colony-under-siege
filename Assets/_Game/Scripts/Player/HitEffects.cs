using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HitEffects — visual feedback when the player takes damage.
///
/// Features:
///   1. Red vignette flash on the HUD — fades out smoothly
///   2. Camera shake on hit — random rotational wobble
///   3. Continuous camera sway while moving — subtle head bob
///
/// Setup:
///   1. Attach this script to the Player root GameObject.
///   2. Create a full-screen red Image on the HUD Canvas:
///      - Right-click HUD Panel → UI → Image → rename HitVignette
///      - Anchor: stretch all (covers full screen)
///      - Color: (1, 0, 0, 0) — red fully transparent
///      - Drag into hitVignette slot
///   3. CameraHolder is found automatically from PlayerController.
/// </summary>
public class HitEffects : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector — Hit Flash
    // ---------------------------------------------------------------

    [Header("Hit Vignette")]
    [Tooltip("Full-screen red Image on the HUD — set color to (1,0,0,0) initially")]
    public Image  hitVignette;

    [Tooltip("How red the flash gets at full intensity (0-1 alpha)")]
    public float  maxVignetteAlpha  = 0.75f;

    [Tooltip("How fast the red fades out after a hit")]
    public float  vignetteFadeSpeed = 1.8f;

    // ---------------------------------------------------------------
    // Inspector — Helmet Damage Overlays
    // ---------------------------------------------------------------

    [Header("Helmet Damage")]
    [Tooltip("Crack overlay shown below 50% HP — light damage")]
    public Image helmetCrack1;

    [Tooltip("Crack overlay shown below 25% HP — heavy damage")]
    public Image helmetCrack2;

    [Tooltip("How fast cracks fade in when threshold is crossed")]
    public float crackFadeSpeed   = 2f;

    [Tooltip("Max alpha for crack overlays at lowest HP")]
    public float crackMaxAlpha    = 0.85f;

    [Tooltip("Optional breathing/pulse effect on cracks when critically low")]
    public bool  crackPulse       = true;

    // ---------------------------------------------------------------
    // Inspector — Camera Shake on Hit
    // ---------------------------------------------------------------

    [Header("Hit Camera Shake")]
    [Tooltip("How violent the camera shake is on a hit")]
    public float  hitShakeMagnitude = 8f;

    [Tooltip("How long the shake lasts in seconds")]
    public float  hitShakeDuration  = 0.35f;

    // ---------------------------------------------------------------
    // Inspector — Running Head Bob
    // ---------------------------------------------------------------

    [Header("Running Head Bob")]
    [Tooltip("Enable continuous camera sway while moving")]
    public bool   enableHeadBob     = true;

    [Tooltip("How fast the head bobs — higher = quicker steps")]
    public float  bobFrequency      = 8f;

    [Tooltip("How much the camera moves up/down while walking")]
    public float  bobAmplitudeY     = 0.09f;

    [Tooltip("How much the camera tilts side to side while walking")]
    public float  bobAmplitudeX     = 0.06f;

    [Tooltip("Minimum movement speed before bob starts")]
    public float  bobSpeedThreshold = 0.1f;

    [Tooltip("Extra bob multiplier when sprinting")]
    public float  sprintBobMultiplier = 1.6f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private PlayerController _player;
    private Transform        _cameraHolder;
    private CharacterController _cc;

    // Vignette
    private float _vignetteAlpha   = 0f;

    // Helmet damage
    private float _crack1Alpha     = 0f;
    private float _crack2Alpha     = 0f;

    // Shake
    private float   _shakeTimer     = 0f;
    private float   _shakeMagnitude = 0f;
    private Vector3 _shakeOffset    = Vector3.zero;

    // Head bob
    private float   _bobTimer       = 0f;
    private Vector3 _bobOffset      = Vector3.zero;
    private Vector3 _cameraBasePos;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _cc     = GetComponent<CharacterController>();

        if (_player != null)
            _cameraHolder = _player.cameraHolder;

        if (_cameraHolder != null)
            _cameraBasePos = _cameraHolder.localPosition;
    }

    private void Start()
    {
        // Start vignette fully transparent
        if (hitVignette != null)
        {
            Color c = hitVignette.color;
            c.a = 0f;
            hitVignette.color = c;
        }

        // Start helmet overlays fully transparent
        SetImageAlpha(helmetCrack1, 0f);
        SetImageAlpha(helmetCrack2, 0f);
    }

    private void Update()
    {
        UpdateVignette();
        UpdateCameraShake();
        UpdateHelmetDamage();

        if (enableHeadBob)
            UpdateHeadBob();
    }

    // ---------------------------------------------------------------
    // Public API — called by PlayerController.TakeDamage()
    // ---------------------------------------------------------------

    /// <summary>
    /// Call this from PlayerController whenever health changes.
    /// Updates crack overlay visibility based on current HP percentage.
    /// </summary>
    public void UpdateHelmetFromHealth(float healthPercent)
    {
        // Below 50% — show crack 1
        float target1 = healthPercent < 0.5f
            ? Mathf.Lerp(crackMaxAlpha * 0.5f, crackMaxAlpha,
                         1f - (healthPercent / 0.5f))
            : 0f;

        // Below 25% — show crack 2
        float target2 = healthPercent < 0.25f
            ? Mathf.Lerp(crackMaxAlpha * 0.4f, crackMaxAlpha,
                         1f - (healthPercent / 0.25f))
            : 0f;

        _crack1Alpha = Mathf.Lerp(_crack1Alpha, target1,
                                  Time.deltaTime * crackFadeSpeed);
        _crack2Alpha = Mathf.Lerp(_crack2Alpha, target2,
                                  Time.deltaTime * crackFadeSpeed);
    }

    /// <summary>
    /// Trigger hit effects — call this from PlayerController.TakeDamage().
    /// Intensity 0-1 scales both vignette and shake strength.
    /// </summary>
    public void TriggerHit(float intensity = 1f)
    {
        // Always flash at least half the max alpha so small hits are still visible
        float minAlpha  = maxVignetteAlpha * 0.5f;
        _vignetteAlpha  = Mathf.Max(minAlpha, maxVignetteAlpha * intensity);

        // Camera shake — minimum shake so even weak hits feel impactful
        float minShake  = hitShakeMagnitude * 0.4f;
        _shakeTimer     = hitShakeDuration;
        _shakeMagnitude = Mathf.Max(minShake, hitShakeMagnitude * intensity);

        Debug.Log($"[HitEffects] Hit triggered — intensity:{intensity:F2} " +
                  $"alpha:{_vignetteAlpha:F2} shake:{_shakeMagnitude:F2}");
    }

    // ---------------------------------------------------------------
    // Helmet damage overlays
    // ---------------------------------------------------------------

    private void UpdateHelmetDamage()
    {
        if (_player == null) return;

        float hp = _player.CurrentHealth;
        float maxHp = _player.maxHealth;
        float pct = Mathf.Clamp01(hp / maxHp);

        // Pulse effect when critically low (below 25%)
        float pulse = 1f;
        if (crackPulse && pct < 0.25f)
            pulse = 0.85f + Mathf.Sin(Time.time * 3f) * 0.15f;

        // Apply alpha to crack overlays
        float a1 = _crack1Alpha * pulse;
        float a2 = _crack2Alpha * pulse;

        SetImageAlpha(helmetCrack1, a1);
        SetImageAlpha(helmetCrack2, a2);

        // Update from health each frame
        UpdateHelmetFromHealth(pct);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    // ---------------------------------------------------------------
    // Vignette
    // ---------------------------------------------------------------

    private void UpdateVignette()
    {
        if (hitVignette == null) return;

        // Fade out over time
        _vignetteAlpha = Mathf.MoveTowards(
            _vignetteAlpha, 0f, Time.deltaTime * vignetteFadeSpeed);

        Color c = hitVignette.color;
        c.a = _vignetteAlpha;
        hitVignette.color = c;
    }

    // ---------------------------------------------------------------
    // Camera shake
    // ---------------------------------------------------------------

    private void UpdateCameraShake()
    {
        if (_cameraHolder == null || _shakeTimer <= 0f) return;

        _shakeTimer -= Time.deltaTime;

        if (_shakeTimer > 0f)
        {
            // Position offset shake — doesn't fight with HandleLook rotation
            float x = Random.Range(-1f, 1f) * _shakeMagnitude * 0.02f;
            float y = Random.Range(-1f, 1f) * _shakeMagnitude * 0.02f;
            _shakeOffset = new Vector3(x, y, 0f);
        }
        else
        {
            // Ease shake offset back to zero smoothly
            _shakeOffset = Vector3.Lerp(_shakeOffset, Vector3.zero, Time.deltaTime * 20f);
        }
    }

    // ---------------------------------------------------------------
    // Head bob
    // ---------------------------------------------------------------

    private void UpdateHeadBob()
    {
        if (_cameraHolder == null || _cc == null) return;

        float speed       = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude;
        bool  isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (speed > bobSpeedThreshold && _cc.isGrounded)
        {
            float speedFactor = Mathf.Clamp01(speed / 8f);
            float mult        = isSprinting ? sprintBobMultiplier : 1f;

            _bobTimer += Time.deltaTime * bobFrequency * speedFactor * mult;

            float bobY = Mathf.Sin(_bobTimer)         * bobAmplitudeY * mult;
            float bobX = Mathf.Sin(_bobTimer * 0.5f)  * bobAmplitudeX * mult;
            float bobZ = isSprinting ? Mathf.Sin(_bobTimer * 0.5f) * 0.012f : 0f;

            _bobOffset = Vector3.Lerp(
                _bobOffset, new Vector3(bobX, bobY, bobZ), Time.deltaTime * 12f);
        }
        else
        {
            _bobOffset = Vector3.Lerp(_bobOffset, Vector3.zero, Time.deltaTime * 8f);
        }

        _cameraHolder.localPosition = _cameraBasePos + _bobOffset + _shakeOffset;
    }
}
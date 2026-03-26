using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// EnemyHealthBar — world-space health bar displayed above an enemy.
///
/// Features:
///   - Always faces the camera (billboard)
///   - Only visible when enemy is damaged or player is within showRange
///   - Smooth fill animation
///   - Optional HP text display
///   - Auto-hides at full health after a delay
///
/// Setup:
///   1. On the enemy prefab, right-click → UI → Canvas
///   2. Canvas: Render Mode = World Space
///      Scale: (0.01, 0.01, 0.01)
///      Position: (0, 2.4, 0) — above the capsule head
///      Width: 100, Height: 14
///   3. Inside Canvas add:
///      - Background Image (dark, full width)
///      - FillImage (colored, Image Type: Filled, Fill Method: Horizontal)
///      - Optional: TextMeshPro for HP numbers
///   4. Attach this script to the Canvas GameObject
///   5. Wire Background, FillImage, and optional HPText slots
///   6. Attach this to EnemyAI and call UpdateHealthBar() when damaged
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The filled image used as the health bar")]
    public Image        fillImage;

    [Header("Colors")]
    public Color fullColor    = new Color(0.2f, 0.85f, 0.2f, 1f);  // Green
    public Color midColor     = new Color(0.95f, 0.75f, 0.1f, 1f); // Yellow
    public Color lowColor     = new Color(0.9f, 0.2f, 0.2f, 1f);   // Red

    [Header("Visibility")]
    [Tooltip("Show bar when player is within this range")]
    public float showRange      = 15f;

    [Tooltip("Seconds to keep bar visible after taking damage before hiding")]
    public float hideDelay      = 3f;

    [Tooltip("Always visible regardless of range or damage")]
    public bool  alwaysVisible  = false;

    [Header("Animation")]
    [Tooltip("How fast the bar fill animates")]
    public float fillSpeed      = 8f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private Camera      _cam;
    private Transform   _player;
    private CanvasGroup _cg;
    private Canvas      _canvas;

    private float _targetFill   = 1f;
    private float _currentFill  = 1f;
    private float _hideTimer    = 0f;
    private bool  _isDamaged    = false;
    private int   _maxHealth    = 100;
    private int   _currentHP    = 100;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _cg     = GetComponent<CanvasGroup>();

        if (_cg == null)
            _cg = gameObject.AddComponent<CanvasGroup>();

        _cg.alpha = 0f;
    }

    private void Start()
    {
        _cam = Camera.main;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        // Start hidden at full health
        SetVisible(false, instant: true);
    }

    private void Update()
    {
        if (_cam == null) return;

        // Billboard — Y axis only, X position stays fixed
        Vector3 forward = _cam.transform.forward;
        forward.y = 0f;
        if (forward != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(forward);

        // Smooth fill animation
        _currentFill = Mathf.Lerp(_currentFill, _targetFill,
                                  Time.deltaTime * fillSpeed);

        if (fillImage != null)
            fillImage.fillAmount = _currentFill;

        // Update bar color based on health
        UpdateBarColor();

        // Determine visibility
        if (alwaysVisible)
        {
            SetVisible(true);
            return;
        }

        // Show when damaged — hide after delay
        if (_isDamaged)
        {
            SetVisible(true);
            _hideTimer -= Time.deltaTime;

            if (_hideTimer <= 0f && Mathf.Approximately(_currentFill, _targetFill))
            {
                _isDamaged = false;
                // Check if player is still nearby before hiding
                if (!IsPlayerNearby())
                    SetVisible(false);
            }
            return;
        }

        // Show when player is nearby
        SetVisible(IsPlayerNearby());
    }

    // ---------------------------------------------------------------
    // Public API — called by EnemyAI
    // ---------------------------------------------------------------

    /// <summary>
    /// Initialize the health bar with max health.
    /// Call from EnemyAI.Start() or Awake().
    /// </summary>
    public void Initialize(int maxHealth)
    {
        _maxHealth   = maxHealth;
        _currentHP   = maxHealth;
        _targetFill  = 1f;
        _currentFill = 1f;

        if (fillImage != null)
            fillImage.fillAmount = 1f;

        SetVisible(false, instant: true);
    }

    /// <summary>
    /// Forces the health bar visible temporarily even at long range.
    /// Called when enemy takes damage so player gets hit confirmation.
    /// </summary>
    public void ForceShow()
    {
        _isDamaged = true;
        _hideTimer = hideDelay;
        SetVisible(true);
    }

    /// <summary>
    /// Update the health bar when the enemy takes damage.
    /// Call from EnemyAI.TakeDamage().
    /// </summary>
    public void UpdateHealth(int currentHP, int maxHP)
{
    _maxHealth  = maxHP;
    _currentHP  = currentHP;
    _targetFill = Mathf.Clamp01((float)currentHP / maxHP);

    _isDamaged  = true;
    _hideTimer  = hideDelay;

    SetVisible(true);
}

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    private void UpdateBarColor()
    {
        if (fillImage == null) return;

        float pct = _targetFill;

        if      (pct > 0.6f) fillImage.color = Color.Lerp(midColor,  fullColor, (pct - 0.6f) / 0.4f);
        else if (pct > 0.3f) fillImage.color = Color.Lerp(lowColor,  midColor,  (pct - 0.3f) / 0.3f);
        else                 fillImage.color = lowColor;
    }

    private bool IsPlayerNearby()
    {
        if (_player == null) return false;
        return Vector3.Distance(transform.position, _player.position) <= showRange;
    }

    private void SetVisible(bool visible, bool instant = false)
    {
        if (_cg == null) return;

        if (visible)
        {
            // Always show instantly — never delay appearance
            _cg.alpha = 1f;
        }
        else
        {
            // Hide — instant or smooth
            float target = 0f;
            if (instant)
                _cg.alpha = target;
            else
                _cg.alpha = Mathf.Lerp(_cg.alpha, target, Time.deltaTime * 8f);
        }
    }
}
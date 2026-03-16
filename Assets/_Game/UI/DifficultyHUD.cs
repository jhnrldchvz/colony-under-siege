using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DifficultyHUD — updates the difficulty metrics panel on the HUD.
///
/// Reads from DifficultyManager every frame and pushes values
/// to the UI elements. Attach to the DifficultyPanel GameObject
/// inside HUD Panel.
///
/// Setup:
///   1. Create a child panel inside HUD Panel named "DifficultyPanel".
///   2. Build the UI elements listed below inside it.
///   3. Attach this script to DifficultyPanel.
///   4. Wire all Inspector slots.
/// </summary>
public class DifficultyHUD : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector — Tier badge
    // ---------------------------------------------------------------

    [Header("Tier")]
    [Tooltip("TextMeshPro showing EASY / NORMAL / HARD")]
    public TextMeshProUGUI tierLabel;

    [Tooltip("Background Image of the tier badge — color changes per tier")]
    public Image tierBadgeBackground;

    [Header("Tier Colors")]
    public Color easyColor   = new Color(0.11f, 0.62f, 0.46f, 1f); // teal
    public Color normalColor = new Color(0.73f, 0.46f, 0.09f, 1f); // amber
    public Color hardColor   = new Color(0.64f, 0.18f, 0.18f, 1f); // red

    // ---------------------------------------------------------------
    // Inspector — Accuracy bars
    // ---------------------------------------------------------------

    [Header("Window Accuracy")]
    [Tooltip("Fill image for the window accuracy bar")]
    public Image windowAccuracyBar;

    [Tooltip("Text showing window accuracy percentage")]
    public TextMeshProUGUI windowAccuracyText;

    [Header("Lifetime Accuracy")]
    [Tooltip("Fill image for the lifetime accuracy bar")]
    public Image lifetimeAccuracyBar;

    [Tooltip("Text showing lifetime accuracy percentage")]
    public TextMeshProUGUI lifetimeAccuracyText;

    // ---------------------------------------------------------------
    // Inspector — Shot counters
    // ---------------------------------------------------------------

    [Header("Shot Counters")]
    public TextMeshProUGUI shotsFiredText;
    public TextMeshProUGUI shotsHitText;

    // ---------------------------------------------------------------
    // Inspector — Update rate
    // ---------------------------------------------------------------

    [Header("Settings")]
    [Tooltip("How often the HUD refreshes in seconds — 0.2 is smooth enough")]
    public float refreshRate = 0.2f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private float _refreshTimer = 0f;
    private DifficultyManager.DifficultyTier _lastTier;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Subscribe to tier change for instant badge update
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.OnTierChanged += OnTierChanged;

        // Initial refresh
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.OnTierChanged -= OnTierChanged;
    }

    private void Update()
    {
        // Throttle updates — no need to refresh every frame
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer < refreshRate) return;
        _refreshTimer = 0f;

        RefreshAccuracy();
        RefreshCounters();
    }

    // ---------------------------------------------------------------
    // Refresh methods
    // ---------------------------------------------------------------

    private void RefreshAll()
    {
        if (DifficultyManager.Instance == null) return;

        OnTierChanged(DifficultyManager.Instance.CurrentTier);
        RefreshAccuracy();
        RefreshCounters();
    }

    private void OnTierChanged(DifficultyManager.DifficultyTier tier)
    {
        _lastTier = tier;

        if (tierLabel != null)
            tierLabel.text = tier.ToString().ToUpper();

        if (tierBadgeBackground != null)
        {
            switch (tier)
            {
                case DifficultyManager.DifficultyTier.Easy:
                    tierBadgeBackground.color = easyColor;
                    break;
                case DifficultyManager.DifficultyTier.Hard:
                    tierBadgeBackground.color = hardColor;
                    break;
                default:
                    tierBadgeBackground.color = normalColor;
                    break;
            }
        }
    }

    private void RefreshAccuracy()
    {
        if (DifficultyManager.Instance == null) return;

        float windowAcc   = DifficultyManager.Instance.WindowAccuracy;
        float lifetimeAcc = DifficultyManager.Instance.LifetimeAccuracy;

        // Window accuracy bar
        if (windowAccuracyBar != null)
            windowAccuracyBar.fillAmount = windowAcc;

        if (windowAccuracyText != null)
            windowAccuracyText.text = $"{windowAcc * 100f:F0}%";

        // Lifetime accuracy bar
        if (lifetimeAccuracyBar != null)
            lifetimeAccuracyBar.fillAmount = lifetimeAcc;

        if (lifetimeAccuracyText != null)
            lifetimeAccuracyText.text = $"{lifetimeAcc * 100f:F0}%";

        // Tint accuracy bars based on performance
        TintBar(windowAccuracyBar,   windowAcc);
        TintBar(lifetimeAccuracyBar, lifetimeAcc);
    }

    private void RefreshCounters()
    {
        if (DifficultyManager.Instance == null) return;

        if (shotsFiredText != null)
            shotsFiredText.text = DifficultyManager.Instance.TotalShotsFired.ToString();

        if (shotsHitText != null)
            shotsHitText.text = DifficultyManager.Instance.TotalShotsHit.ToString();
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    /// <summary>Tints an accuracy bar green → amber → red based on value.</summary>
    private void TintBar(Image bar, float accuracy)
    {
        if (bar == null) return;

        if (accuracy >= 0.65f)
            bar.color = hardColor;   // High accuracy = red (hard coming)
        else if (accuracy >= 0.30f)
            bar.color = normalColor; // Mid accuracy = amber
        else
            bar.color = easyColor;   // Low accuracy = green (easy incoming)
    }
}
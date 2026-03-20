using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// TestMetricsHUD — live on-screen overlay showing all test metrics.
///
/// Setup:
///   1. Create a Canvas (Screen Space Overlay) in the test scene.
///   2. Add a TMP Text covering the top-left corner.
///   3. Attach this script to the Canvas.
///   4. Drag the TMP Text into the metricsText slot.
/// </summary>
public class TestMetricsHUD : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI metricsText;

    [Header("Settings")]
    [Tooltip("Refresh rate in seconds")]
    public float refreshInterval = 0.25f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < refreshInterval) return;
        _timer = 0f;
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (metricsText == null || TestMetricsCollector.Instance == null) return;

        var m  = TestMetricsCollector.Instance;
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("<b>── TEST METRICS ──</b>");
        sb.AppendLine($"Time:      {m.SessionDuration:F1}s");
        sb.AppendLine();

        // Accuracy + kills
        sb.AppendLine("<b>Combat</b>");
        sb.AppendLine($"Shots:     {m.TotalShots}");
        sb.AppendLine($"Hits:      {m.TotalHits}");
        sb.AppendLine($"Accuracy:  {m.Accuracy * 100f:F1}%");
        sb.AppendLine($"Kills:     {m.KillCount}");
        sb.AppendLine();

        // DDA
        string tierColor = m.CurrentTier switch
        {
            "Easy"   => "#44BB44",
            "Hard"   => "#FF4444",
            _        => "#DDBB44"
        };
        sb.AppendLine("<b>DDA</b>");
        sb.AppendLine($"Tier:      <color={tierColor}>{m.CurrentTier}</color>");
        sb.AppendLine($"Changes:   {m.DDAHistory.Count}");
        if (m.DDAHistory.Count > 0)
        {
            var last = m.DDAHistory[m.DDAHistory.Count - 1];
            sb.AppendLine($"Last:      {last.fromTier}→{last.toTier} @ {last.timestamp:F1}s");
        }
        sb.AppendLine();

        // FSM — show last 5 transitions
        sb.AppendLine("<b>FSM (last 5)</b>");
        int start = Mathf.Max(0, m.FSMHistory.Count - 5);
        for (int i = start; i < m.FSMHistory.Count; i++)
        {
            var e = m.FSMHistory[i];
            sb.AppendLine($"{e.timestamp:F1}s {e.enemyName}: {e.fromState}→{e.toState}");
        }
        sb.AppendLine();

        // Detection response time
        sb.AppendLine("<b>Detection</b>");
        sb.AppendLine($"Avg resp:  {m.AvgResponseTime:F2}s");
        sb.AppendLine($"Samples:   {m.DetectionHistory.Count}");

        // Enemy states
        sb.AppendLine();
        sb.AppendLine("<b>Enemy states</b>");
        foreach (var kv in m.EnemyCurrentState)
            sb.AppendLine($"{kv.Key}: {kv.Value}");

        metricsText.text = sb.ToString();
    }
}
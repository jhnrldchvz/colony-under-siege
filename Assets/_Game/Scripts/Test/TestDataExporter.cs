using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// TestDataExporter — exports all collected metrics to CSV files.
/// Press F5 at any time to export, or it auto-exports on session end.
///
/// Output files saved to: [ProjectRoot]/TestData/
///   - session_summary_[timestamp].csv
///   - fsm_transitions_[timestamp].csv
///   - dda_history_[timestamp].csv
///   - detection_events_[timestamp].csv
/// </summary>
public class TestDataExporter : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode exportKey = KeyCode.F5;

    [Tooltip("Auto-export when scene is unloaded or app quits")]
    public bool autoExportOnEnd = true;

    private string _timestamp;
    private string _exportDir;

    private void Start()
    {
        _timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _exportDir = Path.Combine(Application.dataPath, "..", "TestData");
        Directory.CreateDirectory(_exportDir);
        Debug.Log($"[TestExporter] Data will save to: {_exportDir}");
    }

    private void Update()
    {
        if (Input.GetKeyDown(exportKey))
            Export();
    }

    private void OnApplicationQuit()
    {
        if (autoExportOnEnd) Export();
    }

    private void OnDestroy()
    {
        if (autoExportOnEnd) Export();
    }

    // ---------------------------------------------------------------
    // Export all files
    // ---------------------------------------------------------------
    public void Export()
    {
        var m = TestMetricsCollector.Instance;
        if (m == null) { Debug.LogWarning("[TestExporter] No collector found."); return; }

        ExportSummary(m);
        ExportFSM(m);
        ExportDDA(m);
        ExportDetection(m);

        Debug.Log($"[TestExporter] Exported to: {_exportDir}");
    }

    // ---------------------------------------------------------------
    // Session summary
    // ---------------------------------------------------------------
    private void ExportSummary(TestMetricsCollector m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("metric,value");
        sb.AppendLine($"session_duration_s,{m.SessionDuration:F2}");
        sb.AppendLine($"total_shots,{m.TotalShots}");
        sb.AppendLine($"total_hits,{m.TotalHits}");
        sb.AppendLine($"accuracy_percent,{m.Accuracy * 100f:F2}");
        sb.AppendLine($"kill_count,{m.KillCount}");
        sb.AppendLine($"dda_tier_changes,{m.DDAHistory.Count}");
        sb.AppendLine($"avg_detection_response_s,{m.AvgResponseTime:F3}");
        sb.AppendLine($"fsm_total_transitions,{m.FSMHistory.Count}");
        sb.AppendLine($"final_dda_tier,{m.CurrentTier}");

        WriteFile("session_summary", sb.ToString());
    }

    // ---------------------------------------------------------------
    // FSM transitions
    // ---------------------------------------------------------------
    private void ExportFSM(TestMetricsCollector m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("timestamp_s,enemy_name,from_state,to_state");

        foreach (var e in m.FSMHistory)
            sb.AppendLine($"{e.timestamp:F3},{e.enemyName},{e.fromState},{e.toState}");

        WriteFile("fsm_transitions", sb.ToString());
    }

    // ---------------------------------------------------------------
    // DDA history
    // ---------------------------------------------------------------
    private void ExportDDA(TestMetricsCollector m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("timestamp_s,from_tier,to_tier,accuracy_at_change_percent");

        foreach (var e in m.DDAHistory)
            sb.AppendLine($"{e.timestamp:F3},{e.fromTier},{e.toTier},{e.accuracyAtChange * 100f:F2}");

        WriteFile("dda_history", sb.ToString());
    }

    // ---------------------------------------------------------------
    // Detection events
    // ---------------------------------------------------------------
    private void ExportDetection(TestMetricsCollector m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("timestamp_s,enemy_name,response_time_s,dda_tier");

        foreach (var e in m.DetectionHistory)
            sb.AppendLine($"{e.timestamp:F3},{e.enemyName},{e.responseTimeSeconds:F3},{e.tier}");

        WriteFile("detection_events", sb.ToString());
    }

    // ---------------------------------------------------------------
    // Write helper
    // ---------------------------------------------------------------
    private void WriteFile(string name, string content)
    {
        string path = Path.Combine(_exportDir, $"{name}_{_timestamp}.csv");
        File.WriteAllText(path, content);
        Debug.Log($"[TestExporter] Saved: {path}");
    }
}
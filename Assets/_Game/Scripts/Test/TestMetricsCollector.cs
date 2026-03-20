using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TestMetricsCollector — central data store for the test scene.
/// All other test scripts write here. HUD and CSV exporter read from here.
/// Attach to an empty GameObject named "TestMetricsCollector".
/// </summary>
public class TestMetricsCollector : MonoBehaviour
{
    public static TestMetricsCollector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ---------------------------------------------------------------
    // Session meta
    // ---------------------------------------------------------------
    public float   SessionStartTime  { get; private set; }
    public float   SessionDuration   => Time.time - SessionStartTime;

    // ---------------------------------------------------------------
    // Accuracy
    // ---------------------------------------------------------------
    public int TotalShots  { get; private set; } = 0;
    public int TotalHits   { get; private set; } = 0;
    public float Accuracy  => TotalShots > 0 ? (float)TotalHits / TotalShots : 0f;

    // ---------------------------------------------------------------
    // Kill tracking
    // ---------------------------------------------------------------
    public int KillCount { get; private set; } = 0;

    // ---------------------------------------------------------------
    // DDA tier history
    // ---------------------------------------------------------------
    [Serializable]
    public struct DDAEvent
    {
        public float timestamp;
        public string fromTier;
        public string toTier;
        public float accuracyAtChange;
    }
    public List<DDAEvent> DDAHistory { get; } = new List<DDAEvent>();
    public string CurrentTier { get; private set; } = "Normal";

    // ---------------------------------------------------------------
    // FSM transition history
    // ---------------------------------------------------------------
    [Serializable]
    public struct FSMEvent
    {
        public float  timestamp;
        public string enemyName;
        public string fromState;
        public string toState;
    }
    public List<FSMEvent> FSMHistory { get; } = new List<FSMEvent>();

    // Per-enemy current state
    public Dictionary<string, string> EnemyCurrentState { get; } = new Dictionary<string, string>();

    // ---------------------------------------------------------------
    // Detection response times
    // ---------------------------------------------------------------
    [Serializable]
    public struct DetectionEvent
    {
        public float  timestamp;
        public string enemyName;
        public float  responseTimeSeconds;
        public string tier;
    }
    public List<DetectionEvent> DetectionHistory { get; } = new List<DetectionEvent>();
    public float AvgResponseTime => DetectionHistory.Count > 0
        ? DetectionHistory.ConvertAll(d => d.responseTimeSeconds)
                          .Sum() / DetectionHistory.Count
        : 0f;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------
    private void Start()
    {
        SessionStartTime = Time.time;
        Debug.Log("[TestMetrics] Session started.");
    }

    // ---------------------------------------------------------------
    // Public write API
    // ---------------------------------------------------------------
    public void RecordShot(bool hit)
    {
        TotalShots++;
        if (hit) TotalHits++;
    }

    public void RecordKill()
    {
        KillCount++;
    }

    public void RecordDDAChange(string from, string to, float accuracy)
    {
        CurrentTier = to;
        DDAHistory.Add(new DDAEvent
        {
            timestamp        = SessionDuration,
            fromTier         = from,
            toTier           = to,
            accuracyAtChange = accuracy
        });
        Debug.Log($"[TestMetrics] DDA: {from} → {to} at {accuracy*100f:F0}% accuracy");
    }

    public void RecordFSMTransition(string enemyName, string from, string to)
    {
        EnemyCurrentState[enemyName] = to;
        FSMHistory.Add(new FSMEvent
        {
            timestamp  = SessionDuration,
            enemyName  = enemyName,
            fromState  = from,
            toState    = to
        });
    }

    public void RecordDetection(string enemyName, float responseTime)
    {
        DetectionHistory.Add(new DetectionEvent
        {
            timestamp           = SessionDuration,
            enemyName           = enemyName,
            responseTimeSeconds = responseTime,
            tier                = CurrentTier
        });
    }
}

// Extension helper
public static class ListExtensions
{
    public static float Sum(this List<float> list)
    {
        float s = 0f;
        foreach (float f in list) s += f;
        return s;
    }
}
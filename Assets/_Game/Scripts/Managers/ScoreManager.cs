using UnityEngine;

/// <summary>
/// ScoreManager — tracks kills, accuracy, and time across all levels.
/// Persists via DontDestroyOnLoad. Calculate() returns final score breakdown.
///
/// Setup:
///   1. Add to a GameObject in MainMenu scene
///   2. WeaponController already calls DifficultyManager.ReportShot — wire ScoreManager too
///   3. EnemyManager fires OnEnemyKilled — subscribe here
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // ---------------------------------------------------------------
    // Score values — Inspector configurable
    // ---------------------------------------------------------------
    [Header("Kill Points")]
    public int pointsGrunt      = 100;
    public int pointsScout      = 150;
    public int pointsBerserker  = 200;
    public int pointsMutant     = 175;
    public int pointsBoss       = 1000;
    public int pointsGeneric    = 100;  // fallback

    [Header("Bonus Thresholds")]
    public float accuracyBonusHighThreshold = 0.70f;  // 70%+ → 500pts
    public float accuracyBonusMidThreshold  = 0.50f;  // 50%+ → 250pts
    public int   accuracyBonusHigh          = 500;
    public int   accuracyBonusMid           = 250;

    public float timeBonusThreshold         = 300f;   // under 5 min → 200pts
    public int   timeBonusAmount            = 200;

    public int   stageCompleteBonus         = 300;
    public int   deathPenalty               = 100;

    // ---------------------------------------------------------------
    // Runtime tracking
    // ---------------------------------------------------------------
    public int   TotalKills      { get; private set; }
    public int   TotalShots      { get; private set; }
    public int   TotalHits       { get; private set; }
    public int   TotalDeaths     { get; private set; }
    public int   RawKillPoints   { get; private set; }
    public float ElapsedSeconds  { get; private set; }
    private bool _timing         = true;

    // ---------------------------------------------------------------
    // Score data result
    // ---------------------------------------------------------------
    public struct ScoreData
    {
        public int   totalScore;
        public int   killPoints;
        public int   accuracyBonus;
        public int   timeBonus;
        public int   stageBonus;
        public int   deathPenaltyTotal;
        public int   kills;
        public float accuracyPct;
        public float elapsedSeconds;
        public string grade;
    }

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Subscribe per-scene — EnemyManager is per-scene, ScoreManager is persistent
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeToEnemyManager();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                               UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Re-subscribe each scene since EnemyManager is per-scene
        SubscribeToEnemyManager();
    }

    private void SubscribeToEnemyManager()
    {
        if (EnemyManager.Instance != null)
        {
            // Unsubscribe first to prevent double-subscription on scene reload
            EnemyManager.Instance.OnEnemyKilled -= OnEnemyKilledCount;
            EnemyManager.Instance.OnEnemyKilled += OnEnemyKilledCount;
        }
    }

    private void Update()
    {
        if (_timing && GameManager.Instance != null && GameManager.Instance.IsPlaying())
            ElapsedSeconds += Time.deltaTime;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnEnemyKilled -= OnEnemyKilledCount;
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    /// <summary>Called by WeaponController alongside DifficultyManager.ReportShot().</summary>
    public void ReportShot(bool hit)
    {
        TotalShots++;
        if (hit) TotalHits++;
    }

    /// <summary>Call when player dies.</summary>
    public void ReportDeath()
    {
        TotalDeaths++;
        Debug.Log($"[ScoreManager] Death recorded. Total: {TotalDeaths}");
    }

    /// <summary>Stops the timer — call when win screen shows.</summary>
    public void StopTimer() => _timing = false;

    /// <summary>Reset — call on new game.</summary>
    public void Reset()
    {
        TotalKills     = 0;
        TotalShots     = 0;
        TotalHits      = 0;
        TotalDeaths    = 0;
        RawKillPoints  = 0;
        ElapsedSeconds = 0f;
        _timing        = true;
    }

    /// <summary>Calculate and return final score breakdown.</summary>
    public ScoreData Calculate()
    {
        StopTimer();

        float accuracy     = TotalShots > 0 ? (float)TotalHits / TotalShots : 0f;
        int accBonus       = accuracy >= accuracyBonusHighThreshold ? accuracyBonusHigh
                           : accuracy >= accuracyBonusMidThreshold  ? accuracyBonusMid : 0;
        int tBonus         = ElapsedSeconds <= timeBonusThreshold ? timeBonusAmount : 0;
        int deathPenTotal  = TotalDeaths * deathPenalty;
        int total          = RawKillPoints + accBonus + tBonus + stageCompleteBonus - deathPenTotal;
        total              = Mathf.Max(0, total);

        return new ScoreData
        {
            totalScore       = total,
            killPoints       = RawKillPoints,
            accuracyBonus    = accBonus,
            timeBonus        = tBonus,
            stageBonus       = stageCompleteBonus,
            deathPenaltyTotal = deathPenTotal,
            kills            = TotalKills,
            accuracyPct      = accuracy * 100f,
            elapsedSeconds   = ElapsedSeconds,
            grade            = GetGrade(total)
        };
    }

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    // Called by EnemyManager.OnEnemyKilled — now receives enemy name for type-based points.
    // This is the single kill-counting path for ALL enemy types including the boss.
    private void OnEnemyKilledCount(string enemyName)
    {
        TotalKills++;
        string n = enemyName.ToLower();
        int pts = n.Contains("boss")      ? pointsBoss
                : n.Contains("berserker") ? pointsBerserker
                : n.Contains("mutant")    ? pointsMutant
                : n.Contains("scout")     ? pointsScout
                : n.Contains("grunt")     ? pointsGrunt
                : pointsGeneric;
        RawKillPoints += pts;
        Debug.Log($"[ScoreManager] Kill '{enemyName}' +{pts}. Total: {TotalKills}, Pts: {RawKillPoints}");
    }

    private string GetGrade(int score)
    {
        if (score >= 3000) return "S";
        if (score >= 2000) return "A";
        if (score >= 1000) return "B";
        if (score >= 500)  return "C";
        return "D";
    }
}
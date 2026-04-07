using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BossController — Reactor Guardian for Stage [5].
///
/// FSM: Idle → Chase → Attack / Jump → (loop) → Dead
///
/// AI rules:
///   - Within attackRange                 → melee attack
///   - Between attackRange and jumpRange  → chase only (too close to bother jumping)
///   - Beyond jumpRange + cooldown ready  → jump attack
///
/// Phase 2 (≤ 50% HP):
///   - Speed boosted
///   - Initial minion wave spawned (WaveSpawner.SpawnWave)
///   - Periodic additional waves every waveInterval seconds (WaveSpawner.SpawnWave)
///   - Auto-heals every healInterval seconds
///
/// Kill counting:
///   All scoring goes through EnemyManager.DeregisterEnemy → ScoreManager.OnEnemyKilled.
///   BossController never calls ScoreManager directly (avoids double-counting).
/// </summary>
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class BossController : MonoBehaviour, IDamageable, IEnemy
{
    // ---------------------------------------------------------------
    // Inspector — Stats
    // ---------------------------------------------------------------

    [Header("Stats")]
    public int   maxHealth      = 800;
    public float detectionRange = 20f;
    public float attackRange    = 2.5f;
    public float attackDamage   = 25f;
    public float attackCooldown = 2f;
    public float moveSpeed      = 3.5f;

    // ---------------------------------------------------------------
    // Inspector — Jump AI
    // ---------------------------------------------------------------

    [Header("Jump AI")]
    [Tooltip("Boss will only jump when the player is FURTHER than this distance. " +
             "Below this the boss chases and melees instead.")]
    public float jumpMinDistance = 6f;

    [Tooltip("Boss won't jump if player is further than this (avoids comedic missed leaps)")]
    public float jumpMaxDistance = 18f;

    [Tooltip("Seconds between jump attempts")]
    public float jumpCooldown    = 8f;

    [Tooltip("Seconds between jump attempts in Phase 2")]
    public float jumpCooldownP2  = 5f;

    [Header("Jump Physics")]
    [Tooltip("Base windup duration — randomised ±15% each jump")]
    public float chargeWindupTime = 1.0f;
    [Tooltip("Arc angle when player is close (steeper)")]
    public float jumpArcAngleMax  = 55f;
    [Tooltip("Arc angle when player is far (flatter)")]
    public float jumpArcAngleMin  = 25f;
    [Tooltip("Should match Physics.gravity magnitude")]
    public float jumpGravity      = 9.81f;
    public float jumpMaxForce     = 30f;
    public float jumpMinForce     = 8f;

    [Header("Jump Landing")]
    public float jumpDamage        = 40f;
    public float jumpDamageRadius  = 3f;

    [Tooltip("VFX prefab spawned at landing point — e.g. shockwave or dust cloud")]
    public GameObject landingVFX;

    [Header("Camera Shake — Landing")]
    public float landingShakeDuration  = 0.5f;
    public float landingShakeMagnitude = 0.4f;

    // ---------------------------------------------------------------
    // Inspector — Walk Animation
    // ---------------------------------------------------------------

    [Header("Walk Animation")]
    public float walkAnimSpeed = 1f;

    // ---------------------------------------------------------------
    // Inspector — Phase 2
    // ---------------------------------------------------------------

    [Header("Phase 2")]
    [Range(0f, 1f)]
    public float phase2Threshold  = 0.5f;
    public float phase2SpeedBoost = 1.4f;

    [Header("Phase 2 — Auto Heal")]
    public float healInterval = 6f;
    public int   healAmount   = 30;

    [Header("Phase 2 — Waves")]
    [Tooltip("Seconds between additional wave spawns in Phase 2. " +
             "Set 0 or negative to disable interval spawning (first wave only).")]
    public float waveInterval = 30f;

    // ---------------------------------------------------------------
    // Inspector — References
    // ---------------------------------------------------------------

    [Header("References")]
    public WaveSpawner   waveSpawner;
    public BossHealthBar bossHealthBar;
    public Transform     playerTarget;

    // ---------------------------------------------------------------
    // Inspector — Events
    // ---------------------------------------------------------------

    [Header("Events")]
    public UnityEvent onPhase2Start;
    public UnityEvent onBossDefeated;

    // ---------------------------------------------------------------
    // IDamageable / IEnemy
    // ---------------------------------------------------------------

    public bool IsAlive => _currentHealth > 0 && _state != BossState.Dead;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    public enum BossState { Idle, Chase, Attack, Jump, Dead }

    private BossState _state         = BossState.Idle;
    private int       _currentHealth;
    private bool      _phase2Active  = false;
    private float     _attackTimer   = 0f;
    private float     _jumpTimer     = 0f;
    private float     _healTimer     = 0f;
    private float     _waveTimer     = 0f;

    private UnityEngine.AI.NavMeshAgent _agent;
    private BossAnimatorBridge          _anim;
    private Rigidbody                   _rb;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _anim  = GetComponent<BossAnimatorBridge>();
        _rb    = GetComponent<Rigidbody>();

        _currentHealth = maxHealth;
        _agent.speed   = moveSpeed;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }

        EnemyManager.Instance?.RegisterEnemy(this);
        bossHealthBar?.Initialize(maxHealth);

        TestMetricsCollector.Instance?.RecordFSMTransition(gameObject.name, "None", "Idle");
        Debug.Log("[Boss] Reactor Guardian awakened.");

        FixRendererCulling();
    }

    private void FixRendererCulling()
    {
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.updateWhenOffscreen = true;
            smr.localBounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 10f));
        }
    }

    private void OnDestroy() => EnemyManager.Instance?.DeregisterEnemy(this);

    private void Update()
    {
        if (_state == BossState.Dead) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        _attackTimer -= Time.deltaTime;
        _jumpTimer   -= Time.deltaTime;

        if (_phase2Active)
        {
            _healTimer -= Time.deltaTime;
            if (_healTimer <= 0f)
            {
                _healTimer = healInterval;
                if (IsAlive) StartCoroutine(AutoHeal());
            }

            // Periodic additional wave spawns in Phase 2
            if (waveInterval > 0f)
            {
                _waveTimer -= Time.deltaTime;
                if (_waveTimer <= 0f)
                {
                    _waveTimer = waveInterval;
                    waveSpawner?.SpawnWave();
                    Debug.Log("[Boss] Phase 2 interval wave spawned.");
                }
            }
        }

        switch (_state)
        {
            case BossState.Idle:  UpdateIdle();  break;
            case BossState.Chase: UpdateChase(); break;
        }
    }

    // ---------------------------------------------------------------
    // FSM
    // ---------------------------------------------------------------

    private void UpdateIdle()
    {
        if (playerTarget == null) return;
        if (Vector3.Distance(transform.position, playerTarget.position) <= detectionRange)
            SetState(BossState.Chase);
    }

    private void UpdateChase()
    {
        if (playerTarget == null) return;

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        // ── Melee: player within attack range ──
        if (dist <= attackRange && _attackTimer <= 0f)
        {
            SetState(BossState.Attack);
            StartCoroutine(PerformAttack());
            return;
        }

        // ── Jump: player far enough that jumping makes sense ──
        // Requires: outside jumpMinDistance, within jumpMaxDistance, cooldown ready
        bool jumpReady     = _jumpTimer <= 0f;
        bool playerFarEnough = dist >= jumpMinDistance;
        bool playerCloseEnough = dist <= jumpMaxDistance;

        if (jumpReady && playerFarEnough && playerCloseEnough)
        {
            _jumpTimer = _phase2Active ? jumpCooldownP2 : jumpCooldown;
            SetState(BossState.Jump);
            StartCoroutine(PerformJump());
            return;
        }

        // ── Default: chase ──
        _agent.SetDestination(playerTarget.position);
        _anim?.SetSpeed(_agent.velocity.magnitude / moveSpeed * walkAnimSpeed);
    }

    // ---------------------------------------------------------------
    // Attack coroutines
    // ---------------------------------------------------------------

    private IEnumerator PerformAttack()
    {
        _agent.isStopped = true;
        _anim?.TriggerAttack();

        yield return new WaitForSeconds(0.5f); // hit frame

        if (playerTarget != null &&
            Vector3.Distance(transform.position, playerTarget.position) <= attackRange + 0.5f)
        {
            playerTarget.GetComponent<PlayerController>()?.TakeDamage((int)attackDamage);
            Debug.Log($"[Boss] Melee hit for {attackDamage}");
        }

        yield return new WaitForSeconds(attackCooldown - 0.5f);
        _attackTimer     = attackCooldown;
        _agent.isStopped = false;
        SetState(BossState.Chase);
    }

    private IEnumerator PerformJump()
    {
        _agent.isStopped = true;
        _agent.enabled   = false;

        if (_rb != null)
        {
            _rb.freezeRotation = true;
            _rb.isKinematic    = true;
        }

        // Face player before leaping
        if (playerTarget != null)
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            Vector3 lookDir  = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        _anim?.TriggerJump();

        float windup = chargeWindupTime * Random.Range(0.85f, 1.15f);
        yield return new WaitForSeconds(windup);

        if (_state == BossState.Dead)
        {
            ResumeAgent();
            yield break;
        }

        float physicsFlight = 1.5f;

        if (playerTarget != null && _rb != null)
        {
            Vector3 toTarget = playerTarget.position - transform.position;
            float   flatDist = new Vector2(toTarget.x, toTarget.z).magnitude;

            float tNorm    = Mathf.InverseLerp(jumpMinDistance, jumpMaxDistance, flatDist);
            float arcAngle = Mathf.Lerp(jumpArcAngleMax, jumpArcAngleMin, tNorm);
            arcAngle      += Random.Range(-5f, 5f);
            arcAngle       = Mathf.Clamp(arcAngle, jumpArcAngleMin, jumpArcAngleMax);

            float angleRad  = arcAngle * Mathf.Deg2Rad;
            float sinDouble = Mathf.Max(Mathf.Sin(2f * angleRad), 0.01f);
            float speed     = Mathf.Clamp(Mathf.Sqrt(flatDist * jumpGravity / sinDouble),
                                          jumpMinForce, jumpMaxForce);

            physicsFlight = 2f * speed * Mathf.Sin(angleRad) / jumpGravity;

            Vector3 flatDir   = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            Vector3 launchDir = flatDir * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad);

            _rb.isKinematic    = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(launchDir * speed, ForceMode.VelocityChange);

            Debug.Log($"[Boss] Jump — dist:{flatDist:F1}m  speed:{speed:F1}  arc:{arcAngle:F0}°  ETA:{physicsFlight:F2}s");
        }

        yield return new WaitForSeconds(0.3f);

        // Poll for ground
        int   groundMask = ~(LayerMask.GetMask("Enemy") | LayerMask.GetMask("Player"));
        float airTimer   = 0.3f;
        bool  landed     = false;

        while (airTimer < physicsFlight + 1.5f)
        {
            if (Physics.SphereCast(
                    transform.position + Vector3.up * 0.5f,
                    0.35f, Vector3.down, out RaycastHit _, 1.1f, groundMask))
            {
                landed = true;
                break;
            }
            airTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[Boss] Landed ({(landed ? "ground" : "timeout")}) after {airTimer:F2}s");

        // Stop physics
        if (_rb != null)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic     = true;
        }

        // ── Landing effects ──
        Vector3 landPos = transform.position;

        if (landingVFX != null)
        {
            GameObject fx = Instantiate(landingVFX, landPos, Quaternion.identity);
            Destroy(fx, 4f);
        }

        SFXManager.Instance?.PlayBossSlamAt(landPos);
        CameraShake.Instance?.Shake(landingShakeDuration, landingShakeMagnitude);

        // ── AoE slam damage ──
        foreach (Collider col in Physics.OverlapSphere(landPos, jumpDamageRadius))
        {
            PlayerController pc = col.GetComponent<PlayerController>()
                               ?? col.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage((int)jumpDamage);
                Debug.Log($"[Boss] Jump slam hit player for {jumpDamage}!");
            }
        }

        float remaining = Mathf.Clamp(physicsFlight - airTimer, 0f, 1f);
        if (remaining > 0.05f)
            yield return new WaitForSeconds(remaining);

        // Snap to NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(
                transform.position, out UnityEngine.AI.NavMeshHit navHit, 5f,
                UnityEngine.AI.NavMesh.AllAreas))
            transform.position = navHit.position;

        ResumeAgent();
        SetState(BossState.Chase);
    }

    private IEnumerator AutoHeal()
    {
        if (!IsAlive) yield break;
        if (!AgentReady()) yield break;

        _agent.isStopped = true;
        _anim?.TriggerHeal();

        yield return new WaitForSeconds(1f);

        if (!IsAlive || !AgentReady()) yield break;

        _currentHealth = Mathf.Min(_currentHealth + healAmount, maxHealth);
        bossHealthBar?.UpdateHealth(_currentHealth, maxHealth);
        Debug.Log($"[Boss] Auto-healed +{healAmount}. HP: {_currentHealth}/{maxHealth}");

        if (AgentReady()) _agent.isStopped = false;
    }

    // ---------------------------------------------------------------
    // Phase transition
    // ---------------------------------------------------------------

    private void CheckPhaseTransition()
    {
        if (_phase2Active) return;
        if ((float)_currentHealth / maxHealth > phase2Threshold) return;

        _phase2Active = true;
        _agent.speed  = moveSpeed * phase2SpeedBoost;
        _healTimer    = healInterval;
        _jumpTimer    = 0f;       // trigger jump opportunity immediately
        _waveTimer    = waveInterval; // first interval wave after waveInterval seconds

        onPhase2Start?.Invoke();
        waveSpawner?.SpawnWave(); // immediate first wave

        TestMetricsCollector.Instance?.RecordFSMTransition(gameObject.name, "Phase1", "Phase2");
        Debug.Log("[Boss] Phase 2 activated! Spawning initial minion wave.");
    }

    // ---------------------------------------------------------------
    // IDamageable
    // ---------------------------------------------------------------

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        bossHealthBar?.UpdateHealth(_currentHealth, maxHealth);
        SFXManager.Instance?.PlayEnemyHurtAt(transform.position);

        CheckPhaseTransition();

        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        SetState(BossState.Dead);
        StopAllCoroutines();
        if (AgentReady()) _agent.isStopped = true;
        _anim?.TriggerDie();
        SFXManager.Instance?.PlayEnemyDeathAt(transform.position);

        // DeregisterEnemy fires OnEnemyKilled(name) → ScoreManager awards boss points.
        // Do NOT call ScoreManager.ReportKill() here — that would count the kill twice.
        EnemyManager.Instance?.DeregisterEnemy(this);

        TestMetricsCollector.Instance?.RecordFSMTransition(gameObject.name, "Alive", "Dead");
        Debug.Log("[Boss] Reactor Guardian defeated!");
        StartCoroutine(DefeatSequence());
    }

    private IEnumerator DefeatSequence()
    {
        yield return new WaitForSeconds(2f);
        onBossDefeated?.Invoke();

        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerStageWin();
        else
            Debug.LogWarning("[Boss] GameManager not found — win not triggered.");
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void SetState(BossState newState) => _state = newState;

    private void ResumeAgent()
    {
        _agent.enabled = true;
        if (AgentReady()) _agent.isStopped = false;
    }

    private bool AgentReady() =>
        _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;

    // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, jumpMinDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jumpMaxDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, jumpDamageRadius);
    }
}

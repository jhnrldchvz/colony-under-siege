using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BossController — Final boss for Stage [5] Reactor Chamber.
/// Two-phase FSM: Phase 1 (100–50% HP) melee/jump, Phase 2 adds speed, minion waves, and auto-heal.
///
/// States: Idle → Chase → Attack / Jump → (loop) → Dead
/// </summary>
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class BossController : MonoBehaviour, IDamageable, IEnemy
{
    [Header("Stats")]
    public int   maxHealth      = 800;
    public float detectionRange = 20f;
    public float attackRange    = 2.5f;
    public float attackDamage   = 25f;
    public float attackCooldown = 2f;
    public float moveSpeed      = 3.5f;

    [Header("Jump Attack")]
    [Tooltip("Minimum distance from player before a jump is triggered")]
    public float chargeMinDistance = 5f;
    [Tooltip("Base windup duration — randomised ±15% each jump")]
    public float chargeWindupTime  = 1.0f;
    [Tooltip("Arc angle when player is close (steeper)")]
    public float jumpArcAngleMax   = 55f;
    [Tooltip("Arc angle when player is far (flatter)")]
    public float jumpArcAngleMin   = 25f;
    [Tooltip("Should match Physics.gravity magnitude")]
    public float jumpGravity       = 9.81f;
    public float jumpMaxForce      = 30f;
    public float jumpMinForce      = 8f;
    public float jumpDamage        = 40f;
    public float jumpDamageRadius  = 3f;
    public float chargeInterval    = 8f;
    public float chargeIntervalP2  = 5f;

    [Header("Walk Animation")]
    [Tooltip("Speed value at full walk — match blend tree threshold")]
    public float walkAnimSpeed = 1f;

    [Header("Auto Heal — Phase 2")]
    public float healInterval = 6f;
    public int   healAmount   = 30;

    [Header("Phase 2")]
    [Range(0f, 1f)]
    public float phase2Threshold  = 0.5f;
    public float phase2SpeedBoost = 1.4f;

    [Header("References")]
    public WaveSpawner   waveSpawner;
    public BossHealthBar bossHealthBar;
    public Transform     playerTarget;

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

    public enum BossState { Idle, Chase, Attack, Jump, Heal, Dead }

    private BossState _state        = BossState.Idle;
    private int       _currentHealth;
    private bool      _phase2Active = false;
    private float     _attackTimer  = 0f;
    private float     _chargeTimer  = 0f;
    private float     _healTimer    = 0f;

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
            Bounds b = smr.localBounds;
            b.Expand(new Vector3(2f, 3f, 2f));
            smr.localBounds = b;
        }
    }

    private void OnDestroy() => EnemyManager.Instance?.DeregisterEnemy(this);

    private void Update()
    {
        if (_state == BossState.Dead) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;

        _attackTimer -= Time.deltaTime;
        _chargeTimer -= Time.deltaTime;

        if (_phase2Active && _state == BossState.Chase)
        {
            _healTimer -= Time.deltaTime;
            if (_healTimer <= 0f)
            {
                _healTimer = healInterval;
                StartCoroutine(AutoHeal());
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

        if (dist <= attackRange && _attackTimer <= 0f)
        {
            SetState(BossState.Attack);
            StartCoroutine(PerformAttack());
            return;
        }

        if (dist >= chargeMinDistance && _chargeTimer <= 0f)
        {
            _chargeTimer = _phase2Active ? chargeIntervalP2 : chargeInterval;
            SetState(BossState.Jump);
            StartCoroutine(PerformJump());
            return;
        }

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

        yield return new WaitForSeconds(0.5f); // Hit frame

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

        // Face player
        if (playerTarget != null)
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            Vector3 lookDir  = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        _anim?.TriggerJump();

        // Randomise windup ±15% so consecutive jumps feel different
        float windup = chargeWindupTime * Random.Range(0.85f, 1.15f);
        yield return new WaitForSeconds(windup);

        if (_state == BossState.Dead)
        {
            ResumeAgent();
            yield break;
        }

        float physicsFlight = 1.5f; // fallback if no player target

        if (playerTarget != null && _rb != null)
        {
            Vector3 toTarget = playerTarget.position - transform.position;
            float   flatDist = new Vector2(toTarget.x, toTarget.z).magnitude;

            // Arc scales with distance — close jumps are steep, far jumps are flat
            float tNorm    = Mathf.InverseLerp(chargeMinDistance, detectionRange, flatDist);
            float arcAngle = Mathf.Lerp(jumpArcAngleMax, jumpArcAngleMin, tNorm);
            arcAngle      += Random.Range(-5f, 5f);  // subtle per-jump variation
            arcAngle       = Mathf.Clamp(arcAngle, jumpArcAngleMin, jumpArcAngleMax);

            float angleRad  = arcAngle * Mathf.Deg2Rad;
            float sinDouble = Mathf.Max(Mathf.Sin(2f * angleRad), 0.01f);
            float speed     = Mathf.Clamp(Mathf.Sqrt(flatDist * jumpGravity / sinDouble),
                                          jumpMinForce, jumpMaxForce);

            // Physics-derived flight time: T = 2·v·sin(θ) / g
            physicsFlight = 2f * speed * Mathf.Sin(angleRad) / jumpGravity;

            Vector3 flatDir   = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            Vector3 launchDir = flatDir * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad);

            _rb.isKinematic    = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(launchDir * speed, ForceMode.VelocityChange);

            Debug.Log($"[Boss] Jump — dist: {flatDist:F1}m  speed: {speed:F1}  arc: {arcAngle:F0}°  ETA: {physicsFlight:F2}s");
        }

        // Short delay to avoid detecting launch-point floor immediately
        yield return new WaitForSeconds(0.3f);

        // Poll for ground — stops as soon as boss lands rather than waiting a fixed timer
        int   groundMask = ~(LayerMask.GetMask("Enemy") | LayerMask.GetMask("Player"));
        float airTimer   = 0.3f;
        bool  landed     = false;

        while (airTimer < physicsFlight + 1.5f) // +1.5 s grace for slow landings
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

        if (_rb != null)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic     = true;
        }

        // AoE slam on landing
        foreach (Collider col in Physics.OverlapSphere(transform.position, jumpDamageRadius))
        {
            PlayerController pc = col.GetComponent<PlayerController>()
                               ?? col.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage((int)jumpDamage);
                Debug.Log($"[Boss] Jump slam hit player for {jumpDamage}!");
            }
        }

        // Wait out remaining animation clip if we landed early
        float remaining = Mathf.Clamp(physicsFlight - airTimer, 0f, 1f);
        if (remaining > 0.05f)
            yield return new WaitForSeconds(remaining);

        // Snap back to NavMesh surface
        if (UnityEngine.AI.NavMesh.SamplePosition(
                transform.position, out UnityEngine.AI.NavMeshHit navHit, 5f,
                UnityEngine.AI.NavMesh.AllAreas))
            transform.position = navHit.position;

        ResumeAgent();
        SetState(BossState.Chase);
    }

    private IEnumerator AutoHeal()
    {
        if (!IsAlive || !_agent.isOnNavMesh) yield break;

        SetState(BossState.Heal);
        _agent.isStopped = true;
        _anim?.TriggerHeal();

        float clipLen = _anim != null ? _anim.GetClipLength("flex", 4f) : 4f;
        yield return new WaitForSeconds(clipLen);

        if (!IsAlive || !_agent.isOnNavMesh) yield break;

        _currentHealth = Mathf.Min(_currentHealth + healAmount, maxHealth);
        bossHealthBar?.UpdateHealth(_currentHealth, maxHealth);
        Debug.Log($"[Boss] Auto-healed +{healAmount}. HP: {_currentHealth}/{maxHealth}");

        _agent.isStopped = false;
        SetState(BossState.Chase);
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
        _chargeTimer  = 0f; // trigger a charge immediately on phase 2 start

        onPhase2Start?.Invoke();
        waveSpawner?.SpawnWave();

        TestMetricsCollector.Instance?.RecordFSMTransition(gameObject.name, "Phase1", "Phase2");
        Debug.Log("[Boss] Phase 2 activated! Spawning minions.");
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
        StopAllCoroutines(); // cancel AutoHeal / PerformJump / PerformAttack
        _agent.isStopped = true;
        _anim?.TriggerDie();
        SFXManager.Instance?.PlayEnemyDeathAt(transform.position);
        EnemyManager.Instance?.DeregisterEnemy(this);

        TestMetricsCollector.Instance?.RecordFSMTransition(gameObject.name, "Alive", "Dead");
        Debug.Log("[Boss] Reactor Guardian defeated!");
        StartCoroutine(DefeatSequence()); // restarted after StopAllCoroutines
    }

    private IEnumerator DefeatSequence()
    {
        yield return new WaitForSeconds(3f);
        onBossDefeated?.Invoke();
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private void SetState(BossState newState) => _state = newState;

    private void ResumeAgent()
    {
        _agent.enabled   = true;
        _agent.isStopped = false;
    }

    // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeMinDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, jumpDamageRadius);
    }
}

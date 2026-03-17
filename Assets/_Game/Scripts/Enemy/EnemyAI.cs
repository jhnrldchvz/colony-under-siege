using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI — Finite State Machine for all enemy types.
///
/// States:
///   Patrol  → walks between waypoints
///   Chase   → moves toward the player when detected
///   Attack  → deals damage when within attack range
///   Return  → walks back to patrol route when player escapes
///   Dead    → plays death, drops pickup, removes itself
///
/// Setup:
///   1. Attach to an enemy GameObject that has a NavMeshAgent component.
///   2. Bake a NavMesh on your level geometry (Window → AI → Navigation → Bake).
///   3. Create child GameObjects as waypoints and assign them to the
///      patrolPoints array in the Inspector.
///   4. Set the Player tag to "Player" in Edit → Project Settings → Tags.
///   5. Assign an EnemyStats preset — all combat/movement stats come from there.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    // ---------------------------------------------------------------
    // State enum
    // ---------------------------------------------------------------

    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Return,
        Dead
    }

    // ---------------------------------------------------------------
    // Inspector — scene-specific (not stored in EnemyStats)
    // ---------------------------------------------------------------

    [Header("Patrol")]
    [Tooltip("Waypoints the enemy walks between. Assign 2+ transforms.")]
    public Transform[] patrolPoints;

    [Tooltip("How long the enemy waits at each waypoint before moving on")]
    public float patrolWaitTime = 2f;

    [Header("Detection")]
    [Tooltip("Layer mask for line-of-sight check — set to everything except the enemy layer")]
    public LayerMask sightBlockLayers;

    [Header("UI")]
    public EnemyHealthBar healthBar;
    public EnemyNameLabel nameLabel;

    [Header("Preset")]
    [Tooltip("Defines all combat and movement stats for this enemy type")]
    public EnemyStats statPreset;

    [Header("Loot")]
    [Tooltip("Prefab to spawn when this enemy dies. Assign a PickupItem prefab.")]
    public GameObject lootDropPrefab;

    [Header("Death")]
    [Tooltip("Seconds before the enemy GameObject is destroyed after dying")]
    public float destroyDelay = 3f;

    // ---------------------------------------------------------------
    // Public state — readable by EnemyManager
    // ---------------------------------------------------------------

    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
    public bool IsAlive            { get; private set; } = true;

    // ---------------------------------------------------------------
    // Private — runtime stats (set by ApplyStats / DDA)
    // ---------------------------------------------------------------

    private int   maxHealth      = 50;
    private float patrolSpeed    = 2.5f;
    private float chaseSpeed     = 5f;
    private float detectionRange = 10f;
    private float fieldOfView    = 110f;
    private float losePlayerRange= 14f;
    private float attackRange    = 2f;
    private int   attackDamage   = 10;
    private float attackCooldown = 1.5f;
    private float dropChance     = 0.75f;
    private bool  isRanged       = false;
    private float preferredRange = 10f;
    private GameObject projectilePrefab = null;

    // ---------------------------------------------------------------
    // Private — base stats (stored after ApplyStats, used by DDA multipliers)
    // ---------------------------------------------------------------

    private int   _baseHealth;
    private float _basePatrolSpeed;
    private float _baseChaseSpeed;
    private int   _baseAttackDamage;
    private float _baseDetectionRange;
    private float _baseAttackRange;
    private float _baseAttackCooldown;

    // ---------------------------------------------------------------
    // Private — components
    // ---------------------------------------------------------------

    private NavMeshAgent     _agent;
    private Animator         _animator;
    private PlayerController _player;

    // ---------------------------------------------------------------
    // Private — patrol
    // ---------------------------------------------------------------

    private int   _patrolIndex = 0;
    private bool  _isWaiting   = false;
    private float _waitTimer   = 0f;

    // ---------------------------------------------------------------
    // Private — attack / health / return
    // ---------------------------------------------------------------

    private float   _attackTimer = 0f;
    private int     _currentHealth;
    private Vector3 _lastPatrolPosition;

    // ---------------------------------------------------------------
    // Animator parameter hashes — cached for performance
    // ---------------------------------------------------------------

    private static readonly int AnimSpeed  = Animator.StringToHash("Speed");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimDie    = Animator.StringToHash("Die");

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _agent         = GetComponent<NavMeshAgent>();
        _animator      = GetComponent<Animator>();
        _currentHealth = maxHealth;

        if (statPreset != null)
            ApplyStats(statPreset);
        else
            Debug.LogWarning($"[EnemyAI] {gameObject.name} has no statPreset assigned.");

        healthBar?.Initialize(maxHealth);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.GetComponent<PlayerController>();

        if (_player == null)
            Debug.LogWarning("[EnemyAI] Could not find Player.");

        EnemyManager.Instance?.RegisterEnemy(this);

        if (DifficultyManager.Instance != null)
            ApplyDifficultySettings(
                DifficultyManager.Instance.BuildSettings(
                    DifficultyManager.Instance.CurrentTier));

        SetState(EnemyState.Patrol);
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;
        if (!IsAlive) return;

        _attackTimer -= Time.deltaTime;

        switch (CurrentState)
        {
            case EnemyState.Patrol:  UpdatePatrol();  break;
            case EnemyState.Chase:   UpdateChase();   break;
            case EnemyState.Attack:  UpdateAttack();  break;
            case EnemyState.Return:  UpdateReturn();  break;
        }
    }

    // ---------------------------------------------------------------
    // FSM — Patrol
    // ---------------------------------------------------------------

    private void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        SetAnimatorSpeed(_agent.velocity.magnitude);

        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                GoToNextPatrolPoint();
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _lastPatrolPosition = transform.position;
            _isWaiting          = true;
            _waitTimer          = patrolWaitTime;
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        _agent.speed = patrolSpeed;
        _agent.SetDestination(patrolPoints[_patrolIndex].position);
        _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
    }

    // ---------------------------------------------------------------
    // FSM — Chase
    // ---------------------------------------------------------------

    private void UpdateChase()
    {
        if (_player == null || !_player.IsAlive)
        {
            SetState(EnemyState.Return);
            return;
        }

        float dist = Vector3.Distance(transform.position, _player.transform.position);

        if (dist > losePlayerRange)
        {
            SetState(EnemyState.Return);
            return;
        }

        if (isRanged)
        {
            // Ranged enemy — enter attack state when within attack range
            // and reposition to preferred distance
            if (dist <= attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }

            // Move toward preferred range distance
            if (dist > preferredRange)
            {
                // Too far — close in to preferred range
                _agent.speed = chaseSpeed;
                _agent.SetDestination(_player.transform.position);
            }
            else
            {
                // Within preferred range — hold position and face player
                _agent.SetDestination(transform.position);
                FaceTarget(_player.transform.position);
                // Still in range — enter attack
                SetState(EnemyState.Attack);
            }
        }
        else
        {
            // Melee enemy — move directly to player
            if (dist <= attackRange)
            {
                SetState(EnemyState.Attack);
                return;
            }

            _agent.speed = chaseSpeed;
            _agent.SetDestination(_player.transform.position);
        }

        SetAnimatorSpeed(_agent.velocity.magnitude);
    }

    // ---------------------------------------------------------------
    // FSM — Attack
    // ---------------------------------------------------------------

    private void UpdateAttack()
    {
        if (_player == null || !_player.IsAlive)
        {
            SetState(EnemyState.Return);
            return;
        }

        float dist = Vector3.Distance(transform.position, _player.transform.position);

        // If player moved out of attack range — chase again
        if (dist > attackRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        FaceTarget(_player.transform.position);
        SetAnimatorSpeed(0f);

        if (isRanged)
        {
            // Ranged — hold position at preferred distance
            if (dist < preferredRange * 0.5f)
            {
                // Player too close — back away
                Vector3 retreatDir = (transform.position - _player.transform.position).normalized;
                _agent.speed = chaseSpeed;
                _agent.SetDestination(transform.position + retreatDir * 3f);
            }
            else
            {
                _agent.SetDestination(transform.position); // Hold
            }
        }
        else
        {
            // Melee — stop and attack
            _agent.SetDestination(transform.position);
        }

        if (_attackTimer <= 0f)
        {
            PerformAttack();
            _attackTimer = attackCooldown;
        }
    }

    private void PerformAttack()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimAttack);

        if (isRanged)
        {
            // Ranged attack — spawn a projectile the player can dodge
            Vector3 origin      = transform.position + Vector3.up * 1.0f;
            Vector3 playerEye   = _player.GetCameraPosition();
            Vector3 direction   = (playerEye - origin).normalized;

            if (projectilePrefab != null)
            {
                GameObject proj    = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
                EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
                if (ep != null)
                    ep.Init(direction, attackDamage, gameObject);

                Debug.Log($"[EnemyAI] {gameObject.name} fired projectile toward player.");
            }
            else
            {
                // Fallback — instant raycast if no prefab assigned
                Debug.LogWarning($"[EnemyAI] {gameObject.name} has no projectilePrefab assigned.");
                if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
                {
                    PlayerController pc = hit.collider.GetComponent<PlayerController>() ??
                                          hit.collider.GetComponentInParent<PlayerController>();
                    if (pc != null)
                        pc.TakeDamage(attackDamage);
                }
            }
        }
        else
        {
            // Melee attack — direct damage
            _player.TakeDamage(attackDamage);
            Debug.Log($"[EnemyAI] {gameObject.name} melee hit player for {attackDamage}.");
        }
    }

    // ---------------------------------------------------------------
    // FSM — Return
    // ---------------------------------------------------------------

    private void UpdateReturn()
    {
        if (CanSeePlayer())
        {
            SetState(EnemyState.Chase);
            return;
        }

        SetAnimatorSpeed(_agent.velocity.magnitude);

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            SetState(EnemyState.Patrol);
            GoToNextPatrolPoint();
        }
    }

    // ---------------------------------------------------------------
    // State transition
    // ---------------------------------------------------------------

    private void SetState(EnemyState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        switch (newState)
        {
            case EnemyState.Patrol:
                _agent.speed = patrolSpeed;
                break;
            case EnemyState.Chase:
                _agent.speed = chaseSpeed;
                break;
            case EnemyState.Attack:
                _agent.ResetPath();
                break;
            case EnemyState.Return:
                _agent.speed = patrolSpeed;
                if (_lastPatrolPosition != Vector3.zero)
                    _agent.SetDestination(_lastPatrolPosition);
                break;
        }

        Debug.Log($"[EnemyAI] {gameObject.name} state → {newState}");
    }

    // ---------------------------------------------------------------
    // Detection — line-of-sight check
    // ---------------------------------------------------------------

    private bool CanSeePlayer()
    {
        if (_player == null || !_player.IsAlive) return false;

        Vector3 toPlayer = _player.transform.position - transform.position;
        float   distance = toPlayer.magnitude;

        if (distance > detectionRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up * 1f,
                            toPlayer.normalized,
                            distance,
                            sightBlockLayers))
            return false;

        return true;
    }

    // ---------------------------------------------------------------
    // Health — taking damage
    // ---------------------------------------------------------------

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);

        Debug.Log($"[EnemyAI] {gameObject.name} took {amount} damage. HP: {_currentHealth}/{maxHealth}");

        healthBar?.UpdateHealth(_currentHealth, maxHealth);

        if (CurrentState == EnemyState.Patrol || CurrentState == EnemyState.Return)
            SetState(EnemyState.Chase);

        if (_currentHealth <= 0)
            Die();
    }

    // ---------------------------------------------------------------
    // Death
    // ---------------------------------------------------------------

    private void Die()
    {
        if (!IsAlive) return;

        IsAlive = false;
        SetState(EnemyState.Dead);
        _agent.enabled = false;

        if (_animator != null)
            _animator.SetTrigger(AnimDie);

        EnemyManager.Instance?.DeregisterEnemy(this);

        Debug.Log($"[EnemyAI] {gameObject.name} died.");

        if (lootDropPrefab != null && Random.value <= dropChance)
        {
            Vector3    dropPos  = transform.position + Vector3.up * 0.5f;
            GameObject dropped  = Instantiate(lootDropPrefab, dropPos, Quaternion.identity);
            Debug.Log($"[EnemyAI] Dropped: {dropped.name}");
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    // ---------------------------------------------------------------
    // Utility — stat application
    // ---------------------------------------------------------------

    /// <summary>
    /// Called by DifficultyManager when accuracy crosses a threshold.
    /// Uses multipliers so every enemy type scales proportionally
    /// from its own base stats — roster balance is preserved at all tiers.
    /// </summary>
    public void ApplyDifficultySettings(EnemyDifficultySettings settings)
    {
        if (_baseHealth == 0)
        {
            _baseHealth         = maxHealth;
            _basePatrolSpeed    = patrolSpeed;
            _baseChaseSpeed     = chaseSpeed;
            _baseAttackDamage   = attackDamage;
            _baseDetectionRange = detectionRange;
            _baseAttackRange    = attackRange;
            _baseAttackCooldown = attackCooldown;
        }

        patrolSpeed    = _basePatrolSpeed    * settings.patrolSpeedMult;
        chaseSpeed     = _baseChaseSpeed     * settings.chaseSpeedMult;
        attackDamage   = Mathf.RoundToInt(_baseAttackDamage   * settings.damageMult);
        detectionRange = _baseDetectionRange * settings.detectionMult;
        attackRange    = _baseAttackRange    * settings.attackRangeMult;
        attackCooldown = _baseAttackCooldown * settings.cooldownMult;
        maxHealth      = Mathf.RoundToInt(_baseHealth * settings.healthMult);
        _currentHealth = Mathf.Min(_currentHealth, maxHealth);

        if (_agent != null && _agent.enabled)
        {
            _agent.speed = CurrentState == EnemyState.Chase ||
                           CurrentState == EnemyState.Attack
                ? chaseSpeed
                : patrolSpeed;
        }

        Debug.Log($"[EnemyAI] {gameObject.name} DDA updated → " +
                  $"HP:{_currentHealth}/{maxHealth} Dmg:{attackDamage} " +
                  $"Sight:{detectionRange:F1}m Chase:{chaseSpeed:F1}");
    }

    /// <summary>
    /// Applies an EnemyStats ScriptableObject preset to this enemy.
    /// Called in Awake so stats are set before Start() runs.
    /// </summary>
    public void ApplyStats(EnemyStats stats)
    {
        if (stats == null) return;

        maxHealth      = stats.maxHealth;
        _currentHealth = stats.maxHealth;
        patrolSpeed    = stats.patrolSpeed;
        chaseSpeed     = stats.chaseSpeed;
        detectionRange = stats.detectionRange;
        fieldOfView    = stats.fieldOfView;
        losePlayerRange= stats.loseRange;
        attackRange    = stats.attackRange;
        attackDamage   = stats.attackDamage;
        attackCooldown = stats.attackCooldown;
        dropChance     = stats.dropChance;
        isRanged          = stats.isRanged;
        preferredRange    = stats.preferredRange;
        projectilePrefab  = stats.projectilePrefab;

        _baseHealth         = maxHealth;
        _basePatrolSpeed    = patrolSpeed;
        _baseChaseSpeed     = chaseSpeed;
        _baseAttackDamage   = attackDamage;
        _baseDetectionRange = detectionRange;
        _baseAttackRange    = attackRange;
        _baseAttackCooldown = attackCooldown;

        healthBar?.Initialize(maxHealth);
        nameLabel?.RefreshName();

        Debug.Log($"[EnemyAI] {gameObject.name} loaded preset: {stats.enemyName} " +
                  $"HP:{maxHealth} Chase:{chaseSpeed} Dmg:{attackDamage}");
    }

    // ---------------------------------------------------------------
    // Utility — movement / animation helpers
    // ---------------------------------------------------------------

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 8f);
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (_animator != null)
            _animator.SetFloat(AnimSpeed, speed);
    }

    // ---------------------------------------------------------------
    // Debug — visualise detection range and FOV in Scene view
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Vector3 leftBound  = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0,  fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBound  * detectionRange);
        Gizmos.DrawRay(transform.position, rightBound * detectionRange);
    }
}
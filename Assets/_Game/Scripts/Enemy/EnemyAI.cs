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
///   5. Tune detection range, attack range, and damage in the Inspector.
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
    // Inspector — Patrol
    // ---------------------------------------------------------------

    [Header("Patrol")]
    [Tooltip("Waypoints the enemy walks between. Assign 2+ transforms.")]
    public Transform[] patrolPoints;

    [Tooltip("How long the enemy waits at each waypoint before moving on")]
    public float patrolWaitTime = 2f;

    [Tooltip("Movement speed while patrolling")]
    public float patrolSpeed = 2.5f;

    // ---------------------------------------------------------------
    // Inspector — Detection
    // ---------------------------------------------------------------

    [Header("Detection")]
    [Tooltip("Radius within which the enemy can see the player")]
    public float detectionRange = 10f;

    [Tooltip("Field of view angle — enemy only detects player within this cone")]
    public float fieldOfView = 110f;

    [Tooltip("The enemy will lose the player if they exceed this distance")]
    public float losePlayerRange = 14f;

    [Tooltip("Layer mask for line-of-sight check — set to everything except the enemy layer")]
    public LayerMask sightBlockLayers;

    // ---------------------------------------------------------------
    // Inspector — Chase
    // ---------------------------------------------------------------

    [Header("Chase")]
    [Tooltip("Movement speed while chasing the player")]
    public float chaseSpeed = 5f;

    // ---------------------------------------------------------------
    // Inspector — Attack
    // ---------------------------------------------------------------

    [Header("Attack")]
    [Tooltip("Distance at which the enemy stops and begins attacking")]
    public float attackRange = 2f;

    [Tooltip("Damage dealt per hit")]
    public int attackDamage = 10;

    [Tooltip("Seconds between each attack")]
    public float attackCooldown = 1.5f;

    // ---------------------------------------------------------------
    // Inspector — Health
    // ---------------------------------------------------------------

    [Header("Health")]
    [Tooltip("Enemy health points")]
    public int maxHealth = 50;

    // ---------------------------------------------------------------
    // Inspector — Loot
    // ---------------------------------------------------------------

    [Header("Loot")]
    [Tooltip("Prefab to spawn when this enemy dies. Assign a PickupItem prefab.")]
    public GameObject lootDropPrefab;

    [Tooltip("0 = never drops, 1 = always drops. 0.75 = 75% chance.")]
    [Range(0f, 1f)]
    public float dropChance = 0.75f;

    // ---------------------------------------------------------------
    // Inspector — Death
    // ---------------------------------------------------------------

    [Header("Death")]
    [Tooltip("Seconds before the enemy GameObject is destroyed after dying")]
    public float destroyDelay = 3f;

    // ---------------------------------------------------------------
    // Public state — readable by EnemyManager
    // ---------------------------------------------------------------

    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
    public bool IsAlive            { get; private set; } = true;

    // ---------------------------------------------------------------
    // Private — components
    // ---------------------------------------------------------------

    private NavMeshAgent    _agent;
    private Animator        _animator;      // Optional — null-checked throughout
    private PlayerController _player;

    // ---------------------------------------------------------------
    // Private — patrol
    // ---------------------------------------------------------------

    private int   _patrolIndex    = 0;
    private bool  _isWaiting      = false;
    private float _waitTimer      = 0f;

    // ---------------------------------------------------------------
    // Private — attack
    // ---------------------------------------------------------------

    private float _attackTimer    = 0f;

    // ---------------------------------------------------------------
    // Private — health
    // ---------------------------------------------------------------

    private int   _currentHealth;

    // ---------------------------------------------------------------
    // Private — return
    // ---------------------------------------------------------------

    private Vector3 _lastPatrolPosition; // Where to return to after losing player

    // ---------------------------------------------------------------
    // Animator parameter hashes — cached for performance
    // ---------------------------------------------------------------

    private static readonly int AnimSpeed    = Animator.StringToHash("Speed");
    private static readonly int AnimAttack   = Animator.StringToHash("Attack");
    private static readonly int AnimDie      = Animator.StringToHash("Die");

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>(); // Optional — may be null

        _currentHealth = maxHealth;
    }

    private void Start()
    {
        // Find the player by tag — ensure the Player GameObject has tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.GetComponent<PlayerController>();

        if (_player == null)
            Debug.LogWarning("[EnemyAI] Could not find Player. " +
                             "Make sure Player GameObject has tag 'Player'.");

        // Register with EnemyManager so it tracks kill count
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RegisterEnemy(this);

        // Start patrolling
        SetState(EnemyState.Patrol);
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        // Freeze AI while game is not Playing (pause, game over, win)
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;
        if (!IsAlive) return;

        // Tick the attack cooldown every frame
        _attackTimer -= Time.deltaTime;

        // Run the correct FSM state logic
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
        // Always scan for the player while patrolling
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

        // Check if we've reached the current waypoint
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            // Save this as a return point in case we need to come back
            _lastPatrolPosition = transform.position;

            _isWaiting = true;
            _waitTimer = patrolWaitTime;
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        _agent.speed = patrolSpeed;
        _agent.SetDestination(patrolPoints[_patrolIndex].position);

        // Advance to next waypoint, loop back to start
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

        float distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        // Player is close enough to attack
        if (distToPlayer <= attackRange)
        {
            SetState(EnemyState.Attack);
            return;
        }

        // Player escaped — return to patrol
        if (distToPlayer > losePlayerRange)
        {
            SetState(EnemyState.Return);
            return;
        }

        // Keep chasing
        _agent.speed = chaseSpeed;
        _agent.SetDestination(_player.transform.position);
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

        float distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

        // Player moved away — chase again
        if (distToPlayer > attackRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        // Face the player while attacking
        FaceTarget(_player.transform.position);

        // Stop moving while attacking
        _agent.SetDestination(transform.position);
        SetAnimatorSpeed(0f);

        // Attack on cooldown
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

        _player.TakeDamage(attackDamage);

        Debug.Log($"[EnemyAI] {gameObject.name} attacked player for {attackDamage} damage.");
    }

    // ---------------------------------------------------------------
    // FSM — Return
    // ---------------------------------------------------------------

    private void UpdateReturn()
    {
        // If the player wanders back into range, resume chase
        if (CanSeePlayer())
        {
            SetState(EnemyState.Chase);
            return;
        }

        SetAnimatorSpeed(_agent.velocity.magnitude);

        // Once we're back near the last patrol position, resume patrol
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
                _agent.ResetPath(); // Stop moving
                break;

            case EnemyState.Return:
                _agent.speed = patrolSpeed;
                // Head back to last known patrol position
                if (_lastPatrolPosition != Vector3.zero)
                    _agent.SetDestination(_lastPatrolPosition);
                break;
        }

        Debug.Log($"[EnemyAI] {gameObject.name} state → {newState}");
    }

    // ---------------------------------------------------------------
    // Detection — line-of-sight check
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns true if the player is within detection range AND field of view
    /// AND there is no obstacle blocking the line of sight.
    /// </summary>
    private bool CanSeePlayer()
    {
        if (_player == null || !_player.IsAlive) return false;

        Vector3 toPlayer = _player.transform.position - transform.position;
        float   distance = toPlayer.magnitude;

        // Outside detection range — skip
        if (distance > detectionRange) return false;

        // Outside field of view cone — skip
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        // Line-of-sight check — is there a wall in the way?
        if (Physics.Raycast(transform.position + Vector3.up * 1f,
                            toPlayer.normalized,
                            distance,
                            sightBlockLayers))
        {
            return false; // Something is blocking the view
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Health — taking damage
    // ---------------------------------------------------------------

    /// <summary>
    /// Called by WeaponController (bullet hit) when the player shoots this enemy.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);

        Debug.Log($"[EnemyAI] {gameObject.name} took {amount} damage. " +
                  $"HP: {_currentHealth}/{maxHealth}");

        // Getting hit always triggers chase if not already attacking
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

        // Disable NavMeshAgent so the corpse stops moving
        _agent.enabled = false;

        // Play death animation if available
        if (_animator != null)
            _animator.SetTrigger(AnimDie);

        // Notify EnemyManager — updates kill count, checks all-dead condition
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.DeregisterEnemy(this);

        // Roll for loot drop
        if (lootDropPrefab != null && Random.value <= dropChance)
        {
            Vector3 dropPosition = transform.position + Vector3.up * 0.2f;
            Instantiate(lootDropPrefab, dropPosition, Quaternion.identity);
            Debug.Log($"[EnemyAI] {gameObject.name} dropped loot.");
        }

        // Disable collider so bullets/raycasts don't hit the corpse
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destroy after delay to allow death animation to play
        Destroy(gameObject, destroyDelay);

        Debug.Log($"[EnemyAI] {gameObject.name} died.");
    }

    // ---------------------------------------------------------------
    // Utility
    // ---------------------------------------------------------------

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep upright — don't tilt toward a crouching player

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
        // Detection range — white sphere
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Lose player range — red sphere
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);

        // Attack range — yellow sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // FOV cone — cyan lines
        Gizmos.color = Color.cyan;
        Vector3 leftBound  = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0,  fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBound  * detectionRange);
        Gizmos.DrawRay(transform.position, rightBound * detectionRange);
    }
}
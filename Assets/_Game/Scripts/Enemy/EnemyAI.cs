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
///      Patrol Radius controls the wander area around the spawn point.
///   4. Set the Player tag to "Player" in Edit → Project Settings → Tags.
///   5. Assign an EnemyStats preset — all combat/movement stats come from there.
///   6. (Optional) Add an IEnemyAnimator component (e.g. MutantAnimatorBridge)
///      for animated enemies. Enemies without one work perfectly.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IDamageable, IEnemy
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
    [Tooltip("Radius around spawn point within which enemy picks random patrol destinations")]
    public float patrolRadius   = 10f;

    [Tooltip("How long the enemy waits at each patrol point before picking a new one")]
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

    [Tooltip("Explosion or death effect prefab — spawned the moment the enemy dies")]
    public GameObject deathEffectPrefab;

    [Tooltip("Offset from enemy position where the effect spawns")]
    public Vector3 deathEffectOffset = new Vector3(0f, 0.5f, 0f);

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
    private IEnemyAnimator   _anim;
    private PlayerController _player;

    // ---------------------------------------------------------------
    // Private — patrol
    // ---------------------------------------------------------------

    private Vector3 _spawnPoint;
    private bool    _isWaiting      = false;
    private float   _waitTimer      = 0f;
    private bool    _hasDestination = false;

    // ---------------------------------------------------------------
    // Private — attack / health / return
    // ---------------------------------------------------------------

    private float   _attackTimer     = 0f;
    private Vector3 _decoyPosition   = Vector3.zero;
    private float   _decoyTimer      = 0f;
    private bool    _isDistracted    => _decoyTimer > 0f;
    private int     _currentHealth;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        _agent         = GetComponent<NavMeshAgent>();
        _anim          = GetComponent<IEnemyAnimator>();
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
        InitFlash();
        _spawnPoint = transform.position;

        if (DifficultyManager.Instance != null)
            ApplyDifficultySettings(
                DifficultyManager.Instance.BuildSettings(
                    DifficultyManager.Instance.CurrentTier));

        SetState(EnemyState.Patrol);
        PickRandomPatrolPoint();

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

        _anim?.SetSpeed(_agent.velocity.magnitude);

        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting       = false;
                _hasDestination  = false;
            }
            return;
        }

        // Pick a new random destination if we don't have one
        if (!_hasDestination)
        {
            PickRandomPatrolPoint();
            return;
        }

        // Arrived at destination — wait before picking next
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _isWaiting      = true;
            _waitTimer      = patrolWaitTime;
            _hasDestination = false;
        }
    }

    private void PickRandomPatrolPoint()
    {
        // Try up to 8 times to find a valid NavMesh point
        // Samples around current position so enemy explores progressively
        // but clamps to patrolRadius from spawn so it never wanders the whole map
        for (int i = 0; i < 8; i++)
        {
            Vector2 rand2D  = UnityEngine.Random.insideUnitCircle * patrolRadius;
            Vector3 randPos = transform.position + new Vector3(rand2D.x, 0f, rand2D.y);

            // Clamp so enemy never goes beyond patrolRadius * 2 from spawn
            Vector3 fromSpawn = randPos - _spawnPoint;
            if (fromSpawn.magnitude > patrolRadius * 2f)
                randPos = _spawnPoint + fromSpawn.normalized * patrolRadius * 2f;

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(
                    randPos, out hit, patrolRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                _agent.speed = patrolSpeed;
                _agent.SetDestination(hit.position);
                _hasDestination = true;
                return;
            }
        }

        // Could not find valid point — wait and try again
        _isWaiting  = true;
        _waitTimer  = 1f;
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

        if (!_isDistracted && dist > losePlayerRange)
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
            // Melee enemy — move directly to player (or decoy if distracted)
            if (dist <= attackRange && !_isDistracted)
            {
                SetState(EnemyState.Attack);
                return;
            }

            _agent.speed = chaseSpeed;

            if (_isDistracted)
            {
                // Chase decoy position
                _decoyTimer -= Time.deltaTime;
                _agent.SetDestination(_decoyPosition);

                // Arrived at decoy — resume normal chase
                float decoyDist = Vector3.Distance(transform.position, _decoyPosition);
                if (decoyDist <= 1.5f || _decoyTimer <= 0f)
                {
                    _decoyTimer = 0f;
                    Debug.Log($"[EnemyAI] {gameObject.name} reached decoy — resuming chase.");
                }
            }
            else
            {
                _agent.SetDestination(_player.transform.position);
            }
        }

        _anim?.SetSpeed(_agent.velocity.magnitude);
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
        _anim?.SetSpeed(0f);

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
        // Trigger muzzle flash on drone scout bridge if present
        GetComponent<DroneScoutBridge>()?.PlayMuzzleFlash();

        _anim?.TriggerAttack();

        if (isRanged)
        {
            // Ranged attack — spawn a projectile the player can dodge
            Vector3 origin      = transform.position + Vector3.up * 0.15f;
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
            // Melee attack — delay damage to match animation hit frame
            MutantAnimatorBridge bridge = GetComponent<MutantAnimatorBridge>();
            float delay = bridge != null ? bridge.attackHitDelay : 0f;

            if (delay > 0f)
                StartCoroutine(DelayedMeleeDamage(delay));
            else
            {
                _player.TakeDamage(attackDamage);
                Debug.Log($"[EnemyAI] {gameObject.name} melee hit player for {attackDamage}.");
            }
        }
    }

    private System.Collections.IEnumerator DelayedMeleeDamage(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_player != null && _player.IsAlive && IsAlive)
        {
            _player.TakeDamage(attackDamage);
            Debug.Log($"[EnemyAI] {gameObject.name} delayed melee hit for {attackDamage}.");
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

        _anim?.SetSpeed(_agent.velocity.magnitude);

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            SetState(EnemyState.Patrol);
            PickRandomPatrolPoint();
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
                _agent.SetDestination(_spawnPoint);
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

        // Horizontal FOV check — flatten both vectors to XZ plane
        Vector3 forwardFlat  = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 toPlayerFlat = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
        float   hAngle       = Vector3.Angle(forwardFlat, toPlayerFlat);

        // Vertical angle check — how far above/below the enemy the player is
        float vAngle = Mathf.Abs(Mathf.Atan2(toPlayer.y, new Vector2(toPlayer.x, toPlayer.z).magnitude) * Mathf.Rad2Deg);

        // Outside horizontal FOV AND player is not directly above (within 60 degrees vertical)
        if (hAngle > fieldOfView * 0.5f && vAngle < 60f) return false;

        // If player is directly above (vAngle >= 60) and within close range — always detect
        if (vAngle >= 60f && distance > detectionRange * 0.5f) return false;

        // Line of sight check — only blocked by environment geometry
        if (sightBlockLayers.value != 0)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 1f,
                                toPlayer.normalized,
                                distance,
                                sightBlockLayers))
                return false;
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Health — taking damage
    // ---------------------------------------------------------------

    /// <summary>Called by AICoreManager to restore HP while core is active.</summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);

        healthBar?.UpdateHealth(_currentHealth, maxHealth);

        Debug.Log($"[EnemyAI] {gameObject.name} healed +{amount}. HP: {_currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);

        Debug.Log($"[EnemyAI] {gameObject.name} took {amount} damage. HP: {_currentHealth}/{maxHealth}");

        healthBar?.UpdateHealth(_currentHealth, maxHealth);
        healthBar?.ForceShow(); // Show briefly even at long range when hit

        // Flash red on hit
        StartCoroutine(FlashHit());

        // Chase player when damaged regardless of current state
        if (CurrentState != EnemyState.Dead && CurrentState != EnemyState.Chase && CurrentState != EnemyState.Attack)
            SetState(EnemyState.Chase);

        if (_currentHealth <= 0)
            Die();
    }

    // ---------------------------------------------------------------
    // Hit Flash
    // ---------------------------------------------------------------

    private Renderer[]          _renderers;
    private Color[]             _baseColors;
    private MaterialPropertyBlock _propBlock;
    private bool                _flashReady = false;

    private static readonly int ShaderColorID = Shader.PropertyToID("_Color");

    private void InitFlash()
    {
        _renderers  = GetComponentsInChildren<Renderer>();
        _baseColors = new Color[_renderers.Length];
        _propBlock  = new MaterialPropertyBlock();

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _baseColors[i] = _renderers[i].sharedMaterial != null
                    ? _renderers[i].sharedMaterial.color
                    : Color.white;
        }
        _flashReady = true;
    }

    private System.Collections.IEnumerator FlashHit()
    {
        if (!_flashReady) yield break;

        _propBlock.SetColor(ShaderColorID, Color.red);
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].SetPropertyBlock(_propBlock);

        yield return new WaitForSeconds(0.08f);

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _propBlock.SetColor(ShaderColorID, _baseColors[i]);
            _renderers[i].SetPropertyBlock(_propBlock);
        }
    }

    // ---------------------------------------------------------------
    // Death
    // ---------------------------------------------------------------

    // ---------------------------------------------------------------
    // Decoy distraction
    // ---------------------------------------------------------------

    /// <summary>
    /// Forces this enemy to chase a world position (the decoy).
    /// Called by DecoyDevice.EmitNoisePulse().
    /// </summary>
    public void DistractTo(Vector3 position)
    {
        if (!IsAlive || CurrentState == EnemyState.Dead) return;

        TestMetricsCollector.Instance?.RecordFSMTransition(
            gameObject.name, CurrentState.ToString(), "Chase(Decoy)");

        // Store decoy position — UpdateChase will follow it instead of player
        _decoyPosition = position;
        _decoyTimer    = 8f; // Follow decoy for up to 8s

        _agent.speed = chaseSpeed;
        _agent.SetDestination(position);
        SetState(EnemyState.Chase);

        Debug.Log($"[EnemyAI] {gameObject.name} distracted to {position}");
    }

    private void Die()
    {
        ScoreManager.Instance?.ReportKill(gameObject.name);
        if (!IsAlive) return;

        IsAlive = false;
        SetState(EnemyState.Dead);
        _agent.enabled = false;

        _anim?.TriggerDie();

        EnemyManager.Instance?.DeregisterEnemy(this);

        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            Vector3    effectPos = transform.position + deathEffectOffset;
            GameObject effect    = Instantiate(deathEffectPrefab, effectPos, Quaternion.identity);

            // Auto-destroy effect after 3s if it doesn't self-destruct
            Destroy(effect, 3f);
        }

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
    // Utility — movement helpers
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
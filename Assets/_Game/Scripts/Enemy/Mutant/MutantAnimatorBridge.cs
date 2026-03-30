using UnityEngine;

/// <summary>
/// MutantAnimatorBridge — maps EnemyAI FSM states to Mutant animations.
/// Attach alongside EnemyAI on the Mutant prefab root.
///
/// Animator Controller needs these parameters:
///   Speed    (Float)   — controls Walk/Run blend
///   IsChasing (Bool)   — switches Walk to Run
///   Attack   (Trigger) — plays attack animation
///   Die      (Trigger) — plays death animation
///
/// Setup:
///   1. Attach to Mutant root GameObject (same as EnemyAI)
///   2. Assign the Animator component in Inspector
///   3. The bridge reads EnemyAI.CurrentState each frame
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class MutantAnimatorBridge : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Animator Parameter Names")]
    public string paramSpeed     = "Speed";
    public string paramIsChasing = "IsChasing";
    public string paramAttack    = "Attack";
    public string paramDie       = "Die";

    [Header("Attack Variation")]
    [Tooltip("Second attack trigger name — randomly chosen for variety")]
    public string paramAttack2   = "Attack2";

    [Tooltip("Chance to play Attack2 instead of Attack (0-1)")]
    [Range(0f, 1f)]
    public float attack2Chance   = 0.4f;

    [Header("Attack Timing")]
    [Tooltip("Seconds after attack animation starts before damage is applied. " +
             "Match this to the frame where the punch/swipe connects visually. " +
             "Typical Mixamo punch = 0.3s, swipe = 0.4s")]
    public float attackHitDelay  = 0.35f;

    // ---------------------------------------------------------------
    private EnemyAI              _ai;
    private EnemyAI.EnemyState   _lastState;
    private UnityEngine.AI.NavMeshAgent _agent;

    private void Awake()
    {
        _ai    = GetComponent<EnemyAI>();
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_ai == null || animator == null) return;

        EnemyAI.EnemyState state = _ai.CurrentState;

        // Update speed float — drives Walk vs Run blend
        float speed = _agent != null ? _agent.velocity.magnitude : 0f;
        animator.SetFloat(paramSpeed, speed, 0.1f, Time.deltaTime);

        // Update IsChasing bool
        bool chasing = state == EnemyAI.EnemyState.Chase;
        animator.SetBool(paramIsChasing, chasing);

        // Detect state transitions
        if (state == _lastState) return;

        EnemyAI.EnemyState prev = _lastState;
        _lastState = state;

        switch (state)
        {
            case EnemyAI.EnemyState.Attack:
                // Randomly pick attack variation
                if (Random.value < attack2Chance && HasParam(paramAttack2))
                    animator.SetTrigger(paramAttack2);
                else
                    animator.SetTrigger(paramAttack);
                break;

            case EnemyAI.EnemyState.Dead:
                animator.SetTrigger(paramDie);
                break;
        }
    }

    // ---------------------------------------------------------------
    // Check if parameter exists — prevents errors if not set up yet
    // ---------------------------------------------------------------
    private bool HasParam(string paramName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }
}
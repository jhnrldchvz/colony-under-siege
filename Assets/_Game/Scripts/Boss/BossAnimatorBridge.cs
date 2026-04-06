using System;
using UnityEngine;

/// <summary>
/// Maps BossController states to Animator parameters.
///
/// Required Animator parameters:
///   Speed   (Float)   — walk/run blend
///   Attack  (Trigger) — melee swing
///   Attack2 (Trigger) — alternate swing (optional)
///   Jump    (Trigger) — leap animation
///   Heal    (Trigger) — self-heal animation
///   Die     (Trigger) — death
/// </summary>
[RequireComponent(typeof(BossController))]
public class BossAnimatorBridge : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Parameter Names")]
    public string paramSpeed   = "Speed";
    public string paramAttack  = "Attack";
    public string paramAttack2 = "Attack2";
    public string paramJump    = "Jump";
    public string paramHeal    = "Heal";
    public string paramDie     = "Die";

    [Header("Attack Variation")]
    [Range(0f, 1f)]
    public float attack2Chance = 0.35f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void SetSpeed(float speed)
    {
        animator?.SetFloat(paramSpeed, speed, 0.1f, Time.deltaTime);
    }

    public void TriggerAttack()
    {
        if (animator == null) return;
        string param = UnityEngine.Random.value < attack2Chance && HasParam(paramAttack2) ? paramAttack2 : paramAttack;
        animator.SetTrigger(param);
    }

    public void TriggerJump()
    {
        if (HasParam(paramJump))
            animator.SetTrigger(paramJump);
    }

    public void TriggerHeal()
    {
        if (HasParam(paramHeal))
            animator.SetTrigger(paramHeal);
    }

    /// <summary>
    /// Returns the length of the first animation clip whose name contains <paramref name="keyword"/>.
    /// Returns <paramref name="fallback"/> if no match is found.
    /// </summary>
    public float GetClipLength(string keyword, float fallback = 2f)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return fallback;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return clip.length;

        return fallback;
    }

    public void TriggerDie()
    {
        animator?.SetTrigger(paramDie);
    }

    private bool HasParam(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }
}

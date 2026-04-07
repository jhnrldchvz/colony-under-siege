using UnityEngine;

/// <summary>
/// DamageRelay — attach to any child GameObject that has a collider.
/// Forwards damage to the IDamageable on the root or a specified target.
///
/// Use this when:
///   - An enemy has multiple child colliders (body parts, robot limbs)
///   - A crate has child mesh colliders that aren't direct children
///   - Any object where IDamageable is on the root but hits land on children
///
/// Setup:
///   1. Add this to every child collider GameObject.
///   2. Leave "damageable Target" empty — it auto-finds the root IDamageable.
///      OR drag a specific target manually.
/// </summary>
public class DamageRelay : MonoBehaviour, IDamageable
{
    [Tooltip("The IDamageable to forward damage to. " +
             "Leave empty to auto-find in parent hierarchy.")]
    public MonoBehaviour damageableTarget;

    private IDamageable _target;

    private void Awake()
    {
        // Use manually assigned target first
        if (damageableTarget != null)
        {
            _target = damageableTarget as IDamageable;
            return;
        }

        // Check current GO first, then walk up the hierarchy
        Transform t = transform;
        while (t != null)
        {
            // Skip self — avoid infinite loop (DamageRelay itself implements IDamageable)
            if (t != transform)
            {
                _target = t.GetComponent<IDamageable>();
                if (_target != null) break;
            }
            else
            {
                // On self: only find non-DamageRelay IDamageable (e.g. EnemyAI on same GO)
                foreach (var comp in t.GetComponents<IDamageable>())
                {
                    if (comp is DamageRelay) continue;
                    _target = comp;
                    break;
                }
                if (_target != null) break;
            }
            t = t.parent;
        }

        if (_target == null)
            Debug.LogWarning($"[DamageRelay] No IDamageable found in parents of '{gameObject.name}'");
    }

    // ---------------------------------------------------------------
    // IDamageable — forward to root
    // ---------------------------------------------------------------

    public bool IsAlive => _target?.IsAlive ?? false;

    public void TakeDamage(int amount)
    {
        _target?.TakeDamage(amount);
    }
}
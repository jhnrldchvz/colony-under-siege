using UnityEngine;

/// <summary>
/// EnemyProjectile — moves in a straight line and damages the player on hit.
/// Attach to a small sphere prefab. The Scout spawns this on each ranged attack.
///
/// Setup:
///   1. Create a Sphere → scale (0.15, 0.15, 0.15) → rename EnemyProjectile
///   2. Add a Rigidbody — Is Kinematic: ON, Use Gravity: OFF
///   3. Add a Sphere Collider — Is Trigger: ON
///   4. Attach this script
///   5. Create a green emissive material → drag onto sphere
///   6. Save as prefab in Assets/_Game/Prefabs/
///   7. Assign to Stats_Scout → Projectile Prefab slot (once EnemyStats is updated)
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed       = 12f;   // Units per second
    public float maxLifetime = 4f;    // Destroys after this many seconds
    public int   damage      = 12;    // Set by the firing enemy at spawn time

    private Vector3    _direction;
    private float      _lifetime = 0f;
    private GameObject _owner;    // The enemy that fired this — ignored on collision

    // ---------------------------------------------------------------
    // Called by EnemyAI after Instantiate to set direction and damage
    // ---------------------------------------------------------------

    public void Init(Vector3 direction, int dmg, GameObject owner = null)
    {
        _direction = direction.normalized;
        damage     = dmg;
        _owner     = owner;
    }

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Update()
    {
        // Move forward each frame
        transform.position += _direction * speed * Time.deltaTime;

        // Self-destruct after max lifetime
        _lifetime += Time.deltaTime;
        if (_lifetime >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore the enemy that fired this projectile
        if (_owner != null && (other.gameObject == _owner ||
            other.transform.IsChildOf(_owner.transform))) return;

        // Ignore all enemies — only hurts the player
        if (other.GetComponent<EnemyAI>() != null)             return;
        if (other.GetComponentInParent<EnemyAI>() != null)     return;

        // Ignore triggers (pickups, canvases, etc.)
        if (other.isTrigger)                                    return;

        // Hit player
        PlayerController player = other.GetComponent<PlayerController>() ??
                                  other.GetComponentInParent<PlayerController>();
        if (player != null && player.IsAlive)
        {
            player.TakeDamage(damage);
            Debug.Log($"[EnemyProjectile] Hit player for {damage} damage.");
            Destroy(gameObject);
            return;
        }

        // Hit a wall or solid obstacle
        Debug.Log($"[EnemyProjectile] Blocked by: {other.gameObject.name}");
        Destroy(gameObject);
    }
}
using UnityEngine;

/// <summary>
/// EnemyProjectile — fired by Scout Drone. Uses raycast for hit detection
/// instead of trigger/collider overlap — most reliable method in Unity.
///
/// Setup:
///   1. Create Sphere → scale (0.12, 0.12, 0.12)
///   2. Add Rigidbody — Is Kinematic: ON, Use Gravity: OFF
///   3. NO Collider needed — raycast handles detection
///   4. Assign impactEffect (War FX hit spark prefab)
///   5. Save as prefab → assign to EnemyStats.projectilePrefab
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed       = 16f;
    public float maxLifetime = 5f;

    [Header("Combat")]
    public int   damage      = 12;

    [Header("Effects")]
    public GameObject impactEffect;
    public Light      projectileLight;

    // ---------------------------------------------------------------
    private Vector3    _direction;
    private float      _lifetime  = 0f;
    private GameObject _owner;
    private bool       _dead      = false;

    // ---------------------------------------------------------------
    public void Init(Vector3 direction, int dmg, GameObject owner = null)
    {
        _direction = direction.normalized;
        damage     = dmg;
        _owner     = owner;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (_dead) return;

        float stepDist = speed * Time.deltaTime;

        // Raycast ahead each frame — catches any collider in path
        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, stepDist + 0.1f))
        {
            // Ignore owner and its children
            if (_owner != null && (hit.collider.gameObject == _owner ||
                hit.collider.transform.IsChildOf(_owner.transform)))
            {
                MoveStep(stepDist);
                return;
            }

            // Ignore other enemies
            if (hit.collider.GetComponent<IEnemy>() != null ||
                hit.collider.GetComponentInParent<IEnemy>() != null)
            {
                MoveStep(stepDist);
                return;
            }

            // Walk hierarchy for IDamageable — handles player, crates, any damageable
            IDamageable target = hit.collider.GetComponent<IDamageable>()
                              ?? hit.collider.GetComponentInParent<IDamageable>();

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage);
                Debug.Log($"[DroneProjectile] Hit '{hit.collider.name}' for {damage} dmg.");
            }
            else
            {
                Debug.Log($"[DroneProjectile] Hit wall: {hit.collider.name}");
            }

            SpawnImpact(hit.point);
            Kill();
            return;
        }

        MoveStep(stepDist);

        _lifetime += Time.deltaTime;
        if (_lifetime >= maxLifetime)
        {
            SpawnImpact(transform.position);
            Kill();
        }
    }

    private void MoveStep(float dist)
    {
        transform.position += _direction * dist;
    }

    private void SpawnImpact(Vector3 pos)
    {
        if (impactEffect == null) return;
        GameObject fx = Instantiate(impactEffect, pos, Quaternion.identity);
        Destroy(fx, 2f);
    }

    private void Kill()
    {
        _dead = true;
        Destroy(gameObject);
    }
}
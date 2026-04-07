using System.Collections;
using UnityEngine;

/// <summary>
/// ExplosiveBarrel — a destructible object that explodes when its health drops to zero.
///
/// Features:
///   - Takes damage from any weapon via IDamageable (bullets, projectiles, other barrels).
///   - Deals area-of-effect damage to ALL IDamageable targets within the explosion radius
///     (enemies, player, other barrels, destructible crates).
///   - Damage falls off linearly from full at the center to zero at the edge.
///   - Triggers CameraShake.Instance on explosion.
///   - Plays a 3D positional explosion sound via SFXManager.
///   - Flashes red on bullet impact.
///   - Gizmo shows the explosion radius in the Scene view.
///
/// Setup:
///   1. Create a barrel GameObject with a Collider (non-trigger, so bullets can hit it).
///   2. Attach this script.
///   3. Assign an explosionVFX particle prefab (optional).
///   4. Attach CameraShake to the cameraHolder child on the Player.
///   5. Assign explosion clips to SFXManager.explosionSounds in the Inspector.
///   6. Save as a prefab in Assets/_Game/Prefabs/
/// </summary>
public class ExplosiveBarrel : MonoBehaviour, IDamageable
{
    // ---------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------

    [Header("Health")]
    [Tooltip("How many hit points the barrel has before exploding")]
    public int maxHealth = 30;

    [Header("Explosion")]
    [Tooltip("World-space radius that receives area damage")]
    public float explosionRadius = 5f;

    [Tooltip("Maximum damage dealt at the center of the explosion")]
    public int   explosionDamage = 60;

    [Tooltip("When true, damage scales from full at center to zero at radius edge")]
    public bool  linearFalloff   = true;

    [Header("Camera Shake")]
    public float shakeDuration  = 0.45f;
    public float shakeMagnitude = 0.28f;

    [Header("Effects")]
    [Tooltip("Particle system prefab spawned at the barrel's position on explosion")]
    public GameObject explosionVFX;

    [Tooltip("Small hit spark prefab spawned on each bullet impact (optional)")]
    public GameObject hitEffect;

    [Header("Visuals")]
    [Tooltip("Renderer to flash red on hit — auto-found in children if left empty")]
    public Renderer barrelRenderer;

    // ---------------------------------------------------------------
    // IDamageable
    // ---------------------------------------------------------------

    public bool IsAlive { get; private set; } = true;

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private int      _currentHealth;
    private Color    _baseColor;
    private Material _mat;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        _currentHealth = maxHealth;

        if (barrelRenderer == null)
            barrelRenderer = GetComponentInChildren<Renderer>();

        if (barrelRenderer != null)
        {
            _mat       = barrelRenderer.material;
            _baseColor = _mat.color;
        }
    }

    // ---------------------------------------------------------------
    // IDamageable
    // ---------------------------------------------------------------

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        Debug.Log($"[ExplosiveBarrel] Hit for {amount}. HP: {_currentHealth}/{maxHealth}");

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        if (_mat != null)
            StartCoroutine(FlashHit());

        if (_currentHealth <= 0)
            Explode();
    }

    // ---------------------------------------------------------------
    // Explosion
    // ---------------------------------------------------------------

    private void Explode()
    {
        if (!IsAlive) return;
        IsAlive = false;

        Debug.Log($"[ExplosiveBarrel] {gameObject.name} exploded at {transform.position}");

        // --- VFX ---
        if (explosionVFX != null)
        {
            GameObject fx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            Destroy(fx, 5f);
        }

        // --- Camera shake ---
        CameraShake.Instance?.Shake(shakeDuration, shakeMagnitude);

        // --- 3D positional SFX ---
        SFXManager.Instance?.PlayExplosionAt(transform.position);

        // --- Area damage ---
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in hits)
        {
            // Walk hierarchy — handles enemies, player, crates, other barrels
            IDamageable target = col.GetComponent<IDamageable>()
                              ?? col.GetComponentInParent<IDamageable>();

            // Skip null, dead, and self
            if (target == null || !target.IsAlive || ReferenceEquals(target, this))
                continue;

            int damage = explosionDamage;
            if (linearFalloff)
            {
                float dist   = Vector3.Distance(transform.position, col.transform.position);
                float factor = 1f - Mathf.Clamp01(dist / explosionRadius);
                damage       = Mathf.RoundToInt(explosionDamage * factor);
            }

            if (damage > 0)
                target.TakeDamage(damage);
        }

        // --- Disable self immediately so nothing can hit us while VFX plays ---
        Collider self = GetComponent<Collider>();
        if (self != null)           self.enabled           = false;
        if (barrelRenderer != null) barrelRenderer.enabled = false;

        Destroy(gameObject, 0.1f);
    }

    // ---------------------------------------------------------------
    // Hit flash
    // ---------------------------------------------------------------

    private IEnumerator FlashHit()
    {
        if (_mat == null) yield break;
        _mat.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (_mat != null) _mat.color = _baseColor;
    }

    // ---------------------------------------------------------------
    // Editor gizmos — shows explosion radius in Scene view
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.18f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.35f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

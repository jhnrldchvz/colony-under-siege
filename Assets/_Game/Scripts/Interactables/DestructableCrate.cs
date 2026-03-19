using System.Collections;
using UnityEngine;

/// <summary>
/// DestructibleCrate — a breakable object that implements IDamageable.
/// Works with any weapon or projectile that uses the IDamageable interface.
///
/// Setup:
///   1. Create a cube or import a crate model.
///   2. Add a Box Collider (non-trigger).
///   3. Attach this script.
///   4. Optionally assign a destroyEffect prefab (explosion, debris).
///   5. Optionally assign a hitEffect prefab (spark, dent flash).
///   6. Save as prefab in Assets/_Game/Prefabs/
/// </summary>
public class DestructibleCrate : MonoBehaviour, IDamageable
{
    // ---------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------

    [Header("Stats")]
    public int   maxHp          = 30;

    [Header("Effects")]
    [Tooltip("Spawned when crate is destroyed — e.g. WFX_Explosion Small")]
    public GameObject destroyEffect;

    [Tooltip("Spawned on each hit — e.g. spark or impact particle")]
    public GameObject hitEffect;

    [Tooltip("Seconds before destroyed GameObject is cleaned up")]
    public float destroyDelay   = 0.1f;

    [Header("Visuals")]
    [Tooltip("Flash red briefly when hit")]
    public bool flashOnHit      = true;

    [Tooltip("Renderer to flash — auto-found if left empty")]
    public Renderer crateRenderer;

    [Header("Loot (optional)")]
    [Tooltip("Prefab to drop when destroyed — e.g. AmmoPickup or FoodPickup")]
    public GameObject lootPrefab;

    [Range(0f, 1f)]
    [Tooltip("Chance (0–1) of dropping loot on destroy")]
    public float lootDropChance = 0.5f;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private int      _hp;
    private bool     _dead       = false;
    private Color    _baseColor;
    private Material _mat;

    // ---------------------------------------------------------------
    // IDamageable
    // ---------------------------------------------------------------

    public bool IsAlive => !_dead;

    public void TakeDamage(int amount)
    {
        if (_dead) return;

        _hp = Mathf.Max(0, _hp - amount);
        Debug.Log($"[Crate] Hit for {amount}. HP: {_hp}/{maxHp}");

        // Spawn hit effect
        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        // Flash red
        if (flashOnHit) StartCoroutine(FlashHit());

        if (_hp <= 0) Break();
    }

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        _hp = maxHp;

        // Cache renderer for flash effect
        if (crateRenderer == null)
            crateRenderer = GetComponentInChildren<Renderer>();

        if (crateRenderer != null)
        {
            _mat       = crateRenderer.material;
            _baseColor = _mat.color;
        }
    }

    // ---------------------------------------------------------------
    // Break
    // ---------------------------------------------------------------

    private void Break()
    {
        if (_dead) return;
        _dead = true;

        Debug.Log($"[Crate] {gameObject.name} destroyed.");

        // Spawn destruction effect
        if (destroyEffect != null)
        {
            GameObject fx = Instantiate(
                destroyEffect, transform.position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        // Drop loot
        if (lootPrefab != null && Random.value <= lootDropChance)
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.5f;
            Instantiate(lootPrefab, dropPos, Quaternion.identity);
        }

        // Disable collider and renderer immediately
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (crateRenderer != null) crateRenderer.enabled = false;

        // Destroy after short delay so effects play
        Destroy(gameObject, destroyDelay);
    }

    // ---------------------------------------------------------------
    // Flash effect
    // ---------------------------------------------------------------

    private IEnumerator FlashHit()
    {
        if (_mat == null) yield break;

        _mat.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (_mat != null) _mat.color = _baseColor;
    }

    // ---------------------------------------------------------------
    // Gizmos — show HP in scene view
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
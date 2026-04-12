using System.Collections;
using UnityEngine;

/// <summary>
/// DecoyDevice — throwable device that distracts enemies.
/// When it lands, it emits a noise pulse that forces nearby enemies
/// into Chase state toward the decoy instead of the player.
/// Directly demonstrates FSM state manipulation for thesis.
///
/// Setup:
///   1. Create a small sphere prefab → add Rigidbody, SphereCollider
///   2. Add this script
///   3. Assign to DecoyLauncher.decoyPrefab
///   4. Save as prefab in Assets/_Game/Prefabs/
/// </summary>
public class DecoyDevice : MonoBehaviour
{
    [Header("Settings")]
    public float activationDelay  = 0.5f;  // Seconds after landing before activating
    public float distractRadius   = 12f;   // How far enemies are distracted
    public float distractDuration = 6f;    // How long decoy stays active
    public float pulsInterval     = 1.5f;  // Seconds between noise pulses

    [Header("Effects")]
    public GameObject activateEffect;  // VFX on activation — e.g. WFX spark
    public GameObject pulseEffect;     // VFX on each pulse
    public Light      decoyLight;      // Optional point light — flickers on pulse

    // ---------------------------------------------------------------
    private Rigidbody    _rb;
    private bool         _landed      = false;
    private bool         _active      = false;
    private float        _timer       = 0f;
    private float        _pulseTimer  = 0f;
    private GameObject   _smokeInstance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_landed) return;
        _landed = true;

        // Stick to surface — stop physics
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic    = true;

        StartCoroutine(ActivateAfterDelay());
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(activationDelay);

        _active = true;
        _timer  = distractDuration;

        if (activateEffect != null)
        {
            _smokeInstance = Instantiate(activateEffect, transform.position, Quaternion.identity);

            // Remove auto-destruct so smoke lasts full decoy duration
            CFX_AutoDestructShuriken autoDestruct =
                _smokeInstance.GetComponent<CFX_AutoDestructShuriken>();
            if (autoDestruct != null) Destroy(autoDestruct);
        }

        if (decoyLight != null) decoyLight.enabled = true;

        Debug.Log($"[Decoy] Activated at {transform.position}. " +
                  $"Radius: {distractRadius}m, Duration: {distractDuration}s");

        // Immediate first pulse
        EmitNoisePulse();
    }

    private void Update()
    {
        if (!_active) return;

        _timer      -= Time.deltaTime;
        _pulseTimer -= Time.deltaTime;

        if (_pulseTimer <= 0f)
        {
            _pulseTimer = pulsInterval;
            EmitNoisePulse();
        }

        if (_timer <= 0f)
        {
            _active = false;
            Debug.Log("[Decoy] Expired.");

            // Stop smoke gracefully — let particles fade then destroy
            if (_smokeInstance != null)
            {
                ParticleSystem ps = _smokeInstance.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop();
                Destroy(_smokeInstance, 2f);
                _smokeInstance = null;
            }

            Destroy(gameObject, 0.5f);
        }
    }

    // ---------------------------------------------------------------
    // Noise pulse — forces nearby enemies to Chase toward this decoy
    // ---------------------------------------------------------------
    public void EmitNoisePulse()
    {
        if (pulseEffect != null)
        {
            GameObject fx = Instantiate(pulseEffect, transform.position, Quaternion.identity);
            Destroy(fx, 1f);
        }

        if (decoyLight != null)
            StartCoroutine(FlickerLight());

        // Use FindObjectsByType to get ALL EnemyAI in scene
        // OverlapSphere misses enemies whose root is outside the sphere
        // but child colliders are inside — this is more reliable
        EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        int distracted = 0;

        foreach (EnemyAI enemy in allEnemies)
        {
            if (!enemy.IsAlive) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > distractRadius) continue;

            enemy.DistractTo(transform.position);
            distracted++;

            Debug.Log($"[Decoy] Distracted: {enemy.name} at {dist:F1}m");
        }

        Debug.Log($"[Decoy] Pulse — {distracted}/{allEnemies.Length} enemies distracted " +
                  $"within {distractRadius}m of decoy at {transform.position}");

        TestMetricsCollector.Instance?.RecordFSMTransition(
            "DecoyDevice", "Pulse", $"Distracted {distracted} enemies");
    }

    private IEnumerator FlickerLight()
    {
        if (decoyLight == null) yield break;
        float original     = decoyLight.intensity;
        decoyLight.intensity = original * 2f;
        yield return new WaitForSeconds(0.1f);
        decoyLight.intensity = original;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, distractRadius);
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, distractRadius);
    }
}
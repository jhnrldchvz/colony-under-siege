using System.Collections;
using UnityEngine;

/// <summary>
/// DroneScoutBridge — visual controller for the Scout Drone enemy.
/// Handles hover animation, body rotation, and muzzle flash.
///
/// Setup:
///   1. Attach to Scout Drone root alongside EnemyAI
///   2. Assign droneBody (the visual mesh Transform)
///   3. Assign muzzlePoint (empty Transform at gun barrel tip)
///   4. Assign muzzleFlashFX (War FX muzzle prefab)
///   5. Set NavMeshAgent → Base Offset to match drone hover height
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class DroneScoutBridge : MonoBehaviour
{
    [Header("References")]
    public Transform  droneBody;      // Visual mesh — rotates to face player
    public Transform  muzzlePoint;    // Projectile spawn + muzzle flash position
    public GameObject muzzleFlashFX;  // War FX muzzle prefab

    [Header("Hover")]
    [Tooltip("How far the drone bobs up and down")]
    public float hoverAmplitude   = 0.18f;
    [Tooltip("Speed of the hover cycle")]
    public float hoverFrequency   = 1.2f;
    [Tooltip("How smoothly the hover blends")]
    public float hoverSmoothSpeed = 6f;

    [Header("Body Rotation")]
    [Tooltip("How fast the drone body rotates to face the player")]
    public float rotateSpeed = 5f;

    // ---------------------------------------------------------------
    private EnemyAI   _ai;
    private float     _hoverTimer;
    private float     _baseY;        // Y position the drone hovers around
    private Transform _player;

    private void Awake()
    {
        _ai = GetComponent<EnemyAI>();
    }

    private void Start()
    {
        // Capture base Y from current world position
        _baseY      = transform.position.y;
        _hoverTimer = Random.Range(0f, Mathf.PI * 2f);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        if (_ai == null) return;
        UpdateHover();
        UpdateBodyRotation();
    }

    // ---------------------------------------------------------------
    // Hover — independent Y-axis sine wave bob
    // NavMeshAgent controls X/Z, we control Y only
    // ---------------------------------------------------------------
    private void UpdateHover()
    {
        _hoverTimer += Time.deltaTime * hoverFrequency;

        // Target Y position is base height + sine wave offset
        float targetY = _baseY + Mathf.Sin(_hoverTimer) * hoverAmplitude;

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * hoverSmoothSpeed);
        transform.position = pos;

        // Track X/Z movement from NavMeshAgent — keep base Y stable
        _baseY = Mathf.Lerp(_baseY, _baseY, Time.deltaTime);
    }

    // ---------------------------------------------------------------
    // Body rotation — smoothly face player on Y axis only
    // ---------------------------------------------------------------
    private void UpdateBodyRotation()
    {
        if (droneBody == null || _player == null) return;
        if (_ai.CurrentState == EnemyAI.EnemyState.Patrol) return;

        Vector3 dir = _player.position - droneBody.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion target = Quaternion.LookRotation(dir);
        droneBody.rotation = Quaternion.Slerp(
            droneBody.rotation, target, Time.deltaTime * rotateSpeed);
    }

    // ---------------------------------------------------------------
    // Muzzle flash — called by EnemyAI.PerformAttack()
    // ---------------------------------------------------------------
    public void PlayMuzzleFlash()
    {
        if (muzzleFlashFX == null || muzzlePoint == null) return;
        StartCoroutine(SpawnFlash());
    }

    private IEnumerator SpawnFlash()
    {
        GameObject fx = Instantiate(muzzleFlashFX, muzzlePoint.position,
                                    muzzlePoint.rotation);
        Destroy(fx, 0.15f);
        yield return null;
    }
}
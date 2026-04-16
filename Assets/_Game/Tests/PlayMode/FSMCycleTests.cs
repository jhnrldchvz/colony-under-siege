using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// FSMCycleTests — PlayMode integration tests for the EnemyAI finite-state machine (Grunt only).
///
/// Covers Option B:
///   B1 — Enemy starts in Patrol and transitions to Chase when player enters detection range (Whitebox)
///   B2 — Enemy transitions to Attack when player enters attack range (Whitebox)
///   B3 — Enemy returns to Chase when player exits attack range (Whitebox)
///   B4 — Enemy loses player and returns to Patrol when player moves far away (Blackbox)
///   B5 — Enemy stays engaged (Chase or Attack) while player remains nearby (Blackbox)
///
/// Setup notes:
///   • sightBlockLayers is set to 0 in SetUp — the prefab uses Default layer (bit 0),
///     which would block the LOS raycast against the player's CharacterController capsule.
///     Clearing it skips the raycast and relies on distance + FOV only.
///   • A Player-tagged object with PlayerController is required — EnemyAI caches _player
///     via GameObject.FindWithTag("Player") in Start().
///   • LogAssert.ignoreFailingMessages suppresses NavMesh errors (no baked NavMesh in test scene).
///
/// Grunt stats (Stats_Grunt.asset):
///   detectionRange = 30 m   attackRange = 1.8 m   loseRange = 40 m   fieldOfView = 90°
/// </summary>
public class FSMCycleTests
{
    private GameObject _enemyGO;
    private GameObject _playerGO;
    private EnemyAI    _enemy;

    // ── SetUp / TearDown ──────────────────────────────────────────────────────

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;

        // Player must exist before EnemyAI.Start() runs so _player is cached correctly
        _playerGO                       = new GameObject("TestPlayer");
        _playerGO.tag                   = "Player";
        _playerGO.transform.position    = new Vector3(0f, 0f, 1000f); // start far away
        _playerGO.AddComponent<PlayerController>();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/Enemy/Enemy_Grunt.prefab");
        Assert.IsNotNull(prefab, "Enemy_Grunt prefab not found.");

        _enemyGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _enemy   = _enemyGO.GetComponent<EnemyAI>();

        // Default layer mask (bit 0) is set in the prefab — clears so the LOS
        // raycast is skipped and detection relies on distance + FOV only
        _enemy.sightBlockLayers = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (_enemyGO  != null) Object.Destroy(_enemyGO);
        if (_playerGO != null) Object.Destroy(_playerGO);
    }

    // =========================================================================
    // B1 — Patrol → Chase transition (Whitebox)
    // Uses known detectionRange = 30 m and fieldOfView = 90°
    // =========================================================================

    [UnityTest]
    [Category("Whitebox")]
    public IEnumerator B1_Enemy_StartsInPatrol_WhenPlayerIsFarAway()
    {
        // Player is at 1000 m — well beyond detectionRange (30 m) → stays Patrol
        yield return new WaitForSeconds(0.1f);

        Assert.AreEqual(EnemyAI.EnemyState.Patrol, _enemy.CurrentState,
            "Enemy must start in Patrol when the player is outside detection range.");
    }

    [UnityTest]
    [Category("Whitebox")]
    public IEnumerator B1_Enemy_TransitionsToChase_WhenPlayerEntersDetectionRange()
    {
        // +Z is directly in front of the enemy (within FOV 90°), 5 m away — inside detectionRange (30 m)
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(EnemyAI.EnemyState.Chase, _enemy.CurrentState,
            "Enemy must enter Chase when the player moves inside detection range (30 m) and FOV.");
    }

    // =========================================================================
    // B2 — Chase → Attack transition (Whitebox)
    // Uses known attackRange = 1.8 m
    // =========================================================================

    [UnityTest]
    [Category("Whitebox")]
    public IEnumerator B2_Enemy_TransitionsToAttack_WhenPlayerEntersAttackRange()
    {
        yield return null;   // ensure Start() has run

        // Step 1: trigger Chase (5 m — inside detectionRange)
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);
        yield return new WaitForSeconds(0.6f);

        // Step 2: move player inside attackRange (1.8 m)
        _playerGO.transform.position = new Vector3(0f, 0f, 1f);
        yield return new WaitForSeconds(0.6f);

        Assert.AreEqual(EnemyAI.EnemyState.Attack, _enemy.CurrentState,
            "Enemy must enter Attack when the player is within attackRange (1.8 m).");
    }

    // =========================================================================
    // B3 — Attack → Chase transition (Whitebox)
    // Uses known attackRange = 1.8 m and loseRange = 40 m
    // =========================================================================

    [UnityTest]
    [Category("Whitebox")]
    public IEnumerator B3_Enemy_ReturnsToChase_WhenPlayerExitsAttackRange()
    {
        yield return null;   // ensure Start() has run

        // Step 1: get into Attack state (player at 1 m — inside attackRange 1.8 m)
        _playerGO.transform.position = new Vector3(0f, 0f, 1f);
        yield return new WaitForSeconds(0.6f);

        // Step 2: move player outside attackRange (1.8 m) but inside loseRange (40 m)
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);
        yield return new WaitForSeconds(0.6f);

        Assert.AreEqual(EnemyAI.EnemyState.Chase, _enemy.CurrentState,
            "Enemy must return to Chase when player exits attackRange (1.8 m) but stays within loseRange (40 m).");
    }

    // =========================================================================
    // B4 — Enemy loses player and returns to Patrol (Blackbox)
    // No internal threshold values assumed — uses a "clearly out of range" distance
    // =========================================================================

    [UnityTest]
    [Category("Blackbox")]
    public IEnumerator B4_Enemy_LosesPlayer_AndReturnsToPatrol()
    {
        // Pre-condition: trigger Chase
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(EnemyAI.EnemyState.Chase, _enemy.CurrentState,
            "Pre-condition failed: enemy must enter Chase before the blackbox phase.");

        // Move player clearly out of any reasonable engagement range
        _playerGO.transform.position = new Vector3(0f, 0f, 200f);
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(EnemyAI.EnemyState.Patrol, _enemy.CurrentState,
            "Enemy must return to Patrol when the player moves clearly out of range.");
    }

    // =========================================================================
    // B5 — Enemy stays engaged while player remains nearby (Blackbox)
    // No internal threshold values assumed
    // =========================================================================

    [UnityTest]
    [Category("Blackbox")]
    public IEnumerator B5_Enemy_StaysEngaged_WhilePlayerRemainsNearby()
    {
        // Place player close and wait long enough for the FSM to settle
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);
        yield return new WaitForSeconds(1.0f);

        // Both Chase and Attack are valid "engaged" states — the FSM must not drift to Patrol
        bool isEngaged = _enemy.CurrentState == EnemyAI.EnemyState.Chase ||
                         _enemy.CurrentState == EnemyAI.EnemyState.Attack;

        Assert.IsTrue(isEngaged,
            $"Enemy must remain engaged (Chase or Attack) while player is nearby. " +
            $"Got: {_enemy.CurrentState}");
    }
}

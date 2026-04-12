using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 8 — Full 3-State FSM Cycle
// Player is placed on the +Z axis so it is directly in front of the enemy
// (enemies spawn facing +Z by default, FOV half-angle = 55 deg).

public class FSMCycleTests
{
    private GameObject _enemyGO;
    private GameObject _playerGO;
    private EnemyAI    _enemy;

    [SetUp]
    public void SetUp()
    {
        // Suppress NavMesh errors — no NavMesh in bare test scene
        LogAssert.ignoreFailingMessages = true;

        // Place player far away first so enemy starts in Patrol
        _playerGO     = new GameObject("TestPlayer");
        _playerGO.tag = "Player";
        _playerGO.transform.position = new Vector3(0f, 0f, 1000f);
        _playerGO.AddComponent<PlayerController>();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/Enemy/Enemy_Grunt.prefab");
        Assert.IsNotNull(prefab, "Enemy_Grunt prefab not found.");

        _enemyGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _enemy   = _enemyGO.GetComponent<EnemyAI>();

        // sightBlockLayers = 1 (Default layer) in the prefab — the player's CharacterController
        // collider is on Default layer and blocks the LOS raycast. Clear it so
        // the check skips the raycast and relies only on distance + FOV.
        _enemy.sightBlockLayers = 0;
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (_enemyGO  != null) Object.Destroy(_enemyGO);
        if (_playerGO != null) Object.Destroy(_playerGO);
    }

    [UnityTest]
    public IEnumerator Enemy_StartsInPatrol()
    {
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(EnemyAI.EnemyState.Patrol, _enemy.CurrentState);
    }

    [UnityTest]
    public IEnumerator Enemy_TransitionsToChase_WhenPlayerEntersDetectionRange()
    {
        // +Z = directly in front of enemy, 5m away — inside 10m detectionRange, inside 110 deg FOV
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(EnemyAI.EnemyState.Chase, _enemy.CurrentState,
            "Enemy should be in Chase after player enters detection range.");
    }

    [UnityTest]
    public IEnumerator Enemy_TransitionsToAttack_WhenPlayerInAttackRange()
    {
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);   // trigger Chase
        yield return new WaitForSeconds(0.4f);
        _playerGO.transform.position = new Vector3(0f, 0f, 1f);   // within attackRange (2m)
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(EnemyAI.EnemyState.Attack, _enemy.CurrentState,
            "Enemy should be in Attack when player is within attackRange.");
    }

    [UnityTest]
    public IEnumerator Enemy_ReturnsToChase_WhenPlayerLeavesAttackRange()
    {
        _playerGO.transform.position = new Vector3(0f, 0f, 1f);   // in attack range
        yield return new WaitForSeconds(0.4f);
        _playerGO.transform.position = new Vector3(0f, 0f, 5f);   // outside attack range, within loseRange (14m)
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(EnemyAI.EnemyState.Chase, _enemy.CurrentState,
            "Enemy should return to Chase when player exits attackRange.");
    }
}

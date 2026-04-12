using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 11 — Enemy Death Event Chain
// EnemyManager and ScoreManager are created in SetUp because the test scene
// has no pre-loaded game managers.

public class EnemyDeathChainTests
{
    private bool   _killEventFired;
    private System.Action<string> _killHandler;
    private GameObject _enemyGO;
    private EnemyAI    _enemy;
    private GameObject _playerGO;
    private GameObject _enemyManagerGO;
    private GameObject _scoreManagerGO;

    [SetUp]
    public void SetUp()
    {
        // Suppress NavMesh errors — no NavMesh in bare test scene
        LogAssert.ignoreFailingMessages = true;

        _killEventFired = false;
        _killHandler    = _ => _killEventFired = true;

        if (EnemyManager.Instance == null)
        {
            _enemyManagerGO = new GameObject("EnemyManager");
            _enemyManagerGO.AddComponent<EnemyManager>();
        }
        if (ScoreManager.Instance == null)
        {
            _scoreManagerGO = new GameObject("ScoreManager");
            _scoreManagerGO.AddComponent<ScoreManager>();
        }

        // EnemyAI.TakeDamage calls _agent.SetDestination(_player.transform.position)
        // when transitioning to Chase — _player must not be null.
        _playerGO     = new GameObject("TestPlayer");
        _playerGO.tag = "Player";
        _playerGO.transform.position = new Vector3(0f, 0f, 100f); // far away
        _playerGO.AddComponent<PlayerController>();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/Enemy/Enemy_Grunt.prefab");
        Assert.IsNotNull(prefab, "Enemy_Grunt prefab not found.");

        _enemyGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _enemy   = _enemyGO.GetComponent<EnemyAI>();
        _enemy.sightBlockLayers = 0;

        EnemyManager.Instance.OnEnemyKilled += _killHandler;
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnEnemyKilled -= _killHandler;

        if (_enemyGO        != null) Object.Destroy(_enemyGO);
        if (_playerGO       != null) Object.Destroy(_playerGO);
        if (_enemyManagerGO != null) Object.Destroy(_enemyManagerGO);
        if (_scoreManagerGO != null) Object.Destroy(_scoreManagerGO);
    }

    [UnityTest]
    public IEnumerator EnemyDeath_FiresOnEnemyKilled()
    {
        yield return null; // wait for Start()

        int startingKills = ScoreManager.Instance.TotalKills;

        _enemy.TakeDamage(9999);
        yield return new WaitForSeconds(0.3f);

        Assert.IsTrue(_killEventFired, "OnEnemyKilled was not fired after enemy died.");
        Assert.AreEqual(startingKills + 1, ScoreManager.Instance.TotalKills,
            "ScoreManager did not record the kill.");
    }
}

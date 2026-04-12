using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 9 — DDA Fuzzy Score Propagates to All Living Enemies
// Singletons (DifficultyManager, EnemyManager) are created in SetUp because
// the PlayMode test scene has no game managers pre-loaded.

public class DDAIntegrationTests
{
    private GameObject   _diffManagerGO;
    private GameObject   _enemyManagerGO;
    private GameObject[] _enemies;
    private EnemyAI[]    _enemyAIs;

    [SetUp]
    public void SetUp()
    {
        // Suppress NavMesh errors — no NavMesh in bare test scene
        LogAssert.ignoreFailingMessages = true;

        // Create managers if not already present (DontDestroyOnLoad singletons)
        if (DifficultyManager.Instance == null)
        {
            _diffManagerGO = new GameObject("DifficultyManager");
            _diffManagerGO.AddComponent<DifficultyManager>();
        }
        if (EnemyManager.Instance == null)
        {
            _enemyManagerGO = new GameObject("EnemyManager");
            _enemyManagerGO.AddComponent<EnemyManager>();
        }

        // Clear stale enemy references from previous tests — EnemyAI has no
        // OnDestroy that calls DeregisterEnemy (now fixed), but reset here too
        // so ForceApplyFuzzyScore only iterates enemies spawned in this test.
        EnemyManager.Instance?.ResetEnemyData();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/Enemy/Enemy_Grunt.prefab");
        Assert.IsNotNull(prefab, "Enemy_Grunt prefab not found.");

        _enemies  = new GameObject[3];
        _enemyAIs = new EnemyAI[3];
        for (int i = 0; i < 3; i++)
        {
            _enemies[i]  = Object.Instantiate(prefab, new Vector3(i * 5f, 0, 0), Quaternion.identity);
            _enemyAIs[i] = _enemies[i].GetComponent<EnemyAI>();
        }
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        // Reset enemy data before destroying so the manager's HashSet is
        // cleared — prevents stale references leaking into the next test.
        EnemyManager.Instance?.ResetEnemyData();

        foreach (var go in _enemies)
            if (go != null) Object.Destroy(go);

        if (_diffManagerGO  != null) Object.Destroy(_diffManagerGO);
        if (_enemyManagerGO != null) Object.Destroy(_enemyManagerGO);
    }

    [UnityTest]
    public IEnumerator SetScoreHard_UpdatesChaseSpeedOnAllEnemies()
    {
        yield return null; // wait one frame for Start() to run on all enemies

        float[] baseSpeeds = new float[3];
        for (int i = 0; i < 3; i++)
            baseSpeeds[i] = _enemyAIs[i].BaseChaseSpeed;

        DifficultyManager.Instance.ForceApplyFuzzyScore(1.0f);
        yield return null;

        float hardMult = DifficultyManager.Instance.hardChaseSpeedMult;
        for (int i = 0; i < 3; i++)
        {
            float expected = baseSpeeds[i] * hardMult;
            float actual   = _enemyAIs[i].ChaseSpeed;
            Assert.AreEqual(expected, actual, 0.01f,
                $"Enemy {i} ChaseSpeed was not updated to Hard multiplier.");
        }
    }

    [UnityTest]
    public IEnumerator ApplyHardScoreTwice_DoesNotCompound_OnLiveEnemy()
    {
        yield return null; // wait for Start()

        DifficultyManager.Instance.ForceApplyFuzzyScore(1.0f);
        yield return null;
        float afterFirst = _enemyAIs[0].ChaseSpeed;

        DifficultyManager.Instance.ForceApplyFuzzyScore(1.0f);
        yield return null;
        float afterSecond = _enemyAIs[0].ChaseSpeed;

        Assert.AreEqual(afterFirst, afterSecond, 0.01f,
            "Applying Hard score twice compounded stats on a live enemy.");
    }
}

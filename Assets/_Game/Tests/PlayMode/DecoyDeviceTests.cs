using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 10 — DecoyDevice Distraction Radius
// distractRadius = 12f (actual default). Near enemy at 10m, far enemy at 14m.
// NavMesh is not baked in the test scene — NavMeshAgent.SetDestination logs an
// error but does NOT throw; IsDistracted relies only on _decoyTimer, which is
// set before SetDestination is called, so the assertion is still valid.

public class DecoyDeviceTests
{
    private GameObject  _decoyGO;
    private DecoyDevice _decoy;
    private GameObject  _enemyNearGO, _enemyFarGO;
    private EnemyAI     _enemyNear,   _enemyFar;

    [SetUp]
    public void SetUp()
    {
        // Suppress NavMesh "not on NavMesh" errors — expected in a bare test scene
        LogAssert.ignoreFailingMessages = true;

        _decoyGO = new GameObject("TestDecoy");
        _decoyGO.transform.position = Vector3.zero;
        _decoy = _decoyGO.AddComponent<DecoyDevice>();
        _decoy.distractRadius = 12f;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/Enemy/Enemy_Grunt.prefab");
        Assert.IsNotNull(prefab, "Enemy_Grunt prefab not found.");

        _enemyNearGO = Object.Instantiate(prefab, new Vector3(0f, 0f, 10f), Quaternion.identity);
        _enemyNear   = _enemyNearGO.GetComponent<EnemyAI>(); // 10m — within 12m radius

        _enemyFarGO  = Object.Instantiate(prefab, new Vector3(0f, 0f, 14f), Quaternion.identity);
        _enemyFar    = _enemyFarGO.GetComponent<EnemyAI>();  // 14m — outside 12m radius
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (_decoyGO     != null) Object.Destroy(_decoyGO);
        if (_enemyNearGO != null) Object.Destroy(_enemyNearGO);
        if (_enemyFarGO  != null) Object.Destroy(_enemyFarGO);
    }

    [UnityTest]
    public IEnumerator EmitNoisePulse_DistractsEnemyWithinRadius()
    {
        yield return null; // wait for Awake/Start
        _decoy.EmitNoisePulse();
        yield return null;
        Assert.IsTrue(_enemyNear.IsDistracted,
            "Enemy within 12m should be distracted by decoy pulse.");
    }

    [UnityTest]
    public IEnumerator EmitNoisePulse_DoesNotDistractEnemyOutsideRadius()
    {
        yield return null; // wait for Awake/Start
        _decoy.EmitNoisePulse();
        yield return null;
        Assert.IsFalse(_enemyFar.IsDistracted,
            "Enemy at 14m should NOT be distracted (outside 12m distractRadius).");
    }
}

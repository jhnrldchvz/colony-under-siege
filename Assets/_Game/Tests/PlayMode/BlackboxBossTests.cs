using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 14 — Boss Phase 2 Triggers at 50% HP
// Phase2 triggers when: (float)currentHealth / maxHealth <= phase2Threshold (0.5)
// We deal Ceil(maxHealth * 0.5) damage to guarantee the threshold is crossed
// regardless of whether maxHealth is odd or even.

public class BlackboxBossTests
{
    private GameObject     _bossGO;
    private BossController _boss;

    [SetUp]
    public void SetUp()
    {
        // Suppress NavMesh / missing-component errors — no NavMesh in bare test scene
        LogAssert.ignoreFailingMessages = true;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Game/Prefabs/Enemy/Enemy_Boss Variant 1.prefab");
        Assert.IsNotNull(prefab, "Boss prefab not found at 'Assets/_Game/Prefabs/Enemy/Enemy_Boss Variant 1.prefab'");

        _bossGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _boss   = _bossGO.GetComponent<BossController>();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (_bossGO != null) Object.Destroy(_bossGO);
    }

    [UnityTest]
    public IEnumerator Boss_EntersPhase2_AtExactlyHalfHealth()
    {
        // Ceil ensures we always reach exactly or just below 50%, even with odd maxHealth values
        int damage = Mathf.CeilToInt(_boss.maxHealth * 0.5f);

        Assert.IsFalse(_boss.Phase2Active, "Boss should NOT be in Phase 2 at start.");

        _boss.TakeDamage(damage);
        yield return new WaitForSeconds(0.3f);

        Assert.IsTrue(_boss.Phase2Active,
            "Boss should enter Phase 2 when HP drops to 50%.");
    }
}

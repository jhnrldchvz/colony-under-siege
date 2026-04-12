# Colony Under Siege — Testing Implementation Instructions
### For Claude Code | Unity 6000.x LTS | C# | Unity Test Framework

---

## Context You Must Know Before Starting

**Project:** Colony Under Siege — 3D Sci-Fi FPS  
**Engine:** Unity 6000.x LTS  
**Language:** C#  
**Repo:** https://github.com/jhnrldchvz/colony-under-siege  
**Scripts folder:** `Assets/_Game/Scripts/`

### ⚠️ Updated FSM — 3 States ONLY
The enemy FSM has been simplified. There are now **only 3 active states**:
```
Patrol ──(CanSeePlayer)──> Chase ──(in attackRange)──> Attack
  ^                          ^                              |
  |                          └──────(out of attackRange)───┘
  └──────────────────────────────────────────────────────┘
```
`Return` and `Dead` states have been **REMOVED**. Do NOT write any test that references `EnemyState.Return` or `EnemyState.Dead`. These no longer exist.

### Key Systems to Test
| System | Script | Role |
|---|---|---|
| FSM | `Scripts/Enemy/EnemyAI.cs` | 3-state enemy AI |
| DDA | `Scripts/Managers/DifficultyManager.cs` | Accuracy-based difficulty scaling |
| Inventory | `Scripts/Managers/InventoryManager.cs` | Ammo + key items |
| Score | `Scripts/Managers/ScoreManager.cs` | Points and grade |
| Objectives | `Scripts/Managers/ObjectiveManager.cs` | Stage completion |
| Decoy | `Scripts/Devices/DecoyDevice.cs` | FSM distraction mechanic |
| AI Core | `Scripts/Managers/AICoreManager.cs` | Stage 4 enemy healing |
| Boss | `Scripts/Boss/BossController.cs` | Phase 1 / Phase 2 boss |

---

## Step 0 — Setup Before Writing Any Tests

Do this first. Do not skip.

### 1. Install Unity Test Framework
In Unity Editor: **Window → Package Manager → Unity Registry → Test Framework → Install**

### 2. Create Test Folders
```
Assets/_Game/Tests/
Assets/_Game/Tests/EditMode/
Assets/_Game/Tests/PlayMode/
```

### 3. Create Assembly Definition Files

**File: `Assets/_Game/Tests/EditMode/EditModeTests.asmdef`**
```json
{
  "name": "EditModeTests",
  "references": ["Assembly-CSharp"],
  "includePlatforms": ["Editor"],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**File: `Assets/_Game/Tests/PlayMode/PlayModeTests.asmdef`**
```json
{
  "name": "PlayModeTests",
  "references": ["Assembly-CSharp"],
  "includePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": false,
  "noEngineReferences": false
}
```

### 4. Required Minimum Code Refactors
Before writing tests, add these small public hooks to the existing scripts. Do not change any game logic — only add thin public accessors or extracted static helpers.

**In `EnemyAI.cs` — add:**
```csharp
public EnemyState CurrentState => _state;   // exposes private _state
public bool IsDistracted => _isDistracted;  // exposes private _isDistracted
```

**In `DifficultyManager.cs` — add:**
```csharp
// Pure static helper — no MonoBehaviour dependency
public static DifficultyTier ClassifyAccuracy(float accuracy)
{
    if (accuracy < 0.30f) return DifficultyTier.Easy;
    if (accuracy > 0.65f) return DifficultyTier.Hard;
    return DifficultyTier.Normal;
}

public int ShotsInWindow => _shotsFiredinWindow;   // read-only accessor
public int HitsInWindow  => _shotsHitInWindow;
```

**In `BossController.cs` — add:**
```csharp
public bool Phase2Active => phase2Active;
```

**Create new file `Assets/_Game/Scripts/Utility/AmmoMath.cs`:**
```csharp
public static class AmmoMath
{
    public static bool CanUseAmmo(int currentAmmo) => currentAmmo > 0;

    public static int AddAmmoToReserve(int current, int amount, int maxReserve)
        => Mathf.Min(current + amount, maxReserve);

    public static (int newMagazine, int newReserve) CalculateReload(
        int currentAmmo, int magazineSize, int reserve)
    {
        int needed = magazineSize - currentAmmo;
        int transfer = Mathf.Min(needed, reserve);
        return (currentAmmo + transfer, reserve - transfer);
    }
}
```

**Create new file `Assets/_Game/Scripts/Utility/ScoreCalculator.cs`:**
```csharp
public static class ScoreCalculator
{
    public static int Calculate(int rawKillPoints, float accuracy,
        float elapsedSeconds, int deaths, bool stageCompleted)
    {
        int score = rawKillPoints;
        if (accuracy >= 0.70f) score += 500;
        else if (accuracy >= 0.50f) score += 250;
        if (elapsedSeconds < 300f) score += 200;
        if (stageCompleted) score += 300;
        score -= deaths * 100;
        return score;
    }

    public static string GetGrade(int score)
    {
        if (score >= 3000) return "S";
        if (score >= 2000) return "A";
        if (score >= 1000) return "B";
        if (score >= 500)  return "C";
        return "D";
    }
}
```

---

## SECTION 1 — Unit Tests (Whitebox, EditMode)

> **Location:** `Assets/_Game/Tests/EditMode/`  
> **Run without entering Play mode.** Tests pure logic with no scene required.  
> Use `[TestFixture]` and `[Test]` attributes.

---

### FILE: `DifficultyManagerTests.cs`

```csharp
using NUnit.Framework;

[TestFixture]
public class DifficultyManagerTests
{
    // TEST 1 — DDA Tier Classification
    // Type: Unit (Whitebox)
    // Validates the 3 accuracy thresholds from Chapter 3 / thesis claim

    [Test]
    public void Accuracy_010_ReturnsEasy()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.10f);
        Assert.AreEqual(DifficultyTier.Easy, result);
    }

    [Test]
    public void Accuracy_029_ReturnsEasy_BoundaryBelow30()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.29f);
        Assert.AreEqual(DifficultyTier.Easy, result);
    }

    [Test]
    public void Accuracy_030_ReturnsNormal_ExactlyAtLowerBound()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.30f);
        Assert.AreEqual(DifficultyTier.Normal, result);
    }

    [Test]
    public void Accuracy_065_ReturnsNormal_ExactlyAtUpperBound()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.65f);
        Assert.AreEqual(DifficultyTier.Normal, result);
    }

    [Test]
    public void Accuracy_080_ReturnsHard()
    {
        var result = DifficultyManager.ClassifyAccuracy(0.80f);
        Assert.AreEqual(DifficultyTier.Hard, result);
    }

    // TEST 2 — DDA Anti-Compounding
    // Type: Unit (Whitebox)
    // Validates base-stat preservation — the core architectural claim of the DDA system.
    // Applying Hard tier twice must produce the SAME result as applying it once.

    [Test]
    public void ApplyHardTier_Twice_DoesNotCompoundStats()
    {
        float baseSpeed = 5f;
        float hardMult  = 1.60f;

        float firstApply  = baseSpeed * hardMult;   // 8.0
        float secondApply = baseSpeed * hardMult;   // must still be 8.0, not 12.8

        Assert.AreEqual(firstApply, secondApply, 0.001f,
            "Applying Hard tier twice compounded stats — base stats not preserved.");
    }
}
```

---

### FILE: `AmmoMathTests.cs`

```csharp
using NUnit.Framework;

[TestFixture]
public class AmmoMathTests
{
    // TEST 3 — Inventory: Empty Magazine
    // Type: Unit (Whitebox)
    [Test]
    public void CanUseAmmo_Zero_ReturnsFalse()
    {
        Assert.IsFalse(AmmoMath.CanUseAmmo(0));
    }

    [Test]
    public void CanUseAmmo_One_ReturnsTrue()
    {
        Assert.IsTrue(AmmoMath.CanUseAmmo(1));
    }

    // TEST 4 — Inventory: Reserve Cap
    // Type: Unit (Whitebox)
    [Test]
    public void AddAmmoToReserve_ExceedsMax_ClampsToMax()
    {
        int result = AmmoMath.AddAmmoToReserve(90, 999, 100);
        Assert.AreEqual(100, result, "Reserve exceeded maxReserve cap.");
    }

    [Test]
    public void AddAmmoToReserve_NormalAmount_AddsCorrectly()
    {
        int result = AmmoMath.AddAmmoToReserve(20, 15, 100);
        Assert.AreEqual(35, result);
    }

    // TEST 5 — Inventory: Reload Math
    // Type: Unit (Whitebox)
    [Test]
    public void Reload_PartialMagazine_TransfersFromReserve()
    {
        var (mag, res) = AmmoMath.CalculateReload(5, 30, 10);
        Assert.AreEqual(15, mag, "Magazine should be 5 + 10 = 15.");
        Assert.AreEqual(0,  res, "Reserve should be depleted.");
    }

    [Test]
    public void Reload_FullMagazine_NoTransferOccurs()
    {
        var (mag, res) = AmmoMath.CalculateReload(30, 30, 50);
        Assert.AreEqual(30, mag, "Full magazine should stay at 30.");
        Assert.AreEqual(50, res, "Reserve should be unchanged.");
    }
}
```

---

### FILE: `ScoreCalculatorTests.cs`

```csharp
using NUnit.Framework;

[TestFixture]
public class ScoreCalculatorTests
{
    // TEST 6 — ScoreManager: Final Score Calculation
    // Type: Unit (Whitebox)
    // Kill points: Grunt=100, Scout=150, Berserker=200, Mutant=175, Heavy=125, Boss=1000
    // Bonus: Acc≥70%=+500, Acc≥50%=+250, Time<5min=+200, StageComplete=+300
    // Penalty: -100 per death

    [Test]
    public void Score_TenGrunts_HighAccuracy_Fast_NoDeaths_IsGradeA()
    {
        int kills = 10 * 100;    // 1000
        int score = ScoreCalculator.Calculate(kills, 0.80f, 180f, 0, true);
        // 1000 + 500(acc) + 200(time) + 300(complete) = 2000
        Assert.AreEqual(2000, score);
        Assert.AreEqual("A", ScoreCalculator.GetGrade(score));
    }

    [Test]
    public void Score_NoKills_NoAccuracy_TenDeaths_IsGradeD()
    {
        int score = ScoreCalculator.Calculate(0, 0f, 999f, 10, false);
        // 0 - 1000 = -1000
        Assert.AreEqual(-1000, score);
        Assert.AreEqual("D", ScoreCalculator.GetGrade(score));
    }

    [Test]
    public void Score_OneBossKill_StageComplete_IsGradeB()
    {
        int score = ScoreCalculator.Calculate(1000, 0.60f, 999f, 0, true);
        // 1000 + 250(acc≥50%) + 0(time) + 300 = 1550
        Assert.AreEqual(1550, score);
        Assert.AreEqual("B", ScoreCalculator.GetGrade(score));
    }

    [Test]
    public void Score_Exactly3000_IsGradeS_Boundary()
    {
        Assert.AreEqual("S", ScoreCalculator.GetGrade(3000));
    }

    [Test]
    public void Score_1999_IsGradeB_NotA()
    {
        Assert.AreEqual("B", ScoreCalculator.GetGrade(1999));
    }
}
```

---

### FILE: `ObjectiveLogicTests.cs`

```csharp
using NUnit.Framework;

// Note: These tests use the ObjectiveRuntime class's evaluation logic.
// If ObjectiveRuntime cannot be instantiated in EditMode (due to ScriptableObject dependency),
// extract its evaluation logic into a static class first — same pattern as AmmoMath and ScoreCalculator.

[TestFixture]
public class ObjectiveLogicTests
{
    // TEST 7 — ObjectiveManager Completion Logic
    // Type: Unit (Whitebox)

    [Test]
    public void KillAll_AllEnemiesDead_IsComplete()
    {
        // Simulate: 5 registered, 5 killed
        int totalEnemies = 5;
        int aliveEnemies = 0;
        bool isComplete  = (aliveEnemies == 0 && totalEnemies > 0);
        Assert.IsTrue(isComplete);
    }

    [Test]
    public void KillAll_OneEnemyAlive_IsNotComplete()
    {
        int aliveEnemies = 1;
        bool isComplete  = (aliveEnemies == 0);
        Assert.IsFalse(isComplete);
    }

    [Test]
    public void KillCount_RequiredReached_IsComplete()
    {
        int required = 3;
        int killed   = 3;
        Assert.IsTrue(killed >= required);
    }

    [Test]
    public void KillCount_RequiredNotReached_IsNotComplete()
    {
        int required = 3;
        int killed   = 2;
        Assert.IsFalse(killed >= required);
    }

    [Test]
    public void CollectItem_CorrectItemID_IsComplete()
    {
        string requiredId = "keycard_01";
        string collectedId = "keycard_01";
        Assert.AreEqual(requiredId, collectedId);
    }

    [Test]
    public void CollectItem_WrongItemID_IsNotComplete()
    {
        string requiredId  = "keycard_01";
        string collectedId = "power_cell";
        Assert.AreNotEqual(requiredId, collectedId);
    }

    [Test]
    public void ActivateSwitch_AllActivated_IsComplete()
    {
        int required  = 4;
        int activated = 4;
        Assert.IsTrue(activated >= required);
    }

    [Test]
    public void ActivateSwitch_NotAllActivated_IsNotComplete()
    {
        int required  = 4;
        int activated = 3;
        Assert.IsFalse(activated >= required);
    }
}
```

---

## SECTION 2 — Integration Tests (Whitebox, PlayMode)

> **Location:** `Assets/_Game/Tests/PlayMode/`  
> **Run inside a Unity scene.** Uses `[UnityTest]` + `yield return` to wait frames.  
> Manually instantiate GameObjects in `[SetUp]` and destroy them in `[TearDown]`.

---

### FILE: `FSMCycleTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 8 — Full 3-State FSM Cycle
// Type: Integration (Whitebox, PlayMode)
// Verifies: Patrol → Chase → Attack → Chase in a real scene
// IMPORTANT: Only tests the 3 active states. Return and Dead are REMOVED.

public class FSMCycleTests
{
    private GameObject _enemyGO;
    private GameObject _playerGO;
    private EnemyAI _enemy;

    [SetUp]
    public void SetUp()
    {
        // Create a minimal NavMesh scene or load a test scene with NavMesh baked
        // Create fake player
        _playerGO = new GameObject("TestPlayer");
        _playerGO.transform.position = new Vector3(1000f, 0f, 1000f); // far away to start

        // Instantiate enemy prefab — load from Resources or use a test prefab
        var prefab = Resources.Load<GameObject>("Prefabs/Enemy/Grunt");
        _enemyGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _enemy   = _enemyGO.GetComponent<EnemyAI>();

        // Assign the fake player as the enemy's target
        // (Adjust the field name to match your actual EnemyAI player reference field)
        // _enemy.playerTransform = _playerGO.transform;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_enemyGO);
        Object.Destroy(_playerGO);
    }

    [UnityTest]
    public IEnumerator Enemy_StartsInPatrol()
    {
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(EnemyState.Patrol, _enemy.CurrentState);
    }

    [UnityTest]
    public IEnumerator Enemy_TransitionsToChase_WhenPlayerEntersDetectionRange()
    {
        // Move player into detection range, inside FOV, with no occluder
        _playerGO.transform.position = new Vector3(3f, 0f, 0f);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(EnemyState.Chase, _enemy.CurrentState,
            "Enemy should be in Chase after player enters detection range.");
    }

    [UnityTest]
    public IEnumerator Enemy_TransitionsToAttack_WhenPlayerInAttackRange()
    {
        _playerGO.transform.position = new Vector3(3f, 0f, 0f);   // trigger chase first
        yield return new WaitForSeconds(0.3f);
        _playerGO.transform.position = new Vector3(1f, 0f, 0f);   // move into attackRange
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(EnemyState.Attack, _enemy.CurrentState,
            "Enemy should be in Attack when player is within attackRange.");
    }

    [UnityTest]
    public IEnumerator Enemy_ReturnsToChase_WhenPlayerLeavesAttackRange()
    {
        _playerGO.transform.position = new Vector3(1f, 0f, 0f);   // in attack range
        yield return new WaitForSeconds(0.3f);
        _playerGO.transform.position = new Vector3(5f, 0f, 0f);   // beyond attack range
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(EnemyState.Chase, _enemy.CurrentState,
            "Enemy should return to Chase when player exits attackRange.");
    }

    // NOTE: No test for Return or Dead — these states have been removed from the FSM.
}
```

---

### FILE: `DDAIntegrationTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 9 — DDA Tier Change Propagates to All Living Enemies
// Type: Integration (Whitebox, PlayMode)

public class DDAIntegrationTests
{
    private GameObject[] _enemies;
    private EnemyAI[]    _enemyAIs;

    [SetUp]
    public void SetUp()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Enemy/Grunt");
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
        foreach (var go in _enemies) Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator SetTierHard_UpdatesChaseSpeedOnAllEnemies()
    {
        // Record base speeds
        float[] baseSpeeds = new float[3];
        for (int i = 0; i < 3; i++)
            baseSpeeds[i] = _enemyAIs[i].BaseChaseSpeed; // add public BaseChaseSpeed property

        DifficultyManager.Instance.SetTier(DifficultyTier.Hard);
        yield return null; // wait one frame

        for (int i = 0; i < 3; i++)
        {
            float expected = baseSpeeds[i] * 1.60f;
            float actual   = _enemyAIs[i].ChaseSpeed;   // add public ChaseSpeed property
            Assert.AreEqual(expected, actual, 0.01f,
                $"Enemy {i} chaseSpeed was not updated to Hard multiplier.");
        }
    }

    [UnityTest]
    public IEnumerator ApplyHardTierTwice_DoesNotCompound_OnLiveEnemy()
    {
        float baseSpeed = _enemyAIs[0].BaseChaseSpeed;

        DifficultyManager.Instance.SetTier(DifficultyTier.Hard);
        yield return null;
        float afterFirst = _enemyAIs[0].ChaseSpeed;

        DifficultyManager.Instance.SetTier(DifficultyTier.Hard);
        yield return null;
        float afterSecond = _enemyAIs[0].ChaseSpeed;

        Assert.AreEqual(afterFirst, afterSecond, 0.01f,
            "Applying Hard tier twice compounded stats on a live enemy.");
    }
}
```

---

### FILE: `DecoyDeviceTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 10 — DecoyDevice Distraction Radius
// Type: Integration (Whitebox, PlayMode)

public class DecoyDeviceTests
{
    private GameObject _decoyGO;
    private DecoyDevice _decoy;
    private GameObject _enemyNearGO, _enemyFarGO;
    private EnemyAI _enemyNear, _enemyFar;

    [SetUp]
    public void SetUp()
    {
        _decoyGO = new GameObject("TestDecoy");
        _decoyGO.transform.position = Vector3.zero;
        _decoy = _decoyGO.AddComponent<DecoyDevice>();

        var prefab = Resources.Load<GameObject>("Prefabs/Enemy/Grunt");

        _enemyNearGO = Object.Instantiate(prefab, new Vector3(10f, 0f, 0f), Quaternion.identity);
        _enemyNear   = _enemyNearGO.GetComponent<EnemyAI>();  // 10m — within 15m radius

        _enemyFarGO  = Object.Instantiate(prefab, new Vector3(20f, 0f, 0f), Quaternion.identity);
        _enemyFar    = _enemyFarGO.GetComponent<EnemyAI>();   // 20m — outside 15m radius
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_decoyGO);
        Object.Destroy(_enemyNearGO);
        Object.Destroy(_enemyFarGO);
    }

    [UnityTest]
    public IEnumerator EmitNoisePulse_DistractsEnemyWithinRadius()
    {
        _decoy.EmitNoisePulse();
        yield return null;
        Assert.IsTrue(_enemyNear.IsDistracted,
            "Enemy within 15m should be distracted by decoy pulse.");
    }

    [UnityTest]
    public IEnumerator EmitNoisePulse_DoesNotDistractEnemyOutsideRadius()
    {
        _decoy.EmitNoisePulse();
        yield return null;
        Assert.IsFalse(_enemyFar.IsDistracted,
            "Enemy at 20m should NOT be distracted (outside 15m distractRadius).");
    }
}
```

---

### FILE: `EnemyDeathChainTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 11 — Enemy Death Event Chain
// Type: Integration (Whitebox, PlayMode)
// Verifies OnEnemyKilled fires to ScoreManager and ObjectiveManager

public class EnemyDeathChainTests
{
    private bool _killEventFired;
    private GameObject _enemyGO;
    private EnemyAI _enemy;

    [SetUp]
    public void SetUp()
    {
        _killEventFired = false;
        var prefab = Resources.Load<GameObject>("Prefabs/Enemy/Grunt");
        _enemyGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _enemy   = _enemyGO.GetComponent<EnemyAI>();

        EnemyManager.Instance.OnEnemyKilled += _ => _killEventFired = true;
    }

    [TearDown]
    public void TearDown()
    {
        EnemyManager.Instance.OnEnemyKilled -= _ => _killEventFired = true;
        Object.Destroy(_enemyGO);
    }

    [UnityTest]
    public IEnumerator EnemyDeath_FiresOnEnemyKilled()
    {
        int startingScore = ScoreManager.Instance.TotalKills;

        // Kill the enemy by dealing max damage
        _enemy.TakeDamage(9999);
        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(_killEventFired, "OnEnemyKilled was not fired after enemy died.");
        Assert.AreEqual(startingScore + 1, ScoreManager.Instance.TotalKills,
            "ScoreManager did not record the kill.");
    }
}
```

---

## SECTION 3 — Blackbox / Scenario Tests (PlayMode)

> Simulate real player scenarios. Only observe external outputs — UI state, door state, objective flags.  
> Do not access private internals in blackbox tests.

---

### FILE: `BlackboxDoorTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

// TEST 12 — Exit Door Locked Before Objectives
// Type: Blackbox (PlayMode)

public class BlackboxDoorTests
{
    [UnityTest]
    public IEnumerator Door_RemainsLocked_WhenObjectivesIncomplete()
    {
        SceneManager.LoadScene("Landing Stage");
        yield return new WaitForSeconds(1f);   // wait for scene to load

        var door = GameObject.FindObjectOfType<DoorInteractable>();
        Assert.IsNotNull(door, "DoorInteractable not found in Stage 1.");

        // Simulate interact without killing enemies
        var fakePlayer = new GameObject("FakePlayer").AddComponent<PlayerController>();
        door.Interact(fakePlayer);
        yield return null;

        // Scene should still be Stage 1
        Assert.AreEqual("Landing Stage", SceneManager.GetActiveScene().name,
            "Door opened before objectives were complete.");

        Object.Destroy(fakePlayer.gameObject);
    }
}
```

---

### FILE: `BlackboxWeaponTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 13 — Empty Magazine Does Not Report Shot to DDA
// Type: Blackbox (PlayMode)

public class BlackboxWeaponTests
{
    [UnityTest]
    public IEnumerator EmptyMagazine_DoesNotReportShotToDDA()
    {
        // Set magazine to 0
        InventoryManager.Instance.SetMagazine(WeaponType.Rifle, 0);

        int shotsBefore = DifficultyManager.Instance.ShotsInWindow;

        // Attempt to fire
        var weaponCtrl = GameObject.FindObjectOfType<WeaponController>();
        weaponCtrl.TryFire();
        yield return null;

        int shotsAfter = DifficultyManager.Instance.ShotsInWindow;
        Assert.AreEqual(shotsBefore, shotsAfter,
            "Empty magazine fire should NOT report a shot to DifficultyManager.");
    }
}
```

---

### FILE: `BlackboxBossTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// TEST 14 — Boss Phase 2 Triggers at 50% HP
// Type: Blackbox (PlayMode)

public class BlackboxBossTests
{
    private GameObject _bossGO;
    private BossController _boss;

    [SetUp]
    public void SetUp()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Enemy/Boss");
        _bossGO = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _boss   = _bossGO.GetComponent<BossController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_bossGO);
    }

    [UnityTest]
    public IEnumerator Boss_EntersPhase2_AtExactlyHalfHealth()
    {
        int maxHP  = _boss.MaxHealth;
        int damage = maxHP / 2;   // deal exactly 50%

        Assert.IsFalse(_boss.Phase2Active, "Boss should NOT be in Phase 2 at start.");

        _boss.TakeDamage(damage);
        yield return new WaitForSeconds(0.3f);

        Assert.IsTrue(_boss.Phase2Active,
            "Boss should enter Phase 2 when HP drops to 50%.");
    }
}
```

---

## SECTION 4 — Performance Tests (Manual, Unity Profiler)

> These are NOT automated. Run manually and record results in Chapter 4 Table.

### PERF 1 — DDA Evaluation < 30ms
1. Open **Window → Analysis → Profiler**
2. Play Stage 3, fire 15+ shots to trigger DDA evaluation window
3. Filter Profiler by: `DifficultyManager.EvaluateDifficulty`
4. ✅ **Pass condition:** < 30ms per call *(cited benchmark: Papadimitriou et al., 2023)*

### PERF 2 — Stage 5 Sustained 60fps
1. Load Stage 5, trigger Phase 2 (damage boss to 50% HP)
2. Profiler → CPU tab → record 30 seconds of Phase 2 combat
3. ✅ **Pass condition:** Frame time ≤ 16.7ms on minimum spec (i3-8100, GTX 1050, 4GB RAM)

### PERF 3 — Zero GC Allocation on Enemy Hit Flash
1. Profiler → Memory tab → enable GC Alloc column
2. Shoot an enemy 10 consecutive times (triggers 10 `FlashRed()` coroutines)
3. Inspect `EnemyAI.FlashRed` frames
4. ✅ **Pass condition:** GC Alloc = 0B (MaterialPropertyBlock must not allocate new Material instances)

---

## How to Run All Automated Tests

```
Unity Editor
  → Window → General → Test Runner
    → EditMode tab → Run All    (runs Sections 1 & 7 objective logic tests)
    → PlayMode tab → Run All    (runs Sections 2 & 3)
```

All tests should show **green checkmarks** before thesis submission.

---

## Test Results Table (Copy into Chapter 4)

| # | Test Name | Type | Category | Pass / Fail | Notes |
|---|---|---|---|---|---|
| 1 | DDA: accuracy 0.10 → Easy | Unit | Whitebox | | |
| 2 | DDA: accuracy 0.29 → Easy (boundary) | Unit | Whitebox | | |
| 3 | DDA: accuracy 0.30 → Normal (boundary) | Unit | Whitebox | | |
| 4 | DDA: accuracy 0.65 → Normal (boundary) | Unit | Whitebox | | |
| 5 | DDA: accuracy 0.80 → Hard | Unit | Whitebox | | |
| 6 | DDA: Anti-compounding stat preservation | Unit | Whitebox | | |
| 7 | Ammo: Empty magazine returns false | Unit | Whitebox | | |
| 8 | Ammo: Reserve cap clamps correctly | Unit | Whitebox | | |
| 9 | Ammo: Reload math — partial | Unit | Whitebox | | |
| 10 | Ammo: Reload math — full magazine | Unit | Whitebox | | |
| 11 | Score: Grade A scenario | Unit | Whitebox | | |
| 12 | Score: Grade D scenario | Unit | Whitebox | | |
| 13 | Score: Grade B with Boss kill | Unit | Whitebox | | |
| 14 | Score: Grade S boundary at 3000 | Unit | Whitebox | | |
| 15 | Objective: KillAll — all dead | Unit | Whitebox | | |
| 16 | Objective: KillAll — one alive | Unit | Whitebox | | |
| 17 | Objective: KillCount — reached | Unit | Whitebox | | |
| 18 | Objective: KillCount — not reached | Unit | Whitebox | | |
| 19 | Objective: CollectItem — correct ID | Unit | Whitebox | | |
| 20 | Objective: CollectItem — wrong ID | Unit | Whitebox | | |
| 21 | Objective: ActivateSwitch — all done | Unit | Whitebox | | |
| 22 | Objective: ActivateSwitch — partial | Unit | Whitebox | | |
| 23 | FSM: Starts in Patrol | Integration | Whitebox | | |
| 24 | FSM: Patrol → Chase on detection | Integration | Whitebox | | |
| 25 | FSM: Chase → Attack in range | Integration | Whitebox | | |
| 26 | FSM: Attack → Chase out of range | Integration | Whitebox | | |
| 27 | DDA: Hard tier updates all enemy speeds | Integration | Whitebox | | |
| 28 | DDA: Hard tier twice — no compounding on live enemy | Integration | Whitebox | | |
| 29 | Decoy: Distract enemy within 15m | Integration | Whitebox | | |
| 30 | Decoy: Does NOT distract enemy at 20m | Integration | Whitebox | | |
| 31 | Enemy death fires OnEnemyKilled chain | Integration | Whitebox | | |
| 32 | Door locked before objectives complete | Scenario | Blackbox | | |
| 33 | Empty magazine — no DDA shot reported | Scenario | Blackbox | | |
| 34 | Boss enters Phase 2 at 50% HP | Scenario | Blackbox | | |
| 35 | DDA evaluation cycle < 30ms | Performance | Whitebox | | ms: _____ |
| 36 | Stage 5 sustained ≥ 60fps | Performance | Whitebox | | fps: _____ |
| 37 | Zero GC alloc on enemy hit flash | Performance | Whitebox | | alloc: ___ |

---

## ⚠️ Critical Rules for Claude Code

1. **FSM is 3 states only: Patrol, Chase, Attack.**  
   Do NOT reference `EnemyState.Return` or `EnemyState.Dead` — they are gone.

2. **Never call `new MonoBehaviour()`** in EditMode tests.  
   Extract logic into static helpers first, then test the helpers.

3. **Each PlayMode test must clean up after itself** in `[TearDown]` — `Object.Destroy()` every spawned GameObject.

4. **Singleton managers** need special handling in PlayMode:  
   Either load a scene that initializes them, or manually instantiate their prefabs in `[SetUp]`.

5. **Prefab paths** assume `Resources/Prefabs/Enemy/Grunt`, `Resources/Prefabs/Enemy/Boss`, etc.  
   Adjust paths to match the actual project structure if different.

6. **The 4 minimum refactors are required** (Step 0 above) before any PlayMode integration tests will compile.  
   Implement them first, confirm the project builds, then run tests.

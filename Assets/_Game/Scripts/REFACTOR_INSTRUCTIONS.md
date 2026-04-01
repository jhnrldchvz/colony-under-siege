# Colony Under Siege — Codebase Refactor Instructions
# Execute these tasks in order. Each section is independent.
# Project path: /mnt/hdd/Work/Projects/Unity/3D Sci-Fi FPS/Assets/_Game/Scripts/

---

## 1. WeaponController.cs — Split WeaponConfig into subclasses

**Problem:** `WeaponConfig` has fields for muzzle flash, legacy light, stats, AND decoy launcher
all in one class. Decoy launcher doesn't use damage/range/fireRate. Rifle/Pistol don't use
decoyPrefab/decoyThrowForce/decoyUpAngle. This causes noise in the Inspector.

**Fix:**
- Keep `WeaponConfig` as the base class with shared fields only:
  `type`, `displayName`, `icon`, `modelObject`, `barrelTip`, `startingAmmo`
- Create `RaycastWeaponConfig : WeaponConfig` with:
  `muzzleFlashFX`, `muzzleFlashOffset`, `muzzleColor`, `muzzleIntensity`, `muzzleRange`,
  `damage`, `range`, `fireRate`, `reloadTime`
- Create `LauncherWeaponConfig : WeaponConfig` with:
  `projectilePrefab`, `throwForce`, `throwUpAngle`
- In `WeaponController`, cast `Current` to the appropriate subtype before firing:
  `if (Current is LauncherWeaponConfig launcher) FireLauncher(launcher);`
  `else if (Current is RaycastWeaponConfig raycast) FireRaycast(raycast);`
- Remove `isDecoyLauncher` property — replaced by type check
- Remove `muzzleFlashDuration` field from WeaponController — unused
- Remove the `DecoyAmmoPickup.cs` file — superseded by `AmmoPickup.cs` with WeaponType.Decoy

---

## 2. EnemyAI.cs — Make Animator optional

**Problem:** `EnemyAI` caches `_animator` in Awake and calls `SetTrigger`/`SetFloat` directly.
Drone Guard and Scout enemies have no Animator — these calls silently fail every frame.

**Fix:**
- Remove ALL direct Animator calls from `EnemyAI.cs`:
  Remove `_animator`, `AnimSpeed`, `AnimAttack`, `AnimDie` fields
  Remove `SetAnimatorSpeed()` method
  Remove `_animator.SetTrigger(AnimAttack)` in `PerformAttack()`
  Remove `_animator.SetTrigger(AnimDie)` in `Die()`
  Remove all `SetAnimatorSpeed(_agent.velocity.magnitude)` calls in UpdatePatrol/Chase/Attack/Return
- Add a clean animation interface instead — `IEnemyAnimator`:
  ```csharp
  public interface IEnemyAnimator {
      void SetSpeed(float speed);
      void TriggerAttack();
      void TriggerDie();
  }
  ```
- `EnemyAI` gets an optional `IEnemyAnimator _anim` field, populated in `Awake()`:
  `_anim = GetComponent<IEnemyAnimator>();` — null if no animator bridge present
- All animation calls become: `_anim?.SetSpeed(...)`, `_anim?.TriggerAttack()`, etc.
- `MutantAnimatorBridge` implements `IEnemyAnimator` — handles Mixamo animations
- `DroneAnimatorBridge` implements `IEnemyAnimator` if needed — otherwise leave null
- This makes animator completely optional — enemies without a bridge work perfectly

---

## 3. Remove Deprecated FindObjectOfType Calls

**Problem:** `ButtonBridge.cs` and `DoorIndicator.cs` use obsolete `FindObjectOfType<T>()`

**Fix — in ButtonBridge.cs:**
- Replace all `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()`
- Replace all `FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)`

**Fix — in DoorIndicator.cs:**
- Replace all `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()`

---

## 4. InventoryManager.cs — Remove direct UIManager coupling

**Problem:** `NotifyAmmoChanged()` calls `UIManager.Instance.UpdateAmmo()` directly.
This creates a hard dependency — InventoryManager shouldn't know about UIManager.

**Fix:**
- Remove the direct `UIManager.Instance.UpdateAmmo(current, reserve)` call from `NotifyAmmoChanged()`
- `UIManager` already subscribes to `OnAmmoChanged` event — that subscription handles HUD updates
- The event fires correctly — the direct call is redundant and causes double-updates
- Result: `NotifyAmmoChanged()` only fires the event, nothing else

---

## 5. PlayerController.cs — Remove direct UIManager coupling

**Problem:** `TakeDamage()` and `Start()` call `UIManager.Instance.UpdateHealth()` and
`UIManager.Instance.SetMaxHealth()` directly AND fire events for the same purpose.
UIManager subscribes to `OnHealthChanged` and `OnMaxHealthSet` events — the direct calls are redundant.

**Fix:**
- In `TakeDamage()` — remove `if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(CurrentHealth);`
  Keep only: `OnHealthChanged?.Invoke(CurrentHealth);`
- In `Start()` — remove `if (UIManager.Instance != null) UIManager.Instance.SetMaxHealth(maxHealth);`
  Keep only: `OnMaxHealthSet?.Invoke(maxHealth);`
- Verify `UIManager` subscribes to both events in its `Start()` — it already does

---

## 6. ObjectiveManager.cs — Remove direct UIManager coupling

**Problem:** `RefreshHUD()` calls `UIManager.Instance.UpdateObjectiveText()` directly.
Should use an event instead.

**Fix:**
- Add event: `public event Action<string> OnObjectiveTextChanged;`
- In `RefreshHUD()` replace direct call with: `OnObjectiveTextChanged?.Invoke(sb.ToString().TrimEnd());`
- In `UIManager.Start()` subscribe: `ObjectiveManager.Instance.OnObjectiveTextChanged += UpdateObjectiveText;`
- Note: UIManager's `DelayedHUDInit()` coroutine already handles late subscription — use the same pattern

---

## 7. Cleanup — Delete redundant and unused files

**Delete these files (and their .meta files):**
- `Assets/_Game/Scripts/Player/DecoyAmmoPickup.cs` — superseded by AmmoPickup with WeaponType.Decoy
- `Assets/_Game/Scripts/Player/EMPGrenade.cs` — removed feature
- `Assets/_Game/Scripts/Managers/WeaponController_PerformRaycast.cs` — partial duplicate
- `Assets/_Game/Scripts/Enemy/EnemyAI_Addition.cs` — check if empty/unused, delete if so

**Verify before deleting** — search for any references to these class names in other scripts first.

---

## 8. SFXManager.cs — Add null guard pattern

**Problem:** Every call site uses `SFXManager.Instance?.PlayX()` — the null check is at the
call site, not inside the manager. If SFXManager is missing from a scene, there are silent failures.

**Fix:**
- Add a static fallback `NullSFXManager` pattern — or simply add a `[RequireComponent]` note in the summary
- More practically: add `[DefaultExecutionOrder(-50)]` attribute to SFXManager so it initializes
  before WeaponController and PlayerController

---

## 9. MutantAnimatorBridge.cs — Implement IEnemyAnimator

**After completing task 2:**
- Add `using` or direct implementation: `public class MutantAnimatorBridge : MonoBehaviour, IEnemyAnimator`
- Implement `SetSpeed(float speed)`, `TriggerAttack()`, `TriggerDie()` interface methods
- Map these to the existing Animator parameter calls already in the class
- Keep `attackHitDelay` and the `attack2Chance` logic inside `TriggerAttack()`

---

## 10. General Code Quality

- Add `[Header]` attributes to any public fields missing them for Inspector clarity
- Ensure all `Start()` coroutines that subscribe to events use null checks before subscribing
- In `PressurePlate.cs` — `OnTriggerEnter/Exit` — verify the `HashSet<Collider>` correctly handles
  the case where a kinematic object (held box) transitions to non-kinematic (dropped) — the collider
  reference should remain stable
- In `HoldableObject.cs` — the `WatchHoldState` coroutine in `PressurePlateHeavyBox` runs every frame
  via `yield return null` — replace with a property-change check or event from `GrabController`
  to avoid a coroutine running every frame per box

---

## Priority Order
1. Task 2 (EnemyAI animator — highest impact, fixes silent failures)
2. Task 1 (WeaponConfig split — improves Inspector UX)
3. Tasks 4, 5, 6 (decouple UIManager — reduces hidden dependencies)
4. Task 7 (cleanup — reduces confusion)
5. Tasks 3, 8, 9, 10 (polish)

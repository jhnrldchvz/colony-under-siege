# Colony Under Siege — Full System Reference
### Unity 3D Sci-Fi First-Person Shooter
*Comprehensive Codebase Documentation for Thesis Chapters 4–5*

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Project Structure & Folder Organization](#2-project-structure--folder-organization)
3. [Core Interfaces & Contracts](#3-core-interfaces--contracts)
4. [Scriptable Objects & Data Definitions](#4-scriptable-objects--data-definitions)
5. [Game State Machine (GameManager)](#5-game-state-machine-gamemanager)
6. [Player System](#6-player-system)
7. [Enemy AI System — Finite State Machine](#7-enemy-ai-system--finite-state-machine)
8. [Boss System](#8-boss-system)
9. [Objective System](#9-objective-system)
10. [Inventory & Ammo System](#10-inventory--ammo-system)
11. [Dynamic Difficulty Adjustment (DDA)](#11-dynamic-difficulty-adjustment-dda)
12. [Audio Systems](#12-audio-systems)
13. [Interactable Systems](#13-interactable-systems)
14. [Decoy Weapon System](#14-decoy-weapon-system)
15. [AI Core Healing System](#15-ai-core-healing-system)
16. [Level & Scene Management](#16-level--scene-management)
17. [Score System](#17-score-system)
18. [UI Systems](#18-ui-systems)
19. [Test & Metrics Systems](#19-test--metrics-systems)
20. [Design Patterns Employed](#20-design-patterns-employed)
21. [Key System Interactions (Data Flow Diagrams)](#21-key-system-interactions-data-flow-diagrams)
22. [Complete Script File Manifest](#22-complete-script-file-manifest)
23. [Scene-by-Scene Breakdown](#23-scene-by-scene-breakdown)
24. [System Architecture Summary](#24-system-architecture-summary)

---

## 1. Project Overview

**Colony Under Siege** is a single-player, narrative-driven, first-person shooter (FPS) game developed in Unity (6000.x LTS). The game is set in a science-fiction environment in which the player must navigate through five escalating stages of a besieged space colony — from a landing pad overrun by enemy grunts, through engineering sectors, a bio-lab, an AI core, and finally a reactor guarded by a multi-phase boss.

The project serves as both a playable game and a research platform, specifically designed to demonstrate and empirically evaluate:

- **Finite State Machine (FSM)-driven enemy AI** with configurable perception parameters
- **Dynamic Difficulty Adjustment (DDA)** based on real-time player accuracy
- **Objective-driven gameplay** with diverse objective types
- **Weapon mechanics** including a distraction tool (Decoy Launcher) that directly exploits enemy FSM states
- **Modular, interface-based architecture** supporting easy extension

The game was built using Unity's NavMesh system for enemy pathfinding, a custom FSM framework for enemy and game state management, ScriptableObject-based data definitions, and a comprehensive event-driven communication layer between subsystems.

---

## 2. Project Structure & Folder Organization

```
Assets/
└── _Game/
    ├── Art/
    │   ├── FX/                        # Muzzle flash, particle effect prefabs
    │   ├── Materials/                 # Enemy, weapon, environment materials
    │   ├── Models/                    # 3D mesh assets (FBX)
    │   └── Textures/                  # Diffuse maps, normal maps, emission maps
    │
    ├── Audio/
    │   ├── Music/                     # Combat_Background_Music, Exploration_Background_Music
    │   └── SFX/                       # Weapon shots, explosions, reloads, UI sounds
    │
    ├── Data/
    │   ├── Stats_Berserker.asset      # EnemyStats ScriptableObject
    │   ├── Stats_Grunt.asset
    │   ├── Stats_Heavy.asset
    │   ├── Stats_Mutant.asset
    │   └── Stats_Scout.asset
    │
    ├── Prefabs/
    │   ├── Acces Doors/               # Next Level Door.prefab
    │   ├── Damageables/               # Crate.prefab, Explosive Barrel.prefab
    │   ├── Enemy/                     # Grunt, Scout, Berserker, Mutant, Heavy, Boss
    │   ├── Guns/                      # Pistol, Rifle, Decoy Launcher
    │   ├── Holdable/                  # Heavy physics boxes for puzzles
    │   ├── Interactable/              # Ammo pickup, food pickup, objective pickups
    │   ├── Muzzle Flash/              # VFX for rifle and pistol
    │   ├── Objective Items/           # Access Key, Deactivation Device, Power Cell
    │   ├── Player/                    # Main player character prefab
    │   ├── Switchable/                # Terminal switch prefabs
    │   └── Fire.prefab                # Environmental fire hazard
    │
    ├── Scenes/
    │   ├── [0] MainMenu.unity
    │   ├── [1] Landing Stage.unity
    │   ├── [2] Engineering Sector.unity
    │   ├── [3] Bio-Lab.unity
    │   ├── [4] AI Core Control.unity
    │   └── [5] Reactor.unity
    │
    └── Scripts/
        ├── Boss/                      # BossController, WaveSpawner, BossHealthBar, BossAnimatorBridge
        ├── Devices/                   # DecoyLauncher, DecoyDevice
        ├── Enemy/                     # EnemyAI, EnemyProjectile, health bars, animator bridges
        ├── Interactables/
        │   ├── Puzzle/                # PressurePlate, PlateDoorController, PressurePlateHeavyBox
        │   └── (root)                 # Switch, Door, Pickups, Explosives, Crates
        ├── Interfaces/                # IEnemy, IDamageable, IInteractable, IEnemyAnimator, DamageRelay
        ├── Managers/                  # All singleton game managers
        ├── Player/                    # PlayerController, WeaponController, GrabController, HitEffects
        ├── ScriptableObjects/         # EnemyStats, Objective, InstructionSlide, StoryboardSlide
        ├── Test/                      # Metrics collection for research
        └── Utility/                   # CameraShake, ButtonBridge, DifficultyHUD
```

---

## 3. Core Interfaces & Contracts

The architecture is built around four primary interfaces. These enable loose coupling between systems: damage sources do not know the type of their target; the player does not know the type of object it interacts with; the animation system is fully optional.

---

### 3.1 `IEnemy`

**File:** `Scripts/Interfaces/IEnemy.cs`

```csharp
public interface IEnemy
{
    bool IsAlive { get; }
}
```

**Purpose:** Provides a minimal contract for any entity classified as an enemy. The `EnemyManager` maintains a registry of all `IEnemy` instances in the scene, allowing it to query alive counts and determine when all enemies have been defeated.

**Implementors:** `EnemyAI`, `BossController`

---

### 3.2 `IDamageable`

**File:** `Scripts/Interfaces/IDamageable.cs`

```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
    bool IsAlive { get; }
}
```

**Purpose:** Any entity that can receive damage implements this interface. This decouples the weapon system from specific entity types — a bullet, explosion, or environmental hazard only needs to call `TakeDamage()` on whatever it hits.

**Implementors:**
| Class | Role |
|---|---|
| `PlayerController` | Player health |
| `EnemyAI` | Standard enemy health |
| `BossController` | Boss health with phase triggers |
| `ExplosiveBarrel` | Destructible environment object |
| `DestructibleCrate` | Loot container |
| `DamageRelay` | Forwards damage from child colliders to root |

**Design Pattern:** *Strategy Pattern* — the weapon's firing logic is completely agnostic of the target type.

---

### 3.3 `IInteractable`

**File:** `Scripts/Interfaces/IInteractable.cs`

```csharp
public interface IInteractable
{
    void Interact(PlayerController player);
}
```

**Purpose:** All objects the player can interact with (press E) implement this interface. The `PlayerController` fires a raycast and calls `Interact(player)` on whatever it hits. Each interactable handles its own logic internally.

**Implementors:**
| Class | Interaction Result |
|---|---|
| `SwitchInteractable` | Toggle terminal, fire UnityEvent |
| `DoorInteractable` | Load next scene (checks key + objectives) |
| `WeaponPickup` | Grants weapon ammo to inventory |
| `AmmoPickup` | Refills reserve ammo |
| `FoodPickup` | Heals player |
| `ObjectivePickup` | Adds key item to inventory |
| `HoldableObject` | Initiates grab via `GrabController` |

**Design Pattern:** *Visitor Pattern* — `PlayerController` visits interactables; each handles the visit polymorphically.

---

### 3.4 `IEnemyAnimator`

**File:** `Scripts/Interfaces/IEnemyAnimator.cs`

```csharp
public interface IEnemyAnimator
{
    void SetSpeed(float speed);
    void TriggerAttack();
    void TriggerDie();
}
```

**Purpose:** Decouples FSM state transitions from animation system calls. Enemies that have animators attach a bridge component; enemies without animators simply have no bridge. All FSM calls use null-conditional (`?.`) operators, making animation entirely optional.

**Implementors:** `MutantAnimatorBridge`, `DroneScoutBridge`, `BossAnimatorBridge`

**Design Pattern:** *Null Object Pattern* — absence of animator bridge is handled gracefully.

---

### 3.5 `DamageRelay`

**File:** `Scripts/Interfaces/DamageRelay.cs`

**Purpose:** Some enemies have multiple child colliders (body parts). `DamageRelay` sits on each child `GameObject` and forwards any `TakeDamage()` call to the root `IDamageable` component (the `EnemyAI` on the parent).

```csharp
public class DamageRelay : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyAI root;

    public bool IsAlive => root.IsAlive;

    public void TakeDamage(int amount)
    {
        root.TakeDamage(amount);
    }
}
```

This enables hit detection on any body part while centralizing damage processing.

---

## 4. Scriptable Objects & Data Definitions

ScriptableObjects are used throughout the project for data-driven configuration. This allows designers to create new enemy types, objective definitions, or tutorial content without writing code.

---

### 4.1 `EnemyStats`

**File:** `Scripts/ScriptableObjects/EnemyStats.cs`

**Purpose:** Data container for a single enemy archetype. One asset exists per enemy type. `EnemyAI` loads its assigned asset on `Awake()` to configure itself.

**Key Fields:**

| Field | Type | Description |
|---|---|---|
| `enemyName` | `string` | Display name for HUD |
| `maxHealth` | `int` | Starting hit points |
| `patrolSpeed` | `float` | Movement speed during Patrol state |
| `chaseSpeed` | `float` | Movement speed during Chase/Attack |
| `detectionRange` | `float` | Distance at which enemy can see player |
| `fieldOfView` | `float` | Horizontal FOV cone angle (degrees) |
| `attackRange` | `float` | Distance to trigger melee attack |
| `attackDamage` | `int` | Damage dealt per attack |
| `attackCooldown` | `float` | Seconds between attacks |
| `isRanged` | `bool` | Selects ranged vs. melee attack logic |
| `projectilePrefab` | `GameObject` | Projectile prefab (ranged enemies only) |
| `preferredRange` | `float` | Strafe distance for ranged enemies |
| `dropChance` | `float` | Probability of loot drop on death (0–1) |

**DDA Integration:** `DifficultyManager` multiplies all numeric stats by tier-specific multipliers. Base stats are stored separately to prevent compounding.

**Enemy Archetypes:**

| Type | Key Behavior | Range |
|---|---|---|
| Grunt | Melee charger | Close |
| Scout | Ranged shooter, strafes | Medium |
| Berserker | Fast melee, high damage | Close |
| Mutant | Melee with animation hits | Close |
| Heavy | Slow melee, high HP | Close |

---

### 4.2 `Objective`

**File:** `Scripts/ScriptableObjects/Objective.cs`

**Purpose:** Defines a single stage objective. `ObjectiveManager` wraps each asset in an `ObjectiveRuntime` instance that holds mutable session state (current progress, completion flag).

**Objective Types:**

```csharp
public enum ObjectiveType
{
    KillAll,        // Defeat every registered enemy in the scene
    KillCount,      // Defeat at least N enemies
    CollectItem,    // Add a specific key item ID to inventory
    ActivateSwitch  // Activate N switches from a named group
}
```

**Key Fields:**

| Field | Type | Description |
|---|---|---|
| `objectiveType` | `ObjectiveType` | Which evaluation logic to use |
| `displayName` | `string` | Name shown in HUD |
| `requiredCount` | `int` | Kill count or switch count target |
| `requiredItemId` | `string` | Key item ID for CollectItem type |
| `switchGroupId` | `string` | Group name for ActivateSwitch type |

**`ObjectiveRuntime` Methods:**

- `Evaluate()` — Checks current game state against the objective's criteria; sets `IsComplete`.
- `GetDisplayText()` — Returns formatted HUD string:
  - `"[DONE] Kill all enemies"` (complete)
  - `"• Kill 5 enemies  [2/5]"` (in progress)
  - `"• Activate 4 terminals  [2/4]"` (in progress)

---

### 4.3 `InstructionSlide`

**File:** `Scripts/ScriptableObjects/InstructionSlide.cs`

**Purpose:** Data for a single slide in the pre-game tutorial sequence. Contains a `Sprite` (image) and `string` (body text). The `UIManager` renders these in a paginated panel before gameplay begins.

---

### 4.4 `StoryboardSlide`

**File:** `Scripts/ScriptableObjects/StoryboardSlide.cs`

**Purpose:** Data for a narrative cinematic slide. Contains `Sprite`, `title`, and `body` text. Used for intro (before boss) and outro (after boss) storyboard sequences managed by `GameManager`.

---

## 5. Game State Machine (GameManager)

**File:** `Scripts/Managers/GameManager.cs`
**Pattern:** Singleton + State Machine

The `GameManager` is the central authority on the game's current state. All input handling, UI panel visibility, and time scale are controlled through state transitions here.

### 5.1 State Definitions

```csharp
public enum GameState
{
    Playing,        // Active gameplay — all input enabled
    Paused,         // ESC pressed — pause menu, timeScale = 0
    Instructions,   // Pre-game tutorial — ESC blocked, cursor visible
    Storyboard,     // Narrative slides — input blocked during advance
    GameOver,       // Player HP reached 0
    Win             // All objectives complete, door entered
}
```

### 5.2 State Transition Flow

```
[Application Start]
       |
       v
  Instructions  <-- ShowInstructions() called by LevelManager on Start
       |  (player clicks "Start" or presses Enter)
       v
    Playing  <---> Paused (ESC key toggles)
       |
       | (boss encounter in Stage 5)
       v
  Storyboard (intro narrative)
       |  (advance through all slides)
       v
    Playing
       |
       +----> GameOver   (player HP <= 0)
       |
       +----> Win        (objectives complete, door entered)
                |
                v
          Storyboard (outro narrative, Stage 5 only)
                |
                v
           Main Menu
```

### 5.3 Key Events

| Event | Signature | Subscribers |
|---|---|---|
| `OnStateChanged` | `Action<GameState>` | UIManager, PlayerController, WeaponController |
| `OnGameOver` | `Action` | UIManager (shows game over panel) |
| `OnStageWin` | `Action` | UIManager (shows win panel), ScoreManager |

### 5.4 Key Methods

| Method | Description |
|---|---|
| `PauseGame()` | Set timeScale=0, show pause menu, unlock cursor |
| `ResumeGame()` | Set timeScale=1, hide pause menu, lock cursor |
| `StartInstructions()` | Enter Instructions state, show tutorial panel |
| `EndInstructions()` | Return to Playing state, hide tutorial panel |
| `StartStoryboard(slides[])` | Enter Storyboard state, begin slide sequence |
| `EndStoryboard()` | Return to Playing state after all slides shown |
| `TriggerGameOver()` | Enter GameOver state, fire OnGameOver event |
| `TriggerStageWin()` | Enter Win state, fire OnStageWin event, freeze time |

---

## 6. Player System

### 6.1 `PlayerController`

**File:** `Scripts/Player/PlayerController.cs`
**Implements:** `IDamageable`

The central player script handling movement, camera look, health, and interaction.

**Key Serialized Fields:**

| Field | Default | Description |
|---|---|---|
| `maxHealth` | `100` | Starting and maximum HP |
| `moveSpeed` | `5f` | Walk speed (m/s) |
| `sprintSpeed` | `9f` | Sprint speed (m/s) |
| `jumpForce` | `5f` | Upward velocity on jump |
| `mouseSensitivity` | `2f` | Look speed multiplier |
| `interactionRange` | `3f` | E-key raycast distance |
| `clampToNavMesh` | `true` | Prevent walking off NavMesh |

**Properties:**

| Property | Type | Notes |
|---|---|---|
| `CurrentHealth` | `int` | Current hit points |
| `IsAlive` | `bool` | `CurrentHealth > 0` |
| `IsInputBlocked` | `bool` | True when GameState ≠ Playing |

**Movement System:**

- WASD for horizontal movement, Space for jump
- Shift held while moving triggers sprint
- `CharacterController.Move()` with manual gravity accumulation
- Optional NavMesh boundary clamping: if next position is off-mesh, horizontal delta is zeroed

**Camera Look:**

- Mouse X rotates player transform (yaw)
- Mouse Y rotates camera-only (pitch), clamped to ±85°
- Cursor locked and hidden during Playing state

**Interaction:**

- E key fires a raycast from camera center on `Interactable` layer mask
- On hit, calls `IInteractable.Interact(this)`
- Calls `GrabController.TryGrab()` if holding a holdable object

**Events:**

| Event | Fires When |
|---|---|
| `OnHealthChanged(int newHealth)` | Every TakeDamage or Heal call |
| `OnMaxHealthSet(int maxHp)` | Once on Start (for UI initialization) |

**`TakeDamage(int amount):`**
1. Subtracts amount from `CurrentHealth` (clamped to 0)
2. Fires `OnHealthChanged`
3. Calls `HitEffects.PlayHitEffect()`
4. Calls `SFXManager.PlayPlayerHurt()`
5. Reports death to `ScoreManager` if `CurrentHealth` reaches 0
6. Calls `GameManager.TriggerGameOver()` if dead

---

### 6.2 `WeaponController`

**File:** `Scripts/Player/WeaponController.cs`

Manages all three weapon slots, their firing modes, reloading, ammo queries, and switching.

**Weapon Slot Configuration:**

```csharp
public class WeaponSlot
{
    public WeaponType type;          // Pistol, Rifle, Decoy
    public WeaponMode mode;          // Raycast or Launcher
    public float fireRate;           // Seconds between shots
    public int damage;               // Damage per raycast hit
    public float range;              // Raycast max distance
    public int unlockedFromBuildIndex; // Scene index where weapon becomes available
    public GameObject prefabInScene; // Weapon model GameObject
    public GameObject muzzleFlash;   // Muzzle flash VFX
    // Launcher-specific:
    public GameObject projectilePrefab;
    public float throwForce;
    public float throwUpAngle;
}
```

**Default Weapon Array:**

| Index | Type | Mode | Damage | Range | Notes |
|---|---|---|---|---|---|
| 0 | Rifle | Raycast | 5 | 50m | Available from Stage 1 |
| 1 | Pistol | Raycast | 2 | 30m | Available from Stage 1 |
| 2 | Decoy | Launcher | — | — | Locked until Stage 2 |

**Fire Flow (`TryFire()`):**

```
TryFire()
├── Check fireRateCooldown elapsed
├── Check GameState == Playing
├── Check InventoryManager.UseAmmo(currentWeapon.type)
│   └── Returns false if magazine empty → play empty click SFX
├── If mode == Raycast:
│   └── PerformRaycast()
│       ├── Physics.Raycast from camera center
│       ├── If hit IDamageable → TakeDamage(damage)
│       ├── Spawn hit effect at point
│       └── Report shot to DifficultyManager and ScoreManager
├── If mode == Launcher:
│   └── FireLauncher()
│       ├── Instantiate projectilePrefab at camera/barrel
│       └── Apply velocity = (cameraForward rotated up by throwUpAngle) * throwForce
├── Spawn/activate muzzleFlash
├── Play weapon SFX
└── Reset fireRateCooldown
```

**Reload Flow (`TryReload()`):**

1. Check that reserve ammo > 0 and magazine is not full
2. Set `_isReloading = true` (blocks further firing)
3. Play reload animation/SFX
4. Wait `reloadTime` seconds (coroutine)
5. Call `InventoryManager.Reload(currentWeapon.type)`
6. Update HUD ammo display
7. Set `_isReloading = false`

**Weapon Switching:**
- Scroll wheel cycles through unlocked weapons
- Number keys 1/2/3 switch directly
- `unlockedFromBuildIndex` checked against current scene; locked weapons skipped

---

### 6.3 `GrabController`

**File:** `Scripts/Player/GrabController.cs`

Enables physics-based object manipulation for puzzle solving.

**Controls:**

| Input | Action |
|---|---|
| E (near holdable) | Pick up object |
| E (while holding) | Drop object |
| Mouse X/Y + Alt | Rotate held object (world axes) |
| Scroll Wheel | Roll on Z axis |
| Right Click | Throw forward |

**Implementation Details:**

- Picked-up object's `Rigidbody` is set kinematic; object follows camera at fixed hold distance
- Rotation uses world-space axes (not camera-relative) to prevent disorientation
- `Outline` component changes: yellow on hover, cyan while held
- Throw applies `Rigidbody.AddForce()` in camera-forward direction

---

### 6.4 `HitEffects`

**File:** `Scripts/Player/HitEffects.cs`

Provides layered visual and audio feedback when the player receives damage.

**Effects (triggered on each TakeDamage call):**

| Effect | Implementation |
|---|---|
| Red vignette flash | Full-screen CanvasGroup overlay; alpha fades from 1 → 0 over 0.5s |
| Helmet crack overlay 1 | Shown at ≤50% HP; pulsing alpha animation |
| Helmet crack overlay 2 | Shown at ≤25% HP; stronger pulsing |
| Camera positional shake | `CameraShake.Shake(duration, magnitude)` |
| Head bob | Sinusoidal camera Y offset while moving; amplitude scales with speed |

---

## 7. Enemy AI System — Finite State Machine

### 7.1 Architecture Overview

**File:** `Scripts/Enemy/EnemyAI.cs`
**Implements:** `IEnemy`, `IDamageable`

Each enemy is an autonomous agent driven by a 5-state Finite State Machine. All state logic is evaluated in `Update()` via a switch statement on the current `EnemyState` enum value.

```csharp
public enum EnemyState
{
    Patrol,   // Wander between random waypoints
    Chase,    // Pursue player (or decoy) with NavMeshAgent
    Attack,   // Deal damage at close range or fire projectile
    Return,   // Walk back to spawn origin
    Dead      // Play death VFX, drop loot, deregister
}
```

### 7.2 State Transition Diagram

```
          ┌─────────────────────────────────┐
          │                                 │
          v                                 │
    ┌───────────┐   CanSeePlayer()=true     │
    │  PATROL   │──────────────────────────>│
    └───────────┘                           │
          ^                           ┌─────────┐
          │ lost player beyond        │  CHASE  │
          │ losePlayerRange           └─────────┘
          │                                 │
    ┌────────────┐  in attackRange          │
    │   RETURN   │<───────────────────┐     │
    └────────────┘                   │     │
                                     v     │
                               ┌──────────┐│
                               │  ATTACK  ││
                               └──────────┘│
                                     │     │
                                     │ player escapes range
                                     └─────┘

          Any State ──── HP <= 0 ──────> DEAD
```

### 7.3 Patrol State

- Enemy picks a random point within `patrolRadius` of its spawn origin using `NavMesh.SamplePosition()`
- Moves to that point at `patrolSpeed`
- On arrival, waits `patrolWaitTime` seconds, then picks new waypoint
- Continuously calls `CanSeePlayer()` each frame; transitions to Chase immediately on detection

### 7.4 Chase State

- Sets `NavMeshAgent.destination` to player's position each frame
- If `_isDistracted` (decoy active), sets destination to `_decoyPosition` instead
- Checks distance each frame:
  - ≤ `attackRange` → transition to Attack
  - > `losePlayerRange` → transition to Return

### 7.5 Detection System (`CanSeePlayer()`)

Detection uses three-layer validation:

**Layer 1 — Distance Check:**
```csharp
float dist = Vector3.Distance(transform.position, player.position);
if (dist > detectionRange) return false;
```

**Layer 2 — Field of View Check (XZ Plane):**
```csharp
Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
Vector3 flatToPlayer = new Vector3(toPlayer.x, 0, toPlayer.z).normalized;
float angle = Vector3.Angle(flatForward, flatToPlayer);
if (angle > fieldOfView * 0.5f) return false;
```

**Layer 3 — Line of Sight Raycast:**
```csharp
Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
Vector3 targetPos = player.position + Vector3.up * 1f;
if (Physics.Raycast(eyePos, targetPos - eyePos, out hit, detectionRange, sightBlockLayers))
    return false; // Occluded
```

All three conditions must pass for detection.

### 7.6 Attack State

**Melee Attack Flow:**
1. Stop NavMeshAgent movement
2. Face player (`transform.LookAt`)
3. Call `_animator?.TriggerAttack()`
4. After `attackHitDelay` (from animator bridge), call `player.TakeDamage(attackDamage)`
5. Wait `attackCooldown` before next attack
6. If player moves out of `attackRange`, return to Chase

**Ranged Attack Flow:**
1. Maintain `preferredRange` distance (back away if too close)
2. Face player
3. Call `_animator?.TriggerAttack()`
4. Instantiate `projectilePrefab` at eye position
5. `EnemyProjectile.Init(direction, speed, damage)` called on the spawned projectile
6. Wait `attackCooldown`

### 7.7 Return State

- Sets NavMeshAgent destination to `_spawnOrigin`
- On arrival (distance < 1.5m), transitions back to Patrol
- If player re-enters detection range during return, immediately transitions to Chase

### 7.8 Dead State

```
TakeDamage() reduces HP to 0
    │
    └── Die()
        ├── _state = Dead
        ├── Disable NavMeshAgent and Collider
        ├── Call _animator?.TriggerDie()
        ├── Spawn deathEffectPrefab (VFX)
        ├── Spawn lootDropPrefab (if random < dropChance)
        ├── Call EnemyManager.DeregisterEnemy(this)
        │   └── Fires EnemyManager.OnEnemyKilled(enemyName)
        │       ├── ScoreManager awards kill points
        │       ├── ObjectiveManager evaluates KillCount/KillAll
        │       └── TestMetricsCollector.RecordKill()
        └── Destroy(gameObject, 3f)
```

### 7.9 Hit Flash Effect

On every `TakeDamage()` call (while alive), the enemy material flashes red:

```csharp
IEnumerator FlashRed()
{
    _mpb.SetColor("_Color", Color.red);
    _renderer.SetPropertyBlock(_mpb);
    yield return new WaitForSeconds(0.08f);
    _mpb.SetColor("_Color", _originalColor);
    _renderer.SetPropertyBlock(_mpb);
}
```

Uses `MaterialPropertyBlock` to avoid allocating new Material instances — important for performance with many enemies.

### 7.10 Decoy Distraction System

**Called By:** `DecoyDevice.EmitNoisePulse()`

```csharp
public void DistractTo(Vector3 position)
{
    _decoyPosition = position;
    _decoyTimer = 8f;         // Distraction lasts 8 seconds
    if (_state != EnemyState.Dead)
        _state = EnemyState.Chase;
}
```

**In Chase Update:**
```csharp
if (_isDistracted)   // _decoyTimer > 0
    agent.destination = _decoyPosition;
else
    agent.destination = player.position;
```

When the enemy arrives at the decoy position or the timer expires, normal chase behavior resumes.

### 7.11 DDA Integration

On `Awake()`, each `EnemyAI` calls:
```csharp
DifficultySettings settings = DifficultyManager.Instance.BuildSettings(currentTier);
ApplyDifficultySettings(settings);
```

`ApplyDifficultySettings()` multiplies all numeric stats (from `EnemyStats`) by the relevant multiplier in the settings struct. **Base stats are preserved** in separate fields, so subsequent tier changes do not compound.

### 7.12 Animator Bridges

| Bridge | Enemy | Key Overrides |
|---|---|---|
| `MutantAnimatorBridge` | Mutant | `attackHitDelay = 0.6f` — hit registered 600ms into attack animation |
| `DroneScoutBridge` | Scout | `attackHitDelay` at projectile spawn timing |
| `BossAnimatorBridge` | Boss | Triggers for jump, slam, phase 2 roar |

---

## 8. Boss System

### 8.1 `BossController`

**File:** `Scripts/Boss/BossController.cs`
**Implements:** `IEnemy`, `IDamageable`

The Stage 5 boss enemy. Uses a 5-state FSM with a phase 2 trigger at 50% HP.

**States:**

```csharp
public enum BossState
{
    Idle,    // Wait until player enters detection range
    Chase,   // Pursue player via NavMeshAgent
    Attack,  // Melee slam at close range
    Jump,    // Arc-jump to player location with AoE on landing
    Dead     // Death sequence and cleanup
}
```

### 8.2 Phase System

| Phase | Trigger | Changes |
|---|---|---|
| Phase 1 | Game start | Standard melee and jump attacks |
| Phase 2 | HP ≤ 50% of max | Speed×1.4, auto-heal every N seconds, minion wave spawned immediately, periodic waves |

**Phase 2 Activation Code:**
```csharp
if (!phase2Active && CurrentHealth <= maxHealth * phase2Threshold)
{
    phase2Active = true;
    agent.speed *= phase2SpeedBoost;          // 1.4x speed
    waveSpawner.SpawnWave();                  // Immediate minion spawn
    onPhase2Start.Invoke();                   // UnityEvent for VFX, music change
    StartCoroutine(AutoHealRoutine());
    StartCoroutine(PeriodicWaveRoutine());
}
```

### 8.3 Jump Attack System

**Trigger Conditions:**
- Player distance between `jumpMinDistance` and `jumpMaxDistance`
- Jump cooldown fully elapsed
- Not currently jumping

**Physics Calculation:**
```
1. Disable NavMeshAgent, enable Rigidbody physics
2. Determine horizontal distance to player
3. Interpolate arc angle between jumpArcAngleMax (close) and jumpArcAngleMin (far)
4. Compute required launch velocity:
       v = sqrt(distance * gravity / sin(2 * angle))
5. Decompose into horizontal (forward) and vertical (up) components
6. Apply as Rigidbody.velocity
7. Wait for OnCollisionEnter (landing) or timeout
8. Re-enable NavMeshAgent, disable Rigidbody
9. Spawn landingVFX
10. Physics.OverlapSphere(radius=jumpDamageRadius) → TakeDamage(jumpDamage) on all IDamageable
```

### 8.4 `WaveSpawner`

**File:** `Scripts/Boss/WaveSpawner.cs`

Spawns a wave of minion enemies at predefined spawn points.

**Configuration:**

| Field | Description |
|---|---|
| `enemyPrefabs[]` | Pool of enemy prefabs to cycle through |
| `spawnPoints[]` | Transform array of spawn locations in the arena |
| `spawnDelay` | Seconds between each spawn (for dramatic effect) |

**`SpawnWave()` Coroutine:**
```
For each spawnPoint:
    ├── Instantiate enemyPrefabs[i % prefabs.Length] at spawnPoint
    ├── Newly spawned EnemyAI.Start() registers itself with EnemyManager
    └── Yield spawnDelay seconds
```

### 8.5 `BossHealthBar`

**File:** `Scripts/Boss/BossHealthBar.cs`

A world-space or screen-space UI element that tracks `BossController.CurrentHealth / maxHealth`. Subscribes to the boss's `OnHealthChanged` event and updates the slider fill amount.

---

## 9. Objective System

### 9.1 `ObjectiveManager`

**File:** `Scripts/Managers/ObjectiveManager.cs`
**Pattern:** Singleton + Observer

Tracks all stage objectives, evaluates completion conditions each frame, and updates the HUD.

**Initialization:**
```csharp
void Start()
{
    foreach (var sobjective in stageObjectives)
        _runtimes.Add(new ObjectiveRuntime(sobjective));  // Wrap in mutable runtime

    EnemyManager.Instance.OnEnemyKilled += OnEnemyKilled;
    InventoryManager.Instance.OnKeyItemAdded += OnKeyItemAdded;
}
```

**Evaluation on Enemy Killed:**
```csharp
void OnEnemyKilled(string name)
{
    foreach (var rt in _runtimes)
        if (rt.data.objectiveType == ObjectiveType.KillAll ||
            rt.data.objectiveType == ObjectiveType.KillCount)
            rt.Evaluate();
    RefreshHUD();
    CheckAllComplete();
}
```

**`CheckAllComplete()`:** If all `_runtimes` report `IsComplete == true`, fires `OnAllObjectivesComplete` event, which `LevelManager` subscribes to for showing the "Next Level" button.

### 9.2 Objective HUD Format

```
Objectives:
  [DONE] Kill all enemies
  • Collect Access Key      [0/1]
  • Activate 4 terminals    [2/4]
```

Progress brackets are computed live each frame from `EnemyManager.KillCount` and similar counters.

---

## 10. Inventory & Ammo System

### 10.1 `InventoryManager`

**File:** `Scripts/Managers/InventoryManager.cs`
**Pattern:** Singleton

Central store for all ammo counts and collected key items.

**Ammo Data Structure (per weapon type):**

```csharp
Dictionary<WeaponType, int> _currentAmmo;   // Magazine (active rounds)
Dictionary<WeaponType, int> _reserveAmmo;   // Reserve (backpack rounds)
Dictionary<WeaponType, int> _magazineSize;  // Max magazine capacity
Dictionary<WeaponType, int> _maxReserve;    // Max reserve cap
```

**WeaponType Enum:**
```csharp
public enum WeaponType { Pistol, Rifle, Shotgun, Grenade, Decoy }
```

**Key Methods:**

| Method | Description |
|---|---|
| `UseAmmo(type)` | Decrement magazine. Returns `false` if magazine empty. |
| `AddAmmo(type, amount)` | Add to reserve (capped at maxReserve). |
| `Reload(type)` | Transfer min(reserve, magazineSize - current) from reserve to magazine. |
| `GiveWeaponAmmo(type, reserve)` | Full magazine + given reserve amount (for weapon pickups). |
| `AddKeyItem(id)` | Add string ID to HashSet. Fires `OnKeyItemAdded(id)`. |
| `HasKeyItem(id)` | Returns `true` if HashSet contains ID. |

**Events:**

| Event | Fires When |
|---|---|
| `OnAmmoChanged(type, current, reserve)` | Every `UseAmmo`, `AddAmmo`, or `Reload` call |
| `OnKeyItemAdded(itemId)` | New key item collected |

---

## 11. Dynamic Difficulty Adjustment (DDA)

### 11.1 `DifficultyManager`

**File:** `Scripts/Managers/DifficultyManager.cs`
**Pattern:** Singleton + Observer

Monitors player shooting accuracy over a rolling time window and dynamically adjusts enemy statistics to maintain engagement.

### 11.2 Difficulty Tiers

```csharp
public enum DifficultyTier { Easy, Normal, Hard }
```

**Tier Thresholds:**

| Tier | Accuracy Range | Interpretation |
|---|---|---|
| Easy | < 30% | Player struggling; reduce enemy threat |
| Normal | 30% – 65% | Balanced; no adjustment needed |
| Hard | > 65% | Player excelling; increase challenge |

### 11.3 Accuracy Measurement

```csharp
// Called by WeaponController on every shot
public void ReportShot(bool hit)
{
    _shotsFiredinWindow++;
    if (hit) _shotsHitInWindow++;
}

// Evaluated every evaluationInterval seconds
void EvaluateDifficulty()
{
    if (_shotsFiredinWindow < minShotsBeforeEval) return;

    float accuracy = (float)_shotsHitInWindow / _shotsFiredinWindow;
    DifficultyTier newTier = accuracy < 0.30f ? DifficultyTier.Easy
                           : accuracy > 0.65f ? DifficultyTier.Hard
                                              : DifficultyTier.Normal;

    _shotsFiredinWindow = 0;   // Reset window
    _shotsHitInWindow = 0;

    if (newTier != _currentTier)
        SetTier(newTier);
}
```

### 11.4 Stat Multipliers

**`DifficultySettings` Struct:**

| Stat Multiplier | Easy | Normal | Hard |
|---|---|---|---|
| `healthMult` | 0.70 | 1.00 | 1.50 |
| `patrolSpeedMult` | 0.80 | 1.00 | 1.20 |
| `chaseSpeedMult` | 0.70 | 1.00 | 1.60 |
| `damageMult` | 0.60 | 1.00 | 2.00 |
| `detectionMult` | 0.80 | 1.00 | 1.40 |
| `attackRangeMult` | 0.90 | 1.00 | 1.30 |
| `cooldownMult` | 1.50 | 1.00 | 0.60 |

*(Lower cooldown multiplier in Hard = faster attacks)*

### 11.5 Application to Enemies

```csharp
void SetTier(DifficultyTier newTier)
{
    _currentTier = newTier;
    var settings = BuildSettings(newTier);

    // Apply to all currently alive enemies
    foreach (var enemy in FindObjectsByType<EnemyAI>())
        enemy.ApplyDifficultySettings(settings);

    OnTierChanged?.Invoke(newTier);
    TestMetricsCollector.Instance?.RecordDDAChange(previousTier, newTier, lastAccuracy);
}
```

**Important:** `ApplyDifficultySettings()` in `EnemyAI` multiplies *base stats* (stored separately at initialization) by the new multiplier. This prevents compounding across tier changes.

### 11.6 Events

| Event | Subscribers |
|---|---|
| `OnTierChanged(DifficultyTier)` | `DifficultyHUD` (shows current tier label) |

---

## 12. Audio Systems

### 12.1 `AudioManager` (Background Music)

**File:** `Scripts/Managers/AudioManager.cs`
**Pattern:** Singleton

Manages seamless crossfading between exploration and combat music based on enemy presence.

**Dual AudioSource Approach:**

```
Source A (active)    Source B (inactive)
     │                    │
     │ crossfade triggered │
     v                    v
  Volume: 1→0          Volume: 0→1    (over crossfadeDuration seconds)
```

**Combat Detection Logic (checked every second):**
```csharp
if (EnemyManager.Instance.AliveCount > 0)
{
    _combatTimer = combatCooldown;    // Reset cooldown
    if (currentMusic != combatMusic)
        StartCrossfade(combatMusic);
}
else
{
    _combatTimer -= Time.deltaTime;
    if (_combatTimer <= 0 && currentMusic != explorationMusic)
        StartCrossfade(explorationMusic);
}
```

---

### 12.2 `SFXManager` (Sound Effects)

**File:** `Scripts/Managers/SFXManager.cs`
**Pattern:** Singleton

Centralized playback of all sound effects, supporting both 2D (UI, weapons) and 3D (world events) audio.

**2D Sounds (played on persistent AudioSource):**

| Method | Trigger |
|---|---|
| `PlayRifleShot()` | WeaponController fires Rifle |
| `PlayPistolShot()` | WeaponController fires Pistol |
| `PlayEmptyClick()` | Fire attempted with empty magazine |
| `PlayReload()` | Reload coroutine starts |
| `PlayPlayerHurt()` | PlayerController.TakeDamage() |
| `PlayPickup()` | Any pickup collected |
| `PlayThrow()` | Decoy or object thrown |

**3D Sounds (AudioSource.PlayClipAtPoint at world position):**

| Method | Position |
|---|---|
| `PlayExplosion(pos)` | Barrel/grenade explosion location |
| `PlayEnemyDeath(pos)` | Enemy death location |
| `PlayBossStomp(pos)` | Boss jump landing location |

---

## 13. Interactable Systems

### 13.1 `SwitchInteractable` (Terminals)

**File:** `Scripts/Interactables/SwitchInteractable.cs`
**Implements:** `IInteractable`

A toggleable terminal or switch that can fire `UnityEvent`s and report to the `ObjectiveManager`.

**Key Fields:**

| Field | Description |
|---|---|
| `switchId` | Unique identifier for objective tracking |
| `requiredItemIds[]` | Key item IDs required before activation is allowed |
| `isToggleable` | Can be switched off after activation |
| `onSwitchActivated` | UnityEvent fired when switched ON |
| `onSwitchDeactivated` | UnityEvent fired when switched OFF |

**`Interact()` Flow:**
```
1. Check InventoryManager.HasKeyItem() for all requiredItemIds
   └── If any missing: show "Item required" prompt, return
2. Toggle IsOn state
3. Animate handle rotation (OFF pos → ON pos, or reverse)
4. Fire relevant UnityEvent
5. Notify ObjectiveManager.NotifySwitchActivated(switchId)
```

**Proximity UX:**
- Outline fades in on hover (detected via `OnTriggerEnter` on proximity collider)
- World-space label shows "[E] Interact" or "Access Key Required"
- Label fades out when player moves away

---

### 13.2 `DoorInteractable` (Next Level Gate)

**File:** `Scripts/Interactables/DoorInteractable.cs`
**Implements:** `IInteractable`

A stage exit door with configurable requirements.

**Requirements (all must be met to open):**

| Field | Condition |
|---|---|
| `requiredKeyId` | Must be in InventoryManager |
| `requireAllObjectives` | ObjectiveManager.AreAllComplete() must be true |

**`Interact()` Flow:**
```
1. Check InventoryManager.HasKeyItem(requiredKeyId)
   └── Fail: show "Access Key Required"
2. If requireAllObjectives:
   └── Check ObjectiveManager.AreAllComplete()
       └── Fail: show "Complete all objectives first"
3. Call OpenDoor()
   ├── If nextSceneBuildIndex >= 0: SceneManager.LoadScene(index)
   ├── Elif LevelManager configured: LevelManager.LoadNextLevel()
   └── Elif last level: GameManager.TriggerStageWin()
```

---

### 13.3 Pickup System

All pickups are `MonoBehaviour` components on trigger colliders that detect the player on `OnTriggerEnter`.

| Class | Pickup Type | Action |
|---|---|---|
| `WeaponPickup` | Weapon drop | `InventoryManager.GiveWeaponAmmo(type, reserve)` |
| `AmmoPickup` | Ammo refill | `InventoryManager.AddAmmo(type, amount)` |
| `FoodPickup` | Health | `PlayerController.Heal(healAmount)` |
| `ObjectivePickup` | Key item | `InventoryManager.AddKeyItem(itemId)`, UIManager notification |

All pickups play a pickup SFX and destroy themselves after collection.

---

### 13.4 Pressure Plate Puzzle System

**Files:** `Scripts/Interactables/Puzzle/`

A multi-part environmental puzzle where players must place heavy physics boxes on plates to open a door.

**`PressurePlate`:**

| Field | Description |
|---|---|
| `minMass` | Minimum Rigidbody mass to trigger activation |
| `lockOnActivation` | If true, plate stays active once triggered |
| `onActivated` | UnityEvent: turn plate green, notify controller |
| `onDeactivated` | UnityEvent: revert state if not locked |

**`PlateDoorController`:**

- References an array of `PressurePlate` objects
- Maintains a count of currently active plates
- Fires `onAllActivated` only when `activeCount == plates.Length`
- Reports progress to `ObjectiveManager`: `"Activate 3 pressure plates  [1/3]"`
- Fires `onAnyDeactivated` if any plate loses its trigger object (object removed)

**`PressurePlateHeavyBox`:**

- Marks a physics box as a puzzle piece
- When the player picks up this box, **all pressure plates in the scene are highlighted** (yellow outline)
- Provides a visual cue that this object goes on a plate

**Complete Puzzle Flow:**
```
1. Player enters scene; sees locked door, unlit pressure plates
2. Player finds HeavyBox
3. Player picks up HeavyBox → all plates highlight yellow
4. Player places box on plate 1 → plate turns green
5. Player finds box 2, places on plate 2 → plate turns green
6. All plates active → PlateDoorController fires onAllActivated
   └── Door animates open, objective marked complete
```

---

### 13.5 Destructibles

**`ExplosiveBarrel`** (`Scripts/Interactables/ExplosiveBarrel.cs`):

```
TakeDamage(amount):
├── HP -= amount
└── If HP <= 0:
    ├── Spawn explosionVFX at self position
    ├── CameraShake.Instance.Shake(0.4f, 0.5f)
    ├── SFXManager.PlayExplosion(position)
    ├── Physics.OverlapSphere(explosionRadius):
    │   └── For each IDamageable in sphere:
    │       └── TakeDamage(explosionDamage * falloff)
    └── Disable renderer/collider, Destroy(2f)
```

**`DestructibleCrate`** (`Scripts/Interactables/DestructibleCrate.cs`):

- Same HP/death flow as barrel
- On death: call `LootDropper.Drop()` based on `lootDropChance`
- `LootDropper` selects from weighted loot table (AmmoPickup, FoodPickup prefabs)

---

## 14. Decoy Weapon System

### 14.1 Decoy Launcher Integration

The Decoy is Weapon Index 2 in `WeaponController`, configured with `mode = Launcher`.

```csharp
WeaponSlot decoySlot = new WeaponSlot
{
    type             = WeaponType.Decoy,
    mode             = WeaponMode.Launcher,
    fireRate         = 0.1f,
    projectilePrefab = decoyDevicePrefab,
    throwForce       = 15f,
    throwUpAngle     = 20f,
    unlockedFromBuildIndex = 2   // Available from Scene 2 onwards
};
```

**`FireLauncher()` in WeaponController:**
```csharp
Vector3 dir = Quaternion.AngleAxis(-throwUpAngle, camera.right) * camera.forward;
GameObject obj = Instantiate(projectilePrefab, barrelTip.position, Quaternion.identity);
obj.GetComponent<Rigidbody>().linearVelocity = dir * throwForce;
```

---

### 14.2 `DecoyDevice`

**File:** `Scripts/Devices/DecoyDevice.cs`

**Lifecycle:**

```
[Instantiated by WeaponController]
      │
      │ (Physics arc through air)
      │
      └─ OnCollisionEnter(collision)
          ├── Rigidbody.velocity = Vector3.zero (stick to surface)
          ├── Rigidbody.isKinematic = true
          └── StartCoroutine(ActivateAfterDelay())
              ├── Wait activationDelay (e.g., 0.5s)
              ├── Instantiate activateEffect (smoke VFX)
              ├── Enable decoyLight (point light flicker)
              └── Start pulse loop:
                  ├── EmitNoisePulse()  ←──────────────────┐
                  │   ├── FindObjectsByType<EnemyAI>()      │
                  │   └── For each within distractRadius:   │
                  │       └── enemy.DistractTo(position)    │
                  └── Yield pulseInterval ────────────────>─┘
                  (loop until distractDuration elapsed)
                  └── Destroy(gameObject)
```

**`EmitNoisePulse()` Detail:**

```csharp
void EmitNoisePulse()
{
    var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
    foreach (var e in enemies)
    {
        float dist = Vector3.Distance(transform.position, e.transform.position);
        if (dist <= distractRadius)
            e.DistractTo(transform.position);
    }
}
```

**Key Configuration Values:**

| Field | Default | Description |
|---|---|---|
| `activationDelay` | `0.5s` | Delay between landing and first pulse |
| `pulseInterval` | `1.5s` | Time between distraction pulses |
| `distractDuration` | `8s` | Total active lifetime |
| `distractRadius` | `15m` | Radius of noise pulse |

---

## 15. AI Core Healing System

**File:** `Scripts/Managers/AICoreManager.cs`

Stage 4 specific mechanic: an active AI core that continuously heals all enemies while the player has not deactivated it.

**Initialization:**

```csharp
void Start()
{
    StartCoroutine(HealRoutine());
    if (blinkBanner) StartCoroutine(BlinkBanner());
    healingWarningBanner.SetActive(true);
}
```

**`HealRoutine()` Coroutine:**
```csharp
while (_isActive)
{
    yield return new WaitForSeconds(healInterval);
    var enemies = FindObjectsByType<EnemyAI>();
    foreach (var e in enemies)
        if (e.IsAlive) e.Heal(healAmount);
}
```

**Deactivation:**

Called when `TerminalGroupController.onAllDeactivated` fires (all 4 terminals deactivated):

```csharp
public void Deactivate()
{
    _isActive = false;
    StopAllCoroutines();
    healingWarningBanner.SetActive(false);
    onDeactivated.Invoke();   // Spawns Access Key, removes warning UI
}
```

**`TerminalGroupController`:**
- References 4 `SwitchInteractable` terminals
- Counts activations/deactivations
- When all 4 are in "OFF" state, fires `onAllDeactivated`

---

## 16. Level & Scene Management

### 16.1 `LevelManager`

**File:** `Scripts/Managers/LevelManager.cs`
**Pattern:** Singleton

Per-scene coordinator for stage lifecycle events.

**`Start()` Actions:**
1. Clear key items from previous stage (prevents carry-over exploits):
   ```csharp
   InventoryManager.Instance.ClearKeyItems();
   ```
2. Subscribe to `ObjectiveManager.OnAllObjectivesComplete`
3. Call `GameManager.StartInstructions()` if `hasPreGameInstructions`

**On All Objectives Complete:**
```csharp
void OnAllObjectivesComplete()
{
    if (nextSceneBuildIndex >= 0)
        ShowNextLevelButton();
    else
        GameManager.Instance.TriggerStageWin();
}
```

**Scene Build Index Order:**

| Index | Scene |
|---|---|
| 0 | MainMenu |
| 1 | Landing Stage |
| 2 | Engineering Sector |
| 3 | Bio-Lab |
| 4 | AI Core Control |
| 5 | Reactor (Final Boss) |

---

### 16.2 `SaveManager`

**File:** `Scripts/Managers/SaveManager.cs`

Persistence layer using `PlayerPrefs`.

**Save Data:**

| Key | Value | Meaning |
|---|---|---|
| `"SavedScene"` | Build index (int) | Which scene to load on Continue |
| `"HasSave"` | 1 | Save file exists flag |

**Methods:**

| Method | Description |
|---|---|
| `SaveProgress(sceneIndex)` | Write current or next scene index to PlayerPrefs |
| `LoadSavedGame()` | Read saved index, call SceneManager.LoadScene |
| `DeleteSave()` | Remove keys from PlayerPrefs (New Game) |
| `HasSave()` | Returns true if "HasSave" key exists |

---

## 17. Score System

### 17.1 `ScoreManager`

**File:** `Scripts/Managers/ScoreManager.cs`
**Pattern:** Singleton

Tracks all statistics during a play session and calculates a final score on stage completion.

**Tracked Statistics:**

| Stat | Type | Description |
|---|---|---|
| `TotalKills` | `int` | All confirmed enemy kills |
| `TotalShots` | `int` | All fired shots |
| `TotalHits` | `int` | Successful damaging hits |
| `Deaths` | `int` | Player death count |
| `ElapsedSeconds` | `float` | Session playtime (pauses on game over/win) |
| `RawKillPoints` | `int` | Cumulative kill-based points |

**Kill Point Values by Enemy Type:**

| Enemy | Points |
|---|---|
| Grunt | 100 |
| Scout | 150 |
| Berserker | 200 |
| Mutant | 175 |
| Heavy | 125 |
| Boss | 1,000 |
| Unknown | 100 |

**Bonus Points:**

| Condition | Bonus |
|---|---|
| Accuracy ≥ 70% | +500 pts |
| Accuracy ≥ 50% | +250 pts |
| Completion time < 5 minutes | +200 pts |
| Stage completed | +300 pts |

**Death Penalty:** −100 pts per death

**Grade Thresholds:**

| Grade | Minimum Score |
|---|---|
| S | 3,000 |
| A | 2,000 |
| B | 1,000 |
| C | 500 |
| D | < 500 |

**`CalculateFinalScore()` Return Value:**
```csharp
public ScoreResult CalculateFinalScore()
{
    float accuracy = TotalShots > 0 ? (float)TotalHits / TotalShots : 0f;
    int totalScore = RawKillPoints
                   + GetAccuracyBonus(accuracy)
                   + GetTimeBonus(ElapsedSeconds)
                   + (stageCompleted ? 300 : 0)
                   - (Deaths * 100);
    return new ScoreResult { total = totalScore, grade = GetGrade(totalScore), ... };
}
```

---

## 18. UI Systems

### 18.1 `UIManager`

**File:** `Scripts/Managers/UIManager.cs`
**Pattern:** Singleton

Central UI controller managing all panels, HUD elements, and player-facing information.

**UI Panels:**

| Panel | Shown When |
|---|---|
| **HUD** | Always during Playing state |
| **Pause Menu** | GameState == Paused |
| **Game Over Screen** | GameState == GameOver |
| **Win Screen** | GameState == Win |
| **Instructions Panel** | GameState == Instructions |
| **Storyboard Panel** | GameState == Storyboard |
| **Difficulty HUD** | DifficultyManager.OnTierChanged fires |

**HUD Elements:**

| Element | Updates From |
|---|---|
| Ammo counter (current/reserve) | `InventoryManager.OnAmmoChanged` |
| Health bar | `PlayerController.OnHealthChanged` |
| Objective text list | `ObjectiveManager.GetAllDisplayText()` per frame |
| Crosshair | Always visible during Playing |
| Key item icons | `InventoryManager.OnKeyItemAdded` |
| Difficulty tier badge | `DifficultyManager.OnTierChanged` |
| Boss health bar | `BossController.OnHealthChanged` (Stage 5 only) |

**Key Methods:**

| Method | Description |
|---|---|
| `ShowPauseMenu()` | Activate pause panel, unlock cursor |
| `HidePauseMenu()` | Deactivate pause panel, lock cursor |
| `ShowGameOver()` | Activate game over panel, show final stats |
| `ShowWinScreen(score)` | Activate win panel with ScoreResult breakdown |
| `ShowKeyItemByIdAndName(id, name)` | Animate key item icon into HUD |
| `UpdateAmmoDisplay(type, cur, res)` | Refresh ammo counter text |
| `UpdateObjectiveText(lines[])` | Rewrite objective list in HUD |

---

### 18.2 `MainMenuManager`

**File:** `Scripts/Managers/MainMenuManager.cs`

Handles all Scene 0 UI button callbacks.

| Button | Action |
|---|---|
| New Game | `SaveManager.DeleteSave()` → `SceneManager.LoadScene(1)` |
| Continue | `SaveManager.LoadSavedGame()` (hidden if no save exists) |
| Credits | Activate credits panel |
| Back | Deactivate credits panel |
| Quit | `Application.Quit()` |

---

## 19. Test & Metrics Systems

The project includes a dedicated metrics layer for academic research data collection, designed to be non-invasive to gameplay.

### 19.1 `TestMetricsCollector`

**File:** `Scripts/Test/TestMetricsCollector.cs`
**Pattern:** Singleton

Central data store for all research-relevant events.

**Tracked Data Structures:**

```csharp
// Shooting accuracy
public int TotalShots { get; private set; }
public int TotalHits  { get; private set; }
public float Accuracy => TotalShots > 0 ? (float)TotalHits / TotalShots : 0f;

// Enemy kills
public int KillCount { get; private set; }

// DDA history
public List<DDARecord> DDAHistory;
public struct DDARecord
{
    public float timestamp;
    public DifficultyTier fromTier;
    public DifficultyTier toTier;
    public float accuracyAtChange;
}

// FSM state transition history
public List<FSMRecord> FSMHistory;
public struct FSMRecord
{
    public float timestamp;
    public string enemyName;
    public EnemyState fromState;
    public EnemyState toState;
}

// Enemy detection events
public List<DetectionRecord> DetectionHistory;
public struct DetectionRecord
{
    public float timestamp;
    public string enemyName;
    public float responseTimeSeconds;  // Time from player entering range to detection
}
```

**Recording Methods:**

| Method | Called By |
|---|---|
| `RecordShot(bool hit)` | `WeaponController.TryFire()` |
| `RecordKill()` | `EnemyManager.OnEnemyKilled` event handler |
| `RecordDDAChange(from, to, accuracy)` | `DifficultyManager.SetTier()` |
| `RecordFSMTransition(name, from, to)` | `EnemyAI` on every state transition |
| `RecordDetection(name, responseTime)` | `EnemyAI.CanSeePlayer()` first positive result |

---

### 19.2 `TestDataExporter`

**File:** `Scripts/Test/TestDataExporter.cs`

Serializes all `TestMetricsCollector` data to CSV files for analysis.

**Export Files Generated:**

| File | Contents |
|---|---|
| `accuracy_log.csv` | Per-shot hit/miss records with timestamp |
| `dda_changes.csv` | All tier changes with accuracy and timestamp |
| `fsm_transitions.csv` | All FSM state changes across all enemies |
| `detection_events.csv` | Enemy detection response times |
| `session_summary.csv` | Aggregate stats (total kills, accuracy, session duration) |

**Trigger:** Export called on `GameManager.OnStageWin` or `OnGameOver`, or via a manual debug button.

---

### 19.3 `TestMetricsHUD`

**File:** `Scripts/Test/TestMetricsHUD.cs`

An overlay panel (toggled by a debug key, e.g., F1) showing real-time metrics:
- Current accuracy percentage
- Total shots / hits
- Kill count
- Current DDA tier
- Session time

---

## 20. Design Patterns Employed

| Pattern | Where Applied | Purpose |
|---|---|---|
| **Singleton** | All Manager classes | Global access without parameter passing; single source of truth |
| **Observer / Event System** | `GameManager.OnStateChanged`, `EnemyManager.OnEnemyKilled`, `InventoryManager.OnAmmoChanged`, etc. | Decoupled communication; UI reacts to model changes without tight coupling |
| **State Machine** | `EnemyAI` (5 states), `BossController` (5 states), `GameManager` (6 states) | Explicit, debuggable AI and game flow management |
| **Strategy** | `WeaponMode` (Raycast vs. Launcher), `IDamageable` (any damage target) | Swap firing behavior without inheritance; damage-agnostic weapons |
| **Visitor** | `IInteractable.Interact(player)` | Player visits any interactable; polymorphic handling without type checks |
| **Factory** | `WaveSpawner.SpawnWave()`, `LootDropper.Drop()` | Decouple object creation from usage |
| **Null Object** | `IEnemyAnimator` optional bridge | Enemies without animators work without null checks at call sites |
| **Decorator** | `DamageRelay` on child colliders | Extend damage behavior of child objects without modifying them |
| **Command** | `UnityEvent` on `SwitchInteractable` | Designer-wired behaviors without hardcoded references |
| **Data Object** | `EnemyStats`, `Objective` ScriptableObjects | Separate data from behavior; designer-configurable without code |

---

## 21. Key System Interactions (Data Flow Diagrams)

### Scenario 1: Player Fires Weapon and Kills Enemy

```
WeaponController.TryFire()
   │
   ├─ InventoryManager.UseAmmo(type)
   │    └─ Returns false → SFXManager.PlayEmptyClick() [EXIT]
   │
   ├─ PerformRaycast()
   │    ├─ Physics.Raycast(camera, range)
   │    └─ hit.collider.GetComponent<IDamageable>()
   │         └─ EnemyAI.TakeDamage(damage)
   │              ├─ HP -= damage
   │              ├─ FlashRed() coroutine
   │              └─ HP <= 0 → Die()
   │                   ├─ Disable Agent + Collider
   │                   ├─ Spawn deathVFX
   │                   ├─ Spawn loot (random)
   │                   ├─ _animator?.TriggerDie()
   │                   └─ EnemyManager.DeregisterEnemy(this)
   │                        └─ OnEnemyKilled(name) event
   │                             ├─ ScoreManager.AddKillPoints(name)
   │                             ├─ ObjectiveManager.OnEnemyKilled()
   │                             │    └─ Evaluate KillAll/KillCount objectives
   │                             │         └─ If all complete → OnAllObjectivesComplete
   │                             │              └─ LevelManager.ShowNextLevelButton()
   │                             └─ TestMetricsCollector.RecordKill()
   │
   ├─ SpawnMuzzleFlash()
   ├─ SFXManager.PlayRifleShot()
   ├─ InventoryManager.OnAmmoChanged → UIManager.UpdateAmmoDisplay()
   └─ DifficultyManager.ReportShot(hit)
        └─ EvaluateDifficulty() [if interval elapsed]
             └─ If tier changes → SetTier(newTier)
                  ├─ ApplyDifficultySettings() on all EnemyAI
                  ├─ OnTierChanged → DifficultyHUD.UpdateLabel()
                  └─ TestMetricsCollector.RecordDDAChange()
```

---

### Scenario 2: Player Opens Stage Exit Door

```
PlayerController.HandleInteraction() [E key pressed]
   │
   └─ Raycast on Interactable layer
        └─ DoorInteractable.Interact(player)
             ├─ InventoryManager.HasKeyItem(requiredKeyId)?
             │    └─ false → UIManager.ShowLabel("Access Key Required") [EXIT]
             │
             ├─ ObjectiveManager.AreAllComplete()?
             │    └─ false → UIManager.ShowLabel("Complete all objectives first") [EXIT]
             │
             └─ OpenDoor()
                  ├─ SceneManager.LoadScene(nextSceneBuildIndex)
                  │    └─ New scene: LevelManager.Start() clears key items
                  │
                  └─ OR GameManager.TriggerStageWin() [last stage]
                       ├─ GameState = Win
                       ├─ timeScale = 0
                       ├─ ScoreManager.CalculateFinalScore()
                       └─ UIManager.ShowWinScreen(scoreResult)
```

---

### Scenario 3: Boss Phase 2 Activation

```
BossController.Update() [every frame]
   │
   └─ UpdateChase()
        └─ CurrentHealth <= maxHealth * 0.5f AND !phase2Active
             │
             ├─ phase2Active = true
             ├─ agent.speed *= 1.4f
             ├─ onPhase2Start.Invoke() → VFX, music change
             ├─ waveSpawner.SpawnWave()
             │    └─ Coroutine: Instantiate enemies at spawn points (with delay)
             │         └─ Each EnemyAI.Start() → EnemyManager.RegisterEnemy()
             │
             ├─ StartCoroutine(AutoHealRoutine())
             │    └─ Every healInterval: CurrentHealth += healAmount
             │
             └─ StartCoroutine(PeriodicWaveRoutine())
                  └─ Every waveInterval: waveSpawner.SpawnWave()
```

---

### Scenario 4: Decoy Throw and Enemy Distraction

```
WeaponController.FireLauncher() [fire input, Decoy selected]
   │
   ├─ Instantiate(decoyDevicePrefab, barrelTip.position)
   └─ Rigidbody.velocity = (cameraForward.rotated +20° up) * throwForce
        │
        │ [Physics simulation: arc trajectory]
        │
        └─ DecoyDevice.OnCollisionEnter()
             ├─ Rigidbody.isKinematic = true (stick)
             └─ StartCoroutine(ActivateAfterDelay(0.5s))
                  ├─ Instantiate activateEffect VFX
                  ├─ Enable decoyLight
                  └─ Loop every pulseInterval:
                       └─ EmitNoisePulse()
                            └─ For each EnemyAI within 15m:
                                 └─ enemy.DistractTo(decoyPos)
                                      ├─ _decoyPosition = decoyPos
                                      ├─ _decoyTimer = 8f
                                      └─ _state = Chase
                                           └─ UpdateChase():
                                                └─ agent.destination = _decoyPosition
                                                     └─ [Enemy walks to decoy, ignores player]
```

---

### Scenario 5: DDA Tier Upgrade (Easy → Hard)

```
[Player shoots accurately over 5-second window]
   │
   WeaponController.TryFire() [every shot]
   └─ DifficultyManager.ReportShot(hit=true)
        └─ _shotsHitInWindow++, _shotsFiredinWindow++
             │
             └─ EvaluateDifficulty() [after evaluationInterval seconds]
                  ├─ accuracy = 18/20 = 90% → DifficultyTier.Hard
                  ├─ 90% ≠ current Easy tier → SetTier(Hard)
                  │
                  ├─ BuildSettings(Hard) → multipliers {health:1.5, speed:1.6, damage:2.0, ...}
                  ├─ For each EnemyAI in scene:
                  │    └─ ApplyDifficultySettings(settings)
                  │         └─ maxHealth = baseHealth * 1.5
                  │            chaseSpeed = baseChaseSpeed * 1.6
                  │            attackDamage = baseDamage * 2.0
                  │            attackCooldown = baseCooldown * 0.6  (faster)
                  │
                  ├─ OnTierChanged(Hard) → DifficultyHUD.UpdateLabel("HARD")
                  └─ TestMetricsCollector.RecordDDAChange(Easy, Hard, 0.90f)
```

---

## 22. Complete Script File Manifest

### Managers (14 files)

| File | Responsibility |
|---|---|
| `GameManager.cs` | Game state machine, pause, win/lose conditions |
| `LevelManager.cs` | Stage lifecycle, objective completion, next level flow |
| `UIManager.cs` | All UI panels, HUD updates, panel visibility |
| `InventoryManager.cs` | Ammo tracking (magazine + reserve), key item registry |
| `EnemyManager.cs` | Enemy registry, alive count, kill events |
| `ScoreManager.cs` | Kill points, accuracy, time bonuses, grade calculation |
| `ObjectiveManager.cs` | Objective evaluation, HUD text, completion events |
| `DifficultyManager.cs` | DDA: accuracy window, tier thresholds, stat multipliers |
| `AudioManager.cs` | Exploration/combat music crossfading |
| `SFXManager.cs` | 2D and 3D sound effect playback |
| `SaveManager.cs` | PlayerPrefs persistence for save/load/continue |
| `TerminalGroupController.cs` | Multi-terminal group: fires event when all deactivated |
| `MainMenuManager.cs` | Scene 0 menu button callbacks |
| `AICoreManager.cs` | Stage 4 enemy healing while active |

### Player (4 files)

| File | Responsibility |
|---|---|
| `PlayerController.cs` | FPS movement, jump, sprint, look, interaction raycast, health |
| `WeaponController.cs` | Weapon slots, raycast/launcher firing, reload, switching |
| `GrabController.cs` | Physics object pickup, rotate, throw |
| `HitEffects.cs` | Vignette, helmet cracks, camera shake, head bob |

### Enemy (6 files)

| File | Responsibility |
|---|---|
| `EnemyAI.cs` | Core FSM: Patrol, Chase, Attack, Return, Dead |
| `EnemyProjectile.cs` | Scout ranged projectile movement and collision |
| `EnemyHealthBar.cs` | World-space HP bar overlay for enemies |
| `EnemyNameLabel.cs` | World-space name display above enemy head |
| `MutantAnimatorBridge.cs` | IEnemyAnimator implementation for Mutant |
| `DroneScoutBridge.cs` | IEnemyAnimator implementation for Scout |

### Boss (4 files)

| File | Responsibility |
|---|---|
| `BossController.cs` | Boss FSM: Idle, Chase, Attack, Jump, Dead; phase 2 |
| `WaveSpawner.cs` | Spawn minion wave at defined spawn points |
| `BossHealthBar.cs` | Boss HP display (screen-space) |
| `BossAnimatorBridge.cs` | IEnemyAnimator implementation for Boss |

### Interfaces (5 files)

| File | Responsibility |
|---|---|
| `IEnemy.cs` | IsAlive contract for EnemyManager registry |
| `IDamageable.cs` | TakeDamage + IsAlive for all damage targets |
| `IInteractable.cs` | Interact(player) for all E-key interactions |
| `IEnemyAnimator.cs` | SetSpeed, TriggerAttack, TriggerDie for animation bridges |
| `DamageRelay.cs` | Forward damage from child colliders to root IDamageable |

### Interactables (13 files)

| File | Responsibility |
|---|---|
| `SwitchInteractable.cs` | Toggle terminal, fire UnityEvent, notify ObjectiveManager |
| `DoorInteractable.cs` | Stage exit with key + objective requirement checks |
| `WeaponPickup.cs` | Award weapon ammo on collect |
| `AmmoPickup.cs` | Refill reserve ammo on collect |
| `FoodPickup.cs` | Heal player on collect |
| `ObjectivePickup.cs` | Add key item to InventoryManager |
| `HoldableObject.cs` | Physics box with grab/rotate/throw controls |
| `ExplosiveBarrel.cs` | IDamageable destructible with area explosion |
| `DestructibleCrate.cs` | Breakable loot container |
| `LootDropper.cs` | Weighted random loot spawn on death |
| `PressurePlate.cs` | Mass-sensitive floor trigger |
| `PlateDoorController.cs` | Multi-plate group: all-active condition |
| `PressurePlateHeavyBox.cs` | Marks box as puzzle piece; highlights plates on pickup |

### Devices (2 files)

| File | Responsibility |
|---|---|
| `DecoyLauncher.cs` | Weapon config integration for Decoy slot |
| `DecoyDevice.cs` | Throwable distraction object: pulse, attract enemies |

### ScriptableObjects (4 files)

| File | Responsibility |
|---|---|
| `EnemyStats.cs` | Enemy archetype data (health, speed, detection, damage) |
| `Objective.cs` | Objective definition + ObjectiveRuntime mutable wrapper |
| `InstructionSlide.cs` | Tutorial slide data (image + text) |
| `StoryboardSlide.cs` | Narrative slide data (image + title + body) |

### Utilities (4 files)

| File | Responsibility |
|---|---|
| `CameraShake.cs` | Positional camera shake with easing |
| `ButtonBridge.cs` | UI button → arbitrary method forwarding |
| `ButtonDebugger.cs` | Debug tool for testing button events in editor |
| `DifficultyHUD.cs` | On-screen difficulty tier indicator |

### Test / Metrics (4 files)

| File | Responsibility |
|---|---|
| `TestMetricsCollector.cs` | Central data store for research metrics |
| `TestMetricsHUD.cs` | Real-time on-screen metrics overlay (F1 toggle) |
| `TestDataExporter.cs` | CSV export of all collected metrics |
| `EnemyTestObserver.cs` | Debug FSM state observer for enemy AI analysis |

**Total Custom Scripts: 61**

---

## 23. Scene-by-Scene Breakdown

### Scene 0 — Main Menu (Build Index 0)

**Systems Active:** `MainMenuManager`, `AudioManager`, `SaveManager`

**UI Elements:** New Game button, Continue button (hidden if no save), Credits panel, Quit button

**Flow:**
- Continue button enabled only if `SaveManager.HasSave()` returns true
- New Game: deletes save, loads Scene 1
- Continue: loads saved scene index

---

### Scene 1 — Landing Stage (Build Index 1)

**Narrative:** Player arrives at colony outer perimeter, overrun by Grunt-type enemies.

**Enemies:** 5–7 Grunt units (melee, low HP, basic detection)

**Objectives:**
- Kill all enemies (ObjectiveType.KillAll)

**Key Systems:** EnemyAI FSM (first encounter), ObjectiveManager, basic weapon unlocks

**Stage Lock:** Decoy Launcher locked (not yet unlocked)

**Exit Condition:** All Grunts dead → Access Key available → Door interaction loads Scene 2

---

### Scene 2 — Engineering Sector (Build Index 2)

**Narrative:** Internal engineering corridors with environmental hazards.

**Enemies:** Mix of Grunt + Scout (ranged behavior introduced)

**Objectives:**
- Collect Access Key (ObjectiveType.CollectItem)
- Kill 5 enemies (ObjectiveType.KillCount)

**New Mechanics:**
- Decoy Launcher unlocked (first use encouraged via tutorial)
- Explosive Barrels as environmental hazards
- Pressure plate puzzle (optional shortcut)

**Key Systems:** DecoyDevice distraction, ExplosiveBarrel chain reactions

---

### Scene 3 — Bio-Lab (Build Index 3)

**Narrative:** Bio-research lab with mutant enemies and pressure-plate puzzles.

**Enemies:** Mutant + Berserker (animated melee attackers)

**Objectives:**
- Activate all 4 pressure plates simultaneously (ObjectiveType.ActivateSwitch)

**Puzzle:** 4 pressure plates, 3 heavy boxes scattered in the lab. Player must find boxes, place them on all 4 plates (one plate requires stacking or alternative solution).

**Key Systems:** PressurePlate, PlateDoorController, PressurePlateHeavyBox, GrabController, MutantAnimatorBridge

---

### Scene 4 — AI Core Control (Build Index 4)

**Narrative:** Central AI control hub actively healing all enemies.

**Enemies:** Heavy + Berserker (8–10 total, high HP due to continuous healing)

**Objectives:**
- Deactivate all 4 AI terminals (ObjectiveType.ActivateSwitch)

**Unique Mechanic:** `AICoreManager` heals all living enemies by `healAmount` every `healInterval` seconds until all terminals are deactivated. Banner warns player of healing mechanic. On deactivation: healing stops, Access Key spawns.

**Key Systems:** AICoreManager, TerminalGroupController, SwitchInteractable

---

### Scene 5 — Reactor / Final Boss (Build Index 5)

**Narrative:** Colony reactor core, guarded by a heavily-armored Reactor Guardian.

**Enemies:** Boss (Reactor Guardian) — Phase 1 solo, Phase 2 with minion waves

**Objectives:**
- Defeat the Reactor Guardian (ObjectiveType.KillAll with boss registered)

**Boss Encounter Flow:**

| Phase | HP Range | Behavior |
|---|---|---|
| Phase 1 | 100% – 50% | Melee slam, occasional jump attack |
| Phase 2 | 50% – 0% | 1.4× speed, auto-heal, periodic minion wave spawns |

**Storyboard:** Pre-boss intro narrative plays before combat; post-boss outro plays after victory.

**Win Condition:** Boss dies → outro storyboard → return to Main Menu (or credits)

**Key Systems:** BossController, WaveSpawner, BossAnimatorBridge, StoryboardSlide, GameManager storyboard state

---

## 24. System Architecture Summary

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         GAME MANAGERS LAYER                         │
│  GameManager  LevelManager  UIManager  SaveManager  AudioManager    │
│  ScoreManager  DifficultyManager  SFXManager  ObjectiveManager      │
│                   EnemyManager  InventoryManager                    │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ Events / Singleton calls
           ┌────────────────┼─────────────────────┐
           │                │                     │
┌──────────▼───────┐ ┌──────▼────────┐ ┌──────────▼──────┐
│  PLAYER LAYER    │ │  ENEMY LAYER  │ │  WORLD LAYER    │
│                  │ │               │ │                 │
│ PlayerController │ │  EnemyAI FSM  │ │ SwitchInteract  │
│ WeaponController │ │  BossCtrl FSM │ │ DoorInteract    │
│ GrabController   │ │  WaveSpawner  │ │ ExplosiveBarrel │
│ HitEffects       │ │  EnemyStats   │ │ PressurePlate   │
└────────┬─────────┘ └──────┬────────┘ │ ObjectivePickup │
         │                  │          │ AICoreManager   │
         │ IDamageable       │ IEnemy   └─────────────────┘
         └──────────────────┘
                  │
         ┌────────▼──────────┐
         │  INTERFACE LAYER  │
         │  IDamageable      │
         │  IInteractable    │
         │  IEnemyAnimator   │
         │  IEnemy           │
         │  DamageRelay      │
         └────────┬──────────┘
                  │
         ┌────────▼──────────┐
         │   DATA LAYER      │
         │  EnemyStats SO    │
         │  Objective SO     │
         │  InstructionSlide │
         │  StoryboardSlide  │
         └───────────────────┘
```

### Architectural Principles

**1. Data-Driven Configuration:**
All enemy archetypes, objectives, tutorial content, and narrative content are defined as Unity ScriptableObjects. Designers can create new enemy types by creating a new `EnemyStats` asset and prefab without writing code.

**2. Interface-Based Coupling:**
Core gameplay systems interact through interfaces (`IDamageable`, `IInteractable`, `IEnemy`, `IEnemyAnimator`) rather than concrete class references. This allows any class to participate in the damage system, interaction system, or enemy management system without tight dependencies.

**3. Event-Driven Communication:**
All cross-system communication uses C# `Action` events. The score system does not reference the weapon system directly; it listens to `EnemyManager.OnEnemyKilled`. The UI does not poll the inventory; it subscribes to `InventoryManager.OnAmmoChanged`. This prevents circular dependencies and enables independent unit testing of each system.

**4. Singleton Managers:**
All global services (audio, inventory, difficulty, etc.) are singletons accessible via `Manager.Instance`. This pattern is appropriate at this project scale and provides a clean global service locator without dependency injection overhead.

**5. Finite State Machines for AI:**
Both the `EnemyAI` and `BossController` use explicit enum-based FSMs rather than behavior trees or utility AI. This choice provides deterministic, debuggable, and extensible AI behavior that is well-suited to academic analysis of decision-making patterns.

**6. DDA as a Non-Intrusive Layer:**
The `DifficultyManager` operates entirely through the existing stat system. It does not modify enemy prefabs, scene objects, or game rules — only the numeric parameters within `EnemyAI` instances. This makes it toggleable and measurable for research purposes.

**7. Metrics Layer Separation:**
All research data collection is isolated in `Scripts/Test/`. The `TestMetricsCollector` subscribes to the same events that game systems use but never drives game logic. This ensures the research instrumentation cannot affect gameplay outcomes.

---

*Document generated from codebase analysis of Colony Under Siege — Unity 3D Sci-Fi FPS.*
*Working directory: `/mnt/hdd/Work/Projects/Unity/3D Sci-Fi FPS`*
*Total custom scripts: 61 | Scenes: 6 | Enemy archetypes: 5 + 1 Boss*

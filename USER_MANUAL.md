# Colony Under Siege - User's Manual

## Introduction

### Purpose and scope

This manual documents the player-facing operation of Colony Under Siege, a single-player, narrative-driven science-fiction first-person shooter. The document is grounded in the current game implementation, including scene configuration, UI flow, and gameplay systems. Its purpose is to provide a formal, unambiguous reference for players and evaluators, presented in a thesis-style structure.

### Game overview

The game is structured as a sequence of five stages that depict the defense of a besieged colony. Progression follows a linear arc from the initial landing perimeter to a final reactor encounter. Core gameplay combines objective-based combat, navigation, and puzzle solving, supported by a modular weapon system and adaptive difficulty.

## Key features

- Five-stage campaign with distinct environments and mechanics.
- Objective-driven progression (kill, collect, and activation tasks).
- Three-slot weapon system: rifle, pistol, and decoy launcher.
- Enemy AI with finite state behaviors and role-based archetypes.
- Dynamic difficulty adjustment based on player accuracy.
- Integrated UI for objectives, ammo, health, and difficulty status.
- Storyboard sequences before and after the final boss encounter.

## Table of contents

1. Main Menu
   1.1 New Game
   1.2 Tutorial
   1.3 Stage Select
   1.4 Settings
   1.5 Credits
   1.6 Quit
2. Settings
   2.1 Mouse Sensitivity
   2.2 Audio (Master, Music, SFX)
3. Gameplay
   3.1 Game UI
   3.2 Objectives
   3.3 Stages and Mechanics
   3.4 Weapons and Equipment
   3.5 Interactions and Pickups
   3.6 Scoring and Results
4. Installation and Setup (Windows Build)
Appendix A. Requirements and Traceability Matrix
Appendix B. Worst-Case Risk Management Matrix

## 1. Main Menu

The Main Menu is the entry point of the application. It provides access to new play sessions, training content, stage selection, settings, credits, and application exit. A Continue option may appear only when a prior save is available.

### 1.1 New Game

Selecting New Game clears any existing saved progress and loads Stage 1. Use this option to restart the campaign from the beginning.

### 1.2 Tutorial

Selecting Tutorial loads the dedicated tutorial scene. The tutorial presents interactive instruction panels and a completion screen with options to restart the tutorial or return to the Main Menu.

### 1.3 Stage Select

Stage Select opens a card-based selector for the five stages. Each card displays a stage name and description, and the Play button loads the currently selected stage. In builds where progression locking is enabled, later stages remain locked until the player reaches them.

### 1.4 Settings

Settings opens a focused panel for mouse and audio configuration. All changes are applied immediately and persisted for future sessions.

### 1.5 Credits

Credits opens a scrollable panel that lists project roles and contributors.

### 1.6 Quit

Quit closes the application.

## 2. Settings

Settings are accessible from the Main Menu. The values are stored between sessions and applied when gameplay begins.

### 2.1 Mouse Sensitivity

Controls the speed of camera look input. The slider range is 0.5 to 10, with a default value of 2.0.

### 2.2 Audio (Master, Music, SFX)

- Master Volume: global output level for all audio.
- Music Volume: background music level.
- SFX Volume: combat, UI, and environmental effects.

All audio sliders operate on a normalized scale from 0.0 to 1.0.

## 3. Gameplay

Gameplay centers on combat, objectives, and progression through distinct stage environments. The player completes objectives to unlock access keys or exits that advance the campaign.

### 3.1 Game UI

The HUD provides continuous feedback during play:

- Health bar (player hit points).
- Ammo counter (current magazine and reserve).
- Objective list with progress text.
- Crosshair (aiming reference).
- Key item icons (objective items collected).
- Difficulty tier indicator (reflects adaptive difficulty state).
- Boss health bar (Stage 5 only).

Additional panels appear contextually: Pause Menu, Game Over, Win Screen, Instructions, and Storyboard sequences.

### 3.2 Objectives

Objectives are displayed in the HUD and update dynamically. The objective system supports four primary types:

- KillAll: eliminate all registered enemies.
- KillCount: defeat a required number of enemies.
- CollectItem: acquire a specific key item.
- ActivateSwitch: activate or deactivate a set of terminals or switches.

Completion of all objectives in a stage enables the stage exit or spawns a required key item.

### 3.3 Stages and mechanics

Stage 1 - Landing Stage (Build Index 1)
- Narrative: initial landing zone overrun by Grunt enemies.
- Objective: kill all enemies.
- Exit: access key appears after completion.

Stage 2 - Engineering Sector (Build Index 2)
- Narrative: industrial corridors with hazards.
- Objectives: collect access key and defeat a required enemy count.
- Mechanics: decoy launcher unlocks, explosive barrels present, optional pressure plate shortcut.

Stage 3 - Bio-Lab (Build Index 3)
- Narrative: bio-research facility with mutant threats.
- Objective: activate all pressure plates simultaneously.
- Mechanics: heavy boxes and grab-based puzzle solving.

Stage 4 - AI Core Control (Build Index 4)
- Narrative: active AI core reinforces enemies.
- Objective: deactivate all AI terminals.
- Mechanics: enemies are continuously healed until the core is deactivated.

Stage 5 - Reactor (Build Index 5)
- Narrative: final assault against the Reactor Guardian.
- Objective: defeat the boss.
- Mechanics: two-phase boss battle with minion waves in phase two.
- Storyboard: pre-boss and post-boss narrative sequences.

### 3.4 Weapons and equipment

The player has three weapon slots with unlock progression:

- Rifle: raycast weapon with higher damage and range; available from Stage 1.
- Pistol: raycast weapon with lower damage and shorter range; available from Stage 1.
- Decoy Launcher: projectile-based distraction tool; unlocked in Stage 2.

The decoy emits periodic noise pulses that lure nearby enemies, allowing tactical repositioning or objective completion.

### 3.5 Interactions and pickups

The game includes interactable objects and pickups that support objectives and survival:

- Doors and switches (objective progression).
- Objective items (keys and devices that unlock exits).
- Ammo pickups and food pickups (resource recovery).
- Explosive barrels (environmental hazard and tactical tool).
- Holdable objects (used in pressure plate puzzles).

### 3.6 Scoring and results

Upon stage completion, a results panel summarizes performance. Scoring considers enemy kills, accuracy, time, and deaths, producing a total score and grade. Accuracy and time bonuses reward efficient play.

## 4. Installation and setup (Windows Build)

### 4.1 Build location

Windows builds are stored under the Builds directory at the project root. Open the most recent Windows build folder.

### 4.2 Running the game

1. Open the selected Windows build folder.
2. Launch the executable file in that folder.
3. Keep all accompanying data files in the same directory as the executable.

### 4.3 First launch notes

- Audio and mouse settings are saved automatically after adjustment.
- If Continue is not visible, no saved progress is available.
- Quit from the Main Menu to close the application cleanly.

## Appendix A. Requirements and Traceability Matrix

This matrix maps player-facing requirements to their implementation points and verification evidence, grouped by sector.

### A.1 Main Menu

| ID | Requirement statement | Evidence / implementation | Verification approach |
| --- | --- | --- | --- |
| MM-01 | The Main Menu shall provide New Game, Continue, Tutorial, Stage Select, Settings, Credits, and Quit options. | `MainMenuManager` wires buttons; Main Menu panel built by `MainMenuPanelBuilder`. | Visual inspection in Scene 0; click-through of each option. |
| MM-02 | Continue shall be available only when a prior save exists. | `SaveManager.HasSave()` gates Continue visibility; saved scene index stored in PlayerPrefs. | Start with no save (Continue hidden), then save and verify visible. |
| MM-03 | New Game shall clear saved progress and start Stage 1. | `SaveManager.DeleteSave()` followed by `SceneManager.LoadScene(1)` from `MainMenuManager`. | Select New Game and confirm Stage 1 loads. |

### A.2 In-Game

| ID | Requirement statement | Evidence / implementation | Verification approach |
| --- | --- | --- | --- |
| IG-01 | The HUD shall display health, ammo, objectives, crosshair, key items, difficulty tier, and boss health (Stage 5). | `UIManager` manages HUD elements and boss health bar. | Play Stage 1 and Stage 5; confirm all HUD elements appear. |
| IG-02 | The objective system shall support KillAll, KillCount, CollectItem, and ActivateSwitch types. | `Objective` ScriptableObject types and `ObjectiveManager` evaluation logic. | Play stages with each objective type and observe completion text. |
| IG-03 | The weapon system shall support rifle, pistol, and decoy launcher with stage-based unlocks. | `WeaponController` slots; unlock by build index; decoy unlocked in Stage 2. | Verify rifle and pistol in Stage 1; decoy available in Stage 2. |
| IG-04 | The decoy launcher shall distract nearby enemies via periodic pulses. | `DecoyDevice` emits noise pulses within radius and duration. | Deploy decoy near enemies and observe diversion behavior. |
| IG-05 | The campaign shall consist of five stages with defined mechanics and objectives. | Scenes [1] to [5] with stage-specific systems in `SYSTEM_REFERENCE`. | Play through all stages and validate objectives and mechanics. |
| IG-06 | Stage 4 shall heal enemies until all AI terminals are deactivated. | `AICoreManager` heal routine and terminal group deactivation. | Observe healing banner and stop condition after terminal deactivation. |
| IG-07 | Stage 5 shall include a two-phase boss encounter with storyboards before and after combat. | `BossController` phase logic; `GameManager` storyboard state. | Start Stage 5, confirm intro and outro storyboards and phase transition. |
| IG-08 | The game shall provide Pause, Game Over, Win, Instructions, and Storyboard panels. | `GameManager` states; `UIManager` panel control. | Trigger each state and verify correct panel activation. |
| IG-09 | The game shall compute and display a score summary on stage completion. | `ScoreManager` calculation and `UIManager.ShowWinScreen`. | Finish a stage and verify score breakdown and grade. |

### A.3 Tutorial

| ID | Requirement statement | Evidence / implementation | Verification approach |
| --- | --- | --- | --- |
| TU-01 | The tutorial shall load a dedicated scene with instruction panels and a completion screen that offers restart and return to Main Menu actions. | `TutorialManager` drives tutorial UI; `ScreenPodInteractable` opens instruction panels. | Launch Tutorial from Main Menu and complete to verify restart and return options. |

### A.4 Stage Select

| ID | Requirement statement | Evidence / implementation | Verification approach |
| --- | --- | --- | --- |
| SS-01 | Stage Select shall present five stage cards, allow selection, and load the selected stage; locked stages shall be disabled when progress locking is enabled. | `StageSelectPanelBuilder` builds five cards; `MainMenuManager` handles selection and `requireProgressToUnlock`. | Open Stage Select, verify five cards and locking behavior, then play a selected stage. |

### A.5 Settings

| ID | Requirement statement | Evidence / implementation | Verification approach |
| --- | --- | --- | --- |
| ST-01 | The game shall provide configurable mouse sensitivity within 0.5 to 10.0. | Settings sliders in `MainMenuManager` and `UIManager` store `MouseSensitivity`. | Adjust slider and confirm camera look speed changes. |
| ST-02 | The game shall provide Master, Music, and SFX audio controls within 0.0 to 1.0. | Volume sliders in `MainMenuManager` and `UIManager`; `AudioListener` and `AudioManager` updates. | Adjust sliders and verify audible changes in menu and gameplay. |

## Appendix B. Worst-Case Risk Management Matrix

This matrix uses worst-case failure scenarios and documents how the current system mitigates them in the codebase.

| Worst-case risk scenario | Likelihood | Impact | System mitigation in codebase |
| --- | --- | --- | --- |
| Scene transition fails or hangs, leaving player in a blocked state | Medium | High | - `GameManager.LoadScene()` uses `LoadingManager` when available and falls back to direct `SceneManager.LoadScene` if not.<br>- `LoadingManager` performs async loading with progress and controlled activation, reducing transition instability. |
| UI becomes non-interactive because no EventSystem is present after scene changes | Low | High | - `GameManager.EnsureEventSystem()` creates a persistent EventSystem if missing.<br>- Existing EventSystem is preserved across scenes using `DontDestroyOnLoad`. |
| Save data leads to invalid continue flow or missing progress | Medium | High | - `SaveManager.HasSaveData()` gates Continue usage.<br>- `LoadSavedGame()` exits safely when no save exists and logs warning.<br>- `DeleteSave()` clears save keys before New Game starts. |
| Progression soft-lock where next stage cannot load from door/level flow | Medium | High | - `DoorInteractable` validates key and objective completion before opening.<br>- `LevelManager.LoadNextLevel()` falls back to Main Menu when no valid next scene is configured. |
| Time-scale remains frozen after pause/win/game-over transitions | Medium | High | - `GameManager.OnSceneLoaded()` resets time scale and gameplay state on every scene load.<br>- Restart and quit handlers explicitly restore `Time.timeScale = 1f`. |
| Duplicate manager instances create inconsistent state across scenes | Medium | High | - Core managers (`GameManager`, `SaveManager`, `AudioManager`, `DifficultyManager`, `LoadingManager`) enforce singleton guards in `Awake()` and destroy duplicates.<br>- Persistent managers use `DontDestroyOnLoad` for stable cross-scene state. |
| Stage 4 becomes unwinnable due to continuous enemy healing pressure | Medium | High | - `AICoreManager` exposes explicit `Deactivate()` to stop healing loop.<br>- Deactivation hides warning and invokes completion events for progression. |
| Stage 5 combat spikes uncontrollably due to overlapping wave spawns | Medium | High | - `WaveSpawner` blocks concurrent spawn sequences through `_spawning` guard.<br>- `BossController` uses cooldown-based phase logic to constrain attack and wave timing. |
| Performance drops during heavy combat because of repeated full-scene enemy scans | High | High | - Current mitigation is interval-based updates (decoy pulse timer and AI core heal timer), not every frame.<br>- Enemy state checks skip dead entities, reducing unnecessary processing in loops. |
| Audio failure from duplicate listeners or missing audio context across scenes | Low | High | - `AudioManager.EnsureSingleAudioListener()` keeps one active listener and disables duplicates.<br>- Scene-load callback reinitializes music state consistently per scene. |
| Weapon system fails to initialize aiming origin in some scenes | Medium | High | - `WeaponController` cascades camera source selection (`playerController.cameraHolder` then `Camera.main`).<br>- Null-guard checks prevent hard crash paths during fire/reload flow. |
| Critical subsystem missing at runtime (UI/audio/objective/manager references) causes crash cascade | Medium | High | - Widespread null-conditional calls (`?.`) and defensive checks prevent hard crashes.<br>- Warning logs surface missing dependencies for recovery during testing. |

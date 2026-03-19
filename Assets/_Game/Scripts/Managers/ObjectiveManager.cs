using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ObjectiveManager — owns and evaluates all objectives for the current stage.
///
/// Responsibilities:
///   - Holds the list of Objective ScriptableObjects for this stage
///   - Listens to EnemyManager, InventoryManager, and switch events
///   - Evaluates objectives after each relevant event
///   - Updates the HUD objective text via UIManager
///   - Fires OnAllObjectivesComplete when every objective is satisfied
///   - Notifies GameManager to trigger the win state
///
/// Setup:
///   1. Create an empty GameObject named "ObjectiveManager" in the scene.
///   2. Attach this script to it.
///   3. Create Objective assets (right-click → Create → Colony Under Siege → Objective).
///   4. Drag the Objective assets into the Stage Objectives list in the Inspector.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public static ObjectiveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ---------------------------------------------------------------
    // Inspector — assign Objective ScriptableObject assets here
    // ---------------------------------------------------------------

    [Header("Stage Objectives")]
    [Tooltip("Drag Objective ScriptableObject assets here. " +
             "Create them via right-click → Create → Colony Under Siege → Objective")]
    public List<Objective> stageObjectives = new List<Objective>();

    // ---------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------

    /// <summary>Fired when every objective in stageObjectives is complete.</summary>
    public event Action OnAllObjectivesComplete;

    // ---------------------------------------------------------------
    // Private
    // ---------------------------------------------------------------

    private bool _allComplete = false;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Start()
    {
        // Reset all objective runtime state from any previous session
        foreach (Objective obj in stageObjectives)
        {
            if (obj != null) obj.ResetRuntime();
        }

        // Subscribe to systems that drive objective progress
        SubscribeToEvents();

        // Delay initial HUD refresh by 2 frames so ALL enemies finish
        // registering with EnemyManager before we read TotalEnemies
        StartCoroutine(DelayedHUDRefresh());

        Debug.Log($"[ObjectiveManager] Initialized with {stageObjectives.Count} objective(s).");
    }

    private IEnumerator DelayedHUDRefresh()
    {
        yield return null; // frame 1 — enemies call Start() and register
        yield return null; // frame 2 — safety buffer

        RefreshHUD();
        Debug.Log($"[ObjectiveManager] HUD refreshed. " +
                  $"Total enemies: {EnemyManager.Instance?.TotalEnemies ?? 0}");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------
    // Event subscriptions
    // ---------------------------------------------------------------

    private void SubscribeToEvents()
    {
        // Enemy kills drive KillAll and KillCount objectives
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled        += OnEnemyKilled;
            EnemyManager.Instance.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
        }

        // Key item pickups drive CollectItem objectives
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnKeyItemAdded += OnKeyItemAdded;
    }

    private void UnsubscribeFromEvents()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled        -= OnEnemyKilled;
            EnemyManager.Instance.OnAllEnemiesDefeated -= OnAllEnemiesDefeated;
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnKeyItemAdded -= OnKeyItemAdded;
    }

    // ---------------------------------------------------------------
    // Event handlers
    // ---------------------------------------------------------------

    private void OnEnemyKilled(int killCount)
    {
        EvaluateObjectivesOfType(Objective.ObjectiveType.KillCount);
        RefreshHUD();
        CheckAllComplete();
    }

    private void OnAllEnemiesDefeated()
    {
        EvaluateObjectivesOfType(Objective.ObjectiveType.KillAll);
        RefreshHUD();
        CheckAllComplete();
    }

    private void OnKeyItemAdded(string itemId)
    {
        EvaluateObjectivesOfType(Objective.ObjectiveType.CollectItem);
        RefreshHUD();
        CheckAllComplete();
    }

    /// <summary>
    /// Called by SwitchInteractable when the player activates a switch.
    /// Matches the switchId against ActivateSwitch objectives.
    /// </summary>
    public void NotifySwitchActivated(string switchId)
    {
        foreach (Objective obj in stageObjectives)
        {
            if (obj == null || obj.IsComplete) continue;

            if (obj.type == Objective.ObjectiveType.ActivateSwitch &&
                obj.requiredItemId == switchId)
            {
                obj.IsComplete = true;
                Debug.Log($"[ObjectiveManager] Switch objective complete: '{switchId}'");
            }
        }

        RefreshHUD();
        CheckAllComplete();
    }

    // ---------------------------------------------------------------
    // Evaluation
    // ---------------------------------------------------------------

    /// <summary>Evaluates all objectives of a specific type.</summary>
    private void EvaluateObjectivesOfType(Objective.ObjectiveType type)
    {
        foreach (Objective obj in stageObjectives)
        {
            if (obj == null || obj.IsComplete) continue;
            if (obj.type == type) obj.Evaluate();
        }
    }

    /// <summary>Evaluates all objectives regardless of type.</summary>
    public void EvaluateAll()
    {
        foreach (Objective obj in stageObjectives)
        {
            if (obj != null && !obj.IsComplete)
                obj.Evaluate();
        }

        RefreshHUD();
        CheckAllComplete();
    }

    /// <summary>
    /// Returns true if every objective in the list is complete.
    /// </summary>
    public bool AreAllComplete()
    {
        if (stageObjectives.Count == 0) return false;

        foreach (Objective obj in stageObjectives)
        {
            if (obj == null) continue;
            if (!obj.IsComplete) return false;
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Completion check
    // ---------------------------------------------------------------

    private void CheckAllComplete()
    {
        if (_allComplete) return; // Prevent firing twice
        if (!AreAllComplete())    return;

        _allComplete = true;

        Debug.Log("[ObjectiveManager] All objectives complete! Enter the door to proceed.");

        OnAllObjectivesComplete?.Invoke();

        // HUD hint — tell player to go to the door
        RefreshHUD();
    }

    // ---------------------------------------------------------------
    // HUD update
    // ---------------------------------------------------------------

    /// <summary>
    /// Rebuilds the objective text shown in the HUD and pushes it to UIManager.
    /// Called after every objective state change.
    /// </summary>
    private void RefreshHUD()
    {
        if (UIManager.Instance == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (Objective obj in stageObjectives)
        {
            if (obj == null) continue;
            sb.AppendLine(obj.GetDisplayText());
        }

        UIManager.Instance.UpdateObjectiveText(sb.ToString().TrimEnd());
    }
}
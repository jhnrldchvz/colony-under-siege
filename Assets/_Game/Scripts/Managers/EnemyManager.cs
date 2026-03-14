using UnityEngine;

/// <summary>
/// EnemyManager — stub.
/// Full implementation coming next.
/// This file exists only so EnemyAI.cs compiles in the meantime.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterEnemy(EnemyAI enemy)   { }
    public void DeregisterEnemy(EnemyAI enemy) { }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
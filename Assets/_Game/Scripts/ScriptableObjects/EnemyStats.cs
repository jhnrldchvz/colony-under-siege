using UnityEngine;

/// <summary>
/// EnemyStats — ScriptableObject that defines a preset enemy configuration.
/// Create one asset per enemy type via:
/// right-click Project → Create → Colony Under Siege → Enemy Stats
/// </summary>
[CreateAssetMenu(fileName = "EnemyStats_New",
                 menuName  = "Colony Under Siege/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Enemy";

    [Header("Health")]
    public int maxHealth = 50;

    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed  = 5f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float fieldOfView    = 110f;
    public float loseRange      = 14f;

    [Header("Attack")]
    public float attackRange    = 2f;
    public int   attackDamage   = 10;
    public float attackCooldown = 1.5f;

    [Header("Loot")]
    [Range(0f, 1f)]
    public float dropChance = 0.75f;
}
using UnityEngine;

/// <summary>
/// IEnemy — common interface for all enemy types (EnemyAI, TurretEnemy).
/// Allows EnemyManager to track any enemy regardless of implementation.
/// Note: gameObject is intentionally excluded — MonoBehaviour already
/// provides it on all implementors and cannot be redeclared in an interface.
/// </summary>
public interface IEnemy
{
    bool IsAlive { get; }
}
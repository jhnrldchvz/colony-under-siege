/// <summary>
/// IDamageable — implemented by anything that can receive damage.
/// Decouples weapons and projectiles from specific target types.
///
/// Implement on: PlayerController, EnemyAI, destructible crates,
/// bosses, turrets, shields — anything that takes damage.
/// </summary>
public interface IDamageable
{
    /// <summary>Apply damage to this object.</summary>
    void TakeDamage(int amount);

    /// <summary>True while the object is alive and can take damage.</summary>
    bool IsAlive { get; }
}
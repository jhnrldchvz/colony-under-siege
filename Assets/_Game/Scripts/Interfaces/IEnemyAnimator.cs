/// <summary>
/// IEnemyAnimator — optional animation bridge for EnemyAI.
/// Implement this on a MonoBehaviour and attach alongside EnemyAI.
/// Enemies without an implementation work perfectly — all calls are null-safe.
/// </summary>
public interface IEnemyAnimator
{
    void SetSpeed(float speed);
    void TriggerAttack();
    void TriggerDie();
}

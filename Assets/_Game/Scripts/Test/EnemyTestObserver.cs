using UnityEngine;

/// <summary>
/// EnemyTestObserver — attach to every enemy in the test scene.
/// Watches EnemyAI state changes and reports to TestMetricsCollector.
/// Also measures detection response time (how fast enemy notices player).
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class EnemyTestObserver : MonoBehaviour
{
    private EnemyAI    _ai;
    private EnemyAI.EnemyState _lastState;
    private float      _patrolStartTime;
    private bool       _detectionRecorded = false;
    private string     _enemyName;

    private void Start()
    {
        _ai         = GetComponent<EnemyAI>();
        _lastState  = _ai.CurrentState;
        _enemyName  = gameObject.name;
        _patrolStartTime = Time.time;

        // Subscribe to death to record kill
        // We poll state in Update since EnemyAI doesn't expose a state-change event
    }

    private void Update()
    {
        if (_ai == null || !_ai.IsAlive) return;

        EnemyAI.EnemyState current = _ai.CurrentState;

        if (current != _lastState)
        {
            string from = _lastState.ToString();
            string to   = current.ToString();

            TestMetricsCollector.Instance?.RecordFSMTransition(_enemyName, from, to);

            // Measure response time — how long from Patrol to Chase
            if (_lastState == EnemyAI.EnemyState.Patrol &&
                current    == EnemyAI.EnemyState.Chase &&
                !_detectionRecorded)
            {
                float responseTime = Time.time - _patrolStartTime;
                TestMetricsCollector.Instance?.RecordDetection(_enemyName, responseTime);
                _detectionRecorded = true;
            }

            // Reset patrol timer when returning to patrol
            if (current == EnemyAI.EnemyState.Patrol)
            {
                _patrolStartTime    = Time.time;
                _detectionRecorded  = false;
            }

            _lastState = current;
        }

        // Record kill when enemy dies
        if (!_ai.IsAlive)
        {
            TestMetricsCollector.Instance?.RecordKill();
            Destroy(this); // Remove observer after death
        }
    }
}
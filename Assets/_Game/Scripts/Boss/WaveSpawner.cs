using System.Collections;
using UnityEngine;

/// <summary>
/// WaveSpawner — spawns a wave of enemies at defined spawn points.
/// Called by BossController on phase 2 transition.
///
/// Setup:
///   1. Create empty GO "WaveSpawner" in the arena
///   2. Attach this script
///   3. Add spawn point child GOs and drag into spawnPoints[]
///   4. Assign enemy prefabs to wave[]
///   5. Wire BossController.onPhase2Start → WaveSpawner.SpawnWave()
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave")]
    [Tooltip("Enemy prefabs to spawn — one per spawn point or cycled")]
    public GameObject[] enemyPrefabs;

    [Tooltip("World positions where enemies spawn")]
    public Transform[]  spawnPoints;

    [Tooltip("Seconds between each enemy spawn for dramatic effect")]
    public float        spawnDelay = 0.4f;

    // ---------------------------------------------------------------

    private bool _spawning = false;

    /// <summary>
    /// Spawns the configured wave. Safe to call from BossController on any interval —
    /// will skip silently if a previous wave is still spawning.
    /// </summary>
    public void SpawnWave()
    {
        if (_spawning)
        {
            Debug.Log("[WaveSpawner] SpawnWave() called but previous wave still in progress — skipped.");
            return;
        }
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) yield break;
        if (spawnPoints  == null || spawnPoints.Length  == 0) yield break;

        _spawning = true;
        Debug.Log($"[WaveSpawner] Spawning wave — {spawnPoints.Length} enemies.");

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            GameObject prefab = enemyPrefabs[i % enemyPrefabs.Length];
            if (prefab == null) continue;

            Instantiate(prefab, spawnPoints[i].position, spawnPoints[i].rotation);

            yield return new WaitForSeconds(spawnDelay);
        }

        _spawning = false;
        Debug.Log("[WaveSpawner] Wave spawn complete.");
    }
}
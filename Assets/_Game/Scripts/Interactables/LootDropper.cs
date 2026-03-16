using UnityEngine;

public class LootDropper : MonoBehaviour
{
    [System.Serializable]
    public class LootEntry
    {
        public GameObject prefab;
        public float      weight = 1f;
    }

    [Header("Loot Table")]
    public LootEntry[] lootTable;
    public float       nothingWeight = 1f;

    private void Start()
    {
        // Debug — log the entire loot table
        Debug.Log($"[LootDropper] Start — table size={lootTable?.Length ?? 0} " +
                  $"nothingWeight={nothingWeight}");

        if (lootTable != null)
        {
            for (int i = 0; i < lootTable.Length; i++)
            {
                Debug.Log($"[LootDropper] Entry {i}: " +
                          $"prefab={lootTable[i].prefab} " +
                          $"weight={lootTable[i].weight}");
            }
        }

        SpawnLoot();
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        if (lootTable == null || lootTable.Length == 0)
        {
            Debug.LogWarning("[LootDropper] Loot table is EMPTY — nothing to drop.");
            return;
        }

        float totalWeight = nothingWeight;
        foreach (LootEntry entry in lootTable)
            totalWeight += entry.weight;

        float roll = Random.Range(0f, totalWeight);
        Debug.Log($"[LootDropper] Roll={roll:F2} TotalWeight={totalWeight:F2}");

        if (roll < nothingWeight)
        {
            Debug.Log("[LootDropper] Rolled nothing.");
            return;
        }

        float cumulative = nothingWeight;
        foreach (LootEntry entry in lootTable)
        {
            cumulative += entry.weight;
            if (roll < cumulative)
            {
                if (entry.prefab != null)
                {
                    Vector3    pos     = transform.position + Vector3.up * 0.3f;
                    GameObject spawned = Instantiate(entry.prefab, pos, Quaternion.identity);
                    Debug.Log($"[LootDropper] Spawned: {spawned.name} at {pos}");
                }
                else
                {
                    Debug.LogError("[LootDropper] Selected entry prefab is NULL — " +
                                   "drag FoodPickup/AmmoPickup prefabs into the Loot Table slots.");
                }
                return;
            }
        }
    }
}
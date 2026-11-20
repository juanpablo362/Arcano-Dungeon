using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public GameObject skeletonPrefab;
    [Tooltip("Prefabs extras para alternar enemigos al spawnear.")]
    public GameObject[] additionalEnemyPrefabs;
    public Transform[] room1Spawns;
    int activeEnemies;
    int spawnIndex;

    void Start()
    {
        if (room1Spawns == null) return;

        foreach (Transform spawnPoint in room1Spawns)
        {
            if (spawnPoint == null) continue;

            GameObject prefab = NextEnemyPrefab();
            if (prefab == null) continue;

            Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            activeEnemies++;
        }
    }

    GameObject NextEnemyPrefab()
    {
        int extraCount = additionalEnemyPrefabs != null ? additionalEnemyPrefabs.Length : 0;
        int total = (skeletonPrefab != null ? 1 : 0) + extraCount;
        if (total == 0)
            return null;

        for (int attempt = 0; attempt < total; attempt++)
        {
            int index = (spawnIndex + attempt) % total;
            GameObject candidate;
            if (skeletonPrefab != null)
                candidate = index == 0 ? skeletonPrefab : additionalEnemyPrefabs[index - 1];
            else
                candidate = additionalEnemyPrefabs[index];

            if (candidate != null)
            {
                spawnIndex = (spawnIndex + attempt + 1) % total;
                return candidate;
            }
        }

        return skeletonPrefab;
    }

    public void EnemyDefeated()
    {
        activeEnemies--;
        if (activeEnemies == 0)
            Debug.Log("¡Cuarto 1 despejado! Puedes avanzar al Cuarto 2.");
    }
}

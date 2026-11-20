using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RoomController : MonoBehaviour
{
    public enum RoomKind
    {
        Combat,
        Start,
        Boss,
        Treasure
    }

    [Header("Identificación")]
    public string roomName = "Cuarto de Combate";
    public RoomKind roomKind = RoomKind.Combat;

    [Header("Enemigos")]
    public GameObject skeletonPrefab;
    [Tooltip("Prefabs extras (p.ej. Assets/Prefabs/Skeleton). Se alternan con skeletonPrefab.")]
    public GameObject[] additionalEnemyPrefabs;
    public Transform[] normalSpawnPoints;
    [Tooltip("Multiplicador de escala para enemigos normales de esta sala (1 = tamaño del prefab).")]
    public float enemyScale = 1f;

    [Header("Minijefe (opcional)")]
    public bool spawnMiniBoss = false;
    public GameObject miniBossPrefab;
    public Transform miniBossSpawnPoint;
    public float miniBossScale = 1.4f;
    public int miniBossMaxHealth = 200;

    [Header("Puerta / Salida")]
    public GameObject exitDoor;
    public GameObject[] exitDoors;
    public GameObject objectToActivateOnClear;

    [Header("Minimapa")]
    public RoomController[] connectedRooms;

    bool hasTriggered;
    bool isCleared;
    bool playerInside;
    int activeEnemies;
    int spawnIndex;

    public bool HasTriggered => hasTriggered;
    public bool IsCleared => isCleared;
    public int ActiveEnemies => activeEnemies;

    public static event Action<RoomController> RoomEntered;
    public static event Action<RoomController> RoomCleared;
    public static event Action<RoomController> EncounterStarted;

    void Awake()
    {
        BoxCollider triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        // Soul Knight: puertas abiertas hasta que empieza la pelea.
        SetDoorsLocked(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (!playerInside)
        {
            playerInside = true;
            RoomEntered?.Invoke(this);
        }

        // Hub / salas sin enemigos: solo marcar explorada, sin combate ni rejas.
        if (roomKind == RoomKind.Start || !HasEnemySetup())
        {
            if (!hasTriggered)
            {
                hasTriggered = true;
                isCleared = true;
                SetDoorsLocked(false);
                RoomCleared?.Invoke(this);
            }
            return;
        }

        if (!hasTriggered)
            StartEncounter();
    }

    bool HasEnemySetup()
    {
        if (skeletonPrefab != null)
            return true;
        if (additionalEnemyPrefabs != null)
        {
            for (int i = 0; i < additionalEnemyPrefabs.Length; i++)
            {
                if (additionalEnemyPrefabs[i] != null)
                    return true;
            }
        }
        return spawnMiniBoss && miniBossSpawnPoint != null;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;
        playerInside = false;
    }

    static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
               || (other.transform.root != null && other.transform.root.CompareTag("Player"));
    }

    public void StartEncounter()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        EncounterStarted?.Invoke(this);

        // Cierra rejas doradas durante la batalla.
        if (!isCleared)
            SetDoorsLocked(true);

        if (skeletonPrefab == null && (additionalEnemyPrefabs == null || additionalEnemyPrefabs.Length == 0))
        {
            Debug.LogWarning($"[{roomName}] No hay prefabs de enemigo asignados en RoomController.");
            ClearRoom();
            return;
        }

        if (normalSpawnPoints != null)
        {
            foreach (Transform spawnPoint in normalSpawnPoints)
            {
                if (spawnPoint == null) continue;

                GameObject prefab = NextEnemyPrefab();
                if (prefab == null) continue;

                Vector3 spawnPos = new Vector3(spawnPoint.position.x, spawnPoint.position.y + 0.05f, spawnPoint.position.z);
                GameObject enemy = Instantiate(prefab, spawnPos, spawnPoint.rotation);
                if (enemyScale > 0.01f && !Mathf.Approximately(enemyScale, 1f))
                    enemy.transform.localScale = prefab.transform.localScale * enemyScale;
                RegisterEnemy(enemy);
            }
        }

        if (spawnMiniBoss && miniBossSpawnPoint != null)
        {
            GameObject bossPrefab = miniBossPrefab != null
                ? miniBossPrefab
                : (skeletonPrefab != null ? skeletonPrefab : NextEnemyPrefab());
            if (bossPrefab != null)
            {
                Vector3 bossPos = new Vector3(miniBossSpawnPoint.position.x, miniBossSpawnPoint.position.y + 0.05f, miniBossSpawnPoint.position.z);
                GameObject miniBoss = Instantiate(bossPrefab, bossPos, miniBossSpawnPoint.rotation);
                miniBoss.name = "Skeleton_MiniBoss";
                miniBoss.transform.localScale = bossPrefab.transform.localScale * miniBossScale;

                EnemyHealth bossHealth = miniBoss.GetComponent<EnemyHealth>();
                if (bossHealth != null)
                    bossHealth.ConfigureHealth(miniBossMaxHealth);

                RegisterEnemy(miniBoss);
            }
        }

        if (activeEnemies == 0)
            ClearRoom();
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

    void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.SetOwnerRoom(this);

        activeEnemies++;
    }

    public void OnEnemyDefeated()
    {
        if (isCleared) return;

        activeEnemies = Mathf.Max(0, activeEnemies - 1);
        if (activeEnemies <= 0)
            ClearRoom();
    }

    void ClearRoom()
    {
        if (isCleared) return;
        isCleared = true;

        SetDoorsLocked(false);

        if (objectToActivateOnClear != null)
            objectToActivateOnClear.SetActive(true);

        RoomCleared?.Invoke(this);
        Debug.Log($"[{roomName}] ¡Cuarto despejado!");
    }

    void SetDoorsLocked(bool locked)
    {
        if (exitDoor != null)
            exitDoor.SetActive(locked);

        if (exitDoors == null)
            return;

        foreach (GameObject door in exitDoors)
        {
            if (door != null)
                door.SetActive(locked);
        }
    }
}

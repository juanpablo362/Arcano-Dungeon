using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TreasureChest : MonoBehaviour
{
    public GameObject heartPrefab;
    public Transform lidTransform;
    public float interactRange = 2.5f;
    public float openLidAngleX = -105f;
    public float lidOpenSpeed = 5f;
    public float heartForwardDistance = 1.2f;
    public float heartTargetY = 0.8f;
    public float heartLaunchArcHeight = 0.65f;
    public float heartLaunchDuration = 0.45f;
    public string promptMessage = "Presiona [F] para abrir cofre";

    [Header("Loot")]
    public LootDropTable dropTable;

    [Header("Mímico")]
    [Tooltip("Si true (o escena Catacombs), 10% de chance de mímico.")]
    public bool allowMimic;
    [Range(0f, 1f)] public float mimicChance = 0.10f;
    public GameObject mimicPrefab;

    [Header("Legacy fallback weights")]
    public int[] lootWeights = { 28, 16, 16, 16, 14, 10 };

    bool isOpened;
    Transform cachedPlayer;
    Quaternion targetLidRotation;

    public bool IsOpened => isOpened;

    void Awake()
    {
        promptMessage = "Presiona [F] para abrir cofre";

        if (lidTransform == null)
        {
            Transform foundLid = transform.Find("SquareChest_Top");
            if (foundLid != null)
                lidTransform = foundLid;
        }

        targetLidRotation = Quaternion.Euler(openLidAngleX, 0f, 0f);

        if (dropTable == null)
            dropTable = LoadDefaultDropTable();

        string scene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(scene) && scene.IndexOf("Catacomb", System.StringComparison.OrdinalIgnoreCase) >= 0)
            allowMimic = true;
    }

    void Update()
    {
        if (isOpened)
        {
            if (lidTransform != null)
            {
                lidTransform.localRotation = Quaternion.Slerp(
                    lidTransform.localRotation,
                    targetLidRotation,
                    lidOpenSpeed * Time.deltaTime
                );
            }
            return;
        }

        if (cachedPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                cachedPlayer = playerObj.transform;
        }

        if (cachedPlayer == null) return;

        float distance = Vector3.Distance(transform.position, cachedPlayer.position);
        if (distance < interactRange)
            InteractionPromptUI.RequestPrompt(promptMessage, distance);
    }

    public GameObject OpenChest()
    {
        if (isOpened) return null;
        isOpened = true;
        var latch = DungeonSfxCatalog.Instance != null ? DungeonSfxCatalog.Instance.chestOpen : null;
        GameSfx.Play2D(latch, 0.75f);

        if (allowMimic && Random.value < mimicChance)
        {
            SpawnMimic();
            return null;
        }

        Vector3 startPos = transform.position + Vector3.up * 0.55f;
        Vector3 targetPos = transform.position + transform.forward * heartForwardDistance;
        targetPos.y = heartTargetY;

        ItemData rolled = dropTable != null ? dropTable.Roll() : null;
        GameObject spawned = null;

        if (rolled != null)
        {
            spawned = LootVisualFactory.Create(rolled, startPos);
            var pickup = spawned != null ? spawned.GetComponent<ItemPickup>() : null;
            if (pickup != null)
                pickup.Launch(startPos, targetPos, heartLaunchArcHeight, heartLaunchDuration);
            else if (spawned != null)
                spawned.transform.position = targetPos;
            return spawned;
        }

        if (heartPrefab != null)
        {
            spawned = Instantiate(heartPrefab, startPos, Quaternion.identity);
            var heart = spawned.GetComponent<HeartPickup>();
            if (heart != null)
                heart.Launch(startPos, targetPos, heartLaunchArcHeight, heartLaunchDuration);
        }

        return spawned;
    }

    void SpawnMimic()
    {
        InteractionPromptUI.RequestPrompt("¡Mímico!", 0f);

        GameObject prefab = mimicPrefab;
        if (prefab == null)
            prefab = LoadDefaultMimicPrefab();

        Vector3 pos = transform.position + Vector3.up * 0.1f;
        GameObject mimicGo;
        if (prefab != null)
        {
            mimicGo = Instantiate(prefab, pos, transform.rotation);
        }
        else
        {
            mimicGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mimicGo.name = "MimicFallback";
            mimicGo.transform.position = pos;
            mimicGo.transform.localScale = new Vector3(1.1f, 1.0f, 1.1f);
            var cc = mimicGo.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.8f, 0f);
            var eh = mimicGo.AddComponent<EnemyHealth>();
            eh.ConfigureHealth(90);
            mimicGo.AddComponent<EnemyAI>();
            mimicGo.tag = "Enemy";
        }

        mimicGo.name = "Mimic";
        if (mimicGo.GetComponent<MimicEnemy>() == null)
            mimicGo.AddComponent<MimicEnemy>();

        // Ocultar cofre visual.
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>(true))
            c.enabled = false;
    }

    static GameObject LoadDefaultMimicPrefab()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_ArcanoDungeon/Prefabs/Monsters/EnemyOrc.prefab");
#else
        return null;
#endif
    }

    static LootDropTable LoadDefaultDropTable()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<LootDropTable>(
            "Assets/_ArcanoDungeon/Features/Loot/Items/LootDropTable_Default.asset");
#else
        return null;
#endif
    }
}

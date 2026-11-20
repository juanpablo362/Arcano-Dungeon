using System.IO;
using ArcanoDungeon.Features.Loot;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea ItemData del lote A, LootDropTable_Default y los asigna a cofres.
/// </summary>
public static class SetupLootPhaseA
{
    const string ItemsDir = "Assets/_ArcanoDungeon/Features/Loot/Items";
    const string TablePath = ItemsDir + "/LootDropTable_Default.asset";

    [MenuItem("Tools/Arcano/Setup Loot Phase A (items + drop table)", false, 140)]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[LootPhaseA] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        EnsureFolder(ItemsDir);

        var orbital = CreateOrLoadPassive(
            ItemsDir + "/Item_Orbital.asset",
            "orbital", "Orbe Orbital",
            "Esfera de energía que orbita y daña/bloquea.",
            dropWeight: 12,
            hasOrbital: true);

        var chain = CreateOrLoadPassive(
            ItemsDir + "/Item_ChainLightning.asset",
            "chain_lightning", "Anillo de Rayos",
            "Cada 4 hits libera un rayo en cadena.",
            dropWeight: 12,
            hasChain: true);

        var hermes = CreateOrLoadPassive(
            ItemsDir + "/Item_HermesBoots.asset",
            "hermes_boots", "Botas de Hermes",
            "+20% velocidad y -0.4s cooldown de dash.",
            dropWeight: 14,
            moveSpeedMult: 1.2f,
            dashCooldownReduce: 0.4f);

        var aura = CreateOrLoadPassive(
            ItemsDir + "/Item_HolyFireAura.asset",
            "holy_fire_aura", "Aura de Fuego Sagrado",
            "Círculo de 3m que daña enemigos cada 0.5s.",
            dropWeight: 12,
            hasAura: true);

        var potion = CreateOrLoadConsumable(
            ItemsDir + "/Item_CrimsonPotion.asset",
            "crimson_potion", "Poción Carmesí",
            "Restaura 30 de vida.",
            dropWeight: 22,
            heal: 30);

        var crystal = CreateOrLoadConsumable(
            ItemsDir + "/Item_ShieldCrystal.asset",
            "shield_crystal", "Cristal de Escudo",
            "Recarga el escudo y da un boost de velocidad.",
            dropWeight: 16,
            refillShield: true,
            speedBurstMult: 1.35f,
            speedBurstSeconds: 3f);

        var table = AssetDatabase.LoadAssetAtPath<LootDropTable>(TablePath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootDropTable>();
            AssetDatabase.CreateAsset(table, TablePath);
        }

        table.entries = new System.Collections.Generic.List<LootDropEntry>
        {
            new LootDropEntry { item = orbital },
            new LootDropEntry { item = chain },
            new LootDropEntry { item = hermes },
            new LootDropEntry { item = aura },
            new LootDropEntry { item = potion },
            new LootDropEntry { item = crystal },
        };
        EditorUtility.SetDirty(table);

        AssignTableToChests(table);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LootPhaseA] Items + LootDropTable_Default listos y asignados a cofres.");
    }

    static ItemData CreateOrLoadPassive(
        string path, string id, string name, string desc, int dropWeight,
        bool hasOrbital = false, bool hasChain = false, bool hasAura = false,
        float moveSpeedMult = 1f, float dashCooldownReduce = 0f)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.id = id;
        item.displayName = name;
        item.description = desc;
        item.type = ItemType.Passive;
        item.dropWeight = dropWeight;
        item.hasOrbital = hasOrbital;
        item.hasChainLightning = hasChain;
        item.hasHolyAura = hasAura;
        item.moveSpeedMult = moveSpeedMult;
        item.dashCooldownReduce = dashCooldownReduce;
        item.temporarySeconds = 40f;
        EditorUtility.SetDirty(item);
        return item;
    }

    static ItemData CreateOrLoadConsumable(
        string path, string id, string name, string desc, int dropWeight,
        int heal = 0, bool refillShield = false, float speedBurstMult = 1f,
        float speedBurstSeconds = 0f, int score = 0)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.id = id;
        item.displayName = name;
        item.description = desc;
        item.type = ItemType.Consumable;
        item.dropWeight = dropWeight;
        item.healAmount = heal;
        item.refillShield = refillShield;
        item.speedBurstMult = speedBurstMult;
        item.speedBurstSeconds = speedBurstSeconds;
        item.scoreAmount = score;
        item.temporarySeconds = 40f;
        EditorUtility.SetDirty(item);
        return item;
    }

    static void AssignTableToChests(LootDropTable table)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab TreasureChest");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;
            var chests = root.GetComponentsInChildren<TreasureChest>(true);
            bool dirty = false;
            foreach (var c in chests)
            {
                if (c.dropTable != table)
                {
                    c.dropTable = table;
                    dirty = true;
                }
            }

            if (dirty)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // Escena Level1
        string scenePath = "Assets/_ArcanoDungeon/Scenes/Level1.unity";
        if (!File.Exists(scenePath))
            return;

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        bool sceneDirty = false;
        foreach (var chest in Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include))
        {
            if (chest.dropTable != table)
            {
                chest.dropTable = table;
                EditorUtility.SetDirty(chest);
                sceneDirty = true;
            }
        }

        if (sceneDirty)
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        // Si se abrió additive y no era la activa principal, cerrar sin unload forzado peligroso
        if (scene.IsValid() && UnityEditor.SceneManagement.EditorSceneManager.sceneCount > 1)
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}

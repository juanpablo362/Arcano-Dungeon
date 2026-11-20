using System.Collections.Generic;
using System.IO;
using ArcanoDungeon.Features.Loot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupLootFullGdd
{
    const string ItemsDir = "Assets/_ArcanoDungeon/Features/Loot/Items";
    const string TablePath = ItemsDir + "/LootDropTable_Default.asset";
    const string CatacombsTablePath = ItemsDir + "/LootDropTable_Catacombs.asset";

    [MenuItem("Tools/Arcano/Setup Loot Full GDD", false, 141)]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[LootFull] Sal de Play Mode.");
            return;
        }

        EnsureFolder(ItemsDir);

        // Passives existentes + nuevos
        var orbital = UpsertPassive(ItemsDir + "/Item_Orbital.asset", "orbital", "Orbe Orbital",
            "Esfera orbital que daña y bloquea.", 10, hasOrbital: true);
        var chain = UpsertPassive(ItemsDir + "/Item_ChainLightning.asset", "chain_lightning", "Anillo de Rayos",
            "Cada 4 hits libera rayo en cadena.", 10, hasChain: true);
        var hermes = UpsertPassive(ItemsDir + "/Item_HermesBoots.asset", "hermes_boots", "Botas de Hermes",
            "+20% velocidad y -0.4s dash.", 12, moveSpeedMult: 1.2f, dashCooldownReduce: 0.4f);
        var aura = UpsertPassive(ItemsDir + "/Item_HolyFireAura.asset", "holy_fire_aura", "Aura de Fuego Sagrado",
            "Daño periódico en radio 3m.", 10, hasAura: true);
        var vamp = UpsertPassive(ItemsDir + "/Item_VampiricFang.asset", "vampiric_fang", "Colmillo Vampírico",
            "15% al matar: +5 vida o +10 escudo.", 9, hasVampiric: true);
        var lens = UpsertPassive(ItemsDir + "/Item_MultishotLens.asset", "multishot_lens", "Lente de Dispersión",
            "Multidisparo y +25% ancho melee.", 9, hasMultishot: true, arcMult: 1.25f);
        var crystalHeart = UpsertPassive(ItemsDir + "/Item_CrystalHeart.asset", "crystal_heart", "Corazón de Cristal",
            "+30 escudo máx y regen 1s antes.", 8, hasCrystalHeart: true, maxShield: 30, shieldRegenReduce: 1f);

        // Consumables
        var potion = UpsertConsumable(ItemsDir + "/Item_CrimsonPotion.asset", "crimson_potion", "Poción Carmesí",
            "+30 vida.", 18, heal: 30);
        var crystal = UpsertConsumable(ItemsDir + "/Item_ShieldCrystal.asset", "shield_crystal", "Cristal de Escudo",
            "Recarga escudo + velocidad.", 12, refillShield: true, speedBurstMult: 1.35f, speedBurstSeconds: 3f);
        var bomb = UpsertConsumable(ItemsDir + "/Item_PowderBomb.asset", "powder_bomb", "Bomba de Pólvora",
            "+1 carga de bomba (Q).", 8, bombCharges: 1);

        // Weapons
        var volcanic = UpsertWeapon(ItemsDir + "/Item_VolcanicGreatsword.asset", "volcanic_greatsword",
            "Mandoble Volcánico", "Onda de fuego en cada golpe.", 5, WeaponProfileKind.VolcanicGreatsword, 14, 0.15f);
        var blizzard = UpsertWeapon(ItemsDir + "/Item_BlizzardStaff.asset", "blizzard_staff",
            "Báculo de Ventisca", "Ráfaga de hielo que ralentiza.", 5, WeaponProfileKind.BlizzardStaff, 10, 0f);
        var crossbow = UpsertWeapon(ItemsDir + "/Item_RepeatingCrossbow.asset", "repeating_crossbow",
            "Ballesta Repetidora", "Disparo automático al mantener clic.", 5, WeaponProfileKind.RepeatingCrossbow, 8, 0f);
        var gauntlets = UpsertWeapon(ItemsDir + "/Item_EarthquakeGauntlets.asset", "earthquake_gauntlets",
            "Guanteletes del Terremoto", "Onda expansiva con knockback.", 5, WeaponProfileKind.EarthquakeGauntlets, 12, 0.1f);
        var grimoire = UpsertWeapon(ItemsDir + "/Item_SummonerGrimoire.asset", "summoner_grimoire",
            "Grimorio Invocador", "Invoca 2 espíritus aliados.", 4, WeaponProfileKind.SummonerGrimoire, 6, 0f);

        var all = new List<LootDropEntry>
        {
            E(orbital, 10), E(chain, 10), E(hermes, 12), E(aura, 10), E(vamp, 9), E(lens, 9), E(crystalHeart, 8),
            E(potion, 18), E(crystal, 12), E(bomb, 8),
            E(volcanic, 5), E(blizzard, 5), E(crossbow, 5), E(gauntlets, 5), E(grimoire, 4),
        };

        WriteTable(TablePath, all);
        WriteTable(CatacombsTablePath, all);

        AssignTables();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LootFull] Items + tablas actualizadas (Default + Catacombs).");
    }

    static LootDropEntry E(ItemData item, int weight) => new LootDropEntry { item = item, weightOverride = weight };

    static void WriteTable(string path, List<LootDropEntry> entries)
    {
        var table = AssetDatabase.LoadAssetAtPath<LootDropTable>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<LootDropTable>();
            AssetDatabase.CreateAsset(table, path);
        }
        table.entries = new List<LootDropEntry>(entries);
        EditorUtility.SetDirty(table);
    }

    static ItemData UpsertPassive(string path, string id, string name, string desc, int weight,
        bool hasOrbital = false, bool hasChain = false, bool hasAura = false,
        bool hasVampiric = false, bool hasMultishot = false, bool hasCrystalHeart = false,
        float moveSpeedMult = 1f, float dashCooldownReduce = 0f, float arcMult = 1f,
        int maxShield = 0, float shieldRegenReduce = 0f)
    {
        var item = LoadOrCreate(path);
        item.id = id;
        item.displayName = name;
        item.description = desc;
        item.type = ItemType.Passive;
        item.dropWeight = weight;
        item.hasOrbital = hasOrbital;
        item.hasChainLightning = hasChain;
        item.hasHolyAura = hasAura;
        item.hasVampiric = hasVampiric;
        item.hasMultishot = hasMultishot;
        item.hasCrystalHeart = hasCrystalHeart;
        item.moveSpeedMult = moveSpeedMult;
        item.dashCooldownReduce = dashCooldownReduce;
        item.meleeArcAngleMult = arcMult;
        item.maxShieldBonus = maxShield;
        item.shieldRegenDelayReduce = shieldRegenReduce;
        item.temporarySeconds = 40f;
        EditorUtility.SetDirty(item);
        return item;
    }

    static ItemData UpsertConsumable(string path, string id, string name, string desc, int weight,
        int heal = 0, bool refillShield = false, float speedBurstMult = 1f,
        float speedBurstSeconds = 0f, int score = 0, int bombCharges = 0)
    {
        var item = LoadOrCreate(path);
        item.id = id;
        item.displayName = name;
        item.description = desc;
        item.type = ItemType.Consumable;
        item.dropWeight = weight;
        item.healAmount = heal;
        item.refillShield = refillShield;
        item.speedBurstMult = speedBurstMult;
        item.speedBurstSeconds = speedBurstSeconds;
        item.scoreAmount = score;
        item.bombCharges = bombCharges;
        item.temporarySeconds = 10f;
        EditorUtility.SetDirty(item);
        return item;
    }

    static float ResolveWeaponDuration(WeaponProfileKind profile)
    {
        // ~10s con variación por arma (máx < 40).
        return profile switch
        {
            WeaponProfileKind.VolcanicGreatsword => 12f,
            WeaponProfileKind.BlizzardStaff => 10f,
            WeaponProfileKind.RepeatingCrossbow => 9f,
            WeaponProfileKind.EarthquakeGauntlets => 11f,
            WeaponProfileKind.SummonerGrimoire => 10f,
            _ => 10f,
        };
    }

    static ItemData UpsertWeapon(string path, string id, string name, string desc, int weight,
        WeaponProfileKind profile, int dmgBonus, float reachBonus)
    {
        var item = LoadOrCreate(path);
        item.id = id;
        item.displayName = name;
        item.description = desc;
        item.type = ItemType.Weapon;
        item.dropWeight = weight;
        item.weaponProfile = profile;
        item.temporarySeconds = ResolveWeaponDuration(profile);
        item.weaponDamageBonus = dmgBonus;
        item.weaponReachBonus = reachBonus;
        item.weaponFireCooldown = profile == WeaponProfileKind.RepeatingCrossbow ? 0.16f : 0.45f;
        item.weaponProjectileCount = 3;
        item.weaponSlowPercent = 0.5f;
        item.weaponSlowSeconds = 3f;
        item.weaponQuakeRadius = 3.2f;
        item.weaponQuakeDamage = 35;
        item.weaponSummonCount = 2;
        item.weaponSummonSeconds = 10f;
        EditorUtility.SetDirty(item);
        return item;
    }

    static ItemData LoadOrCreate(string path)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null)
            return item;
        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    static void AssignTables()
    {
        var defaultTable = AssetDatabase.LoadAssetAtPath<LootDropTable>(TablePath);
        var catTable = AssetDatabase.LoadAssetAtPath<LootDropTable>(CatacombsTablePath);

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("TreasureChest"))
                continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = false;
            foreach (var c in root.GetComponentsInChildren<TreasureChest>(true))
            {
                if (c.dropTable == null)
                {
                    c.dropTable = defaultTable;
                    dirty = true;
                }
            }
            if (dirty)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssignSceneChests("Assets/_ArcanoDungeon/Scenes/Level1.unity", defaultTable, allowMimic: false);
        AssignSceneChests("Assets/_ArcanoDungeon/Scenes/CatacombsLevel.unity", catTable, allowMimic: true);
    }

    static void AssignSceneChests(string scenePath, LootDropTable table, bool allowMimic)
    {
        if (!File.Exists(scenePath) || table == null)
            return;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        bool dirty = false;
        foreach (var chest in Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include))
        {
            chest.dropTable = table;
            chest.allowMimic = allowMimic;
            EditorUtility.SetDirty(chest);
            dirty = true;
        }
        if (dirty)
            EditorSceneManager.SaveScene(scene);
        if (scene.IsValid() && EditorSceneManager.sceneCount > 1)
            EditorSceneManager.CloseScene(scene, true);
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

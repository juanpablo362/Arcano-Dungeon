using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Infrastructure.Assets;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class BuildDungeonRuntimeAssets : IPreprocessBuildWithReport
{
    public const string CatalogPath = "Assets/_ArcanoDungeon/Resources/DungeonRuntimeAssets.asset";
    public int callbackOrder => -100;
    public void OnPreprocessBuild(BuildReport report) => Rebuild();

    [MenuItem("Tools/Arcano/Rebuild Runtime Asset Catalog")]
    public static void Rebuild()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<DungeonRuntimeAssets>(CatalogPath);
        if (catalog == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            AssetDatabase.Refresh();
            catalog = ScriptableObject.CreateInstance<DungeonRuntimeAssets>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in typeof(LootVfxLibrary).GetFields(BindingFlags.Static | BindingFlags.Public))
            if (field.IsLiteral && field.FieldType == typeof(string))
                keys.Add((string)field.GetRawConstantValue());

        string[] sources =
        {
            "Assets/_ArcanoDungeon/Features/Combat/HeroSpecialAttack.cs",
            "Assets/_ArcanoDungeon/Features/CharacterSelection/CharacterSelectPresenter.cs"
        };
        foreach (var source in sources)
            foreach (Match match in Regex.Matches(File.ReadAllText(source), "\"(Assets/[^\"\\r\\n]+\\.(?:prefab|fbx)(?:\\|[^\"\\r\\n]+)?)\""))
                keys.Add(match.Groups[1].Value);

        const string gems = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/";
        string[] names = { "SphereGem", "Thunder", "Ruby", "Ruby2", "Diamondo", "HeartGem", "SpeedChevYellow", "Bomb", "StarGem3", "BluePotion", "Coin", "RedPotion", "StarGem1" };
        foreach (var name in names)
            keys.Add(gems + name + ".prefab");

        var entries = new List<DungeonRuntimeAssets.Entry>();
        foreach (var key in keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var parts = key.Split('|');
            UnityEngine.Object asset = parts.Length > 1
                ? AssetDatabase.LoadAllAssetsAtPath(parts[0]).OfType<AnimationClip>().FirstOrDefault(a => a.name == parts[1])
                : AssetDatabase.LoadAssetAtPath<GameObject>(key);
            if (asset != null)
                entries.Add(new DungeonRuntimeAssets.Entry { key = key, asset = asset });
        }
        catalog.entries = entries.ToArray();
        catalog.defaultDropTable = AssetDatabase.LoadAssetAtPath<LootDropTable>("Assets/_ArcanoDungeon/Features/Loot/Items/LootDropTable_Default.asset");
        catalog.mimicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ArcanoDungeon/Prefabs/Monsters/EnemyOrc.prefab");
        catalog.rarePassiveDrops = new[] { "Orbital", "ChainLightning", "HolyFireAura", "VampiricFang" }
            .Select(n => AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_ArcanoDungeon/Features/Loot/Items/Item_" + n + ".asset"))
            .Where(a => a != null).ToArray();
        if (catalog.defaultDropTable == null || catalog.mimicPrefab == null || catalog.rarePassiveDrops.Length != 4)
            throw new BuildFailedException("El catálogo de recompensas de Arcano está incompleto.");
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }
}

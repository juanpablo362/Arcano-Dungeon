using System;
using System.Collections.Generic;
using ArcanoDungeon.Features.Loot;
using UnityEngine;

namespace ArcanoDungeon.Infrastructure.Assets
{
    // Serialized references keep these assets and their dependencies in player builds.
    public sealed class DungeonRuntimeAssets : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public UnityEngine.Object asset;
        }

        public Entry[] entries = Array.Empty<Entry>();
        public LootDropTable defaultDropTable;
        public GameObject mimicPrefab;
        public ItemData[] rarePassiveDrops = Array.Empty<ItemData>();

        static DungeonRuntimeAssets instance;
        Dictionary<string, UnityEngine.Object> lookup;
        public static DungeonRuntimeAssets Instance => instance != null
            ? instance : instance = Resources.Load<DungeonRuntimeAssets>("DungeonRuntimeAssets");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => instance = null;

        public static T Load<T>(string key) where T : UnityEngine.Object
        {
            var catalog = Instance;
            if (catalog == null || string.IsNullOrEmpty(key))
                return null;
            if (catalog.lookup == null)
            {
                catalog.lookup = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
                foreach (var entry in catalog.entries)
                    if (!string.IsNullOrEmpty(entry.key) && entry.asset != null)
                        catalog.lookup[entry.key] = entry.asset;
            }
            return catalog.lookup.TryGetValue(key, out var asset) ? asset as T : null;
        }
    }
}

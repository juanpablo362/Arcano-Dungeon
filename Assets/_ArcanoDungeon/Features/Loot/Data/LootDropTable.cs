using System.Collections.Generic;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    [System.Serializable]
    public class LootDropEntry
    {
        public ItemData item;
        [Tooltip("Si > 0, sobreescribe item.dropWeight.")]
        public int weightOverride;
    }

    [CreateAssetMenu(menuName = "Arcano/Loot/Drop Table", fileName = "LootDropTable_")]
    public class LootDropTable : ScriptableObject
    {
        public List<LootDropEntry> entries = new List<LootDropEntry>();

        public ItemData Roll()
        {
            if (entries == null || entries.Count == 0)
                return null;

            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.item == null)
                    continue;
                total += ResolveWeight(e);
            }

            if (total <= 0)
                return null;

            int roll = Random.Range(0, total);
            int acc = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.item == null)
                    continue;
                acc += ResolveWeight(e);
                if (roll < acc)
                    return e.item;
            }

            return entries[0].item;
        }

        static int ResolveWeight(LootDropEntry e)
        {
            if (e.weightOverride > 0)
                return e.weightOverride;
            return Mathf.Max(0, e.item != null ? e.item.dropWeight : 0);
        }
    }
}

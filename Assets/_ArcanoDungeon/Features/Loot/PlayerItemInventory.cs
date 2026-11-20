using System;
using System.Collections.Generic;
using ArcanoDungeon.Features.Loot.Behaviours;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Inventario de pasivos de la run + consumibles + armas temporales.
    /// Los pasivos se acumulan si salen más de una vez.
    /// </summary>
    public class PlayerItemInventory : MonoBehaviour
    {
        readonly List<ItemData> passives = new List<ItemData>();
        readonly Dictionary<string, int> passiveStacks = new Dictionary<string, int>();

        PlayerStats stats;
        TemporaryWeaponController tempWeapon;

        public IReadOnlyList<ItemData> Passives => passives;
        public event Action Changed;

        public int GetStackCount(ItemData item)
        {
            if (item == null)
                return 0;
            string id = IdOf(item);
            return passiveStacks.TryGetValue(id, out int n) ? n : 0;
        }

        public int GetStackCount(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;
            return passiveStacks.TryGetValue(id, out int n) ? n : 0;
        }

        public bool HasPassiveFlag(Func<ItemData, bool> pred)
        {
            for (int i = 0; i < passives.Count; i++)
            {
                if (pred(passives[i]))
                    return true;
            }
            return false;
        }

        void Awake()
        {
            EnsureStats();
        }

        void EnsureStats()
        {
            if (stats == null)
                stats = GetComponent<PlayerStats>() ?? gameObject.AddComponent<PlayerStats>();
            if (tempWeapon == null)
                tempWeapon = GetComponent<TemporaryWeaponController>()
                             ?? gameObject.AddComponent<TemporaryWeaponController>();
            if (GetComponent<PowderBombController>() == null)
                gameObject.AddComponent<PowderBombController>();
        }

        public string Collect(ItemData item)
        {
            if (item == null)
                return "";

            EnsureStats();

            switch (item.type)
            {
                case ItemType.Consumable:
                    return stats.ApplyConsumable(item);

                case ItemType.Passive:
                    return AddPassive(item);

                case ItemType.Weapon:
                    return tempWeapon.Equip(item);

                default:
                    return item.displayName;
            }
        }

        string AddPassive(ItemData item)
        {
            string id = IdOf(item);
            int stacks = GetStackCount(id);

            if (stacks <= 0)
            {
                passives.Add(item);
                passiveStacks[id] = 1;
                stats.ApplyItemModifiers(item);
                SpawnOrStackBehaviours(item, firstPickup: true);
                Changed?.Invoke();
                return $"Equipado: {item.displayName}";
            }

            passiveStacks[id] = stacks + 1;
            stats.ApplyItemModifiers(item);
            SpawnOrStackBehaviours(item, firstPickup: false);
            Changed?.Invoke();
            return $"Apilado: {item.displayName} x{stacks + 1}";
        }

        void SpawnOrStackBehaviours(ItemData item, bool firstPickup)
        {
            if (item.hasOrbital)
            {
                var orb = GetComponent<OrbitalPassive>();
                if (orb == null)
                {
                    orb = gameObject.AddComponent<OrbitalPassive>();
                    orb.Configure(item);
                }
                else if (!firstPickup)
                    orb.AddStack(item);
            }

            if (item.hasChainLightning)
            {
                var chain = GetComponent<ChainLightningPassive>();
                if (chain == null)
                {
                    chain = gameObject.AddComponent<ChainLightningPassive>();
                    chain.Configure(item);
                }
                else if (!firstPickup)
                    chain.AddStack(item);
            }

            if (item.hasHolyAura)
            {
                var aura = GetComponent<HolyFireAuraPassive>();
                if (aura == null)
                {
                    aura = gameObject.AddComponent<HolyFireAuraPassive>();
                    aura.Configure(item);
                }
                else if (!firstPickup)
                    aura.AddStack(item);
            }

            if (item.hasVampiric)
            {
                var vamp = GetComponent<VampiricPassive>();
                if (vamp == null)
                {
                    vamp = gameObject.AddComponent<VampiricPassive>();
                    vamp.Configure(item);
                }
                else if (!firstPickup)
                    vamp.AddStack(item);
            }
        }

        static string IdOf(ItemData item)
        {
            return string.IsNullOrEmpty(item.id) ? item.name : item.id;
        }
    }
}

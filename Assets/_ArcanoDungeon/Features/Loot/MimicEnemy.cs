using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Cofre trampa: al morir suelta un pasivo raro.
    /// </summary>
    public class MimicEnemy : MonoBehaviour
    {
        [SerializeField] ItemData[] rarePassiveDrops;

        void OnEnable()
        {
            var health = GetComponent<EnemyHealth>() ?? GetComponentInChildren<EnemyHealth>();
            if (health != null)
                EnemyHealth.AnyEnemyDied += OnAnyDeath;
        }

        void OnDisable()
        {
            EnemyHealth.AnyEnemyDied -= OnAnyDeath;
        }

        void OnAnyDeath(EnemyHealth dead)
        {
            if (dead == null)
                return;
            if (dead.gameObject != gameObject && !dead.transform.IsChildOf(transform) && dead.transform.root != transform.root)
                return;

            EnemyHealth.AnyEnemyDied -= OnAnyDeath;
            DropRarePassive();
        }

        void DropRarePassive()
        {
            ItemData item = null;
            if (rarePassiveDrops != null && rarePassiveDrops.Length > 0)
                item = rarePassiveDrops[Random.Range(0, rarePassiveDrops.Length)];

            if (item == null)
                item = LoadFallbackRare();

            if (item == null)
                return;

            Vector3 pos = transform.position + Vector3.up * 0.5f;
            var go = LootVisualFactory.Create(item, pos);
            var pickup = go != null ? go.GetComponent<ItemPickup>() : null;
            pickup?.Launch(pos, pos + transform.forward * 1.1f + Vector3.up * 0.2f, 0.55f, 0.4f);
        }

        static ItemData LoadFallbackRare()
        {
            var catalog = ArcanoDungeon.Infrastructure.Assets.DungeonRuntimeAssets.Instance;
            var drops = catalog != null ? catalog.rarePassiveDrops : null;
            return drops != null && drops.Length > 0 ? drops[Random.Range(0, drops.Length)] : null;
        }

        public void ConfigureRareDrops(ItemData[] drops)
        {
            rarePassiveDrops = drops;
        }
    }
}

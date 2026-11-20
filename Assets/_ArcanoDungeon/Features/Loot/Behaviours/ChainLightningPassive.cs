using ArcanoDungeon.Features.Combat;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot.Behaviours
{
    /// <summary>
    /// Cada N hits libera un rayo que salta a enemigos cercanos.
    /// </summary>
    public class ChainLightningPassive : MonoBehaviour
    {
        int everyHits = 4;
        int maxJumps = 2;
        float radius = 4.5f;
        int damage = 22;

        int hitCounter;
        MeleeWeaponController melee;
        static readonly Collider[] Buffer = new Collider[24];

        public void Configure(ItemData item)
        {
            if (item == null)
                return;
            everyHits = Mathf.Max(1, item.chainEveryHits);
            maxJumps = Mathf.Max(1, item.chainMaxJumps);
            radius = item.chainRadius;
            damage = item.chainDamage;
        }

        public void AddStack(ItemData item)
        {
            everyHits = Mathf.Max(1, everyHits - 1);
            maxJumps += 1;
            damage += item != null ? Mathf.Max(6, item.chainDamage / 2) : 8;
            radius += 0.4f;
        }

        void OnEnable()
        {
            melee = GetComponent<MeleeWeaponController>() ?? GetComponentInParent<MeleeWeaponController>();
            if (melee != null)
                melee.EnemyHit += OnEnemyHit;
        }

        void OnDisable()
        {
            if (melee != null)
                melee.EnemyHit -= OnEnemyHit;
        }

        void OnEnemyHit(EnemyHealth enemy, Vector3 hitPoint)
        {
            NotifyHit(enemy, hitPoint);
        }

        public void NotifyHit(EnemyHealth primary, Vector3 hitPoint)
        {
            hitCounter++;
            if (hitCounter < everyHits)
                return;

            hitCounter = 0;
            FireChain(primary, hitPoint);
        }

        void FireChain(EnemyHealth primary, Vector3 origin)
        {
            int jumps = 0;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, Buffer, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count && jumps < maxJumps; i++)
            {
                var col = Buffer[i];
                if (col == null)
                    continue;

                var enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead || enemy == primary)
                    continue;

                enemy.TakeDamage(damage);
                jumps++;
                Vector3 target = enemy.transform.position + Vector3.up * 1f;
                SpawnBoltFx(origin, target);
                LootVfxLibrary.SpawnFirst(target, Quaternion.identity, null, 0.55f, 1.5f,
                    LootVfxLibrary.ElectroHit, LootVfxLibrary.Lightning2);
            }

            SpawnFlash(origin);
        }

        static void SpawnFlash(Vector3 pos)
        {
            var fx = LootVfxLibrary.SpawnFirst(pos, Quaternion.identity, null, 0.65f, 1.8f,
                LootVfxLibrary.Lightning1, LootVfxLibrary.ElectroHit, LootVfxLibrary.LightningAura);
            if (fx != null)
                return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ChainFlash";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.28f;
            Object.Destroy(go, 0.15f);
        }

        static void SpawnBoltFx(Vector3 from, Vector3 to)
        {
            var go = new GameObject("ChainBolt");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.07f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            lr.startColor = new Color(0.6f, 0.9f, 1f, 1f);
            lr.endColor = new Color(0.4f, 0.7f, 1f, 0.2f);
            Object.Destroy(go, 0.2f);
        }
    }
}

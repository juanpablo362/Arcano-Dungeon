using System.Collections.Generic;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot.Behaviours
{
    /// <summary>
    /// Orbe/escudo mágico que orbita. Cada stack añade un orbe extra.
    /// </summary>
    public class OrbitalPassive : MonoBehaviour
    {
        float radius = 1.6f;
        float orbitSpeed = 220f;
        int contactDamage = 12;
        float tickSeconds = 0.35f;
        int stacks = 1;

        readonly List<Transform> orbs = new List<Transform>();
        float angle;
        float nextDamageTime;
        static readonly Collider[] Buffer = new Collider[16];

        public void Configure(ItemData item)
        {
            if (item == null)
                return;
            radius = item.orbitalRadius;
            orbitSpeed = item.orbitalSpeed;
            contactDamage = item.orbitalContactDamage;
            tickSeconds = Mathf.Max(0.1f, item.orbitalTickSeconds);
            stacks = 1;
        }

        public void AddStack(ItemData item)
        {
            stacks++;
            if (item != null)
            {
                contactDamage += Mathf.Max(4, item.orbitalContactDamage / 2);
                orbitSpeed += 18f;
                tickSeconds = Mathf.Max(0.12f, tickSeconds * 0.92f);
            }

            SpawnOrb();
        }

        void Start()
        {
            SpawnOrb();
            angle = UnityEngine.Random.Range(0f, 360f);
        }

        void SpawnOrb()
        {
            var fx = LootVfxLibrary.SpawnFirst(
                transform.position,
                Quaternion.identity,
                transform,
                0.28f,
                -1f,
                LootVfxLibrary.MagicShieldBlue,
                LootVfxLibrary.MagicEffect8,
                LootVfxLibrary.LightningAura);

            Transform orb;
            if (fx != null)
            {
                orb = fx.transform;
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "OrbitalShieldFallback";
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.28f;
                Destroy(go.GetComponent<Collider>());
                orb = go.transform;
            }

            orbs.Add(orb);
        }

        void Update()
        {
            if (orbs.Count == 0)
                return;

            angle += orbitSpeed * Time.deltaTime;
            float step = 360f / Mathf.Max(1, orbs.Count);

            for (int i = orbs.Count - 1; i >= 0; i--)
            {
                var orb = orbs[i];
                if (orb == null)
                {
                    orbs.RemoveAt(i);
                    continue;
                }

                float rad = (angle + step * i) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, 1.05f, Mathf.Sin(rad) * radius);
                orb.position = transform.position + offset;
                BlockEnemyProjectiles(orb.position);
            }

            if (Time.time < nextDamageTime)
                return;

            nextDamageTime = Time.time + tickSeconds;
            for (int i = 0; i < orbs.Count; i++)
            {
                if (orbs[i] != null)
                    DamageNearbyEnemies(orbs[i].position);
            }
        }

        void DamageNearbyEnemies(Vector3 pos)
        {
            int count = Physics.OverlapSphereNonAlloc(pos, 0.5f, Buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null)
                    continue;
                var enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead)
                    continue;
                enemy.TakeDamage(contactDamage);
            }
        }

        void BlockEnemyProjectiles(Vector3 pos)
        {
            int count = Physics.OverlapSphereNonAlloc(pos, 0.55f, Buffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null)
                    continue;

                var fb = col.GetComponentInParent<FireballProjectile>();
                if (fb != null && fb.IsEnemyProjectile)
                {
                    Destroy(fb.gameObject);
                    continue;
                }

                int enemyProjLayer = LayerMask.NameToLayer("EnemyProjectile");
                if (enemyProjLayer >= 0 && col.gameObject.layer == enemyProjLayer)
                    Destroy(col.gameObject);
            }
        }
    }
}

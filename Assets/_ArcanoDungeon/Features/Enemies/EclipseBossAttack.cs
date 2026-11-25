using System.Collections;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Assassin;
using UnityEngine;

namespace ArcanoDungeon.Features.Levels
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EclipseBossAttack : MonoBehaviour
    {
        public Material warningMaterial;
        public float radius = 4.8f;
        public float warningSeconds = 1.4f;
        public int damage = 24;
        EnemyHealth health;
        LineRenderer ring;
        float nextNova;
        bool casting;

        void Start()
        {
            health = GetComponent<EnemyHealth>();
            var root = new GameObject("EclipseNovaWarning");
            root.transform.SetParent(transform, false);
            ring = root.AddComponent<LineRenderer>();
            ring.sharedMaterial = warningMaterial;
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = 64;
            ring.widthMultiplier = 0.12f;
            ring.enabled = false;
            nextNova = Time.time + 5f;
        }

        void Update()
        {
            if (health.IsDead)
            {
                if (ring != null) ring.enabled = false;
                return;
            }
            var target = GameObject.FindGameObjectWithTag("Player");
            if (!casting && target != null && !AssassinAbilities.IsConcealed(target.transform) && Time.time >= nextNova)
                StartCoroutine(Nova());
        }

        IEnumerator Nova()
        {
            casting = true;
            Vector3 origin = transform.position;
            ring.enabled = true;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                ring.SetPosition(i, origin + new Vector3(Mathf.Cos(angle) * radius, 0.10f, Mathf.Sin(angle) * radius));
            }
            float end = Time.time + warningSeconds;
            while (Time.time < end && !health.IsDead)
            {
                var target = GameObject.FindGameObjectWithTag("Player");
                if (target == null || AssassinAbilities.IsConcealed(target.transform))
                {
                    ring.enabled = false; casting = false; nextNova = Time.time + 1f; yield break;
                }
                ring.widthMultiplier = 0.10f + 0.13f * Mathf.PingPong(Time.time * 5f, 1f);
                yield return null;
            }
            ring.enabled = false;
            if (!health.IsDead)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && !AssassinAbilities.IsConcealed(player.transform))
                {
                    var delta = player.transform.position - origin;
                    if (new Vector2(delta.x, delta.z).sqrMagnitude <= radius * radius && Mathf.Abs(delta.y) < 3f)
                        player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
                    LootVfxLibrary.SpawnFirst(origin, Quaternion.identity, null, 1.1f, 2f,
                        LootVfxLibrary.GroundAoe, LootVfxLibrary.ElectroHit);
                }
            }
            nextNova = Time.time + (health.currentHealth < health.maxHealth / 2 ? 5f : 8f);
            casting = false;
        }

        void OnDisable()
        {
            StopAllCoroutines();
            casting = false;
            if (ring != null) ring.enabled = false;
        }
    }
}

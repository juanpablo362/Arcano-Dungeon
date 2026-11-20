using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Minion aliado simple (grimorio): persigue y daña al enemigo más cercano.
    /// </summary>
    public class AllyMinionAI : MonoBehaviour
    {
        float life;
        int damage = 12;
        float speed = 4.2f;
        float tick = 0.45f;
        float nextHit;
        float spawnTime;

        public static AllyMinionAI Spawn(Vector3 position, float lifetime, int dmg)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "AllySpirit";
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            Object.Destroy(go.GetComponent<Collider>());

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    var c = new Color(0.55f, 0.85f, 1f, 0.85f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                    rend.sharedMaterial = mat;
                }
            }

            LootVfxLibrary.SpawnFirst(position, Quaternion.identity, go.transform, 0.4f, -1f,
                LootVfxLibrary.MagicEffect8, LootVfxLibrary.HolyHit);

            var ai = go.AddComponent<AllyMinionAI>();
            ai.life = lifetime;
            ai.damage = dmg;
            ai.spawnTime = Time.time;
            Object.Destroy(go, lifetime + 0.1f);
            return ai;
        }

        void Update()
        {
            if (Time.time - spawnTime >= life)
            {
                Destroy(gameObject);
                return;
            }

            EnemyHealth target = FindNearestEnemy(10f);
            if (target == null)
                return;

            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
            {
                transform.position += to.normalized * speed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }

            if (Time.time < nextHit || to.magnitude > 1.35f)
                return;

            nextHit = Time.time + tick;
            target.TakeDamage(damage);
        }

        EnemyHealth FindNearestEnemy(float range)
        {
            var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
            EnemyHealth best = null;
            float bestSq = range * range;
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead)
                    continue;
                float sq = (e.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = e;
                }
            }
            return best;
        }
    }
}

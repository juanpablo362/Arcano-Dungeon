using UnityEngine;

namespace ArcanoDungeon.Features.Loot.Behaviours
{
    /// <summary>
    /// Aura de fuego en el suelo (Vefects Fire Floor + círculo Hovl).
    /// </summary>
    public class HolyFireAuraPassive : MonoBehaviour
    {
        float radius = 3f;
        float tickSeconds = 0.5f;
        int damage = 8;

        float nextTick;
        Transform fxRoot;
        static readonly Collider[] Buffer = new Collider[32];

        public void Configure(ItemData item)
        {
            if (item == null)
                return;
            radius = item.holyAuraRadius;
            tickSeconds = Mathf.Max(0.15f, item.holyAuraTickSeconds);
            damage = item.holyAuraDamage;
        }

        public void AddStack(ItemData item)
        {
            damage += item != null ? Mathf.Max(4, item.holyAuraDamage) : 8;
            radius += 0.4f;
            tickSeconds = Mathf.Max(0.18f, tickSeconds * 0.9f);
            RefreshFallbackScale();
        }

        void RefreshFallbackScale()
        {
            if (fxRoot == null)
                return;
            for (int i = 0; i < fxRoot.childCount; i++)
            {
                var child = fxRoot.GetChild(i);
                if (child != null && child.name.Contains("Fallback"))
                    child.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            }
        }

        void Start()
        {
            var root = new GameObject("HolyFireAuraFX");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            fxRoot = root.transform;

            // Fuego de suelo (radio ~3m).
            float fireScale = Mathf.Clamp(radius / 2.2f, 0.85f, 1.6f);
            var floor = LootVfxLibrary.SpawnFirst(
                transform.position,
                Quaternion.identity,
                fxRoot,
                fireScale,
                -1f,
                LootVfxLibrary.FireFloor,
                LootVfxLibrary.FireFloorSimple,
                LootVfxLibrary.FireEffect3);
            if (floor != null)
                floor.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var circle = LootVfxLibrary.Spawn(
                LootVfxLibrary.MagicCircle,
                transform.position,
                Quaternion.identity,
                fxRoot,
                radius * 0.55f,
                -1f);
            if (circle != null)
                circle.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            // Fallback visual si faltan packs.
            if (floor == null && circle == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "HolyFireAuraRingFallback";
                go.transform.SetParent(fxRoot, false);
                go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
                Object.Destroy(go.GetComponent<Collider>());
            }
        }

        void LateUpdate()
        {
            if (fxRoot != null)
                fxRoot.position = transform.position;
        }

        void Update()
        {
            if (Time.time < nextTick)
                return;

            nextTick = Time.time + tickSeconds;
            Vector3 center = transform.position + Vector3.up * 0.5f;
            int count = Physics.OverlapSphereNonAlloc(center, radius, Buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null)
                    continue;
                var enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead)
                    continue;
                enemy.TakeDamage(damage);
            }
        }
    }
}

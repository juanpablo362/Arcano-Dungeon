using UnityEngine;

namespace ArcanoDungeon.Features.Assassin
{
    public sealed class PoisonKnifeProjectile : MonoBehaviour
    {
        [SerializeField] float speed = 22f;
        [SerializeField] float maxDistance = 35f;
        [SerializeField] float hitRadius = .075f;
        [SerializeField] ParticleSystem poisonEffect;
        Transform owner;
        int damage, poisonDamage, ticks;
        float interval, travelled;
        bool consumed;
        readonly RaycastHit[] hits = new RaycastHit[64];
        readonly Collider[] overlaps = new Collider[32];

        public void Launch(Transform shooter, Vector3 direction, int hitDamage, int tickDamage, int tickCount, float tickInterval)
        {
            owner = shooter; damage = hitDamage; poisonDamage = tickDamage; ticks = tickCount; interval = tickInterval;
            transform.rotation = Quaternion.LookRotation(direction.normalized);
            Destroy(gameObject, maxDistance / speed + .2f);
        }

        bool IsObstacle(Collider collider) => collider != null && !collider.isTrigger && (owner == null || !collider.transform.IsChildOf(owner));

        void Update()
        {
            if (consumed) return;
            int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, hitRadius, overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlapCount; i++)
                if (IsObstacle(overlaps[i])) { Hit(overlaps[i]); return; }
            float distance = Mathf.Min(speed * Time.deltaTime, maxDistance - travelled);
            int count = Physics.SphereCastNonAlloc(transform.position, hitRadius, transform.forward, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue; Collider obstacle = null;
            for (int i = 0; i < count; i++)
                if (IsObstacle(hits[i].collider) && hits[i].distance < nearest) { nearest = hits[i].distance; obstacle = hits[i].collider; }
            if (obstacle != null) { Hit(obstacle); return; }
            transform.position += transform.forward * distance; travelled += distance;
            if (travelled >= maxDistance) { consumed = true; Destroy(gameObject); }
        }

        void Hit(Collider collider)
        {
            consumed = true;
            var enemy = collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damage);
                if (!enemy.IsDead)
                {
                    var poison = enemy.GetComponent<PoisonedEnemy>() ?? enemy.gameObject.AddComponent<PoisonedEnemy>();
                    poison.Apply(poisonDamage, ticks, interval, poisonEffect);
                }
            }
            Destroy(gameObject);
        }
    }
}

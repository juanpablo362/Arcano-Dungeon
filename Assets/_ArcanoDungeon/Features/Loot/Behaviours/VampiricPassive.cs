using UnityEngine;

namespace ArcanoDungeon.Features.Loot.Behaviours
{
    public class VampiricPassive : MonoBehaviour
    {
        float chance = 0.15f;
        int heal = 5;
        int shield = 10;

        PlayerStats stats;
        PlayerHealth health;

        public void Configure(ItemData item)
        {
            if (item == null)
                return;
            chance = Mathf.Clamp01(item.vampiricChance);
            heal = item.vampiricHeal;
            shield = item.vampiricShield;
        }

        public void AddStack(ItemData item)
        {
            float addChance = item != null ? item.vampiricChance : 0.1f;
            chance = Mathf.Clamp(chance + addChance * 0.65f, 0f, 0.75f);
            heal += item != null ? Mathf.Max(2, item.vampiricHeal / 2) : 3;
            shield += item != null ? Mathf.Max(4, item.vampiricShield / 2) : 5;
        }

        void OnEnable()
        {
            EnemyHealth.AnyEnemyDied += OnEnemyDied;
            stats = GetComponent<PlayerStats>() ?? GetComponentInParent<PlayerStats>();
            health = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        }

        void OnDisable()
        {
            EnemyHealth.AnyEnemyDied -= OnEnemyDied;
        }

        void OnEnemyDied(EnemyHealth _)
        {
            if (Random.value > chance)
                return;

            if (Random.value < 0.5f)
            {
                if (health != null)
                    health.Heal(heal);
            }
            else
            {
                if (stats == null)
                    stats = GetComponent<PlayerStats>();
                stats?.AddShield(shield);
            }
        }
    }
}

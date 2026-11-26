using UnityEngine;

namespace ArcanoDungeon.Features.Assassin
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class PoisonedEnemy : MonoBehaviour
    {
        EnemyHealth health;
        ParticleSystem effect;
        int damage, ticksRemaining;
        float interval, nextTick;
        public bool IsPoisoned => ticksRemaining > 0 && health != null && !health.IsDead;
        public int TicksRemaining => ticksRemaining;

        public void Apply(int tickDamage, int ticks, float tickInterval, ParticleSystem effectPrefab)
        {
            if (health == null) health = GetComponent<EnemyHealth>();
            if (health.IsDead) return;
            bool fresh = ticksRemaining <= 0;
            damage = fresh ? tickDamage : Mathf.Max(damage, tickDamage);
            interval = Mathf.Max(.1f, tickInterval);
            ticksRemaining = Mathf.Max(ticksRemaining, ticks);
            if (fresh) nextTick = Time.time + interval;
            enabled = true;
            if (effect == null && effectPrefab != null) effect = Instantiate(effectPrefab, transform);
            if (effect != null && !effect.isPlaying) effect.Play(true);
        }

        void Update()
        {
            if (health == null || health.IsDead) { End(); return; }
            while (ticksRemaining > 0 && Time.time >= nextTick)
            {
                ticksRemaining--; nextTick += interval; health.TakeDamage(damage);
                if (health.IsDead) break;
            }
            if (ticksRemaining <= 0 || health.IsDead) End();
        }

        void End()
        {
            ticksRemaining = 0;
            if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            enabled = false;
        }
        void OnDisable() { if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
    }
}

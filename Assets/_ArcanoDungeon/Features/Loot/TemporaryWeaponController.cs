using System;
using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Infrastructure.Audio;
using ArcanoDungeon.Infrastructure.Input;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Arma temporal de cofre (~8–12s). Aplica perfiles y restaura el arma base.
    /// </summary>
    public class TemporaryWeaponController : MonoBehaviour
    {
        ItemData active;
        float endsAt;
        float durationTotal;
        int damageBonusApplied;
        float reachBonusApplied;

        MeleeWeaponController melee;
        PlayerController playerController;
        PlayerInputReader input;

        bool savedUseFireball;

        static readonly Collider[] Buffer = new Collider[24];

        public bool IsActive => active != null && Time.time < endsAt;
        public ItemData ActiveItem => active;
        public float Remaining => IsActive ? endsAt - Time.time : 0f;
        public float DurationTotal => durationTotal;
        public float RemainingNormalized =>
            !IsActive || durationTotal <= 0.01f ? 0f : Mathf.Clamp01(Remaining / durationTotal);

        public event Action Changed;

        void Awake()
        {
            melee = GetComponent<MeleeWeaponController>() ?? GetComponentInParent<MeleeWeaponController>();
            playerController = GetComponent<PlayerController>() ?? GetComponentInParent<PlayerController>();
            input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();
        }

        void OnEnable()
        {
            if (melee != null)
            {
                melee.EnemyHit += OnEnemyHit;
                melee.AttackStarted += OnMeleeAttackStarted;
            }
        }

        void OnDisable()
        {
            if (melee != null)
            {
                melee.EnemyHit -= OnEnemyHit;
                melee.AttackStarted -= OnMeleeAttackStarted;
            }
            ClearWeapon(false);
        }

        void Update()
        {
            if (active != null && Time.time >= endsAt)
            {
                ClearWeapon(true);
                return;
            }

            if (!IsActive || active == null)
                return;

            var profile = active.weaponProfile;
            if (profile == WeaponProfileKind.RepeatingCrossbow)
                TickCrossbow();
            else if (profile == WeaponProfileKind.BlizzardStaff)
                TickBlizzard();
        }

        public string Equip(ItemData item)
        {
            if (item == null || item.type != ItemType.Weapon)
                return "";

            ClearWeapon(false);
            EnsureRefs();

            active = item;
            durationTotal = ResolveDuration(item);
            endsAt = Time.time + durationTotal;

            damageBonusApplied = item.weaponDamageBonus;
            reachBonusApplied = item.weaponReachBonus;
            if (melee != null)
            {
                if (damageBonusApplied != 0)
                    melee.AddDamageBonus(damageBonusApplied);
                if (Mathf.Abs(reachBonusApplied) > 0.001f)
                    melee.AddReachBonus(reachBonusApplied);
            }

            if (playerController != null)
            {
                savedUseFireball = playerController.useFireballOnRightClick;
                if (item.weaponProfile == WeaponProfileKind.BlizzardStaff
                    || item.weaponProfile == WeaponProfileKind.RepeatingCrossbow
                    || item.weaponProfile == WeaponProfileKind.SummonerGrimoire)
                {
                    // Evitar fireball normal solapándose; el runtime dispara.
                    playerController.useFireballOnRightClick = false;
                }
            }

            Changed?.Invoke();
            InteractionPromptUI.RequestPrompt($"{item.displayName} ({Mathf.RoundToInt(durationTotal)}s)", 0f);
            return $"Arma: {item.displayName} ({Mathf.RoundToInt(durationTotal)}s)";
        }

        public void RestoreWeapon(ItemData item, float remaining, float total)
        {
            Equip(item);
            durationTotal = Mathf.Max(remaining, total);
            endsAt = Time.time + Mathf.Max(0f, remaining);
        }

        static float ResolveDuration(ItemData item)
        {
            float configured = item != null ? item.temporarySeconds : 10f;
            // Si un asset viejo aún tiene 40s, cae al rango corto por perfil.
            if (configured > 20f || configured < 3f)
            {
                return item.weaponProfile switch
                {
                    WeaponProfileKind.VolcanicGreatsword => 12f,
                    WeaponProfileKind.BlizzardStaff => 10f,
                    WeaponProfileKind.RepeatingCrossbow => 9f,
                    WeaponProfileKind.EarthquakeGauntlets => 11f,
                    WeaponProfileKind.SummonerGrimoire => 10f,
                    _ => 10f,
                };
            }

            return Mathf.Clamp(configured, 6f, 15f);
        }

        void ClearWeapon(bool announce)
        {
            if (active == null)
                return;

            if (melee != null)
            {
                if (damageBonusApplied != 0)
                    melee.AddDamageBonus(-damageBonusApplied);
                if (Mathf.Abs(reachBonusApplied) > 0.001f)
                    melee.AddReachBonus(-reachBonusApplied);
            }

            if (playerController != null)
                playerController.useFireballOnRightClick = savedUseFireball;

            if (announce)
                InteractionPromptUI.RequestPrompt("Arma base restaurada", 0f);

            active = null;
            damageBonusApplied = 0;
            reachBonusApplied = 0f;
            durationTotal = 0f;
            endsAt = 0f;
            Changed?.Invoke();
        }

        void EnsureRefs()
        {
            if (melee == null)
                melee = GetComponent<MeleeWeaponController>();
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            if (input == null)
                input = GetComponent<PlayerInputReader>();
        }

        void OnEnemyHit(EnemyHealth enemy, Vector3 hitPoint)
        {
            if (!IsActive || active == null || enemy == null)
                return;

            if (active.weaponProfile == WeaponProfileKind.VolcanicGreatsword)
                FireVolcanicWave(hitPoint);
        }

        void OnMeleeAttackStarted()
        {
            if (!IsActive || active == null)
                return;

            if (active.weaponProfile == WeaponProfileKind.EarthquakeGauntlets)
                DoEarthquake();
            else if (active.weaponProfile == WeaponProfileKind.SummonerGrimoire)
                SummonAllies();
        }

        float nextCrossbowShot;
        float nextBlizzardShot;

        void TickCrossbow()
        {
            if (input == null || !input.AttackHeld)
                return;
            if (Time.time < nextCrossbowShot)
                return;

            nextCrossbowShot = Time.time + Mathf.Max(0.08f, active.weaponFireCooldown);
            Vector3 aim = ResolveAim();
            float spread = UnityEngine.Random.Range(-7f, 7f);
            Vector3 dir = Quaternion.Euler(0f, spread, 0f) * aim;
            SpawnProjectile(dir, Mathf.Max(12, 20 + active.weaponDamageBonus), 22f, false);
        }

        void TickBlizzard()
        {
            // RMB / Secondary o Attack: ráfaga de 3.
            bool fire = (input != null && (input.AttackPressed || input.SecondaryPressed));
            if (!fire || Time.time < nextBlizzardShot)
                return;

            nextBlizzardShot = Time.time + 0.55f;
            Vector3 aim = ResolveAim();
            int count = Mathf.Max(1, active.weaponProjectileCount);
            float step = 12f;
            float start = -step * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, start + step * i, 0f) * aim;
                SpawnProjectile(dir, Mathf.Max(10, 18 + active.weaponDamageBonus), 14f, true);
            }
        }

        void FireVolcanicWave(Vector3 origin)
        {
            Vector3 aim = ResolveAim();
            LootVfxLibrary.SpawnFirst(origin + aim * 0.5f, Quaternion.LookRotation(aim), null, 0.8f, 1.8f,
                LootVfxLibrary.FireSmall, LootVfxLibrary.FireEffect1, LootVfxLibrary.AoESlashOrange);

            Vector3 boxCenter = transform.position + Vector3.up * 1f + aim * 2.2f;
            int count = Physics.OverlapBoxNonAlloc(boxCenter, new Vector3(1.1f, 1f, 2.4f), Buffer,
                Quaternion.LookRotation(aim), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var enemy = Buffer[i] != null ? Buffer[i].GetComponentInParent<EnemyHealth>() : null;
                if (enemy == null || enemy.IsDead)
                    continue;
                enemy.TakeDamage(Mathf.Max(12, 18 + active.weaponDamageBonus / 2));
            }
        }

        void DoEarthquake()
        {
            float rad = active.weaponQuakeRadius > 0f ? active.weaponQuakeRadius : 3.2f;
            int dmg = active.weaponQuakeDamage > 0 ? active.weaponQuakeDamage : 35;
            Vector3 center = transform.position;

            var catalog = DungeonSfxCatalog.Instance;
            if (catalog != null && catalog.groundImpact != null)
                GameSfx.PlayCombat(catalog.groundImpact);

            LootVfxLibrary.SpawnFirst(center, Quaternion.identity, null, 1.15f, 2.2f,
                LootVfxLibrary.GroundAoe, LootVfxLibrary.Explosion1);

            int count = Physics.OverlapSphereNonAlloc(center, rad, Buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var enemy = Buffer[i] != null ? Buffer[i].GetComponentInParent<EnemyHealth>() : null;
                if (enemy == null || enemy.IsDead)
                    continue;
                enemy.TakeDamage(dmg);
                var ai = enemy.GetComponent<EnemyAI>();
                Vector3 push = enemy.transform.position - center;
                push.y = 0f;
                ai?.ApplyKnockback(push.normalized, 1f);
            }
        }

        void SummonAllies()
        {
            int n = Mathf.Clamp(active.weaponSummonCount, 1, 3);
            float life = active.weaponSummonSeconds > 0f ? active.weaponSummonSeconds : 10f;
            for (int i = 0; i < n; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 1.2f;
                Vector3 pos = transform.position + offset + Vector3.up * 0.1f;
                AllyMinionAI.Spawn(pos, life, Mathf.Max(8, 10 + active.weaponDamageBonus / 2));
            }
        }

        void SpawnProjectile(Vector3 dir, int damage, float speed, bool applySlow)
        {
            GameObject prefab = playerController != null ? playerController.fireballPrefab : null;
            Vector3 spawn = transform.position + Vector3.up * 1.2f + dir * 0.7f;
            GameObject ball;
            if (prefab != null)
                ball = Instantiate(prefab, spawn, Quaternion.LookRotation(dir));
            else
            {
                ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.transform.position = spawn;
                ball.transform.localScale = Vector3.one * 0.25f;
                Destroy(ball.GetComponent<Collider>());
            }

            var proj = ball.GetComponent<FireballProjectile>() ?? ball.AddComponent<FireballProjectile>();
            proj.damage = damage;
            proj.speed = speed;
            proj.Launch(transform, dir);

            if (applySlow)
            {
                var slow = ball.AddComponent<ProjectileSlowOnHit>();
                slow.Configure(active.weaponSlowPercent, active.weaponSlowSeconds);
            }

            if (applySlow)
            {
                LootVfxLibrary.SpawnFirst(spawn, Quaternion.identity, null, 0.35f, 1f,
                    LootVfxLibrary.MagicEffect8);
            }
        }

        Vector3 ResolveAim()
        {
            var aim = GetComponent<ArcanoDungeon.Features.Player.View.AimRaycaster>()
                      ?? GetComponentInParent<ArcanoDungeon.Features.Player.View.AimRaycaster>();
            if (aim != null)
            {
                Vector3 d = aim.GetFlatAimDirection();
                if (d.sqrMagnitude > 0.001f)
                    return d;
            }

            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }
    }

    public class ProjectileSlowOnHit : MonoBehaviour
    {
        float slowPct = 0.5f;
        float seconds = 3f;
        bool done;

        public void Configure(float pct, float secs)
        {
            slowPct = pct > 0f ? pct : 0.5f;
            seconds = secs > 0f ? secs : 3f;
        }

        void Update()
        {
            if (done)
                return;
            var hits = Physics.OverlapSphere(transform.position, 0.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                var enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead)
                    continue;
                enemy.GetComponent<EnemyAI>()?.ApplySlow(1f - slowPct, seconds);
                done = true;
                return;
            }
        }
    }
}

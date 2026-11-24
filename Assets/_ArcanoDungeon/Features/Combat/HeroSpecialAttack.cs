using System.Collections;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player;
using ArcanoDungeon.Features.Player.View;
using ArcanoDungeon.Infrastructure.Audio;
using ArcanoDungeon.Infrastructure.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcanoDungeon.Features.Combat
{
    /// <summary>
    /// Ataque especial por héroe (tecla E). Consume estamina.
    /// </summary>
    public class HeroSpecialAttack : MonoBehaviour
    {
        const string CfxrGroundHit = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR2 Ground Hit.prefab";
        const string CfxrFlash = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Flash.prefab";
        const string CfxrSwordCross = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab";
        const string CfxrHitMisc = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR3 Hit Misc A.prefab";
        const string CfxrSlashText = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Texts/CFXR _SLASH_.prefab";
        const string CfxrMagicPoof = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab";

        [Header("Refs")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerStamina stamina;
        [SerializeField] AimRaycaster aimRaycaster;
        [SerializeField] Animator animator;
        [SerializeField] GameObject fireballPrefab;

        [Header("Costes / timing")]
        [SerializeField] float cooldown = 0.85f;
        [SerializeField] float mageDamage = 70f;
        [SerializeField] float knightDamage = 55f;
        [SerializeField] float misakiDamage = 42f;
        [SerializeField] float yukoDamage = 60f;
        [SerializeField] float knightRadius = 3.2f;
        [SerializeField] float misakiRange = 3.8f;
        [SerializeField] float yukoRadius = 3.6f;

        float nextSpecialTime;
        bool specialQueued;
        static readonly int AttackTrigger = Animator.StringToHash("Attack");
        static readonly Collider[] HitBuffer = new Collider[32];
        static Material s_ringMat;
        static Material s_particleMat;

        void Awake()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();
            if (stamina == null)
                stamina = GetComponent<PlayerStamina>() ?? GetComponentInParent<PlayerStamina>();
            if (aimRaycaster == null)
                aimRaycaster = GetComponent<AimRaycaster>() ?? GetComponentInParent<AimRaycaster>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (fireballPrefab == null)
            {
                var pc = GetComponent<PlayerController>() ?? GetComponentInParent<PlayerController>();
                if (pc != null && pc.fireballPrefab != null)
                    fireballPrefab = pc.fireballPrefab;
            }
        }

        void OnEnable()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();
            if (input != null)
                input.OnSpecial += QueueSpecial;
        }

        void OnDisable()
        {
            if (input != null)
                input.OnSpecial -= QueueSpecial;
        }

        void QueueSpecial() => specialQueued = true;

        void Update()
        {
            bool pressed = specialQueued
                           || (input != null && input.SpecialPressed)
                           || (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame);
            specialQueued = false;

            if (!pressed)
                return;
            if (PauseMenuController.IsOpen)
                return;
            if (Time.time < nextSpecialTime)
                return;

            if (stamina == null)
                stamina = GetComponent<PlayerStamina>();
            if (stamina != null && !stamina.TryConsumeSpecial())
                return;

            nextSpecialTime = Time.time + cooldown;
            CastSpecial(GameSession.SelectedHero);
        }

        int BonusDamage
        {
            get
            {
                var stats = GetComponent<PlayerStats>() ?? GetComponentInParent<PlayerStats>();
                if (stats != null)
                    return stats.BonusDamage;
                var buffs = GetComponent<PlayerLootBuffs>() ?? GetComponentInParent<PlayerLootBuffs>();
                return buffs != null ? buffs.BonusDamage : 0;
            }
        }

        void CastSpecial(HeroId hero)
        {
            Vector3 aim = ResolveAim();
            Face(aim);

            if (animator != null && HasTrigger(AttackTrigger))
                animator.SetTrigger(AttackTrigger);

            switch (hero)
            {
                case HeroId.Mage:
                    CastMageNova(aim);
                    break;
                case HeroId.Knight:
                    CastKnightShockwave();
                    break;
                case HeroId.Misaki:
                    StartCoroutine(CastMisakiFlurry(aim));
                    break;
                case HeroId.Yuko:
                    CastYukoCleave(aim);
                    break;
                default:
                    CastKnightShockwave();
                    break;
            }
        }

        bool HasTrigger(int hash)
        {
            if (animator == null)
                return false;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Trigger)
                    return true;
            }
            return false;
        }

        void CastMageNova(Vector3 aim)
        {
            if (fireballPrefab == null)
            {
                DamageInCone(aim, 4.2f, 70f, Mathf.RoundToInt(mageDamage) + BonusDamage);
                SpawnImpactBurst(transform.position + Vector3.up * 1f + aim * 1.5f,
                    new Color(0.45f, 0.8f, 1f), 1.4f);
                TrySpawnCfxr(CfxrMagicPoof, transform.position + Vector3.up * 1f + aim * 1.2f, 1.1f);
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 1.2f + aim * 0.6f;
            float[] angles = { -18f, 0f, 18f };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[i], 0f) * aim;
                var ball = Instantiate(fireballPrefab, origin, Quaternion.LookRotation(dir));
                var proj = ball.GetComponent<FireballProjectile>() ?? ball.AddComponent<FireballProjectile>();
                proj.damage = Mathf.RoundToInt(mageDamage) + BonusDamage;
                proj.speed = 16f;
                proj.Launch(transform, dir);
            }
        }

        void CastKnightShockwave()
        {
            var earth = DungeonSfxCatalog.Instance != null ? DungeonSfxCatalog.Instance.knightSpecial : null;
            GameSfx.Play2D(earth, 0.8f);

            Vector3 feet = transform.position;
            Vector3 center = feet + Vector3.up * 0.15f;

            DamageInRadius(feet + Vector3.up * 0.9f, knightRadius, Mathf.RoundToInt(knightDamage) + BonusDamage);

            // Golpe de escudo: VFX de packs + anillo de apoyo.
            StartCoroutine(AnimateShockwaveRing(center, knightRadius, new Color(1f, 0.92f, 0.55f, 0.95f), 0.42f));
            SpawnDustBurst(center, new Color(0.85f, 0.72f, 0.4f), knightRadius);
            SpawnSparkBurst(center + Vector3.up * 0.35f, new Color(1f, 0.95f, 0.65f), 28);
            SpawnFlashLight(center + Vector3.up * 0.8f, new Color(1f, 0.9f, 0.55f), 4.5f, 0.22f);

            LootVfxLibrary.SpawnFirst(center, Quaternion.identity, null, 1.1f, 2.5f,
                LootVfxLibrary.GroundAoe, LootVfxLibrary.Explosion1, LootVfxLibrary.AoESlashOrange);
            LootVfxLibrary.SpawnFirst(center, Quaternion.identity, null, 0.9f, 2.2f,
                LootVfxLibrary.FireFloorSimple, LootVfxLibrary.FireSmall, LootVfxLibrary.HolyHit);
            TrySpawnCfxr(CfxrGroundHit, center, 1.15f);
            TrySpawnCfxr(CfxrSwordCross, feet + Vector3.up * 1.1f + transform.forward * 0.4f, 0.9f);
        }

        IEnumerator CastMisakiFlurry(Vector3 aim)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 tip = transform.position + Vector3.up * 1f + aim * (1.2f + i * 0.55f);
                DamageInCone(aim, misakiRange, 55f, Mathf.RoundToInt(misakiDamage) + BonusDamage);
                SpawnSlashArc(tip, aim, new Color(1f, 0.45f, 0.75f), 0.9f);
                SpawnSparkBurst(tip, new Color(1f, 0.55f, 0.85f), 12);
                if (i == 1)
                    TrySpawnCfxr(CfxrHitMisc, tip, 0.85f);
                yield return new WaitForSeconds(0.08f);
            }
        }

        void CastYukoCleave(Vector3 aim)
        {
            Vector3 tip = transform.position + Vector3.up * 1f + aim * 1.4f;
            DamageInCone(aim, yukoRadius, 100f, Mathf.RoundToInt(yukoDamage) + BonusDamage);
            SpawnSlashArc(tip, aim, new Color(0.55f, 0.85f, 1f), 1.5f);
            SpawnSparkBurst(tip, new Color(0.7f, 0.9f, 1f), 22);
            SpawnFlashLight(tip, new Color(0.6f, 0.85f, 1f), 3.2f, 0.18f);
            TrySpawnCfxr(CfxrSwordCross, tip, 1.2f);
            TrySpawnCfxr(CfxrSlashText, tip + Vector3.up * 0.3f, 0.9f);
        }

        void DamageInRadius(Vector3 center, float radius, int damage)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, HitBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = HitBuffer[i];
                if (col == null || col.isTrigger)
                    continue;
                ApplyEnemyDamage(col, damage);
            }
        }

        void DamageInCone(Vector3 forward, float range, float halfAngle, int damage)
        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            int count = Physics.OverlapSphereNonAlloc(origin, range, HitBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = HitBuffer[i];
                if (col == null || col.isTrigger)
                    continue;

                Vector3 to = col.bounds.center - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                {
                    float ang = Vector3.Angle(forward, to.normalized);
                    if (ang > halfAngle)
                        continue;
                }

                ApplyEnemyDamage(col, damage);
            }
        }

        static void ApplyEnemyDamage(Collider col, int damage)
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null)
                return;
            enemy.TakeDamage(damage);
        }

        Vector3 ResolveAim()
        {
            if (aimRaycaster != null)
            {
                Vector3 d = aimRaycaster.GetFlatAimDirection();
                if (d.sqrMagnitude > 0.001f)
                    return d;
            }

            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        }

        void Face(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                return;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        // ─── VFX ─────────────────────────────────────────────────────────────

        static IEnumerator AnimateShockwaveRing(Vector3 center, float maxRadius, Color color, float duration)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "KnightShockwaveRing";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);

            var rend = ring.GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = GetRingMaterial(color);

            // Anillo hueco aproximado: segundo cilindro más delgado encima no hace falta;
            // el scale XZ crece y la alpha baja.
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float ease = 1f - (1f - u) * (1f - u);
                float diam = Mathf.Lerp(0.35f, maxRadius * 2f, ease);
                float height = Mathf.Lerp(0.045f, 0.012f, u);
                ring.transform.localScale = new Vector3(diam, height, diam);

                if (rend != null && rend.material != null)
                {
                    Color c = color;
                    c.a = Mathf.Lerp(0.9f, 0f, u * u);
                    SetMatColor(rend.material, c);
                }

                yield return null;
            }

            Object.Destroy(ring);
        }

        static void SpawnDustBurst(Vector3 pos, Color color, float radius)
        {
            var go = new GameObject("SpecialDustBurst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = 0.55f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(color.r, color.g, color.b, 0.75f),
                new Color(color.r * 0.6f, color.g * 0.55f, color.b * 0.4f, 0.15f));
            main.gravityModifier = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 48;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(0.25f, radius * 0.18f);
            shape.radiusThickness = 0.2f;

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color * 0.7f, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.25f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLife.color = grad;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.6f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Object.Destroy(go, 1.2f);
        }

        static void SpawnSparkBurst(Vector3 pos, Color color, int count)
        {
            var go = new GameObject("SpecialSparkBurst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = color;
            main.gravityModifier = 1.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = count + 8;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.4f;
            renderer.velocityScale = 0.1f;

            ps.Play();
            Object.Destroy(go, 1f);
        }

        static void SpawnSlashArc(Vector3 pos, Vector3 forward, Color color, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SpecialSlashArc";
            Object.Destroy(go.GetComponent<Collider>());
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up)
                                    * Quaternion.Euler(0f, 0f, 35f);
            go.transform.localScale = new Vector3(scale * 1.4f, scale * 0.55f, 1f);

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(GetRingMaterial(color));
                SetMatColor(mat, new Color(color.r, color.g, color.b, 0.75f));
                rend.material = mat;
            }

            Object.Destroy(go, 0.18f);
        }

        static void SpawnImpactBurst(Vector3 pos, Color color, float scale)
        {
            SpawnDustBurst(pos, color, scale);
            SpawnSparkBurst(pos, color, 16);
            SpawnFlashLight(pos, color, 2.5f, 0.15f);
        }

        static void SpawnFlashLight(Vector3 pos, Color color, float intensity, float duration)
        {
            var go = new GameObject("SpecialFlashLight");
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = 6f;
            light.shadows = LightShadows.None;
            Object.Destroy(go, duration + 0.05f);
        }

        static void TrySpawnCfxr(string assetPath, Vector3 pos, float scale)
        {
            var prefab = LoadEditorPrefab(assetPath);
            if (prefab == null)
                return;

            var fx = Object.Instantiate(prefab, pos, Quaternion.identity);
            fx.transform.localScale = Vector3.one * scale;
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                if (!ps.isPlaying)
                    ps.Play(true);
            }
            Object.Destroy(fx, 3.5f);
        }

        static GameObject LoadEditorPrefab(string assetPath)
        {
            return ArcanoDungeon.Infrastructure.Assets.DungeonRuntimeAssets.Load<GameObject>(assetPath);
        }

        static Material GetRingMaterial(Color color)
        {
            if (s_ringMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Sprites/Default");
                s_ringMat = new Material(shader) { name = "SpecialRingMat" };
                ConfigureTransparent(s_ringMat);
                SetMatColor(s_ringMat, color);
            }

            var instance = new Material(s_ringMat);
            ConfigureTransparent(instance);
            SetMatColor(instance, color);
            return instance;
        }

        static void ConfigureTransparent(Material mat)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        static Material GetParticleMaterial()
        {
            if (s_particleMat != null)
                return s_particleMat;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            s_particleMat = new Material(shader) { name = "SpecialParticleMat" };
            SetMatColor(s_particleMat, Color.white);
            return s_particleMat;
        }

        static void SetMatColor(Material mat, Color color)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", color);
        }
    }
}

using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player.Motor;
using ArcanoDungeon.Infrastructure.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcanoDungeon.Features.Assassin
{
    [DefaultExecutionOrder(80)]
    public sealed class AssassinAbilities : MonoBehaviour
    {
        [SerializeField] PoisonKnifeProjectile knifePrefab;
        [SerializeField] Transform throwingHand;
        [SerializeField] ParticleSystem shadowPrefab;
        [SerializeField] float shadowScale = 1f;
        [SerializeField] float throwCooldown = .55f;
        [SerializeField] int knifeDamage = 20;
        [SerializeField] int poisonDamage = 6;
        [SerializeField] int poisonTicks = 4;
        [SerializeField] float poisonInterval = 1f;
        PlayerController player;
        PlayerInputReader input;
        PlayerMotor motor;
        MeleeWeaponController melee;
        Animator animator;
        ParticleSystem shadow;
        SkinnedMeshRenderer[] skins;
        MaterialPropertyBlock[][] originalBlocks;
        bool shadowVisible;
        float nextThrow, throwingUntil;
        readonly RaycastHit[] aimHits = new RaycastHit[64];

        public bool IsHidden => isActiveAndEnabled && player != null && player.IsCrouching;
        public bool IsThrowing => Time.time < throwingUntil;
        public bool ShadowVisible => shadowVisible;
        public float ThrowCooldownRemaining => Mathf.Max(0f, nextThrow - Time.time);
        public event System.Action<PoisonKnifeProjectile> KnifeThrown;

        public static bool IsConcealed(Transform candidate)
        {
            var ability = candidate != null ? candidate.GetComponentInParent<AssassinAbilities>() : null;
            return ability != null && ability.IsHidden;
        }

        void Awake()
        {
            player = GetComponent<PlayerController>(); input = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>(); melee = GetComponent<MeleeWeaponController>();
            animator = GetComponentInChildren<Animator>();
            skins = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (shadowPrefab != null)
            {
                shadow = Instantiate(shadowPrefab, transform);
                shadow.transform.localScale *= shadowScale;
                shadow.transform.localPosition *= shadowScale;
                shadow.name = "Cobertura de sombra"; shadow.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (shadowVisible != IsHidden) SetShadow(IsHidden);
            if (PauseMenuController.IsOpen || !CameraFollow.IsCursorLocked) return;
            bool pressed = input != null ? input.SecondaryPressed : Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            if (pressed) TryThrowAt(ResolveAimPoint());
        }

        Vector3 ResolveAimPoint()
        {
            var camera = Camera.main;
            if (camera == null) return transform.position + Vector3.up + transform.forward * 30f;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            var point = ray.GetPoint(35f); float nearest = 35f;
            int count = Physics.RaycastNonAlloc(ray, aimHits, nearest, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = aimHits[i];
                if (hit.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance; point = hit.point;
            }
            return point;
        }

        public bool TryThrowAt(Vector3 target)
        {
            if (!isActiveAndEnabled || knifePrefab == null || Time.time < nextThrow || PauseMenuController.IsOpen) return false;
            if ((melee != null && melee.IsAttacking) || (motor != null && motor.IsDashing)) return false;
            var health = GetComponent<PlayerHealth>();
            if (health != null && health.currentHealth <= 0) return false;
            var flat = target - transform.position; flat.y = 0;
            if (flat.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(flat);
            var origin = (throwingHand != null ? throwingHand.position : transform.position + Vector3.up) + transform.forward * .12f;
            var direction = target - origin;
            if (direction.sqrMagnitude < .001f) direction = transform.forward;
            var knife = Instantiate(knifePrefab, origin, Quaternion.LookRotation(direction));
            var stats = GetComponent<PlayerStats>();
            knife.Launch(transform, direction.normalized, knifeDamage + (stats != null ? stats.BonusDamage : 0), poisonDamage, poisonTicks, poisonInterval);
            nextThrow = Time.time + throwCooldown; throwingUntil = Time.time + .48f;
            motor?.NotifyAimingOrAttacking(.48f);
            if (animator != null)
            {
                var state = IsHidden ? "ThrowKnifeHidden" : "ThrowKnife";
                if (animator.HasState(0, Animator.StringToHash(state))) animator.CrossFadeInFixedTime(state, .06f, 0);
            }
            KnifeThrown?.Invoke(knife);
            return true;
        }

        void SetShadow(bool visible)
        {
            shadowVisible = visible;
            if (visible)
            {
                originalBlocks = new MaterialPropertyBlock[skins.Length][];
                for (int r = 0; r < skins.Length; r++)
                {
                    var renderer = skins[r]; if (renderer == null) continue;
                    var materials = renderer.sharedMaterials;
                    originalBlocks[r] = new MaterialPropertyBlock[materials.Length];
                    for (int m = 0; m < materials.Length; m++)
                    {
                        var saved = new MaterialPropertyBlock(); renderer.GetPropertyBlock(saved, m); originalBlocks[r][m] = saved;
                        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block, m);
                        var baseColor = materials[m].HasProperty("_BaseColor") ? materials[m].GetColor("_BaseColor") : Color.white;
                        block.SetColor("_BaseColor", Color.Lerp(baseColor * .10f, new Color(.035f, .012f, .07f), .7f));
                        block.SetColor("_Color", new Color(.035f, .012f, .07f));
                        renderer.SetPropertyBlock(block, m);
                    }
                }
                if (shadow != null) { shadow.gameObject.SetActive(true); shadow.Play(true); }
            }
            else
            {
                if (originalBlocks != null)
                    for (int r = 0; r < skins.Length; r++)
                        if (skins[r] != null && originalBlocks[r] != null)
                            for (int m = 0; m < originalBlocks[r].Length; m++) skins[r].SetPropertyBlock(originalBlocks[r][m], m);
                originalBlocks = null;
                if (shadow != null) { shadow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); shadow.gameObject.SetActive(false); }
            }
        }

        void OnDisable() { if (shadowVisible) SetShadow(false); }
    }
}

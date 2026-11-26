using System;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ArcanoDungeon.Features.Levels
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EclipseSealCrystal : MonoBehaviour
    {
        [SerializeField, Min(1)] int health = 45;
        [SerializeField] RoomController room;
        [SerializeField] Mesh shardMesh;
        [SerializeField] Material shardMaterial;

        int remainingHealth;
        BoxCollider hitbox;
        RectTransform healthBar;
        Image healthFill;

        public bool IsBroken { get; private set; }
        public event Action Broken;

        bool CanTakeDamage => room != null && room.IsCleared && !IsBroken;

        void Awake()
        {
            remainingHealth = health;
            hitbox = GetComponent<BoxCollider>();
            CreateHealthBar();
            UpdateVulnerability();
        }

        public void Unlock(RoomController clearedRoom)
        {
            if (clearedRoom == room && !IsBroken)
                UpdateVulnerability();
        }

        void UpdateVulnerability()
        {
            hitbox.enabled = CanTakeDamage;
            healthFill.color = CanTakeDamage ? new Color(0.77f, 0.36f, 1f) : new Color(0.48f, 0.43f, 0.58f);
        }

        public bool TakeDamage(int damage)
        {
            if (damage <= 0 || !CanTakeDamage)
                return false;

            remainingHealth = Mathf.Max(0, remainingHealth - damage);
            healthFill.rectTransform.localScale = new Vector3((float)remainingHealth / health, 1f, 1f);
            if (remainingHealth > 0)
                return true;

            IsBroken = true;
            PlayShatter();
            var catalog = DungeonSfxCatalog.Instance;
            if (catalog != null && catalog.groundImpact != null)
                GameSfx.PlayCombat(catalog.groundImpact);
            gameObject.SetActive(false);
            Broken?.Invoke();
            return true;
        }

        void CreateHealthBar()
        {
            var root = new GameObject("Vida del cristal", typeof(RectTransform), typeof(Canvas));
            healthBar = (RectTransform)root.transform;
            healthBar.SetParent(transform, false);
            var worldScale = transform.lossyScale;
            healthBar.localScale = new Vector3(0.01f / worldScale.x, 0.01f / worldScale.y, 0.01f / worldScale.z);
            healthBar.sizeDelta = new Vector2(124f, 13f);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            AddBarImage("Borde", Color.white, 0f);
            AddBarImage("Fondo", new Color(0.075f, 0.035f, 0.13f), 1f);
            healthFill = AddBarImage("Vida", Color.white, 2f);
            healthFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        Image AddBarImage(string label, Color color, float inset)
        {
            var image = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var rect = image.rectTransform;
            rect.SetParent(healthBar, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
                healthBar.SetPositionAndRotation(transform.position + Vector3.up * 1.16f, camera.transform.rotation);
        }

        void PlayShatter()
        {
            if (shardMesh == null || shardMaterial == null)
                return;

            var burst = new GameObject("Fragmentos del sello");
            burst.transform.SetParent(transform.parent, false);
            burst.transform.position = transform.position;
            var particles = burst.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.19f);
            main.gravityModifier = 0.75f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.24f;
            var spin = particles.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-9f, 9f);
            var shrink = particles.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = burst.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = shardMesh;
            renderer.sharedMaterial = shardMaterial;
            particles.Play();
        }
    }
}

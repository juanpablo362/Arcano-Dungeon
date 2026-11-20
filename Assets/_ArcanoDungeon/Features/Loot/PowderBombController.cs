using ArcanoDungeon.Infrastructure.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Bombas de pólvora: cargas + tecla Q. Explota a los 2s.
    /// </summary>
    public class PowderBombController : MonoBehaviour
    {
        [SerializeField] int maxCharges = 3;
        [SerializeField] int damage = 70;
        [SerializeField] float radius = 3.4f;
        [SerializeField] float fuseSeconds = 2f;
        [SerializeField] float playerSelfDamageScale = 0.45f;

        PlayerInputReader input;
        int charges;

        public int Charges => charges;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();
        }

        void Update()
        {
            bool pressed = (input != null && input.BombPressed)
                           || (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame);
            if (!pressed || PauseMenuController.IsOpen)
                return;
            if (charges <= 0)
            {
                InteractionPromptUI.RequestPrompt("Sin bombas", 0f);
                return;
            }

            charges--;
            SpawnBomb(transform.position + transform.forward * 0.6f);
            InteractionPromptUI.RequestPrompt($"Bomba ({charges})", 0f);
        }

        public void AddCharges(int amount)
        {
            if (amount <= 0)
                return;
            charges = Mathf.Min(maxCharges, charges + amount);
        }

        void SpawnBomb(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PowderBomb";
            go.transform.position = pos + Vector3.up * 0.35f;
            go.transform.localScale = Vector3.one * 0.35f;
            Object.Destroy(go.GetComponent<Collider>());

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    var c = new Color(0.15f, 0.12f, 0.1f, 1f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                    rend.sharedMaterial = mat;
                }
            }

            var fuse = go.AddComponent<PowderBombFuse>();
            fuse.Configure(damage, radius, fuseSeconds, transform, playerSelfDamageScale);
        }
    }

    public class PowderBombFuse : MonoBehaviour
    {
        int damage;
        float radius;
        float fuse;
        Transform owner;
        float selfScale;
        float spawnTime;

        public void Configure(int dmg, float rad, float fuseSec, Transform bombOwner, float playerScale)
        {
            damage = dmg;
            radius = rad;
            fuse = fuseSec;
            owner = bombOwner;
            selfScale = playerScale;
            spawnTime = Time.time;
        }

        void Update()
        {
            if (Time.time - spawnTime < fuse)
                return;
            Explode();
        }

        void Explode()
        {
            Vector3 center = transform.position;
            LootVfxLibrary.SpawnFirst(center, Quaternion.identity, null, 1.2f, 2.5f,
                LootVfxLibrary.Explosion1, LootVfxLibrary.GroundAoe, LootVfxLibrary.FireMedium);

            var cols = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null)
                    continue;

                var enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(damage);
                    var ai = enemy.GetComponent<EnemyAI>();
                    Vector3 push = (enemy.transform.position - center);
                    push.y = 0f;
                    ai?.ApplyKnockback(push.normalized, 1f);
                    continue;
                }

                var ph = col.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    int selfDmg = Mathf.RoundToInt(damage * selfScale);
                    if (selfDmg > 0)
                        ph.TakeDamage(selfDmg);
                }
            }

            Destroy(gameObject);
        }
    }
}

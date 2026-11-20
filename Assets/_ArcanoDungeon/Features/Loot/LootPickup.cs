using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Pickup de cofre: cura o buff permanente. Se recoge con F.
    /// </summary>
    public class LootPickup : MonoBehaviour
    {
        [SerializeField] LootItemKind kind = LootItemKind.Heal;
        [SerializeField] float rotationSpeed = 95f;
        [SerializeField] float bobAmplitude = 0.12f;
        [SerializeField] float bobSpeed = 2.4f;
        [SerializeField] float interactRange = 2.5f;

        Transform cachedPlayer;
        bool isLaunching;
        Vector3 launchStartPos;
        Vector3 launchEndPos;
        float launchArcHeight = 0.7f;
        float launchDuration = 0.45f;
        float launchElapsed;
        Vector3 restPos;
        float bobPhase;
        bool collected;

        public LootItemKind Kind => kind;

        public void Configure(LootItemKind lootKind)
        {
            kind = lootKind;
            UpdatePrompt();
        }

        public void Launch(Vector3 startPos, Vector3 endPos, float arcHeight = 0.7f, float duration = 0.45f)
        {
            launchStartPos = startPos;
            launchEndPos = endPos;
            launchArcHeight = arcHeight;
            launchDuration = Mathf.Max(0.05f, duration);
            launchElapsed = 0f;
            isLaunching = true;
            transform.position = startPos;
            restPos = endPos;
        }

        void Awake()
        {
            UpdatePrompt();
            bobPhase = Random.Range(0f, Mathf.PI * 2f);
            restPos = transform.position;
        }

        void Update()
        {
            if (collected)
                return;

            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

            if (isLaunching)
            {
                launchElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(launchElapsed / launchDuration);
                Vector3 pos = Vector3.Lerp(launchStartPos, launchEndPos, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * launchArcHeight;
                transform.position = pos;

                if (t >= 1f)
                {
                    isLaunching = false;
                    restPos = launchEndPos;
                    transform.position = restPos;
                }
            }
            else
            {
                float y = restPos.y + Mathf.Sin((Time.time + bobPhase) * bobSpeed) * bobAmplitude;
                transform.position = new Vector3(restPos.x, y, restPos.z);
            }

            if (cachedPlayer == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    cachedPlayer = playerObj.transform;
            }

            if (cachedPlayer == null)
                return;

            float distance = Vector3.Distance(transform.position, cachedPlayer.position);
            if (distance < interactRange)
                InteractionPromptUI.RequestPrompt(GetPrompt(), distance);
        }

        public void Collect(GameObject playerObj)
        {
            if (collected || playerObj == null)
                return;

            collected = true;

            var buffs = playerObj.GetComponent<PlayerLootBuffs>()
                        ?? playerObj.GetComponentInParent<PlayerLootBuffs>();
            if (buffs == null)
                buffs = playerObj.AddComponent<PlayerLootBuffs>();

            string msg = buffs.Apply(kind);
            InteractionPromptUI.RequestPrompt(msg, 0f);
            Destroy(gameObject);
        }

        void UpdatePrompt()
        {
            // prompt se consulta en GetPrompt
        }

        string GetPrompt()
        {
            return kind switch
            {
                LootItemKind.Heal => "Presiona [F] · Recuperar vida",
                LootItemKind.MaxHealth => "Presiona [F] · Vida máxima",
                LootItemKind.Speed => "Presiona [F] · Velocidad",
                LootItemKind.Strength => "Presiona [F] · Fuerza",
                LootItemKind.Reach => "Presiona [F] · Alcance de arma",
                LootItemKind.Stamina => "Presiona [F] · Estamina",
                _ => "Presiona [F] para recoger",
            };
        }
    }
}

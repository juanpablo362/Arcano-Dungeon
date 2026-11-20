using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Pickup de cofre basado en ItemData. Se recoge con F.
    /// Lanza en arco y luego usa física para no atravesar el suelo.
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] ItemData item;
        [SerializeField] float rotationSpeed = 110f;
        [SerializeField] float interactRange = 2.5f;

        Transform cachedPlayer;
        Rigidbody body;
        bool isLaunching;
        Vector3 launchStartPos;
        Vector3 launchEndPos;
        float launchArcHeight = 0.7f;
        float launchDuration = 0.45f;
        float launchElapsed;
        bool collected;
        bool settled;

        public ItemData Item => item;

        public void Configure(ItemData data)
        {
            item = data;
        }

        public void Launch(Vector3 startPos, Vector3 endPos, float arcHeight = 0.7f, float duration = 0.45f)
        {
            EnsureBody();
            launchStartPos = startPos;
            launchEndPos = endPos;
            launchArcHeight = arcHeight;
            launchDuration = Mathf.Max(0.05f, duration);
            launchElapsed = 0f;
            isLaunching = true;
            settled = false;
            transform.position = startPos;

            if (body != null)
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        void Awake()
        {
            EnsureBody();
        }

        void EnsureBody()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                LootVisualFactory.EnsurePickupPhysics(gameObject);
            body = GetComponent<Rigidbody>();
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
                    FinishLaunch();
            }

            if (cachedPlayer == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    cachedPlayer = playerObj.transform;
                    IgnorePlayerCollision(playerObj);
                }
            }

            if (cachedPlayer == null)
                return;

            float distance = Vector3.Distance(transform.position, cachedPlayer.position);
            if (distance < interactRange)
                InteractionPromptUI.RequestPrompt(GetPrompt(), distance);
        }

        void FinishLaunch()
        {
            isLaunching = false;
            EnsureBody();
            if (body == null)
                return;

            // Física real: cae / apoya en el suelo sin atravesarlo.
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.down * 0.5f;
            body.WakeUp();
        }

        void FixedUpdate()
        {
            if (collected || isLaunching || body == null || body.isKinematic)
                return;

            // Cuando casi quieto, congelar para que no empuje al jugador.
            if (!settled && body.linearVelocity.sqrMagnitude < 0.02f)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.4f, Vector3.down, out var hit, 1.2f,
                        ~0, QueryTriggerInteraction.Ignore))
                {
                    settled = true;
                    body.isKinematic = true;
                    transform.position = hit.point + Vector3.up * 0.12f;
                }
            }
        }

        void IgnorePlayerCollision(GameObject playerObj)
        {
            var myCol = GetComponent<Collider>();
            if (myCol == null || playerObj == null)
                return;

            foreach (var col in playerObj.GetComponentsInChildren<Collider>(true))
            {
                if (col != null)
                    Physics.IgnoreCollision(myCol, col, true);
            }

            var cc = playerObj.GetComponent<CharacterController>();
            // CharacterController no es Collider clásico; el Ignore evita empujones con BoxCollider del player.
        }

        public void Collect(GameObject playerObj)
        {
            if (collected || playerObj == null || item == null)
                return;

            collected = true;

            var inventory = playerObj.GetComponent<PlayerItemInventory>()
                            ?? playerObj.GetComponentInParent<PlayerItemInventory>();
            if (inventory == null)
                inventory = playerObj.AddComponent<PlayerItemInventory>();

            string msg = inventory.Collect(item);
            if (!string.IsNullOrEmpty(msg))
                InteractionPromptUI.RequestPrompt(msg, 0f);

            Destroy(gameObject);
        }

        string GetPrompt()
        {
            if (item == null)
                return "Presiona [F] para recoger";

            string name = string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;
            return item.type switch
            {
                ItemType.Passive => $"Presiona [F] · {name} (pasivo)",
                ItemType.Consumable => $"Presiona [F] · {name}",
                ItemType.Weapon => $"Presiona [F] · {name} (arma)",
                _ => $"Presiona [F] · {name}",
            };
        }
    }
}

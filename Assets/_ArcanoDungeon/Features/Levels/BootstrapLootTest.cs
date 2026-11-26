using ArcanoDungeon.Features.Loot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcanoDungeon.Features.Levels
{
    public sealed class BootstrapLootTest : MonoBehaviour
    {
        PlayerHealth playerHealth;

        void Awake()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var pickup in root.GetComponentsInChildren<ItemPickup>(true))
                    pickup.Reusable = true;
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.f6Key.wasPressedThisFrame)
                return;

            if (playerHealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerHealth = player.GetComponent<PlayerHealth>();
            }

            if (playerHealth != null && playerHealth.currentHealth > 1)
                playerHealth.TakeDamage(Mathf.Min(25, playerHealth.currentHealth - 1));
        }
    }
}

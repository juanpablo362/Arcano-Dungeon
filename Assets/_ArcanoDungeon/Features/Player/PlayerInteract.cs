using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Infrastructure.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcanoDungeon.Features.Player
{
    /// <summary>
    /// Interacción con F: abrir cofres y recoger pickups cercanos.
    /// </summary>
    public class PlayerInteract : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] float interactRange = 2.6f;

        void Awake()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();
        }

        void OnEnable()
        {
            if (input == null)
                input = GetComponent<PlayerInputReader>() ?? GetComponentInParent<PlayerInputReader>();
            if (input != null)
                input.OnInteract += TryInteract;
        }

        void OnDisable()
        {
            if (input != null)
                input.OnInteract -= TryInteract;
        }

        void Update()
        {
            bool pressed = (input != null && input.InteractPressed)
                           || (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame);
            if (!pressed)
                return;
            if (PauseMenuController.IsOpen)
                return;

            TryInteract();
        }

        void TryInteract()
        {
            if (TryCollectNearestItem())
                return;
            if (TryCollectNearestLootLegacy())
                return;
            if (TryCollectNearestHeart())
                return;
            TryOpenNearestChest();
        }

        bool TryCollectNearestItem()
        {
            ItemPickup best = null;
            float bestDist = float.MaxValue;
            foreach (var pickup in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude))
            {
                if (pickup == null)
                    continue;
                float d = Vector3.Distance(transform.position, pickup.transform.position);
                if (d <= interactRange && d < bestDist)
                {
                    bestDist = d;
                    best = pickup;
                }
            }

            if (best == null)
                return false;

            best.Collect(gameObject);
            return true;
        }

        bool TryCollectNearestLootLegacy()
        {
            LootPickup best = null;
            float bestDist = float.MaxValue;
            foreach (var pickup in Object.FindObjectsByType<LootPickup>(FindObjectsInactive.Exclude))
            {
                if (pickup == null)
                    continue;
                float d = Vector3.Distance(transform.position, pickup.transform.position);
                if (d <= interactRange && d < bestDist)
                {
                    bestDist = d;
                    best = pickup;
                }
            }

            if (best == null)
                return false;

            best.Collect(gameObject);
            return true;
        }

        bool TryCollectNearestHeart()
        {
            HeartPickup best = null;
            float bestDist = float.MaxValue;
            foreach (var pickup in Object.FindObjectsByType<HeartPickup>(FindObjectsInactive.Exclude))
            {
                if (pickup == null)
                    continue;
                float d = Vector3.Distance(transform.position, pickup.transform.position);
                if (d <= interactRange && d < bestDist)
                {
                    bestDist = d;
                    best = pickup;
                }
            }

            if (best == null)
                return false;

            best.Collect(gameObject);
            return true;
        }

        void TryOpenNearestChest()
        {
            TreasureChest bestChest = null;
            float bestChestDist = float.MaxValue;
            foreach (var chest in Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Exclude))
            {
                if (chest == null || chest.IsOpened)
                    continue;
                float d = Vector3.Distance(transform.position, chest.transform.position);
                if (d <= interactRange && d < bestChestDist)
                {
                    bestChestDist = d;
                    bestChest = chest;
                }
            }

            bestChest?.OpenChest();
        }
    }
}

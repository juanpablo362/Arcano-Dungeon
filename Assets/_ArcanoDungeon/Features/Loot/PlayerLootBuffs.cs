using System.Collections.Generic;
using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Features.Player;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Buffs permanentes de run (cofre). Acumula bonos y los aplica a salud/motor/arma.
    /// </summary>
    public class PlayerLootBuffs : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] CharacterBase character;
        [SerializeField] PlayerController playerController;
        [SerializeField] MeleeWeaponController melee;
        [SerializeField] PlayerStamina stamina;

        public int BonusMaxHealth { get; private set; }
        public float BonusSpeed { get; private set; }
        public int BonusDamage { get; private set; }
        public float BonusReach { get; private set; }
        public float BonusMaxStamina { get; private set; }
        readonly List<LootItemKind> runUpgrades = new List<LootItemKind>();
        public IReadOnlyList<LootItemKind> RunUpgrades => runUpgrades;

        void Awake()
        {
            ResolveRefs();
        }

        void ResolveRefs()
        {
            if (health == null)
                health = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
            if (character == null)
                character = GetComponent<CharacterBase>() ?? GetComponentInParent<CharacterBase>();
            if (playerController == null)
                playerController = GetComponent<PlayerController>() ?? GetComponentInParent<PlayerController>();
            if (melee == null)
                melee = GetComponent<MeleeWeaponController>() ?? GetComponentInParent<MeleeWeaponController>();
            if (stamina == null)
                stamina = GetComponent<PlayerStamina>() ?? GetComponentInParent<PlayerStamina>();
        }

        public string Apply(LootItemKind kind)
        {
            ResolveRefs();
            if (kind != LootItemKind.Heal && System.Enum.IsDefined(typeof(LootItemKind), kind))
                runUpgrades.Add(kind);

            switch (kind)
            {
                case LootItemKind.Heal:
                {
                    int amount = 30;
                    if (health != null)
                        health.Heal(amount);
                    return $"+{amount} Vida";
                }
                case LootItemKind.MaxHealth:
                {
                    int amount = 20;
                    BonusMaxHealth += amount;
                    if (health != null)
                        health.IncreaseMaxHealth(amount);
                    return $"+{amount} Vida máxima";
                }
                case LootItemKind.Speed:
                {
                    float amount = 0.45f;
                    BonusSpeed += amount;
                    var stats = GetComponent<PlayerStats>();
                    if (stats != null)
                        stats.AddBaseMoveSpeed(amount);
                    else if (character != null)
                        character.moveSpeed += amount;
                    return $"+{amount:0.##} Velocidad";
                }
                case LootItemKind.Strength:
                {
                    int amount = 8;
                    BonusDamage += amount;
                    if (melee != null)
                        melee.AddDamageBonus(amount);
                    if (playerController != null)
                    {
                        playerController.fireballDamage += amount;
#pragma warning disable CS0618
                        playerController.attackDamage += amount;
#pragma warning restore CS0618
                    }
                    return $"+{amount} Fuerza";
                }
                case LootItemKind.Reach:
                {
                    float amount = 0.22f;
                    BonusReach += amount;
                    if (melee != null)
                        melee.AddReachBonus(amount);
                    return $"+{amount:0.##} Alcance";
                }
                case LootItemKind.Stamina:
                {
                    float amount = 20f;
                    BonusMaxStamina += amount;
                    if (stamina != null)
                        stamina.IncreaseMaxStamina(amount);
                    return $"+{amount:0} Estamina";
                }
                default:
                    return "Loot";
            }
        }
    }
}

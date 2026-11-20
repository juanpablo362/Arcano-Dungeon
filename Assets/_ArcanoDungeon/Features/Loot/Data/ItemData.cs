using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    [CreateAssetMenu(menuName = "Arcano/Loot/Item Data", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        [Header("Identidad")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ItemType type = ItemType.Passive;
        public Sprite icon;
        public GameObject worldPrefab;
        [Tooltip("Peso relativo en tablas de drop.")]
        public int dropWeight = 10;

        [Header("Weapon")]
        [Tooltip("Duración del arma temporal. Ideal ~8–12s.")]
        public float temporarySeconds = 10f;
        public WeaponProfileKind weaponProfile = WeaponProfileKind.None;
        public int weaponDamageBonus = 10;
        public float weaponReachBonus;
        public float weaponFireCooldown = 0.18f;
        public int weaponProjectileCount = 3;
        public float weaponSlowPercent = 0.5f;
        public float weaponSlowSeconds = 3f;
        public float weaponQuakeRadius = 3.2f;
        public int weaponQuakeDamage = 35;
        public int weaponSummonCount = 2;
        public float weaponSummonSeconds = 10f;

        [Header("Modifiers de stats")]
        public float moveSpeedMult = 1f;
        public float dashCooldownReduce;
        public int maxShieldBonus;
        public float shieldRegenDelayReduce;
        public int damageBonus;
        public float reachBonus;
        public float meleeArcAngleMult = 1f;
        public int maxHealthBonus;
        public float maxStaminaBonus;

        [Header("Consumable")]
        public int healAmount;
        public bool refillShield;
        public float speedBurstMult = 1f;
        public float speedBurstSeconds;
        public int scoreAmount;
        public int bombCharges;

        [Header("Passive behaviours")]
        public bool hasOrbital;
        public bool hasChainLightning;
        public bool hasHolyAura;
        public bool hasVampiric;
        public bool hasMultishot;
        public bool hasCrystalHeart;

        [Header("Tuning comportamientos")]
        public float orbitalRadius = 1.6f;
        public float orbitalSpeed = 220f;
        public int orbitalContactDamage = 12;
        public float orbitalTickSeconds = 0.35f;
        public int chainEveryHits = 4;
        public int chainMaxJumps = 2;
        public float chainRadius = 4.5f;
        public int chainDamage = 22;
        public float holyAuraRadius = 3f;
        public float holyAuraTickSeconds = 0.5f;
        public int holyAuraDamage = 8;
        public float vampiricChance = 0.15f;
        public int vampiricHeal = 5;
        public int vampiricShield = 10;
        public float multishotArcAngle = 1.25f;
        public float multishotSpread = 16f;
    }
}

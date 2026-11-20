using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Features.Player;
using ArcanoDungeon.Features.Player.Motor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Stats de run: escudo, multiplicadores y bonos aplicados por ítems.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Escudo")]
        [SerializeField] float baseMaxShield = 30f;
        [SerializeField] float shieldRegenPerSecond = 8f;
        [SerializeField] float shieldRegenDelay = 3f;

        [Header("UI")]
        [SerializeField] Slider shieldSlider;
        [SerializeField] TextMeshProUGUI shieldText;

        PlayerHealth health;
        CharacterBase character;
        PlayerController playerController;
        MeleeWeaponController melee;
        PlayerStamina stamina;
        PlayerMotor motor;

        float currentShield;
        float maxShield;
        float nextShieldRegenTime;
        float baseMoveSpeed;
        float moveSpeedMult = 1f;
        float temporarySpeedMult = 1f;
        float temporarySpeedUntil;
        float dashCooldownReduce;
        float meleeArcAngleMult = 1f;

        public float CurrentShield => currentShield;
        public float MaxShield => maxShield;
        public float MoveSpeedMultiplier => moveSpeedMult * GetTemporarySpeedMult();
        public float DashCooldownReduce => dashCooldownReduce;
        public float MeleeArcAngleMult => meleeArcAngleMult;
        public int BonusDamage { get; private set; }
        public bool HasMultishot { get; private set; }
        public float MultishotSpread { get; private set; } = 16f;
        /// <summary>Nivel de multidisparo (1 = 3 proyectiles, cada stack suma +2).</summary>
        public int MultishotLevel { get; private set; }

        void Awake()
        {
            ResolveRefs();
            maxShield = baseMaxShield;
            currentShield = maxShield;
            if (character != null)
                baseMoveSpeed = character.moveSpeed;
        }

        void Start()
        {
            EnsureShieldUi();
            RefreshShieldUi();
            ApplyMoveSpeed();
        }

        void Update()
        {
            if (Time.time >= temporarySpeedUntil && temporarySpeedMult > 1.001f)
            {
                temporarySpeedMult = 1f;
                ApplyMoveSpeed();
            }

            if (currentShield >= maxShield || Time.time < nextShieldRegenTime || maxShield <= 0f)
                return;

            currentShield = Mathf.Min(maxShield, currentShield + shieldRegenPerSecond * Time.deltaTime);
            RefreshShieldUi();
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
            if (motor == null)
                motor = GetComponent<PlayerMotor>() ?? GetComponentInParent<PlayerMotor>();
        }

        public void ApplyItemModifiers(ItemData item)
        {
            if (item == null)
                return;

            ResolveRefs();

            if (item.maxHealthBonus > 0 && health != null)
                health.IncreaseMaxHealth(item.maxHealthBonus);

            if (item.maxStaminaBonus > 0f && stamina != null)
                stamina.IncreaseMaxStamina(item.maxStaminaBonus);

            if (item.damageBonus != 0)
            {
                BonusDamage += item.damageBonus;
                if (melee != null)
                    melee.AddDamageBonus(item.damageBonus);
                if (playerController != null)
                {
                    playerController.fireballDamage += item.damageBonus;
#pragma warning disable CS0618
                    playerController.attackDamage += item.damageBonus;
#pragma warning restore CS0618
                }
            }

            if (Mathf.Abs(item.reachBonus) > 0.001f && melee != null)
                melee.AddReachBonus(item.reachBonus);

            if (item.maxShieldBonus > 0)
            {
                maxShield += item.maxShieldBonus;
                currentShield = Mathf.Min(maxShield, currentShield + item.maxShieldBonus);
                if (shieldSlider != null)
                    shieldSlider.maxValue = maxShield;
                RefreshShieldUi();
            }

            if (item.moveSpeedMult > 0f && Mathf.Abs(item.moveSpeedMult - 1f) > 0.001f)
            {
                moveSpeedMult *= item.moveSpeedMult;
                ApplyMoveSpeed();
            }

            if (item.dashCooldownReduce > 0f && motor != null)
            {
                dashCooldownReduce += item.dashCooldownReduce;
                motor.AddDashCooldownReduce(item.dashCooldownReduce);
            }

            if (item.meleeArcAngleMult > 0f && Mathf.Abs(item.meleeArcAngleMult - 1f) > 0.001f)
            {
                meleeArcAngleMult *= item.meleeArcAngleMult;
                if (melee != null)
                    melee.SetArcAngleMultiplier(meleeArcAngleMult);
            }

            if (item.hasMultishot)
            {
                HasMultishot = true;
                MultishotLevel = Mathf.Max(1, MultishotLevel + 1);
                MultishotSpread = item.multishotSpread > 0f ? item.multishotSpread : MultishotSpread;
                float arc = item.multishotArcAngle > 0f ? item.multishotArcAngle : 1.25f;
                // Primer stack aplica el arco; stacks extra lo refuerzan un poco.
                float arcFactor = MultishotLevel <= 1 ? arc : Mathf.Lerp(1f, arc, 0.45f);
                meleeArcAngleMult *= arcFactor;
                if (melee != null)
                    melee.SetArcAngleMultiplier(meleeArcAngleMult);
            }

            if (item.hasCrystalHeart || item.shieldRegenDelayReduce > 0f)
            {
                float reduce = item.shieldRegenDelayReduce > 0f ? item.shieldRegenDelayReduce : 1f;
                shieldRegenDelay = Mathf.Max(0.5f, shieldRegenDelay - reduce);
            }
        }

        public void AddShield(int amount)
        {
            if (amount <= 0 || maxShield <= 0f)
                return;
            currentShield = Mathf.Min(maxShield, currentShield + amount);
            RefreshShieldUi();
        }

        public string ApplyConsumable(ItemData item)
        {
            if (item == null)
                return "";

            ResolveRefs();
            string msg = item.displayName;

            if (item.healAmount > 0 && health != null)
            {
                health.Heal(item.healAmount);
                msg = $"+{item.healAmount} Vida";
            }

            if (item.refillShield)
            {
                currentShield = maxShield;
                RefreshShieldUi();
                msg = "Escudo restaurado";
            }

            if (item.speedBurstSeconds > 0f && item.speedBurstMult > 1f)
            {
                temporarySpeedMult = item.speedBurstMult;
                temporarySpeedUntil = Time.time + item.speedBurstSeconds;
                ApplyMoveSpeed();
                msg += msg.Length > 0 ? " + velocidad" : "Velocidad";
            }

            if (item.scoreAmount > 0)
            {
                RunScore.Add(item.scoreAmount);
                msg = $"+{item.scoreAmount} monedas (total {RunScore.Current})";
            }

            if (item.bombCharges > 0)
            {
                var bombs = GetComponent<PowderBombController>()
                            ?? gameObject.AddComponent<PowderBombController>();
                bombs.AddCharges(item.bombCharges);
                msg = $"+{item.bombCharges} bomba(s) ({bombs.Charges})";
            }

            return msg;
        }

        /// <summary>Absorbe daño con escudo. Devuelve daño restante a la vida.</summary>
        public int AbsorbDamage(int amount)
        {
            if (amount <= 0)
                return 0;

            nextShieldRegenTime = Time.time + shieldRegenDelay;

            if (currentShield <= 0f || maxShield <= 0f)
                return amount;

            float absorbed = Mathf.Min(currentShield, amount);
            currentShield -= absorbed;
            RefreshShieldUi();
            return Mathf.Max(0, amount - Mathf.RoundToInt(absorbed));
        }

        public void NotifyDamaged()
        {
            nextShieldRegenTime = Time.time + shieldRegenDelay;
        }

        float GetTemporarySpeedMult()
        {
            return Time.time < temporarySpeedUntil ? temporarySpeedMult : 1f;
        }

        void ApplyMoveSpeed()
        {
            if (character == null)
                return;
            if (baseMoveSpeed <= 0.01f)
                baseMoveSpeed = character.moveSpeed;
            character.moveSpeed = baseMoveSpeed * MoveSpeedMultiplier;
        }

        void EnsureShieldUi()
        {
            if (shieldSlider != null)
            {
                shieldSlider.minValue = 0f;
                shieldSlider.maxValue = maxShield;
                return;
            }

            var healthBar = GameObject.Find("PlayerHealthBar");
            if (healthBar == null)
                return;

            var healthRt = healthBar.GetComponent<RectTransform>();
            var clone = Object.Instantiate(healthBar, healthBar.transform.parent);
            clone.name = "PlayerShieldBar";
            var rt = clone.GetComponent<RectTransform>();
            if (rt != null && healthRt != null)
            {
                rt.anchorMin = healthRt.anchorMin;
                rt.anchorMax = healthRt.anchorMax;
                rt.pivot = healthRt.pivot;
                rt.sizeDelta = healthRt.sizeDelta;
                // Encima de estamina: vida 0, escudo -18, estamina -36 approx.
                rt.anchoredPosition = healthRt.anchoredPosition + new Vector2(0f, -18f);
            }

            shieldSlider = clone.GetComponent<Slider>();
            if (shieldSlider != null)
            {
                shieldSlider.minValue = 0f;
                shieldSlider.maxValue = maxShield;
                if (shieldSlider.fillRect != null)
                {
                    var fillImg = shieldSlider.fillRect.GetComponent<Image>();
                    if (fillImg != null)
                        fillImg.color = new Color(0.45f, 0.85f, 1f, 1f);
                }
            }

            var healthText = GameObject.Find("PlayerHealthText");
            if (healthText != null && shieldText == null)
            {
                var textClone = Object.Instantiate(healthText, healthText.transform.parent);
                textClone.name = "PlayerShieldText";
                var trt = textClone.GetComponent<RectTransform>();
                var htrt = healthText.GetComponent<RectTransform>();
                if (trt != null && htrt != null)
                    trt.anchoredPosition = htrt.anchoredPosition + new Vector2(0f, -18f);
                shieldText = textClone.GetComponent<TextMeshProUGUI>();
                if (shieldText != null)
                    shieldText.color = new Color(0.55f, 0.9f, 1f, 1f);
            }
        }

        void RefreshShieldUi()
        {
            if (shieldSlider != null)
                shieldSlider.value = currentShield;
            if (shieldText != null)
                shieldText.text = $"{Mathf.RoundToInt(currentShield)} / {Mathf.RoundToInt(maxShield)}";
        }
    }
}

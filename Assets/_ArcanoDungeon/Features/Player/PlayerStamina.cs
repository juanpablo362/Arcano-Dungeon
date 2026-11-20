using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArcanoDungeon.Features.Player
{
    /// <summary>
    /// Estamina del jugador: se gasta en dash y ataque especial, se regenera al no usarse.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Estamina")]
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float regenPerSecond = 18f;
        [SerializeField] float regenDelay = 0.55f;
        [SerializeField] float dashCost = 22f;
        [SerializeField] float specialCost = 40f;

        [Header("UI")]
        [SerializeField] Slider staminaSlider;
        [SerializeField] TextMeshProUGUI staminaText;

        float current;
        float nextRegenTime;

        public float Current => current;
        public float Max => maxStamina;
        public float Normalized => maxStamina <= 0f ? 0f : Mathf.Clamp01(current / maxStamina);
        public float DashCost => dashCost;
        public float SpecialCost => specialCost;

        void Awake()
        {
            current = maxStamina;
        }

        void Start()
        {
            EnsureUi();
            RefreshUi();
        }

        void Update()
        {
            if (current >= maxStamina || Time.time < nextRegenTime)
                return;

            current = Mathf.Min(maxStamina, current + regenPerSecond * Time.deltaTime);
            RefreshUi();
        }

        public bool Has(float cost) => current >= cost;

        public bool TryConsume(float cost)
        {
            if (cost <= 0f)
                return true;
            if (current < cost)
                return false;

            current -= cost;
            nextRegenTime = Time.time + regenDelay;
            RefreshUi();
            return true;
        }

        public bool TryConsumeDash() => TryConsume(dashCost);

        public bool TryConsumeSpecial() => TryConsume(specialCost);

        public void IncreaseMaxStamina(float amount)
        {
            if (amount <= 0f)
                return;

            maxStamina += amount;
            current = Mathf.Min(maxStamina, current + amount);
            if (staminaSlider != null)
                staminaSlider.maxValue = maxStamina;
            RefreshUi();
        }

        void EnsureUi()
        {
            if (staminaSlider == null)
            {
                var existing = GameObject.Find("PlayerStaminaBar");
                if (existing != null)
                    staminaSlider = existing.GetComponent<Slider>();
            }

            if (staminaSlider != null)
            {
                staminaSlider.minValue = 0f;
                staminaSlider.maxValue = maxStamina;
                return;
            }

            var healthBar = GameObject.Find("PlayerHealthBar");
            var hud = GameObject.Find("HUDCanvas");
            if (healthBar == null || hud == null)
                return;

            var healthRt = healthBar.GetComponent<RectTransform>();
            var clone = Object.Instantiate(healthBar, healthBar.transform.parent);
            clone.name = "PlayerStaminaBar";
            var rt = clone.GetComponent<RectTransform>();
            if (rt != null && healthRt != null)
            {
                rt.anchorMin = healthRt.anchorMin;
                rt.anchorMax = healthRt.anchorMax;
                rt.pivot = healthRt.pivot;
                rt.sizeDelta = healthRt.sizeDelta;
                // Debajo de vida y escudo (-18).
                rt.anchoredPosition = healthRt.anchoredPosition + new Vector2(0f, -54f);
            }

            staminaSlider = clone.GetComponent<Slider>();
            if (staminaSlider != null)
            {
                staminaSlider.minValue = 0f;
                staminaSlider.maxValue = maxStamina;
                // Pintar fill en cian/amarillo estamina.
                if (staminaSlider.fillRect != null)
                {
                    var fillImg = staminaSlider.fillRect.GetComponent<UnityEngine.UI.Image>();
                    if (fillImg != null)
                        fillImg.color = new Color(0.95f, 0.82f, 0.2f, 1f);
                }
            }

            // Texto opcional debajo/al lado.
            var healthText = GameObject.Find("PlayerHealthText");
            if (healthText != null && staminaText == null)
            {
                var textClone = Object.Instantiate(healthText, healthText.transform.parent);
                textClone.name = "PlayerStaminaText";
                var trt = textClone.GetComponent<RectTransform>();
                var htrt = healthText.GetComponent<RectTransform>();
                if (trt != null && htrt != null)
                    trt.anchoredPosition = htrt.anchoredPosition + new Vector2(0f, -54f);
                staminaText = textClone.GetComponent<TextMeshProUGUI>();
                if (staminaText != null)
                    staminaText.color = new Color(1f, 0.9f, 0.35f, 1f);
            }
        }

        void RefreshUi()
        {
            if (staminaSlider != null)
                staminaSlider.value = current;
            if (staminaText != null)
                staminaText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(maxStamina)}";
        }
    }
}

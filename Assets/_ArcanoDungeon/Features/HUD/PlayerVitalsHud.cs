using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcanoDungeon.Features.HUD
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerVitalsHud : MonoBehaviour
    {
        public const string ResourcePath = "UI/PlayerVitalsHUD";
        [SerializeField] Slider healthBar;
        [SerializeField] Slider shieldBar;
        [SerializeField] Slider staminaBar;
        [SerializeField] TextMeshProUGUI healthText;
        [SerializeField] TextMeshProUGUI shieldText;
        [SerializeField] TextMeshProUGUI staminaText;
        PlayerHealth health;
        PlayerStats stats;
        PlayerStamina stamina;
        bool reconciled;

        public Slider HealthBar => healthBar;
        public Slider ShieldBar => shieldBar;
        public Slider StaminaBar => staminaBar;

        public static bool IsGameplayScene(string name) => name == "Level1" || name == "CatacombsLevel" || name == "CastleLevel";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Listen()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsGameplayScene(scene.name)) return;
            foreach (var existing in FindObjectsByType<PlayerVitalsHud>())
                if (existing.gameObject.scene == scene) return;
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null) { Debug.LogError("Missing the shared player vitals HUD prefab."); return; }
            var instance = Instantiate(prefab); instance.name = "PlayerVitalsHUD";
            SceneManager.MoveGameObjectToScene(instance, scene);
        }

        void Awake() => HideLegacyBars();

        void HideLegacyBars()
        {
            foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
            {
                if (rect.transform.IsChildOf(transform)) continue;
                switch (rect.name)
                {
                    case "PlayerHealthBar": case "PlayerHealthText": case "HealthCaption":
                    case "PlayerShieldBar": case "PlayerShieldText": case "ShieldCaption":
                    case "PlayerStaminaBar": case "PlayerStaminaText": case "StaminaCaption":
                        rect.gameObject.SetActive(false); break;
                }
            }
        }

        void LateUpdate()
        {
            if (!reconciled) { HideLegacyBars(); reconciled = true; }
            if (health == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) return;
                health = player.GetComponent<PlayerHealth>();
                stats = player.GetComponent<PlayerStats>(); stamina = player.GetComponent<PlayerStamina>();
                if (health == null) return;
                health.healthSlider = healthBar; health.healthText = healthText;
            }
            SetValue(healthBar, healthText, health.currentHealth, health.maxHealth);
            SetValue(shieldBar, shieldText, stats != null ? stats.CurrentShield : 0, stats != null ? stats.MaxShield : 0);
            SetValue(staminaBar, staminaText, stamina != null ? stamina.Current : 0, stamina != null ? stamina.Max : 0);
        }

        static void SetValue(Slider bar, TextMeshProUGUI text, float current, float maximum)
        {
            bar.minValue = 0; bar.maxValue = Mathf.Max(1, maximum); bar.SetValueWithoutNotify(current);
            var value = Mathf.RoundToInt(current) + " / " + Mathf.RoundToInt(maximum);
            if (text.text != value) text.text = value;
        }
    }
}

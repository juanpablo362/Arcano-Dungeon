using System.Collections;
using ArcanoDungeon.Core;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player.Motor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    public int currentHealth;

    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Feedback de daño")]
    public Color hurtFlashColor = new Color(1f, 0.15f, 0.12f, 1f);
    public int hurtFlashCount = 1;
    public float hurtFlashInterval = 0.08f;

    PlayerMotor motor;
    PlayerStats stats;
    Renderer[] renderers;
    MaterialPropertyBlock block;
    Coroutine flashRoutine;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        stats = GetComponent<PlayerStats>();
        renderers = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider == null)
        {
            GameObject barObj = GameObject.Find("PlayerHealthBar");
            if (barObj != null)
                healthSlider = barObj.GetComponent<Slider>();
        }

        if (healthText == null)
        {
            GameObject textObj = GameObject.Find("PlayerHealthText");
            if (textObj != null)
                healthText = textObj.GetComponent<TextMeshProUGUI>();
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
        }

        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(Mathf.RoundToInt(amount));
    }

    public void TakeDamage(int amount)
    {
        if (motor != null && motor.IsInvulnerable)
            return;

        if (stats == null)
            stats = GetComponent<PlayerStats>();

        int remaining = amount;
        if (stats != null)
            remaining = stats.AbsorbDamage(amount);

        if (remaining > 0)
            currentHealth = Mathf.Max(0, currentHealth - remaining);

        UpdateHealthUI();
        PlayHurtFlash();

        if (currentHealth <= 0)
        {
            GameSession.ResetRun();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void RestoreCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 1, maxHealth);
        UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0) return;

        maxHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (healthSlider != null)
            healthSlider.maxValue = maxHealth;

        UpdateHealthUI();
    }

    void PlayHurtFlash()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HurtFlashRoutine());
    }

    IEnumerator HurtFlashRoutine()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        for (int i = 0; i < hurtFlashCount; i++)
        {
            SetFlash(true);
            yield return new WaitForSeconds(hurtFlashInterval);
            SetFlash(false);
            yield return new WaitForSeconds(hurtFlashInterval);
        }

        SetFlash(false);
        flashRoutine = null;
    }

    void SetFlash(bool on)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null)
                continue;

            r.GetPropertyBlock(block);
            if (on)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
                    block.SetColor(BaseColorId, hurtFlashColor);
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(ColorId))
                    block.SetColor(ColorId, hurtFlashColor);
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(EmissionColorId))
                    block.SetColor(EmissionColorId, hurtFlashColor * 1.4f);
            }
            else
            {
                block.Clear();
            }

            r.SetPropertyBlock(on ? block : null);
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }
}

using System;
using System.Collections;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0618
public class EnemyHealth : MonoBehaviour
{
    public static event Action<EnemyHealth> AnyEnemyDied;

    public int maxHealth = 140;
    public int currentHealth;
    public Slider healthSlider;
    public Image healthFillImage;
    public Transform healthCanvasTransform;

    [Header("Muerte")]
    [Tooltip("Tiempo total hasta Destroy. Debe cubrir el clip Death (~1.8s) + hold del último frame.")]
    public float deathDestroyDelay = 2.2f;
    public string deathTrigger = "Die";
    public string hitTrigger = "Hit";
    public string deathStateName = "Death";

    public bool IsDead => isDead;

    bool isDead;
    RoomController ownerRoom;
    Animator animator;
    EnemyAI enemyAI;
    CharacterController characterController;
    static readonly int DieHash = Animator.StringToHash("Die");
    static readonly int DeathHash = Animator.StringToHash("Death");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DamageHash = Animator.StringToHash("Damage");
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        characterController = GetComponent<CharacterController>();
        UpdateHealthUI();
    }

    public void SetOwnerRoom(RoomController room)
    {
        ownerRoom = room;
    }

    public void ConfigureHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = newMaxHealth;
        UpdateHealthUI();
    }

    void LateUpdate()
    {
        if (healthCanvasTransform != null && Camera.main != null)
        {
            healthCanvasTransform.rotation = Quaternion.LookRotation(
                healthCanvasTransform.position - Camera.main.transform.position
            );
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateHealthUI();

        PlaySkeletonHitSfx();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayHitReaction();
    }

    void PlaySkeletonHitSfx()
    {
        if (!LooksLikeSkeletonOrLich())
            return;

        var catalog = DungeonSfxCatalog.Instance;
        if (catalog != null && catalog.skeletonHit != null)
            GameSfx.PlayCombat(catalog.skeletonHit);
    }

    bool LooksLikeSkeletonOrLich()
    {
        string n = gameObject.name ?? "";
        return n.IndexOf("Skeleton", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("Skell", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("Lich", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("esqueleto", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void PlayHitReaction()
    {
        if (animator != null)
        {
            // Evitar T-pose: hitstop del jugador u otras pausas pueden dejar speed bajo.
            if (animator.speed < 0.2f)
                animator.speed = 1f;

            if (animator.HasState(0, HitHash))
                animator.CrossFade(HitHash, 0.05f, 0, 0f);
            else
                TriggerIfExists(hitTrigger, HitHash, DamageHash);
        }
        else
            TriggerIfExists(hitTrigger, HitHash, DamageHash);

        if (enemyAI != null && !enemyAI.isStaggered)
            enemyAI.NotifyHitReaction(0.08f);
    }

    void UpdateHealthUI()
    {
        float normalized = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        if (healthSlider != null)
            healthSlider.value = normalized;

        if (healthFillImage != null)
            healthFillImage.fillAmount = normalized;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var catalog = DungeonSfxCatalog.Instance;
        if (catalog != null && catalog.groundImpact != null)
            GameSfx.PlayCombat(catalog.groundImpact);

        try { AnyEnemyDied?.Invoke(this); }
        catch { /* listeners no deben tumbar la muerte */ }

        if (ownerRoom != null)
            ownerRoom.OnEnemyDefeated();
        else
        {
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
                waveManager.EnemyDefeated();
        }

        if (enemyAI != null)
            enemyAI.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        if (healthCanvasTransform != null)
            healthCanvasTransform.gameObject.SetActive(false);

        gameObject.tag = "Untagged";

        PlayDeathAnimation();
        StartCoroutine(HoldDeathAndDestroy());
    }

    void PlayDeathAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = 1f;

        if (HasParam(SpeedHash, AnimatorControllerParameterType.Float))
            animator.SetFloat(SpeedHash, 0f);

        // Limpiar triggers residuales para que Any State no interrumpa la muerte.
        ResetAllTriggers();
        TriggerIfExists(deathTrigger, DieHash, DeathHash);

        int stateHash = Animator.StringToHash(deathStateName);
        if (!animator.HasState(0, stateHash))
            stateHash = DeathHash;

        if (animator.HasState(0, stateHash))
        {
            // Play directo evita CrossFade que a veces cae al default/T-pose.
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }
    }

    IEnumerator HoldDeathAndDestroy()
    {
        // Clip Death procedural ~1.8s; hold del último frame evita T-pose.
        const float fallbackDeathClipLen = 1.8f;
        float wait = Mathf.Max(deathDestroyDelay, fallbackDeathClipLen + 0.4f);

        if (animator != null)
        {
            // Esperar a entrar en Death.
            for (int i = 0; i < 8; i++)
            {
                yield return null;
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(deathStateName) || info.IsName("Death") || info.IsTag("Death") || info.IsName("Die"))
                {
                    wait = Mathf.Max(wait, info.length + 0.4f);
                    break;
                }
            }

            var deathInfo = animator.GetCurrentAnimatorStateInfo(0);
            float clipLen = deathInfo.length > 0.05f ? deathInfo.length : fallbackDeathClipLen;
            float freezeAt = Mathf.Max(0.05f, clipLen * 0.92f);

            // Congelar el último frame antes de que Write Defaults vuelva a T-pose.
            yield return new WaitForSeconds(freezeAt);
            if (animator != null)
            {
                int stateHash = Animator.StringToHash(deathStateName);
                if (!animator.HasState(0, stateHash))
                    stateHash = DeathHash;
                if (animator.HasState(0, stateHash))
                    animator.Play(stateHash, 0, 0.99f);
                animator.speed = 0f;
                animator.enabled = true;
                animator.Update(0f);
            }

            float remaining = wait - freezeAt;
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);
        }
        else
        {
            yield return new WaitForSeconds(wait);
        }

        Destroy(gameObject);
    }

    void TriggerIfExists(string preferred, params int[] hashes)
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(preferred) && HasTriggerName(preferred))
        {
            animator.ResetTrigger(preferred);
            animator.SetTrigger(preferred);
            return;
        }

        for (int i = 0; i < hashes.Length; i++)
        {
            if (!HasParam(hashes[i], AnimatorControllerParameterType.Trigger))
                continue;
            animator.ResetTrigger(hashes[i]);
            animator.SetTrigger(hashes[i]);
            return;
        }
    }

    void ResetAllTriggers()
    {
        if (animator == null)
            return;

        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(param.nameHash);
        }
    }

    bool HasTriggerName(string triggerName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger && param.name == triggerName)
                return true;
        }
        return false;
    }

    bool HasParam(int hash, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == hash && param.type == type)
                return true;
        }
        return false;
    }
}

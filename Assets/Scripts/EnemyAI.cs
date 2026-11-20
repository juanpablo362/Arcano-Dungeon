using System.Collections;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public float aggroRange = 40f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 15;
    public float moveSpeed = 2.8f;
    public float rotationSpeed = 8f;
    public float gravity = -20f;

    public float knockbackDistance = 1.5f;
    public float knockbackDuration = 0.25f;
    public float staggerDuration = 0.6f;

    public bool isStaggered { get; private set; }

    private Animator animator;
    private CharacterController controller;
    private EnemyHealth enemyHealth;
    private Transform playerTarget;
    private float verticalVelocity = -2f;
    private float nextAttackTime;
    private float startMoveDelay;
    private float aggroActivatedTime = -1f;
    private bool playedAggroRoar;
    private float nextPlayerSearchTime;
    private Coroutine hitReactionRoutine;
    private Coroutine attackRoutine;
    private bool isAttacking;
    private bool hasDealtDamageThisAttack;
    private int speedParam = -1;
    private int attackParam = -1;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackStateHash = Animator.StringToHash("Attack");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            CacheAnimatorParams();
        }

        controller = GetComponent<CharacterController>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        moveSpeed = Random.Range(moveSpeed * 0.92f, moveSpeed * 1.08f);
        startMoveDelay = Random.Range(0.05f, 0.25f);
        ResolvePlayer(true);

        // Evita CharacterController atrapado al spawnear dentro del suelo/colliders.
        if (controller != null)
        {
            controller.enabled = false;
            transform.position += Vector3.up * 0.05f;
            controller.enabled = true;
        }
    }

    void CacheAnimatorParams()
    {
        speedParam = -1;
        attackParam = -1;
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (var p in animator.parameters)
        {
            if (p.nameHash == SpeedHash)
                speedParam = SpeedHash;
            if (p.nameHash == AttackHash && p.type == AnimatorControllerParameterType.Trigger)
                attackParam = AttackHash;
        }
    }

    void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            SetSpeed(0f);
            isAttacking = false;
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            return;
        }

        // Si un hitstop externo dejó speed bajo, recuperar para no quedar en T-pose / Attack congelado.
        EnsureAnimatorPlaying();

        ApplyGravityStep();

        if (isStaggered)
        {
            SetSpeed(0f);
            MoveVerticalOnly();
            return;
        }

        ResolvePlayer(false);
        if (playerTarget == null)
        {
            SetSpeed(0f);
            MoveVerticalOnly();
            return;
        }

        Vector3 flatTarget = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        Vector3 toPlayer = flatTarget - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > aggroRange)
        {
            aggroActivatedTime = -1f;
            playedAggroRoar = false;
            SetSpeed(0f);
            MoveVerticalOnly();
            return;
        }

        if (aggroActivatedTime < 0f)
        {
            aggroActivatedTime = Time.time + startMoveDelay;
            if (!playedAggroRoar)
            {
                playedAggroRoar = true;
                var catalog = DungeonSfxCatalog.Instance;
                if (catalog != null && catalog.monsterAggroRoar != null)
                    GameSfx.PlayCombat(catalog.monsterAggroRoar);
            }
        }

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Time.time < aggroActivatedTime)
        {
            SetSpeed(0f);
            MoveVerticalOnly();
            return;
        }

        Vector3 moveDirection = Vector3.zero;
        if (distance > attackRange)
        {
            moveDirection = toPlayer.normalized;
            SetSpeed(1f);
        }
        else
        {
            SetSpeed(0f);
            TryAttack(toPlayer);
        }

        Move(moveDirection);
    }

    void TryAttack(Vector3 toPlayer)
    {
        if (isAttacking || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        EnsureAnimatorPlaying();
        isAttacking = true;
        hasDealtDamageThisAttack = false;

        var catalog = DungeonSfxCatalog.Instance;
        if (catalog != null)
        {
            var growl = catalog.RandomAttackGrowl();
            if (growl != null)
                GameSfx.PlayCombat(growl);
        }

        if (animator != null && attackParam != -1)
        {
            animator.ResetTrigger(attackParam);
            animator.SetTrigger(attackParam);
        }
        else if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(AttackHitOnce(toPlayer));
    }

    IEnumerator AttackHitOnce(Vector3 toPlayerAtStart)
    {
        // Un solo tick de daño en el frame de impacto (~40% del clip), no al trigger.
        float clipLen = 1.0f;
        if (animator != null)
        {
            yield return null;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == AttackStateHash && info.length > 0.05f)
                clipLen = info.length / Mathf.Max(0.01f, info.speedMultiplier);
        }

        float hitDelay = clipLen * 0.40f;
        float endDelay = Mathf.Max(hitDelay + 0.05f, clipLen * 0.95f);

        yield return new WaitForSeconds(hitDelay);
        ApplyAttackDamageOnce(toPlayerAtStart);

        float remaining = Mathf.Max(0.05f, endDelay - hitDelay);
        yield return new WaitForSeconds(remaining);

        isAttacking = false;
        attackRoutine = null;
    }

    void ApplyAttackDamageOnce(Vector3 toPlayerFallback)
    {
        if (hasDealtDamageThisAttack)
            return;
        hasDealtDamageThisAttack = true;

        if (enemyHealth != null && enemyHealth.IsDead)
            return;
        if (playerTarget == null)
            return;

        Vector3 flatTarget = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float distance = Vector3.Distance(flatTarget, transform.position);
        if (distance > attackRange * 1.35f)
            return;

        var player = playerTarget.gameObject;
        var playerController = player.GetComponent<PlayerController>()
            ?? player.GetComponentInParent<PlayerController>();
        Vector3 toPlayer = flatTarget - transform.position;
        if (toPlayer.sqrMagnitude < 0.001f)
            toPlayer = toPlayerFallback;

        if (playerController != null && playerController.isBlocking)
        {
            Vector3 pushDir = -toPlayer.normalized;
            if (pushDir.sqrMagnitude < 0.001f)
                pushDir = -transform.forward;
            StartCoroutine(KnockbackAndStagger(pushDir));
            return;
        }

        var playerHealth = player.GetComponent<PlayerHealth>()
            ?? player.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(attackDamage);
    }

    void ApplyGravityStep()
    {
        if (controller == null)
            return;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    void MoveVerticalOnly()
    {
        Move(Vector3.zero);
    }

    void Move(Vector3 horizontalDir)
    {
        if (controller == null)
            return;

        if (!controller.enabled)
            controller.enabled = true;

        Vector3 worldDelta = (horizontalDir * moveSpeed * Time.deltaTime)
            + (Vector3.up * verticalVelocity * Time.deltaTime);

        Vector3 before = transform.position;
        CollisionFlags flags = controller.Move(worldDelta);

        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // Si el CC no puede avanzar (atrapado), desplazar en horizontal a mano.
        if (horizontalDir.sqrMagnitude > 0.01f)
        {
            Vector3 moved = transform.position - before;
            Vector3 horizontalMoved = new Vector3(moved.x, 0f, moved.z);
            float intended = moveSpeed * Time.deltaTime;
            if (intended > 0.0001f && horizontalMoved.magnitude < intended * 0.05f)
            {
                controller.enabled = false;
                transform.position = before + new Vector3(worldDelta.x, 0f, worldDelta.z);
                controller.enabled = true;
            }
        }
    }

    void SetSpeed(float value)
    {
        if (animator == null)
            return;

        if (speedParam != -1)
            animator.SetFloat(speedParam, value);
        else
            animator.SetFloat("Speed", value);
    }

    void ResolvePlayer(bool force)
    {
        if (!force && playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            return;
        if (!force && Time.time < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.time + 0.35f;
        playerTarget = null;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            playerTarget = tagged.transform;
            return;
        }

        var controllerPlayer = Object.FindAnyObjectByType<PlayerController>();
        if (controllerPlayer != null)
        {
            playerTarget = controllerPlayer.transform;
            return;
        }

        var health = Object.FindAnyObjectByType<PlayerHealth>();
        if (health != null)
            playerTarget = health.transform;
    }

    public void NotifyHitReaction(float duration = 0.08f)
    {
        if (!isActiveAndEnabled || (enemyHealth != null && enemyHealth.IsDead))
            return;

        // No reiniciar stagger si ya está activo: evita quedar congelado bajo golpes seguidos.
        if (isStaggered)
            return;

        EnsureAnimatorPlaying();

        if (hitReactionRoutine != null)
            StopCoroutine(hitReactionRoutine);
        hitReactionRoutine = StartCoroutine(HitReaction(Mathf.Clamp(duration, 0.05f, 0.15f)));
    }

    public void ApplyKnockback(Vector3 pushDirection, float customStagger = -1f)
    {
        if (!isActiveAndEnabled || (enemyHealth != null && enemyHealth.IsDead))
            return;
        if (pushDirection.sqrMagnitude < 0.001f)
            return;

        if (customStagger > 0f)
            staggerDuration = customStagger;

        if (hitReactionRoutine != null)
        {
            StopCoroutine(hitReactionRoutine);
            hitReactionRoutine = null;
        }

        StartCoroutine(KnockbackAndStagger(pushDirection.normalized));
    }

    Coroutine slowRoutine;
    float slowBaseSpeed = -1f;

    public void ApplySlow(float speedMult, float seconds)
    {
        if (!isActiveAndEnabled || (enemyHealth != null && enemyHealth.IsDead))
            return;
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);
        slowRoutine = StartCoroutine(SlowRoutine(Mathf.Clamp(speedMult, 0.1f, 1f), Mathf.Max(0.1f, seconds)));
    }

    IEnumerator SlowRoutine(float mult, float seconds)
    {
        if (slowBaseSpeed < 0f)
            slowBaseSpeed = moveSpeed;
        moveSpeed = slowBaseSpeed * mult;
        yield return new WaitForSeconds(seconds);
        moveSpeed = slowBaseSpeed;
        slowBaseSpeed = -1f;
        slowRoutine = null;
    }

    void EnsureAnimatorPlaying()
    {
        if (animator == null)
            return;
        // speed==0 solo es válido en muerte (EnemyHealth); vivo no debería quedar en cámara lenta.
        if (animator.speed > 0f && animator.speed < 0.2f)
            animator.speed = 1f;
        else if (animator.speed <= 0f && (enemyHealth == null || !enemyHealth.IsDead))
            animator.speed = 1f;
    }

    IEnumerator HitReaction(float duration)
    {
        isStaggered = true;
        // No forzar Speed=0 todo el stagger: solo una pausa mínima.
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ApplyGravityStep();
            MoveVerticalOnly();
            yield return null;
        }

        hitReactionRoutine = null;
        if (enemyHealth == null || !enemyHealth.IsDead)
            isStaggered = false;
    }

    IEnumerator KnockbackAndStagger(Vector3 pushDirection)
    {
        if (hitReactionRoutine != null)
        {
            StopCoroutine(hitReactionRoutine);
            hitReactionRoutine = null;
        }

        isStaggered = true;
        SetSpeed(0f);

        float elapsed = 0f;
        float pushSpeed = knockbackDistance / Mathf.Max(0.01f, knockbackDuration);
        float originalSpeed = moveSpeed;
        moveSpeed = pushSpeed;
        while (elapsed < knockbackDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            ApplyGravityStep();
            Move(pushDirection.normalized);
            yield return null;
        }

        moveSpeed = originalSpeed;

        float remainingStagger = Mathf.Max(0f, staggerDuration - knockbackDuration);
        float staggerElapsed = 0f;
        while (staggerElapsed < remainingStagger)
        {
            staggerElapsed += Time.deltaTime;
            ApplyGravityStep();
            MoveVerticalOnly();
            yield return null;
        }

        isStaggered = false;
    }
}

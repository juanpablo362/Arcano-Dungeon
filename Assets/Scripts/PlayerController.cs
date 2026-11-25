using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Features.Player.View;
using ArcanoDungeon.Features.Player.Motor;
using ArcanoDungeon.Infrastructure.Input;
using ArcanoDungeon.Features.Assassin;
using UnityEngine;

#pragma warning disable CS0618
public class PlayerController : CharacterBase
{
    [System.Obsolete("Usar MeleeWeaponController para daño/rango melee.")]
    public float attackRadius = 2.5f;
    [System.Obsolete("Usar MeleeWeaponController para daño/rango melee.")]
    public int attackDamage = 35;

    [Header("Ataque cuerpo a cuerpo")]
    [Tooltip("Legacy cooldown; el timing real lo llevan Animation Events + buffer del MeleeWeaponController.")]
    public float attackCooldown = 0.65f;
    [SerializeField] MeleeWeaponController meleeWeapon;

    [Header("Mago - Bola de fuego")]
    public bool useFireballOnRightClick = false;
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballCooldown = 0.75f;
    public int fireballDamage = 45;

    [Header("Nuevo motor (Fase 1)")]
    [SerializeField] PlayerInputReader inputReader;
    [SerializeField] PlayerMotor playerMotor;
    [SerializeField] AimRaycaster aimRaycaster;

    public bool isBlocking { get; private set; }

    /// <summary>True mientras se realiza un ataque a distancia.</summary>
    public bool IsChannelingRanged => Time.time < rangedChannelUntil || (assassin != null && assassin.IsThrowing);
    AssassinAbilities assassin;

    bool hasBlockingParam;
    float nextFireballTime;
    float rangedChannelUntil;
    bool useNewMotor;

    protected override void Awake()
    {
        base.Awake();
        assassin = GetComponent<AssassinAbilities>();

        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
        if (playerMotor == null)
            playerMotor = GetComponent<PlayerMotor>();
        if (aimRaycaster == null)
            aimRaycaster = GetComponent<AimRaycaster>();
        if (meleeWeapon == null)
            meleeWeapon = GetComponent<MeleeWeaponController>();

        useNewMotor = playerMotor != null && inputReader != null;

        if (CharacterAnimator != null)
        {
            foreach (AnimatorControllerParameter param in CharacterAnimator.parameters)
            {
                if (param.name == "isBlocking" && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasBlockingParam = true;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (!CameraFollow.IsCursorLocked)
        {
            isBlocking = false;
            if (CharacterAnimator != null && hasBlockingParam)
                CharacterAnimator.SetBool("isBlocking", false);

            if (!useNewMotor)
                ApplyLocomotion(Vector3.zero, false, false, false);
            return;
        }

        UpdateSecondaryState();

        // Escudo / bloqueo: siempre mirar a donde apunta la cámara (no al movimiento).
        if (isBlocking)
            FaceDirectionImmediate(ResolveAimDirection());

        if (useNewMotor)
        {
            // Locomoción la lleva PlayerMotor; aquí solo combate/bloqueo.
            if (isBlocking)
                playerMotor.NotifyAimingOrAttacking(0.05f);

            if (inputReader.AttackPressed)
                HandleAttack();
            if (inputReader.SecondaryPressed && useFireballOnRightClick)
                HandleSecondaryPressed();
            return;
        }

        // Fallback legacy (sin PlayerMotor).
        LegacyLocomotionAndCombat();
    }

    void UpdateSecondaryState()
    {
        bool secondaryHeld = inputReader != null
            ? inputReader.SecondaryHeld
            : (UnityEngine.InputSystem.Mouse.current != null
               && UnityEngine.InputSystem.Mouse.current.rightButton.isPressed);

        if (useFireballOnRightClick || (assassin != null && assassin.isActiveAndEnabled))
        {
            isBlocking = false;
        }
        else
        {
            isBlocking = secondaryHeld;
            if (isBlocking)
                playerMotor?.NotifyAimingOrAttacking(0.05f);
        }

        if (CharacterAnimator != null && hasBlockingParam)
            CharacterAnimator.SetBool("isBlocking", isBlocking);
    }

    void HandleAttack()
    {
        if (assassin != null && assassin.IsThrowing) return;
        if (!CameraFollow.IsCursorLocked)
            return;

        playerMotor?.NotifyAimingOrAttacking();
        TryMeleeAttack(ResolveAimDirection());
    }

    /// <summary>Delegado público por si otros sistemas quieren forzar melee.</summary>
    public void RequestMeleeAttack()
    {
        TryMeleeAttack(ResolveAimDirection());
    }

    void HandleSecondaryPressed()
    {
        if (!CameraFollow.IsCursorLocked)
            return;

        if (useFireballOnRightClick)
        {
            playerMotor?.NotifyAimingOrAttacking();
            TryCastFireball();
        }
    }

    void LegacyLocomotionAndCombat()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;

        bool rightPressed = mouse != null && mouse.rightButton.isPressed;
        bool rightClicked = mouse != null && mouse.rightButton.wasPressedThisFrame;

        if (assassin != null && assassin.isActiveAndEnabled)
        {
            isBlocking = false;
        }
        else if (useFireballOnRightClick)
        {
            isBlocking = false;
            if (rightClicked)
                TryCastFireball();
        }
        else
        {
            isBlocking = rightPressed;
        }

        if (CharacterAnimator != null && hasBlockingParam)
            CharacterAnimator.SetBool("isBlocking", isBlocking);

        float horizontal = 0f;
        float vertical = 0f;
        bool wantsRun = false;
        bool wantsCrouch = false;
        bool wantsJump = false;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            wantsRun = keyboard.leftShiftKey.isPressed;
            wantsCrouch = keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed;
            wantsJump = keyboard.spaceKey.wasPressedThisFrame;
        }

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 moveDirection = Vector3.zero;

        if (inputDir.sqrMagnitude > 0.001f)
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            if (camTransform != null)
            {
                Vector3 camForward = camTransform.forward;
                Vector3 camRight = camTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();
                moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
            }
            else
            {
                moveDirection = inputDir;
            }
        }

        float speedScale = isBlocking ? 0.5f : 1f;
        bool meleeAttacking = meleeWeapon != null && meleeWeapon.IsAttacking;
        if (meleeAttacking)
        {
            speedScale *= meleeWeapon.AttackMoveSpeedMultiplier;
            if (meleeWeapon.LocksMoveInput)
                moveDirection = Vector3.zero;
            wantsRun = false;
        }
        else if (IsChannelingRanged)
        {
            speedScale *= 0.3f;
            wantsRun = false;
        }

        Vector3 face = moveDirection;
        // Bloqueo o disparo: orientar al aim/cámara. Sin eso, el escudo mira al WASD.
        if (isBlocking || meleeAttacking || IsChannelingRanged
            || (mouse != null && mouse.leftButton.isPressed))
            face = ResolveAimDirection();

        ApplyLocomotion(moveDirection, wantsRun, wantsCrouch, wantsJump, speedScale, face);

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            TryMeleeAttack(ResolveAimDirection());
    }

    void TryMeleeAttack(Vector3 aimDirection)
    {
        if (aimDirection.sqrMagnitude < 0.001f)
            aimDirection = transform.forward;

        FaceDirectionImmediate(aimDirection);

        if (meleeWeapon != null)
        {
            meleeWeapon.TryAttack(aimDirection);
            return;
        }

        // Fallback mínimo si falta MeleeWeaponController (no debería ocurrir en roster).
        if (CharacterAnimator != null)
            CharacterAnimator.SetTrigger("Attack");
    }

    void TryCastFireball()
    {
        if (fireballPrefab == null || Time.time < nextFireballTime)
            return;

        Vector3 aimDir = ResolveAimDirection();
        if (aimDir.sqrMagnitude < 0.001f)
            aimDir = transform.forward;

        FaceDirectionImmediate(aimDir);
        nextFireballTime = Time.time + fireballCooldown;
        // Ventana corta de canalización: 30% move + sin dash (PlayerMotor).
        rangedChannelUntil = Time.time + Mathf.Clamp(fireballCooldown * 0.45f, 0.25f, 0.55f);
        playerMotor?.NotifyAimingOrAttacking(rangedChannelUntil - Time.time);

        if (CharacterAnimator != null)
            CharacterAnimator.SetTrigger("Attack");

        Vector3 spawnPos = transform.position + Vector3.up * 1.2f + aimDir * 0.7f + transform.right * 0.2f;
        if (aimRaycaster != null)
        {
            Vector3 toPoint = aimRaycaster.AimPoint - spawnPos;
            if (toPoint.sqrMagnitude > 0.01f)
                aimDir = toPoint.normalized;
        }

        float[] spreads = { 0f };
        var stats = GetComponent<ArcanoDungeon.Features.Loot.PlayerStats>();
        if (stats != null && stats.HasMultishot)
        {
            int level = Mathf.Max(1, stats.MultishotLevel);
            int sidePairs = level; // 1→3, 2→5, 3→7 proyectiles
            int count = 1 + sidePairs * 2;
            spreads = new float[count];
            float step = stats.MultishotSpread;
            for (int s = 0; s < count; s++)
                spreads[s] = (s - sidePairs) * step;
        }

        for (int i = 0; i < spreads.Length; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, spreads[i], 0f) * aimDir;
            GameObject ball = Instantiate(fireballPrefab, spawnPos, Quaternion.LookRotation(dir));
            FireballProjectile projectile = ball.GetComponent<FireballProjectile>();
            if (projectile == null)
                projectile = ball.AddComponent<FireballProjectile>();

            projectile.damage = fireballDamage;
            projectile.Launch(transform, dir);
        }
    }

    Vector3 ResolveAimDirection()
    {
        if (aimRaycaster != null)
            return aimRaycaster.GetFlatAimDirection();
        return GetCameraFlatForward();
    }

    static Vector3 GetCameraFlatForward()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.forward;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }
}

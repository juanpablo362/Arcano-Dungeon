using System;
using System.Collections;
using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Infrastructure.Input;
using ArcanoDungeon.Features.Player.View;
using UnityEngine;
using ArcanoDungeon.Features.Player;

namespace ArcanoDungeon.Features.Player.Motor
{
    /// <summary>
    /// Motor de jugador: WASD relativo a cámara, salto, dash con i-frames y rotación aim/move.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] ThirdPersonCameraRig cameraRig;
        [SerializeField] AimRaycaster aimRaycaster;
        [SerializeField] CharacterBase character;
        [SerializeField] MeleeWeaponController meleeWeapon;
        [SerializeField] PlayerStamina stamina;

        [Header("Dash")]
        [SerializeField] float dashDistance = 5f;
        [SerializeField] float dashDuration = 0.18f;
        [SerializeField] float dashCooldown = 1.5f;
        [SerializeField] float dashIFrames = 0.3f;
        float dashCooldownReduce;

        [Header("Aim")]
        [SerializeField] float aimRotationHold = 0.25f;

        CharacterController controller;
        PlayerController playerController;
        float nextDashTime;
        float invulnerableUntil;
        float aimUntil;
        bool isDashing;
        Coroutine dashRoutine;

        public bool IsDashing => isDashing;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public bool IsAimingOrAttacking => Time.time < aimUntil;
        public float EffectiveDashCooldown => Mathf.Max(0.2f, dashCooldown - dashCooldownReduce);
        public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
        public float DashCooldownNormalized =>
            EffectiveDashCooldown <= 0f ? 0f : Mathf.Clamp01(DashCooldownRemaining / EffectiveDashCooldown);

        public void AddDashCooldownReduce(float seconds)
        {
            if (seconds <= 0f)
                return;
            dashCooldownReduce += seconds;
        }

        public event Action OnDashStarted;
        public event Action OnDashEnded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null)
                input = GetComponent<PlayerInputReader>();
            if (character == null)
                character = GetComponent<CharacterBase>();
            playerController = GetComponent<PlayerController>();
            if (meleeWeapon == null)
                meleeWeapon = GetComponent<MeleeWeaponController>();
            if (stamina == null)
                stamina = GetComponent<PlayerStamina>();
            if (aimRaycaster == null)
                aimRaycaster = GetComponent<AimRaycaster>();
            if (cameraRig == null)
                cameraRig = FindAnyObjectByType<ThirdPersonCameraRig>();
        }

        void OnDisable()
        {
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
                dashRoutine = null;
                isDashing = false;
            }
        }

        void Update()
        {
            if (character == null || input == null)
                return;

            bool attackLocksDash = IsAttackMovementLocked();
            if (!isDashing && input.DashPressed && !attackLocksDash)
                TryDash();

            if (isDashing)
                return;

            if (!CameraFollow.IsCursorLocked)
            {
                character.DriveLocomotion(Vector3.zero, false, false, false);
                return;
            }

            Vector3 moveDirection = ComputeCameraRelativeMove(input.Move);
            Vector3 faceDirection = moveDirection;
            bool blocking = playerController != null && playerController.isBlocking;
            bool castingRanged = playerController != null && playerController.IsChannelingRanged;
            bool meleeAttacking = meleeWeapon != null && meleeWeapon.IsAttacking;
            bool lockMove = meleeWeapon != null && meleeWeapon.LocksMoveInput;

            // Espada/dagas: WASD bloqueado durante el swing (el lunge lo aplica MeleeWeaponController).
            if (lockMove)
                moveDirection = Vector3.zero;

            if (IsAimingOrAttacking || blocking || meleeAttacking || castingRanged)
                faceDirection = ResolveAimFlatForward();

            float speedScale = blocking ? 0.5f : 1f;
            if (meleeAttacking)
                speedScale *= meleeWeapon.AttackMoveSpeedMultiplier;
            else if (castingRanged)
                speedScale *= 0.3f;

            bool canSprint = input.SprintHeld
                             && !blocking
                             && !attackLocksDash
                             && speedScale >= 0.99f;

            character.DriveLocomotion(
                moveDirection,
                canSprint,
                input.CrouchHeld,
                input.JumpPressed && !lockMove,
                speedScale,
                faceDirection);
        }

        bool IsAttackMovementLocked()
        {
            if (meleeWeapon != null && meleeWeapon.BlocksDashAndSprint)
                return true;
            if (playerController != null && playerController.IsChannelingRanged)
                return true;
            return false;
        }

        public void SetCameraRig(ThirdPersonCameraRig rig)
        {
            cameraRig = rig;
        }

        public void NotifyAimingOrAttacking(float holdSeconds = -1f)
        {
            float hold = holdSeconds > 0f ? holdSeconds : aimRotationHold;
            aimUntil = Mathf.Max(aimUntil, Time.time + hold);
        }

        public void SetBlockingSpeedScale(bool isBlocking)
        {
            // Hook reservado: el scale se aplica en Update leyendo un flag si se necesita.
            // Por ahora PlayerController puede reducir moveSpeed temporalmente.
        }

        void TryDash()
        {
            if (!CameraFollow.IsCursorLocked || isDashing || Time.time < nextDashTime)
                return;
            if (IsAttackMovementLocked())
                return;
            if (stamina != null && !stamina.TryConsumeDash())
                return;

            Vector3 dir = ComputeCameraRelativeMove(input != null ? input.Move : Vector2.zero);
            if (dir.sqrMagnitude < 0.001f)
                dir = ResolveAimFlatForward();
            if (dir.sqrMagnitude < 0.001f)
                dir = transform.forward;

            dir.y = 0f;
            dir.Normalize();

            nextDashTime = Time.time + EffectiveDashCooldown;
            invulnerableUntil = Time.time + dashIFrames;
            dashRoutine = StartCoroutine(DashRoutine(dir));
        }

        IEnumerator DashRoutine(Vector3 direction)
        {
            isDashing = true;
            OnDashStarted?.Invoke();

            float elapsed = 0f;
            float speed = dashDistance / Mathf.Max(0.01f, dashDuration);

            while (elapsed < dashDuration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                if (controller != null && controller.enabled)
                    controller.Move(direction * speed * dt);
                yield return null;
            }

            isDashing = false;
            dashRoutine = null;
            OnDashEnded?.Invoke();
        }

        Vector3 ComputeCameraRelativeMove(Vector2 stick)
        {
            if (stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            // Igual que CesarArcanoDungeon: movimiento relativo a Camera.main (plano XZ).
            Vector3 forward;
            Vector3 right;
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                forward = cam.transform.forward;
                right = cam.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }
            else if (cameraRig != null)
            {
                forward = cameraRig.FlatForward;
                right = Quaternion.Euler(0f, 90f, 0f) * forward;
            }
            else
            {
                forward = transform.forward;
                right = transform.right;
            }

            Vector3 move = (forward * stick.y + right * stick.x);
            return move.sqrMagnitude > 0.001f ? move.normalized : Vector3.zero;
        }

        Vector3 ResolveAimFlatForward()
        {
            if (aimRaycaster != null)
                return aimRaycaster.GetFlatAimDirection();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                Vector3 f = cam.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude > 0.001f)
                    return f.normalized;
            }

            if (cameraRig != null)
                return cameraRig.FlatForward;
            return transform.forward;
        }
    }
}

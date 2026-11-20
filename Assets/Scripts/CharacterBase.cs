using UnityEngine;

#pragma warning disable CS0618
[RequireComponent(typeof(CharacterController))]
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Locomoción")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.6f;
    public float crouchSpeedMultiplier = 0.5f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    public float jumpHeight = 1.4f;

    [Header("Agacharse")]
    public float crouchHeight = 1.2f;
    public float heightChangeSpeed = 12f;

    public bool IsGrounded { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }

    protected CharacterController Controller { get; private set; }
    protected Animator CharacterAnimator { get; private set; }
    protected float VerticalVelocity { get; set; }

    float standingHeight;
    float standingCenterY;
    bool hasIsMovingParam;
    bool hasIsRunningParam;
    bool hasIsCrouchingParam;
    bool hasJumpTrigger;

    protected virtual void Awake()
    {
        Controller = GetComponent<CharacterController>();
        CharacterAnimator = GetComponent<Animator>();
        if (CharacterAnimator == null)
            CharacterAnimator = GetComponentInChildren<Animator>();

        if (CharacterAnimator != null)
        {
            CharacterAnimator.applyRootMotion = false;
            CacheAnimatorParameters();
        }

        if (Controller != null)
        {
            standingHeight = Controller.height;
            standingCenterY = Controller.center.y;
            if (crouchHeight >= standingHeight)
                crouchHeight = Mathf.Max(0.5f, standingHeight * 0.6f);
        }
    }

    /// <summary>API pública para que PlayerMotor impulse la locomoción sin duplicar lógica.</summary>
    public void DriveLocomotion(
        Vector3 moveDirection,
        bool wantsRun,
        bool wantsCrouch,
        bool wantsJump,
        float speedScale = 1f,
        Vector3 faceDirection = default)
    {
        ApplyLocomotion(moveDirection, wantsRun, wantsCrouch, wantsJump, speedScale, faceDirection);
    }

    /// <param name="faceDirection">
    /// Dirección de mirada (XZ). Si es cero, mira hacia el movimiento.
    /// Útil para strafear mirando al aim en vez de a donde caminas.
    /// </param>
    protected void ApplyLocomotion(
        Vector3 moveDirection,
        bool wantsRun,
        bool wantsCrouch,
        bool wantsJump,
        float speedScale = 1f,
        Vector3 faceDirection = default)
    {
        if (Controller == null)
            return;

        IsGrounded = Controller.isGrounded;
        IsMoving = moveDirection.sqrMagnitude > 0.001f;

        // No correr en el aire ni agachado.
        IsRunning = wantsRun && IsMoving && IsGrounded && !wantsCrouch;
        IsCrouching = wantsCrouch && IsGrounded;

        UpdateCapsuleHeight();

        Vector3 lookDirection = faceDirection;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.001f && IsMoving)
            lookDirection = moveDirection;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        if (IsGrounded && VerticalVelocity < 0f)
            VerticalVelocity = -2f;

        if (wantsJump && CanJump())
            Jump();

        VerticalVelocity += gravity * Time.deltaTime;

        float speed = moveSpeed * Mathf.Max(0f, speedScale);
        if (IsCrouching)
            speed *= crouchSpeedMultiplier;
        else if (IsRunning)
            speed *= runSpeedMultiplier;

        Vector3 velocity = (moveDirection * speed) + (Vector3.up * VerticalVelocity);
        Controller.Move(velocity * Time.deltaTime);

        UpdateAnimator();
    }

    protected void FaceDirectionImmediate(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    protected virtual bool CanJump()
    {
        return IsGrounded && !IsCrouching;
    }

    protected virtual void Jump()
    {
        VerticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (CharacterAnimator != null && hasJumpTrigger)
            CharacterAnimator.SetTrigger("Jump");
    }

    protected virtual void UpdateAnimator()
    {
        if (CharacterAnimator == null)
            return;

        if (hasIsMovingParam)
            CharacterAnimator.SetBool("isMoving", IsMoving);
        if (hasIsRunningParam)
            CharacterAnimator.SetBool("isRunning", IsRunning);
        if (hasIsCrouchingParam)
            CharacterAnimator.SetBool("isCrouching", IsCrouching);
    }

    void UpdateCapsuleHeight()
    {
        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.MoveTowards(
            Controller.height,
            targetHeight,
            heightChangeSpeed * Time.deltaTime);

        float heightDelta = standingHeight - newHeight;
        Vector3 center = Controller.center;
        center.y = standingCenterY - (heightDelta * 0.5f);

        Controller.height = newHeight;
        Controller.center = center;
    }

    void CacheAnimatorParameters()
    {
        foreach (AnimatorControllerParameter param in CharacterAnimator.parameters)
        {
            if (param.name == "isMoving" && param.type == AnimatorControllerParameterType.Bool)
                hasIsMovingParam = true;
            else if (param.name == "isRunning" && param.type == AnimatorControllerParameterType.Bool)
                hasIsRunningParam = true;
            else if (param.name == "isCrouching" && param.type == AnimatorControllerParameterType.Bool)
                hasIsCrouchingParam = true;
            else if (param.name == "Jump" && param.type == AnimatorControllerParameterType.Trigger)
                hasJumpTrigger = true;
        }
    }
}

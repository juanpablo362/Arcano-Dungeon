using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcanoDungeon.Infrastructure.Input
{
    /// <summary>
    /// Lee el InputActionAsset del jugador y expone valores cacheados (sin Find en Update).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] string actionMapName = "Player";

        public InputActionAsset ActionsAsset
        {
            get => actions;
            set => actions = value;
        }

        InputActionMap map;
        InputAction move;
        InputAction look;
        InputAction jump;
        InputAction dash;
        InputAction attack;
        InputAction secondary;
        InputAction sprint;
        InputAction crouch;
        InputAction pause;
        InputAction special;
        InputAction interact;
        InputAction bomb;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DashPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool SecondaryPressed { get; private set; }
        public bool SecondaryHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool PausePressed { get; private set; }
        public bool SpecialPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool BombPressed { get; private set; }

        public event Action OnAttack;
        public event Action OnSecondary;
        public event Action OnDash;
        public event Action OnJump;
        public event Action OnPause;
        public event Action OnSpecial;
        public event Action OnInteract;
        public event Action OnBomb;

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] Asigna un InputActionAsset.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] No existe el ActionMap '{actionMapName}'.", this);
                enabled = false;
                return;
            }

            move = map.FindAction("Move", throwIfNotFound: false);
            look = map.FindAction("Look", throwIfNotFound: false);
            jump = map.FindAction("Jump", throwIfNotFound: false);
            dash = map.FindAction("Dash", throwIfNotFound: false);
            attack = map.FindAction("Attack", throwIfNotFound: false);
            secondary = map.FindAction("Secondary", throwIfNotFound: false);
            sprint = map.FindAction("Sprint", throwIfNotFound: false);
            crouch = map.FindAction("Crouch", throwIfNotFound: false);
            pause = map.FindAction("Pause", throwIfNotFound: false);
            special = map.FindAction("Special", throwIfNotFound: false);
            interact = map.FindAction("Interact", throwIfNotFound: false);
            bomb = map.FindAction("Bomb", throwIfNotFound: false);
        }

        void OnEnable()
        {
            map?.Enable();
            Bind(jump, OnJumpPerformed);
            Bind(dash, OnDashPerformed);
            Bind(attack, OnAttackPerformed);
            Bind(secondary, OnSecondaryPerformed);
            Bind(pause, OnPausePerformed);
            Bind(special, OnSpecialPerformed);
            Bind(interact, OnInteractPerformed);
            Bind(bomb, OnBombPerformed);
        }

        void OnDisable()
        {
            Unbind(jump, OnJumpPerformed);
            Unbind(dash, OnDashPerformed);
            Unbind(attack, OnAttackPerformed);
            Unbind(secondary, OnSecondaryPerformed);
            Unbind(pause, OnPausePerformed);
            Unbind(special, OnSpecialPerformed);
            Unbind(interact, OnInteractPerformed);
            Unbind(bomb, OnBombPerformed);
            map?.Disable();
        }

        void Update()
        {
            JumpPressed = false;
            DashPressed = false;
            AttackPressed = false;
            SecondaryPressed = false;
            PausePressed = false;
            SpecialPressed = false;
            InteractPressed = false;
            BombPressed = false;

            Move = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
            Look = look != null ? look.ReadValue<Vector2>() : Vector2.zero;
            AttackHeld = attack != null && attack.IsPressed();
            SecondaryHeld = secondary != null && secondary.IsPressed();
            SprintHeld = sprint != null && sprint.IsPressed();
            CrouchHeld = crouch != null && crouch.IsPressed();

            if (jump != null && jump.WasPressedThisFrame()) JumpPressed = true;
            if (dash != null && dash.WasPressedThisFrame()) DashPressed = true;
            if (attack != null && attack.WasPressedThisFrame()) AttackPressed = true;
            if (secondary != null && secondary.WasPressedThisFrame()) SecondaryPressed = true;
            if (pause != null && pause.WasPressedThisFrame()) PausePressed = true;
            if (special != null && special.WasPressedThisFrame()) SpecialPressed = true;
            if (interact != null && interact.WasPressedThisFrame()) InteractPressed = true;
            if (bomb != null && bomb.WasPressedThisFrame()) BombPressed = true;

            // Fallback teclado si el asset aún no tiene las acciones (hot-reload).
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    SpecialPressed = true;
                    OnSpecial?.Invoke();
                }
                if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    InteractPressed = true;
                    OnInteract?.Invoke();
                }
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    BombPressed = true;
                    OnBomb?.Invoke();
                }
            }
        }

        static void Bind(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action == null) return;
            action.performed += handler;
        }

        static void Unbind(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action == null) return;
            action.performed -= handler;
        }

        void OnJumpPerformed(InputAction.CallbackContext _)
        {
            JumpPressed = true;
            OnJump?.Invoke();
        }

        void OnDashPerformed(InputAction.CallbackContext _)
        {
            DashPressed = true;
            OnDash?.Invoke();
        }

        void OnAttackPerformed(InputAction.CallbackContext _)
        {
            AttackPressed = true;
            OnAttack?.Invoke();
        }

        void OnSecondaryPerformed(InputAction.CallbackContext _)
        {
            SecondaryPressed = true;
            OnSecondary?.Invoke();
        }

        void OnPausePerformed(InputAction.CallbackContext _)
        {
            PausePressed = true;
            OnPause?.Invoke();
        }

        void OnSpecialPerformed(InputAction.CallbackContext _)
        {
            SpecialPressed = true;
            OnSpecial?.Invoke();
        }

        void OnInteractPerformed(InputAction.CallbackContext _)
        {
            InteractPressed = true;
            OnInteract?.Invoke();
        }

        void OnBombPerformed(InputAction.CallbackContext _)
        {
            BombPressed = true;
            OnBomb?.Invoke();
        }
    }
}

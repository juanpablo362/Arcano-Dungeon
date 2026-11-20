using ArcanoDungeon.Infrastructure.Input;
using Unity.Cinemachine;
using UnityEngine;

namespace ArcanoDungeon.Features.Player.View
{
    /// <summary>
    /// Orquesta look del jugador sobre un CameraTarget + Cinemachine Third Person Follow.
    /// Mantiene compatibilidad con CameraFollow.IsCursorLocked.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] Transform cameraTarget;
        [SerializeField] PlayerInputReader input;
        [SerializeField] CinemachineCamera virtualCamera;
        [SerializeField] CinemachineThirdPersonFollow thirdPersonFollow;

        [Header("Look")]
        [SerializeField] float lookSensitivity = 0.35f;
        // Pitch negativo = mirar arriba. Limitado para no ver por encima del techo.
        [SerializeField] float minPitch = -8f;
        [SerializeField] float maxPitch = 35f;
        [SerializeField] bool invertY;

        [Header("Shoulder (Cinemachine)")]
        // Offset relativo al CameraTarget (pecho del chibi). Evitar Y alto: AvoidObstacles + suelo = vista cenital.
        [SerializeField] Vector3 shoulderOffset = new Vector3(0.35f, 0.1f, 0f);
        [SerializeField] float cameraDistance = 4f;
        [SerializeField] float verticalArmLength = 0f;
        [SerializeField] float cameraSide = 1f;
        [SerializeField] Vector3 cameraTargetLocalPosition = new Vector3(0f, 0.7f, 0f);
        [SerializeField] float fieldOfView = 60f;
        [SerializeField] bool avoidObstacles = true;

        float yaw;
        float pitch = 2f;
        bool initialized;

        public Transform CameraTarget => cameraTarget;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public Vector3 FlatForward
        {
            get
            {
                Vector3 f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                return f.normalized;
            }
        }

        void Awake()
        {
            if (input == null)
                input = GetComponentInParent<PlayerInputReader>() ?? GetComponent<PlayerInputReader>();

            if (cameraTarget == null)
            {
                var existing = transform.Find("CameraTarget");
                if (existing != null)
                    cameraTarget = existing;
            }

            ApplyFollowSettings();
            CameraFollow.SetCursorLocked(true);
        }

        void OnEnable()
        {
            if (input != null)
                input.OnPause += ToggleCursorLock;
        }

        void OnDisable()
        {
            if (input != null)
                input.OnPause -= ToggleCursorLock;
        }

        void Start()
        {
            Transform follow = ResolveFollowTarget();
            if (follow != null && !initialized)
            {
                yaw = follow.eulerAngles.y;
                initialized = true;
            }

            BindVirtualCameraTarget();
            ApplyFollowSettings();
            CameraFollow.SetCursorLocked(true);
        }

        void LateUpdate()
        {
            AutoBindPlayerIfNeeded();

            // Esc / Pause abre el menú de pausa; RMB re-lock solo si no está pausado.
            if (PauseMenuController.IsOpen)
                return;

            if (!CameraFollow.IsCursorLocked
                && UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
            {
                CameraFollow.SetCursorLocked(true);
            }

            if (!CameraFollow.IsCursorLocked || input == null || cameraTarget == null)
                return;

            Vector2 look = input.Look;
            yaw += look.x * lookSensitivity;
            float yDelta = look.y * lookSensitivity;
            pitch += invertY ? yDelta : -yDelta;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void AutoBindPlayerIfNeeded()
        {
            if (cameraTarget != null && virtualCamera != null
                && virtualCamera.Target.TrackingTarget == cameraTarget)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                SetFollowTarget(player.transform);
        }

        public void SetFollowTarget(Transform playerRoot)
        {
            if (playerRoot == null)
                return;

            if (input == null)
                input = playerRoot.GetComponent<PlayerInputReader>();

            // Rebind pause after late input assignment.
            if (input != null)
            {
                input.OnPause -= ToggleCursorLock;
                input.OnPause += ToggleCursorLock;
            }

            var t = playerRoot.Find("CameraTarget");
            if (t == null)
            {
                var go = new GameObject("CameraTarget");
                t = go.transform;
                t.SetParent(playerRoot, false);
            }

            t.localPosition = cameraTargetLocalPosition;
            cameraTarget = t;

            if (!initialized)
            {
                yaw = playerRoot.eulerAngles.y;
                pitch = 2f;
                initialized = true;
            }

            // World rotation: el follow de Cinemachine lee la orientación del TrackingTarget.
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);

            BindVirtualCameraTarget();
            ApplyFollowSettings();
        }

        void BindVirtualCameraTarget()
        {
            if (virtualCamera == null)
                virtualCamera = GetComponentInChildren<CinemachineCamera>(true);
            if (thirdPersonFollow == null && virtualCamera != null)
                thirdPersonFollow = virtualCamera.GetComponent<CinemachineThirdPersonFollow>();

            if (virtualCamera == null || cameraTarget == null)
                return;

            var target = virtualCamera.Target;
            target.TrackingTarget = cameraTarget;
            // Third Person Follow usa la rotación del TrackingTarget; no hace falta LookAt.
            target.LookAtTarget = null;
            virtualCamera.Target = target;
            var priority = virtualCamera.Priority;
            priority.Value = 10;
            virtualCamera.Priority = priority;

            // Forzar que la cámara del brain sea la MainCamera activa.
            var brain = FindAnyObjectByType<CinemachineBrain>();
            if (brain != null)
            {
                var cam = brain.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.tag = "MainCamera";
                    cam.enabled = true;
                }
            }
        }

        void ApplyFollowSettings()
        {
            if (thirdPersonFollow == null && virtualCamera != null)
                thirdPersonFollow = virtualCamera.GetComponent<CinemachineThirdPersonFollow>();

            if (thirdPersonFollow == null)
                return;

            thirdPersonFollow.ShoulderOffset = shoulderOffset;
            thirdPersonFollow.CameraDistance = cameraDistance;
            thirdPersonFollow.VerticalArmLength = verticalArmLength;
            thirdPersonFollow.CameraSide = cameraSide;
            thirdPersonFollow.Damping = new Vector3(0.1f, 0.2f, 0.3f);

            var obstacles = thirdPersonFollow.AvoidObstacles;
            obstacles.Enabled = true;
            obstacles.IgnoreTag = "Player";
            obstacles.CameraRadius = 0.25f;
            obstacles.CollisionFilter = ~0;
            thirdPersonFollow.AvoidObstacles = obstacles;
        }

        Transform ResolveFollowTarget()
        {
            if (cameraTarget != null)
                return cameraTarget.parent != null ? cameraTarget.parent : cameraTarget;
            return null;
        }

        void ToggleCursorLock()
        {
            if (PauseMenuController.Instance != null)
            {
                PauseMenuController.Instance.Toggle();
                return;
            }

            CameraFollow.SetCursorLocked(!CameraFollow.IsCursorLocked);
        }
    }
}

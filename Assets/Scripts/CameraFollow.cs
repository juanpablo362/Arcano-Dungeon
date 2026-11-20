using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Órbita tercera persona — mismo feel que CesarArcanoDungeon/Assets/Scripts/CameraFollow.cs
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static bool IsCursorLocked { get; private set; } = true;

    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float mouseSensitivity = 0.25f;
    public float distance = 6.5f;
    public float heightOffset = 1.1f;
    public float minPitch = 5f;
    public float maxPitch = 65f;
    public float collisionOffset = 0.3f;
    public float minDistance = 2f; // un poco más alto que Cesar (0.5) para mazmorras estrechas

    float yaw;
    float pitch = 22f;
    bool yawInitialized;

    void Start()
    {
        SetCursorLocked(true);
        var cam = GetComponent<Camera>();
        if (cam != null)
            cam.fieldOfView = 60f;
    }

    void OnDisable()
    {
        // No desbloquear cursor si solo se desactiva el componente en editor.
    }

    void LateUpdate()
    {
        // Esc lo maneja PauseMenuController; aquí solo re-lock con RMB si no hay pausa.
        if (PauseMenuController.IsOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
            && PauseMenuController.Instance == null)
            SetCursorLocked(!IsCursorLocked);

        if (!IsCursorLocked
            && Mouse.current != null
            && Mouse.current.rightButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }

        if (Mouse.current != null && IsCursorLocked)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (!yawInitialized)
        {
            yaw = player.transform.eulerAngles.y;
            pitch = 22f;
            yawInitialized = true;
        }

        // Altura del pecho del jugador (igual que Cesar)
        Vector3 targetPivot = player.transform.position + Vector3.up * heightOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 dirToCamera = -(rotation * Vector3.forward);
        Vector3 desiredPosition = targetPivot + (dirToCamera * distance);
        Vector3 finalPosition = desiredPosition;

        if (Physics.Linecast(
                targetPivot,
                desiredPosition,
                out RaycastHit hit,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
            {
                float hitDistance = Mathf.Max(hit.distance - collisionOffset, minDistance);
                finalPosition = targetPivot + (dirToCamera * hitDistance);
            }
        }

        transform.position = finalPosition;
        transform.LookAt(targetPivot);
    }

    public static void SetCursorLocked(bool locked)
    {
        IsCursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

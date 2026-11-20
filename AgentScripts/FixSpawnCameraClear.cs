using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixSpawnCameraClear
{
    public static string Main()
    {
        // En Play: si hay pared entre cámara y jugador, girar yaw 180° / buscar ángulo libre.
        if (EditorApplication.isPlaying)
        {
            var follow = Object.FindAnyObjectByType<CameraFollow>();
            var player = GameObject.FindGameObjectWithTag("Player");
            var cam = Camera.main;
            if (follow == null || player == null || cam == null)
                return "missing";

            var yawF = typeof(CameraFollow).GetField("yaw",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pitchF = typeof(CameraFollow).GetField("pitch",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float bestYaw = player.transform.eulerAngles.y;
            float bestClear = -1f;
            Vector3 pivot = player.transform.position + Vector3.up * follow.heightOffset;

            for (int i = 0; i < 8; i++)
            {
                float y = player.transform.eulerAngles.y + i * 45f;
                Quaternion rot = Quaternion.Euler(22f, y, 0f);
                Vector3 dir = -(rot * Vector3.forward);
                Vector3 desired = pivot + dir * follow.distance;
                float clear = follow.distance;
                if (Physics.Linecast(pivot, desired, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
                        clear = hit.distance;
                }
                if (clear > bestClear)
                {
                    bestClear = clear;
                    bestYaw = y;
                }
            }

            yawF?.SetValue(follow, bestYaw);
            pitchF?.SetValue(follow, 22f);

            return string.Format(
                "play fix yaw={0:F1} clear={1:F2} player={2} cam={3}",
                bestYaw, bestClear,
                player.transform.position.ToString("F2"),
                cam.transform.position.ToString("F2"));
        }

        return "not playing";
    }
}

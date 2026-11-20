using UnityEngine;

public static class FixCesarCamFov
{
    public static string Main()
    {
        var cam = Camera.main;
        var follow = Object.FindAnyObjectByType<CameraFollow>();
        if (cam == null)
            return "no main cam";

        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;

        // Forzar pitch inicial como Cesar (22°) si el jugador existe.
        var p = GameObject.FindGameObjectWithTag("Player");
        string extra = "";
        if (follow != null && p != null)
        {
            // Empujar un poco el yaw a la orientación del player para no mirar la pared de spawn.
            var so = new UnityEditor.SerializedObject(follow);
            // No hay pitch serializado como field privado accesible fácil; usar reflexión.
            var pitchField = typeof(CameraFollow).GetField("pitch",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var yawField = typeof(CameraFollow).GetField("yaw",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pitchField != null) pitchField.SetValue(follow, 22f);
            if (yawField != null) yawField.SetValue(follow, p.transform.eulerAngles.y);
            extra = " pitch=22 yaw=" + p.transform.eulerAngles.y.ToString("F1");
        }

        return "fov=" + cam.fieldOfView + " cam=" + cam.name + extra;
    }
}

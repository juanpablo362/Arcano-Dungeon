using UnityEngine;

public static class DiagnoseCesarCam
{
    public static string Main()
    {
        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        var p = GameObject.FindGameObjectWithTag("Player");
        float hd = (cam != null && p != null) ? cam.transform.position.y - p.transform.position.y : 0f;
        float dist = (cam != null && p != null)
            ? Vector3.Distance(cam.transform.position, p.transform.position + Vector3.up * 1.4f)
            : 0f;
        return string.Format(
            "playing={0} follow={1} enabled={2} sens={3} distSet={4} height={5} camPos={6} player={7} heightDelta={8:F2} orbitDist={9:F2}",
            UnityEditor.EditorApplication.isPlaying,
            follow != null,
            follow != null && follow.enabled,
            follow != null ? follow.mouseSensitivity : 0f,
            follow != null ? follow.distance : 0f,
            follow != null ? follow.heightOffset : 0f,
            cam != null ? cam.transform.position.ToString("F2") : "null",
            p != null,
            hd,
            dist);
    }
}

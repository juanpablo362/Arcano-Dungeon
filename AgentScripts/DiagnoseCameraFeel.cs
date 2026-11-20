using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public static class DiagnoseCameraFeel
{
    public static string Main()
    {
        var cam = Camera.main;
        var p = GameObject.FindGameObjectWithTag("Player");
        var rig = Object.FindAnyObjectByType<ThirdPersonCameraRig>();
        var follow = rig != null ? rig.GetComponentInChildren<CinemachineThirdPersonFollow>(true) : null;

        string camPos = cam != null ? cam.transform.position.ToString("F2") : "null";
        string playerPos = p != null ? p.transform.position.ToString("F2") : "null";
        float pitch = rig != null ? rig.Pitch : 0f;
        float yaw = rig != null ? rig.Yaw : 0f;
        float dist = follow != null ? follow.CameraDistance : 0f;
        string shoulder = follow != null ? follow.ShoulderOffset.ToString("F2") : "null";
        float sens = 0f;
        if (rig != null)
            sens = new SerializedObject(rig).FindProperty("lookSensitivity").floatValue;

        float heightDelta = 0f;
        if (cam != null && p != null)
            heightDelta = cam.transform.position.y - p.transform.position.y;

        return string.Format(
            "playing={0} player={1} camPos={2} playerPos={3} heightDelta={4:F2} pitch={5:F1} yaw={6:F1} dist={7:F2} shoulder={8} sens={9:F2}",
            EditorApplication.isPlaying,
            p != null,
            camPos,
            playerPos,
            heightDelta,
            pitch,
            yaw,
            dist,
            shoulder,
            sens);
    }
}

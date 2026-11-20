using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEngine;

public static class DiagnoseCameraDeep
{
    public static string Main()
    {
        var cam = Camera.main;
        var p = GameObject.FindGameObjectWithTag("Player");
        var rig = Object.FindAnyObjectByType<ThirdPersonCameraRig>();
        var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        var follow = vcam != null ? vcam.GetComponent<CinemachineThirdPersonFollow>() : null;
        var brain = Object.FindAnyObjectByType<CinemachineBrain>();

        if (p == null || cam == null || rig == null)
            return "missing refs playing=" + UnityEditor.EditorApplication.isPlaying;

        var target = rig.CameraTarget;
        var toPlayer = p.transform.position - cam.transform.position;
        float lookDownDeg = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(cam.transform.forward, Vector3.right).normalized,
            cam.transform.forward,
            cam.transform.right);

        // Ángulo real cámara→jugador en vertical
        Vector3 flatFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        float camPitchFromFwd = Mathf.Asin(Mathf.Clamp(cam.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        string comps = "";
        if (vcam != null)
        {
            foreach (var c in vcam.GetComponents<MonoBehaviour>())
                comps += c.GetType().Name + ";";
        }

        var targetEuler = target != null ? target.eulerAngles.ToString("F1") : "null";
        var targetLocal = target != null ? target.localPosition.ToString("F2") : "null";
        var targetParent = target != null && target.parent != null ? target.parent.name : "null";
        var playerScale = p.transform.lossyScale.ToString("F2");
        var playerEuler = p.transform.eulerAngles.ToString("F1");

        string tracking = "null";
        string lookAt = "null";
        if (vcam != null)
        {
            tracking = vcam.Target.TrackingTarget != null ? vcam.Target.TrackingTarget.name + "@" + vcam.Target.TrackingTarget.position.ToString("F2") : "null";
            lookAt = vcam.Target.LookAtTarget != null ? vcam.Target.LookAtTarget.name : "null";
        }

        return string.Format(
            "camPos={0} camFwd={1} camPitchFwdY={2:F1} heightDelta={3:F2} dist3D={4:F2} " +
            "rigPitch={5:F1} rigYaw={6:F1} targetEuler={7} targetLocal={8} parent={9} " +
            "playerPos={10} playerEuler={11} scale={12} tracking={13} lookAt={14} " +
            "followDist={15} shoulder={16} arm={17} side={18} comps=[{19}] brain={20}",
            cam.transform.position.ToString("F2"),
            cam.transform.forward.ToString("F2"),
            camPitchFromFwd,
            cam.transform.position.y - p.transform.position.y,
            toPlayer.magnitude,
            rig.Pitch,
            rig.Yaw,
            targetEuler,
            targetLocal,
            targetParent,
            p.transform.position.ToString("F2"),
            playerEuler,
            playerScale,
            tracking,
            lookAt,
            follow != null ? follow.CameraDistance : -1f,
            follow != null ? follow.ShoulderOffset.ToString("F2") : "null",
            follow != null ? follow.VerticalArmLength : -1f,
            follow != null ? follow.CameraSide : -1f,
            comps,
            brain != null ? brain.name : "null");
    }
}

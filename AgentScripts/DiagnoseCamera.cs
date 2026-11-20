using System.Text;
using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DiagnoseCamera
{
    public static string Main()
    {
        var sb = new StringBuilder();
        EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        sb.AppendLine("Cameras=" + cameras.Length);
        foreach (var c in cameras)
        {
            sb.AppendLine("  CAM " + GetPath(c.transform)
                + " enabled=" + c.enabled
                + " tag=" + c.tag
                + " pos=" + c.transform.position
                + " brain=" + (c.GetComponent<CinemachineBrain>() != null)
                + " brainEnabled=" + (c.GetComponent<CinemachineBrain>() != null && c.GetComponent<CinemachineBrain>().enabled)
                + " follow=" + (c.GetComponent("CameraFollow") != null));
        }

        var brains = Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include);
        sb.AppendLine("Brains=" + brains.Length);

        var vcams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
        sb.AppendLine("CinemachineCameras=" + vcams.Length);
        foreach (var v in vcams)
        {
            var follow = v.GetComponent<CinemachineThirdPersonFollow>();
            sb.AppendLine("  VCAM " + GetPath(v.transform)
                + " active=" + v.isActiveAndEnabled
                + " priority=" + v.Priority.Value
                + " tracking=" + (v.Target.TrackingTarget != null ? GetPath(v.Target.TrackingTarget) : "NULL")
                + " lookAt=" + (v.Target.LookAtTarget != null ? GetPath(v.Target.LookAtTarget) : "NULL")
                + " followComp=" + (follow != null)
                + " shoulder=" + (follow != null ? follow.ShoulderOffset.ToString() : "-")
                + " dist=" + (follow != null ? follow.CameraDistance.ToString() : "-"));
        }

        var rig = Object.FindAnyObjectByType<ThirdPersonCameraRig>();
        sb.AppendLine("Rig=" + (rig != null ? GetPath(rig.transform) : "NULL"));
        if (rig != null)
        {
            var so = new SerializedObject(rig);
            sb.AppendLine("  ser.cameraTarget=" + RefName(so, "cameraTarget"));
            sb.AppendLine("  ser.virtualCamera=" + RefName(so, "virtualCamera"));
            sb.AppendLine("  ser.thirdPersonFollow=" + RefName(so, "thirdPersonFollow"));
            sb.AppendLine("  ser.input=" + RefName(so, "input"));
            sb.AppendLine("  CameraTarget prop=" + (rig.CameraTarget != null ? GetPath(rig.CameraTarget) : "NULL"));
        }

        var spawners = Object.FindObjectsByType<PlayerSpawner>(FindObjectsInactive.Include);
        sb.AppendLine("PlayerSpawners=" + spawners.Length);
        foreach (var s in spawners)
            sb.AppendLine("  spawner start=" + (s.startPosition != null ? s.startPosition.position.ToString() : "null")
                + " knight=" + (s.knightPrefab != null) + " mage=" + (s.magePrefab != null));

        // Prefab check
        var prefab = PrefabUtility.LoadPrefabContents("Assets/_ArcanoDungeon/Prefabs/PlayerCameraRig.prefab");
        sb.AppendLine("Prefab children:");
        foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            sb.AppendLine("  " + GetPath(t) + " local=" + t.localPosition + " world=" + t.position);
        var prefabRig = prefab.GetComponent<ThirdPersonCameraRig>();
        if (prefabRig != null)
        {
            var so = new SerializedObject(prefabRig);
            sb.AppendLine("Prefab ser.virtualCamera=" + RefName(so, "virtualCamera"));
            sb.AppendLine("Prefab ser.thirdPersonFollow=" + RefName(so, "thirdPersonFollow"));
        }
        PrefabUtility.UnloadPrefabContents(prefab);

        return sb.ToString();
    }

    static string RefName(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        if (p == null) return "NO_PROP";
        return p.objectReferenceValue != null ? p.objectReferenceValue.name : "NULL";
    }

    static string GetPath(Transform t)
    {
        var p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}

using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixCameraCorner
{
    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
        int disabled = 0;

        // Solo una MainCamera: la del PlayerCameraRig con CinemachineBrain.
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        Camera keep = null;
        foreach (var c in cameras)
        {
            if (c.GetComponent<CinemachineBrain>() != null)
            {
                keep = c;
                break;
            }
        }

        if (keep == null)
        {
            var rig = Object.FindAnyObjectByType<ThirdPersonCameraRig>();
            if (rig != null)
                keep = rig.GetComponentInChildren<Camera>(true);
        }

        foreach (var c in cameras)
        {
            if (keep != null && c == keep)
            {
                c.tag = "MainCamera";
                c.enabled = true;
                var brain = c.GetComponent<CinemachineBrain>();
                if (brain != null) brain.enabled = true;
                continue;
            }

            // Desactivar cámaras legacy (la de la esquina).
            if (c.CompareTag("MainCamera") || c.gameObject.name.Contains("Main Camera"))
            {
                c.tag = "Untagged";
                c.enabled = false;
                var listener = c.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
                disabled++;
            }
        }

        // Asegurar que el VCAM tiene prioridad y pipeline ok
        var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Priority = 10;
            var follow = vcam.GetComponent<CinemachineThirdPersonFollow>();
            if (follow == null)
                follow = vcam.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(0.6f, 1.5f, 0f);
            follow.CameraDistance = 3.5f;
            follow.VerticalArmLength = 0.4f;
            follow.CameraSide = 1f;
        }

        var rigComp = Object.FindAnyObjectByType<ThirdPersonCameraRig>();
        if (rigComp != null && vcam != null)
        {
            var so = new SerializedObject(rigComp);
            so.FindProperty("virtualCamera").objectReferenceValue = vcam;
            so.FindProperty("thirdPersonFollow").objectReferenceValue =
                vcam.GetComponent<CinemachineThirdPersonFollow>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "OK disabledLegacyCameras=" + disabled
               + " keep=" + (keep != null ? keep.name : "NULL")
               + " keepPos=" + (keep != null ? keep.transform.position.ToString() : "-");
    }
}

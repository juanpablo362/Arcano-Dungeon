using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UseCesarCamera2
{
    public static string Main()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        var scene = EditorSceneManager.OpenScene(
            "Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);

        // Desactivar todo el stack Cinemachine.
        foreach (var rig in Object.FindObjectsByType<ThirdPersonCameraRig>(FindObjectsInactive.Include))
        {
            rig.gameObject.SetActive(false);
            EditorUtility.SetDirty(rig.gameObject);
        }

        foreach (var brain in Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include))
        {
            brain.enabled = false;
            EditorUtility.SetDirty(brain);
        }

        foreach (var vcam in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include))
        {
            vcam.enabled = false;
            EditorUtility.SetDirty(vcam);
        }

        // Asegurar Main Camera independiente (no hija del rig desactivado).
        Camera main = null;
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (c.GetComponent<CameraFollow>() != null || c.CompareTag("MainCamera"))
            {
                // Si está bajo un padre inactivo, sacarla a la raíz.
                if (!c.gameObject.activeInHierarchy || c.GetComponentInParent<ThirdPersonCameraRig>() != null)
                {
                    c.transform.SetParent(null, true);
                    c.gameObject.SetActive(true);
                }
                main = c;
                break;
            }
        }

        if (main == null)
        {
            var go = new GameObject("Main Camera");
            main = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        main.gameObject.name = "Main Camera";
        main.tag = "MainCamera";
        main.enabled = true;
        main.fieldOfView = 60f;
        main.nearClipPlane = 0.1f;
        main.farClipPlane = 500f;
        main.transform.SetParent(null, true);
        main.gameObject.SetActive(true);

        // Un solo AudioListener
        bool keptListener = false;
        foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
        {
            if (!keptListener && l.gameObject == main.gameObject)
            {
                l.enabled = true;
                keptListener = true;
            }
            else
            {
                l.enabled = false;
            }
        }
        if (!keptListener)
            main.gameObject.AddComponent<AudioListener>();

        var follow = main.GetComponent<CameraFollow>();
        if (follow == null)
            follow = main.gameObject.AddComponent<CameraFollow>();

        follow.enabled = true;
        var so = new SerializedObject(follow);
        so.FindProperty("mouseSensitivity").floatValue = 0.25f;
        so.FindProperty("distance").floatValue = 6.5f;
        so.FindProperty("heightOffset").floatValue = 1.4f;
        so.FindProperty("minPitch").floatValue = 5f;
        so.FindProperty("maxPitch").floatValue = 65f;
        so.FindProperty("collisionOffset").floatValue = 0.3f;
        so.FindProperty("minDistance").floatValue = 2f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(follow);
        EditorUtility.SetDirty(main);

        // Otras cámaras: quitar tag MainCamera
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (c == main) continue;
            if (c.CompareTag("MainCamera"))
                c.tag = "Untagged";
            if (c.GetComponent<CinemachineBrain>() != null)
                c.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        return "OK MainCamera root + CameraFollow Cesar; FOV=60 minDist=2";
    }
}

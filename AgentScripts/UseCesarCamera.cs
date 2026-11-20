using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UseCesarCamera
{
    public static string Main()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        var scene = EditorSceneManager.OpenScene(
            "Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);

        // Apagar Cinemachine / ThirdPerson rig para que no pelee con CameraFollow.
        int disabledRigs = 0;
        foreach (var rig in Object.FindObjectsByType<ThirdPersonCameraRig>(FindObjectsInactive.Include))
        {
            rig.gameObject.SetActive(false);
            disabledRigs++;
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

        // Main Camera con CameraFollow estilo Cesar.
        Camera main = Camera.main;
        if (main == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            main = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        main.tag = "MainCamera";
        main.enabled = true;
        main.fieldOfView = 60f;

        // Quitar listeners duplicados
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        bool kept = false;
        foreach (var l in listeners)
        {
            if (!kept && l.GetComponent<Camera>() == main)
            {
                l.enabled = true;
                kept = true;
            }
            else
                l.enabled = false;
        }

        var follow = main.GetComponent<CameraFollow>();
        if (follow == null)
            follow = main.gameObject.AddComponent<CameraFollow>();

        var so = new SerializedObject(follow);
        so.FindProperty("mouseSensitivity").floatValue = 0.25f;
        so.FindProperty("distance").floatValue = 6.5f;
        so.FindProperty("heightOffset").floatValue = 1.4f;
        so.FindProperty("minPitch").floatValue = 5f;
        so.FindProperty("maxPitch").floatValue = 65f;
        so.FindProperty("collisionOffset").floatValue = 0.3f;
        so.FindProperty("minDistance").floatValue = 0.5f;
        var smooth = so.FindProperty("followSmooth");
        if (smooth != null) smooth.floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        follow.enabled = true;
        EditorUtility.SetDirty(follow);
        EditorUtility.SetDirty(main.gameObject);

        // Bootstrap no debe reactivar el rig Cinemachine.
        foreach (var boot in Object.FindObjectsByType<PlayerCameraBootstrap>(FindObjectsInactive.Include))
        {
            boot.enabled = false;
            EditorUtility.SetDirty(boot);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        return string.Format(
            "OK Cesar cam: CameraFollow sens=0.25 dist=6.5 height=1.4; disabledRigs={0}; main={1}",
            disabledRigs,
            main.name);
    }
}

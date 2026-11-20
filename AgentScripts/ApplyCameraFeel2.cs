using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyCameraFeel2
{
    public static string Main()
    {
        // Pecho más bajo + menos pitch = menos vista “desde el techo”.
        var shoulder = new Vector3(0.4f, 0.05f, 0f);
        float distance = 3.6f;
        float arm = 0f;
        var targetLocal = new Vector3(0f, 0.95f, 0f);

        void ApplyToRig(ThirdPersonCameraRig rig)
        {
            var so = new SerializedObject(rig);
            so.FindProperty("lookSensitivity").floatValue = 0.35f;
            so.FindProperty("minPitch").floatValue = -25f;
            so.FindProperty("maxPitch").floatValue = 28f;
            so.FindProperty("shoulderOffset").vector3Value = shoulder;
            so.FindProperty("cameraDistance").floatValue = distance;
            so.FindProperty("verticalArmLength").floatValue = arm;
            so.FindProperty("cameraTargetLocalPosition").vector3Value = targetLocal;
            so.ApplyModifiedPropertiesWithoutUndo();

            var follow = rig.GetComponentInChildren<CinemachineThirdPersonFollow>(true);
            if (follow != null)
            {
                follow.ShoulderOffset = shoulder;
                follow.CameraDistance = distance;
                follow.VerticalArmLength = arm;
                follow.CameraSide = 1f;
                EditorUtility.SetDirty(follow);
            }

            EditorUtility.SetDirty(rig);
        }

        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        var prefabPath = "Assets/_ArcanoDungeon/Prefabs/PlayerCameraRig.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        var prefabRig = prefab.GetComponent<ThirdPersonCameraRig>();
        if (prefabRig != null) ApplyToRig(prefabRig);
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        var scene = EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
        foreach (var rig in Object.FindObjectsByType<ThirdPersonCameraRig>(FindObjectsInactive.Include))
            ApplyToRig(rig);

        string[] players =
        {
            "Assets/_ArcanoDungeon/Prefabs/ElaraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/LyraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/KaedePlayer.prefab",
        };
        foreach (var path in players)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            var t = root.transform.Find("CameraTarget");
            if (t != null) t.localPosition = targetLocal;
            var boot = root.GetComponent<PlayerCameraBootstrap>();
            if (boot != null)
            {
                var so = new SerializedObject(boot);
                so.FindProperty("cameraTargetLocalPosition").vector3Value = targetLocal;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(boot);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "OK feel2: sens=0.35 targetY=0.95 shoulderY=0.05 dist=3.6 pitchClamp=28";
    }
}

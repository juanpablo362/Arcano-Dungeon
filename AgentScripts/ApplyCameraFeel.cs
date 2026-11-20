using ArcanoDungeon.Features.Player.View;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyCameraFeel
{
    public static string Main()
    {
        var shoulder = new Vector3(0.45f, 0.2f, 0f);
        float distance = 4.25f;
        float arm = 0.15f;

        void ApplyToRig(ThirdPersonCameraRig rig)
        {
            var so = new SerializedObject(rig);
            so.FindProperty("lookSensitivity").floatValue = 0.28f;
            so.FindProperty("minPitch").floatValue = -20f;
            so.FindProperty("maxPitch").floatValue = 35f;
            so.FindProperty("shoulderOffset").vector3Value = shoulder;
            so.FindProperty("cameraDistance").floatValue = distance;
            so.FindProperty("verticalArmLength").floatValue = arm;
            so.FindProperty("cameraTargetLocalPosition").vector3Value = new Vector3(0f, 1.15f, 0f);
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
        }

        var prefabPath = "Assets/_ArcanoDungeon/Prefabs/PlayerCameraRig.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        var prefabRig = prefab.GetComponent<ThirdPersonCameraRig>();
        if (prefabRig != null) ApplyToRig(prefabRig);
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        var scene = EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
        foreach (var rig in Object.FindObjectsByType<ThirdPersonCameraRig>(FindObjectsInactive.Include))
            ApplyToRig(rig);

        // CameraTarget height on player prefabs
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
            if (t != null) t.localPosition = new Vector3(0f, 1.15f, 0f);
            var boot = root.GetComponent<PlayerCameraBootstrap>();
            if (boot != null)
            {
                var so = new SerializedObject(boot);
                so.FindProperty("cameraTargetLocalPosition").vector3Value = new Vector3(0f, 1.15f, 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "OK camera feel: sens=0.28 shoulderY=0.2 dist=4.25 maxPitch=35";
    }
}

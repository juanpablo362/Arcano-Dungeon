using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CleanupVcamComposer
{
    public static string Main()
    {
        int removed = 0;

        void Clean(GameObject root)
        {
            foreach (var composer in root.GetComponentsInChildren<CinemachineRotationComposer>(true))
            {
                Object.DestroyImmediate(composer);
                removed++;
            }
        }

        var prefabPath = "Assets/_ArcanoDungeon/Prefabs/PlayerCameraRig.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        Clean(prefab);
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);

        var scene = EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
        foreach (var vcam in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include))
            Clean(vcam.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Removed RotationComposer count=" + removed;
    }
}

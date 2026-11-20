using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DestroyLegacyMainCamera
{
    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
        int destroyed = 0;

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (var c in cameras)
        {
            bool isLegacy = c.GetComponent<Unity.Cinemachine.CinemachineBrain>() == null
                            && (c.gameObject.name == "Main Camera" || c.transform.parent == null);
            if (!isLegacy)
                continue;

            // Solo destruir la cámara raíz legacy, no la del rig.
            if (c.transform.root.name == "PlayerCameraRig")
                continue;

            Object.DestroyImmediate(c.gameObject);
            destroyed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Destroyed legacy cameras=" + destroyed;
    }
}

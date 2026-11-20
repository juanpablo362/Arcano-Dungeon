using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneMainMenuDepth
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";

    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var depth = GameObject.Find("/MenuDepth");
        if (depth == null)
            return "MenuDepth missing";

        Set("ColumnL", new Vector3(-5.6f, 0f, 8.8f), new Vector3(0f, 18f, 0f), new Vector3(1.35f, 1.7f, 1.35f));
        Set("ColumnR", new Vector3(7.6f, 0f, 9.2f), new Vector3(0f, -12f, 0f), new Vector3(1.4f, 1.85f, 1.4f));
        Set("ColumnFarL", new Vector3(-3.2f, 0f, 11.6f), new Vector3(0f, 8f, 0f), new Vector3(1.05f, 1.45f, 1.05f));
        Set("ColumnFarR", new Vector3(4.8f, 0f, 12.0f), new Vector3(0f, -20f, 0f), new Vector3(1.1f, 1.5f, 1.1f));
        Set("BannerL", new Vector3(-4.9f, 2.6f, 8.4f), new Vector3(0f, 25f, 0f), new Vector3(1.25f, 1.25f, 1.25f));
        Set("BannerR", new Vector3(6.5f, 2.9f, 8.9f), new Vector3(0f, -30f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
        Set("TorchL", new Vector3(-4.4f, 1.5f, 7.8f), new Vector3(0f, 35f, 0f), Vector3.one * 1.15f);
        Set("TorchR", new Vector3(6.2f, 1.7f, 8.2f), new Vector3(0f, -40f, 0f), Vector3.one * 1.2f);

        // Luces de antorcha un poco más fuertes.
        BoostLight("TorchLightL", 3.4f, 10f);
        BoostLight("TorchLightR", 3.2f, 10.5f);

        RenderSettings.fogDensity = 0.028f;
        RenderSettings.fogColor = new Color(0.02f, 0.015f, 0.04f, 1f);

        var backdrop = GameObject.Find("/MenuBackdrop");
        if (backdrop != null)
        {
            backdrop.transform.position = new Vector3(1.2f, 3.0f, 15.5f);
            backdrop.transform.localScale = new Vector3(36f, 20f, 1f);
            EditorUtility.SetDirty(backdrop);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Depth tuned for visibility.";
    }

    static void Set(string name, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = GameObject.Find("/MenuDepth/" + name);
        if (go == null)
            return;
        Undo.RecordObject(go.transform, "Tune depth prop");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        EditorUtility.SetDirty(go);
    }

    static void BoostLight(string name, float intensity, float range)
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.name != name)
                continue;
            Undo.RecordObject(light, "Boost torch light");
            light.intensity = intensity;
            light.range = range;
            EditorUtility.SetDirty(light);
        }
    }
}

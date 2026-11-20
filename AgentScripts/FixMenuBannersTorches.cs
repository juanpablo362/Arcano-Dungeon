using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixMenuBannersTorches
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";

    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Las banderas del pack tienen pivot raro: subirlas y rotarlas para que cuelguen visibles.
        FixProp("BannerL", new Vector3(-4.0f, 6.2f, 7.0f), new Vector3(0f, 180f, 0f), new Vector3(1.8f, 1.8f, 1.8f));
        FixProp("BannerR", new Vector3(5.8f, 6.5f, 7.4f), new Vector3(0f, 0f, 0f), new Vector3(1.9f, 1.9f, 1.9f));

        // Antorchas un poco más al frente y con fuego local.
        FixProp("TorchL", new Vector3(-3.4f, 2.0f, 5.8f), new Vector3(0f, 40f, 0f), Vector3.one * 1.8f);
        FixProp("TorchR", new Vector3(5.1f, 2.2f, 6.1f), new Vector3(0f, -38f, 0f), Vector3.one * 1.85f);
        FixFire("TorchL");
        FixFire("TorchR");

        // Luces de relleno en columnas para que no se pierdan en la niebla.
        EnsureFillLight("ColumnL", new Vector3(-4.8f, 3.5f, 7.0f), new Color(1f, 0.55f, 0.25f), 2.2f, 7f);
        EnsureFillLight("ColumnR", new Vector3(6.8f, 3.7f, 7.4f), new Color(1f, 0.55f, 0.25f), 2.0f, 7f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Fixed banners/torches/column lights.";
    }

    static void FixProp(string name, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = GameObject.Find("/MenuDepth/" + name);
        if (go == null)
            return;
        Undo.RecordObject(go.transform, "Fix menu prop");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        EditorUtility.SetDirty(go);
    }

    static void FixFire(string torchName)
    {
        var torch = GameObject.Find("/MenuDepth/" + torchName);
        if (torch == null)
            return;

        foreach (Transform child in torch.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.Contains("Fire"))
                continue;
            Undo.RecordObject(child, "Fix torch fire");
            child.localPosition = new Vector3(0f, 0.55f, 0f);
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            var ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            EditorUtility.SetDirty(child.gameObject);
        }
    }

    static void EnsureFillLight(string parentName, Vector3 worldPos, Color color, float intensity, float range)
    {
        var parent = GameObject.Find("/MenuDepth/" + parentName);
        if (parent == null)
            return;

        var existing = parent.transform.Find("FillLight");
        Light light;
        if (existing != null)
        {
            light = existing.GetComponent<Light>();
        }
        else
        {
            var go = new GameObject("FillLight");
            go.transform.SetParent(parent.transform, true);
            light = go.AddComponent<Light>();
        }

        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.transform.position = worldPos;
        EditorUtility.SetDirty(light);
    }
}

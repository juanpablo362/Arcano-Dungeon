using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BoostMainMenuLook
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";

    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Empujar paredes traseras para más profundidad.
        Push("CastleRuins/BackLeft", new Vector3(-5.2f, 2.1f, 9.8f), 1.35f);
        Push("CastleRuins/BackTall", new Vector3(-1.0f, 3.2f, 10.4f), 1.4f);
        Push("CastleRuins/BackGapCap", new Vector3(1.9f, 1.4f, 10.1f), 1.25f);
        Push("CastleRuins/BackRight", new Vector3(5.2f, 3.0f, 10.3f), 1.35f);
        Push("CastleRuins/BackEnd", new Vector3(8.4f, 1.3f, 9.6f), 1.2f);

        // Props de profundidad entre altar y muro lejano.
        SetDepth("ColumnL", new Vector3(-4.8f, 0f, 7.4f), new Vector3(0f, 20f, 0f), new Vector3(1.6f, 2.1f, 1.6f));
        SetDepth("ColumnR", new Vector3(6.8f, 0f, 7.8f), new Vector3(0f, -16f, 0f), new Vector3(1.7f, 2.2f, 1.7f));
        SetDepth("ColumnFarL", new Vector3(-2.6f, 0f, 9.2f), new Vector3(0f, 10f, 0f), new Vector3(1.25f, 1.7f, 1.25f));
        SetDepth("ColumnFarR", new Vector3(4.2f, 0f, 9.5f), new Vector3(0f, -18f, 0f), new Vector3(1.3f, 1.8f, 1.3f));
        SetDepth("BannerL", new Vector3(-4.2f, 2.9f, 7.1f), new Vector3(0f, 28f, 0f), new Vector3(1.6f, 1.7f, 1.6f));
        SetDepth("BannerR", new Vector3(5.9f, 3.1f, 7.5f), new Vector3(0f, -32f, 0f), new Vector3(1.7f, 1.8f, 1.7f));
        SetDepth("TorchL", new Vector3(-3.7f, 1.7f, 6.6f), new Vector3(0f, 40f, 0f), Vector3.one * 1.45f);
        SetDepth("TorchR", new Vector3(5.4f, 1.9f, 6.9f), new Vector3(0f, -38f, 0f), Vector3.one * 1.5f);

        BoostLight("TorchLightL", 4.5f, 12f);
        BoostLight("TorchLightR", 4.2f, 12.5f);

        var backdrop = GameObject.Find("/MenuBackdrop");
        if (backdrop != null)
        {
            Undo.RecordObject(backdrop.transform, "Backdrop depth");
            backdrop.transform.position = new Vector3(1.2f, 3.4f, 16.5f);
            backdrop.transform.localScale = new Vector3(44f, 24f, 1f);
            EditorUtility.SetDirty(backdrop);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.032f;
        RenderSettings.fogColor = new Color(0.018f, 0.012f, 0.038f, 1f);

        StrengthenButtons();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Boosted menu depth + button contrast.";
    }

    static void Push(string path, Vector3 pos, float scaleMul)
    {
        var go = GameObject.Find(path);
        if (go == null)
            return;
        Undo.RecordObject(go.transform, "Push back wall");
        go.transform.position = pos;
        go.transform.localScale *= scaleMul;
        EditorUtility.SetDirty(go);
    }

    static void SetDepth(string name, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = GameObject.Find("/MenuDepth/" + name);
        if (go == null)
            return;
        Undo.RecordObject(go.transform, "Set depth prop");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        EditorUtility.SetDirty(go);
    }

    static void BoostLight(string name, float intensity, float range)
    {
        foreach (var light in Object.FindObjectsByType<Light>())
        {
            if (light.name != name)
                continue;
            Undo.RecordObject(light, "Boost torch");
            light.intensity = intensity;
            light.range = range;
            light.color = new Color(1f, 0.58f, 0.25f);
            EditorUtility.SetDirty(light);
        }
    }

    static void StrengthenButtons()
    {
        var panel = GameObject.Find("/Canvas/MenuPanel");
        if (panel == null)
            return;

        foreach (Transform child in panel.transform)
        {
            var row = child.GetComponent<MenuButtonRow>();
            var fill = child.Find("Fill")?.GetComponent<Image>();
            var frame = child.Find("Frame")?.GetComponent<Image>();
            var glow = child.Find("Glow")?.GetComponent<Image>();
            var rootImg = child.GetComponent<Image>();

            if (rootImg != null)
            {
                rootImg.color = new Color(0.04f, 0.03f, 0.025f, 0.35f);
                EditorUtility.SetDirty(rootImg);
            }

            if (fill != null)
            {
                fill.color = new Color(0.10f, 0.08f, 0.05f, 0.88f);
                Stretch(fill.rectTransform, 5f, 5f);
                EditorUtility.SetDirty(fill);
            }

            if (frame != null)
            {
                frame.color = new Color(0.78f, 0.58f, 0.24f, 0.82f);
                Stretch(frame.rectTransform, 0f, 0f);
                EditorUtility.SetDirty(frame);
            }

            if (glow != null)
            {
                glow.color = new Color(1f, 0.8f, 0.35f, 0f);
                Stretch(glow.rectTransform, -12f, -8f);
                EditorUtility.SetDirty(glow);
            }

            if (row != null)
            {
                Undo.RecordObject(row, "Stronger button colors");
                row.fillNormal = new Color(0.10f, 0.08f, 0.05f, 0.88f);
                row.fillActive = new Color(0.28f, 0.17f, 0.05f, 0.95f);
                row.frameNormal = new Color(0.72f, 0.52f, 0.22f, 0.78f);
                row.frameActive = new Color(1f, 0.84f, 0.42f, 1f);
                row.glowColor = new Color(1f, 0.82f, 0.4f, 0.55f);
                row.fill = fill;
                row.frame = frame;
                row.glow = glow;
                EditorUtility.SetDirty(row);
            }
        }
    }

    static void Stretch(RectTransform rt, float insetX, float insetY)
    {
        if (rt == null)
            return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(insetX, insetY);
        rt.offsetMax = new Vector2(-insetX, -insetY);
    }
}

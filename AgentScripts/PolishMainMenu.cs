using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class PolishMainMenu
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";
    const string UiFolder = "Assets/_ArcanoDungeon/Art/UI/MainMenu";
    const string ColumnPrefab = "Assets/Gridness Studios/Elementary Dungeon Pack Lite/Prefabs/Column_Half.prefab";
    const string TorchPrefab = "Assets/Gridness Studios/Elementary Dungeon Pack Lite/Prefabs/Torch.001.prefab";
    const string FlagPrefab = "Assets/Gridness Studios/Elementary Dungeon Pack Lite/Prefabs/Flag.004.prefab";

    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureUiSprites(out Sprite fillSprite, out Sprite frameSprite, out Sprite glowSprite);
        StyleButtons(fillSprite, frameSprite, glowSprite);
        EnsureQuitButton();
        DressDepth();
        TuneAtmosphere();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "MainMenu polished: styled buttons, Quit/SALIR, depth props, fog.";
    }

    static void EnsureUiSprites(out Sprite fillSprite, out Sprite frameSprite, out Sprite glowSprite)
    {
        Directory.CreateDirectory(UiFolder.Replace('/', Path.DirectorySeparatorChar));
        fillSprite = EnsureSprite(UiFolder + "/ButtonFill.png", CreateFillTexture(), 8, 8, 8, 8);
        frameSprite = EnsureSprite(UiFolder + "/ButtonFrame.png", CreateFrameTexture(), 10, 10, 10, 10);
        glowSprite = EnsureSprite(UiFolder + "/ButtonGlow.png", CreateGlowTexture(), 24, 24, 24, 24);
    }

    static Sprite EnsureSprite(string path, Texture2D tex, int l, int b, int r, int t)
    {
        var abs = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllBytes(abs, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.spriteBorder = new Vector4(l, b, r, t);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Texture2D CreateFillTexture()
    {
        const int w = 64;
        const int h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float vy = y / (h - 1f);
            for (int x = 0; x < w; x++)
            {
                float vx = x / (w - 1f);
                // Piedra oscura que se aclara suavemente hacia la derecha.
                float shade = Mathf.Lerp(0.07f, 0.16f, Mathf.SmoothStep(0f, 1f, vx));
                shade += (vy - 0.5f) * 0.03f;
                float a = Mathf.Lerp(0.88f, 0.42f, Mathf.SmoothStep(0.15f, 1f, vx));
                tex.SetPixel(x, y, new Color(shade * 1.05f, shade * 0.9f, shade * 0.7f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateFrameTexture()
    {
        const int s = 64;
        const int border = 6;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            bool edge = x < border || y < border || x >= s - border || y >= s - border;
            if (!edge)
            {
                tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                continue;
            }

            float edgeT = 1f;
            if (x < border) edgeT = Mathf.Min(edgeT, x / (float)border);
            if (y < border) edgeT = Mathf.Min(edgeT, y / (float)border);
            if (x >= s - border) edgeT = Mathf.Min(edgeT, (s - 1 - x) / (float)border);
            if (y >= s - border) edgeT = Mathf.Min(edgeT, (s - 1 - y) / (float)border);

            float gold = Mathf.Lerp(0.35f, 0.95f, edgeT);
            tex.SetPixel(x, y, new Color(0.72f * gold, 0.55f * gold, 0.22f * gold, 0.92f));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D CreateGlowTexture()
    {
        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float nx = Mathf.Abs(x - c.x) / c.x;
            float ny = Mathf.Abs(y - c.y) / c.y;
            // Glow tipo “marco suave” más fuerte en bordes horizontales.
            float edge = Mathf.Max(nx, ny);
            float a = Mathf.Clamp01(Mathf.Pow(edge, 2.2f) * (1f - Mathf.Pow(edge, 6f)) * 1.8f);
            tex.SetPixel(x, y, new Color(1f, 0.82f, 0.4f, a));
        }
        tex.Apply();
        return tex;
    }

    static void StyleButtons(Sprite fillSprite, Sprite frameSprite, Sprite glowSprite)
    {
        var panel = GameObject.Find("/Canvas/MenuPanel");
        if (panel == null)
            throw new System.Exception("MenuPanel not found");

        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(540f, 430f);

        foreach (Transform child in panel.transform)
            StyleButton(child.gameObject, fillSprite, frameSprite, glowSprite);
    }

    static void StyleButton(GameObject buttonGo, Sprite fillSprite, Sprite frameSprite, Sprite glowSprite)
    {
        var image = buttonGo.GetComponent<Image>();
        if (image != null)
        {
            // Fondo base casi transparente; el Fill aporta el degradado.
            image.color = new Color(0.05f, 0.04f, 0.03f, 0.18f);
            image.sprite = null;
            image.type = Image.Type.Simple;
        }

        var fill = EnsureChildImage(buttonGo.transform, "Fill");
        fill.sprite = fillSprite;
        fill.type = Image.Type.Sliced;
        fill.color = new Color(0.08f, 0.07f, 0.06f, 0.72f);
        fill.raycastTarget = false;
        Stretch(fill.rectTransform, 4f, 4f);

        var frame = EnsureChildImage(buttonGo.transform, "Frame");
        frame.sprite = frameSprite;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.55f, 0.42f, 0.22f, 0.55f);
        frame.raycastTarget = false;
        Stretch(frame.rectTransform, 0f, 0f);

        var glow = EnsureChildImage(buttonGo.transform, "Glow");
        glow.sprite = glowSprite;
        glow.type = Image.Type.Sliced;
        glow.color = new Color(1f, 0.78f, 0.35f, 0f);
        glow.raycastTarget = false;
        glow.enabled = false;
        Stretch(glow.rectTransform, -10f, -6f);

        // Detrás del highlight/texto existente.
        fill.transform.SetSiblingIndex(0);
        frame.transform.SetSiblingIndex(1);
        glow.transform.SetSiblingIndex(2);

        var row = buttonGo.GetComponent<MenuButtonRow>();
        if (row != null)
        {
            Undo.RecordObject(row, "Wire polished button visuals");
            row.fill = fill;
            row.frame = frame;
            row.glow = glow;
            row.fillNormal = new Color(0.08f, 0.07f, 0.06f, 0.72f);
            row.fillActive = new Color(0.20f, 0.13f, 0.05f, 0.86f);
            row.frameNormal = new Color(0.55f, 0.42f, 0.22f, 0.55f);
            row.frameActive = new Color(0.95f, 0.78f, 0.38f, 0.95f);
            row.glowColor = new Color(1f, 0.78f, 0.35f, 0.38f);
            EditorUtility.SetDirty(row);
        }

        // Highlight existente más sutil y dorado.
        var highlight = buttonGo.transform.Find("Highlight");
        if (highlight != null)
        {
            var hi = highlight.GetComponent<Image>();
            if (hi != null)
            {
                hi.color = new Color(1f, 0.82f, 0.4f, 0.16f);
                EditorUtility.SetDirty(hi);
            }
        }
    }

    static Image EnsureChildImage(Transform parent, string name)
    {
        var existing = parent.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
        }

        return go.GetComponent<Image>();
    }

    static void Stretch(RectTransform rt, float insetX, float insetY)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(insetX, insetY);
        rt.offsetMax = new Vector2(-insetX, -insetY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    static void EnsureQuitButton()
    {
        var panel = GameObject.Find("/Canvas/MenuPanel");
        var options = panel.transform.Find("Options");
        if (options == null)
            throw new System.Exception("Options button missing");

        var existing = panel.transform.Find("Quit");
        GameObject quitGo;
        if (existing != null)
        {
            quitGo = existing.gameObject;
        }
        else
        {
            quitGo = Object.Instantiate(options.gameObject, panel.transform);
            quitGo.name = "Quit";
            quitGo.transform.SetAsLastSibling();
        }

        var label = quitGo.transform.Find("Label");
        if (label != null)
        {
            var text = label.GetComponent<Text>();
            if (text != null)
            {
                text.text = "SALIR";
                EditorUtility.SetDirty(text);
            }
        }

        var button = quitGo.GetComponent<Button>();
        button.interactable = true;
        button.onClick = new Button.ButtonClickedEvent();

        var controller = GameObject.Find("/Canvas").GetComponent<MainMenuController>();
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, controller.QuitGame);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(quitGo);
    }

    static void DressDepth()
    {
        var root = GameObject.Find("/MenuDepth");
        if (root != null)
            Object.DestroyImmediate(root);

        root = new GameObject("MenuDepth");
        Undo.RegisterCreatedObjectUndo(root, "Create MenuDepth");

        var column = AssetDatabase.LoadAssetAtPath<GameObject>(ColumnPrefab);
        var torch = AssetDatabase.LoadAssetAtPath<GameObject>(TorchPrefab);
        var flag = AssetDatabase.LoadAssetAtPath<GameObject>(FlagPrefab);

        // Columnas lejanas (más atrás que las paredes actuales ~z=6.7).
        Place(column, root.transform, "ColumnL", new Vector3(-6.2f, 0f, 11.5f), new Vector3(0f, 18f, 0f), new Vector3(1.4f, 1.8f, 1.4f));
        Place(column, root.transform, "ColumnR", new Vector3(8.4f, 0f, 12.2f), new Vector3(0f, -12f, 0f), new Vector3(1.5f, 2.0f, 1.5f));
        Place(column, root.transform, "ColumnFarL", new Vector3(-3.8f, 0f, 14.8f), new Vector3(0f, 8f, 0f), new Vector3(1.1f, 1.5f, 1.1f));
        Place(column, root.transform, "ColumnFarR", new Vector3(5.5f, 0f, 15.4f), new Vector3(0f, -20f, 0f), new Vector3(1.2f, 1.6f, 1.2f));

        // Estandartes.
        Place(flag, root.transform, "BannerL", new Vector3(-5.4f, 2.8f, 10.8f), new Vector3(0f, 25f, 0f), new Vector3(1.3f, 1.3f, 1.3f));
        Place(flag, root.transform, "BannerR", new Vector3(7.2f, 3.1f, 11.4f), new Vector3(0f, -30f, 0f), new Vector3(1.4f, 1.4f, 1.4f));

        // Antorchas lejanas con luz cálida.
        var torchL = Place(torch, root.transform, "TorchL", new Vector3(-4.8f, 1.6f, 9.8f), new Vector3(0f, 35f, 0f), Vector3.one * 1.2f);
        var torchR = Place(torch, root.transform, "TorchR", new Vector3(6.8f, 1.8f, 10.4f), new Vector3(0f, -40f, 0f), Vector3.one * 1.25f);
        AddTorchLight(torchL.transform, "TorchLightL", new Color(1f, 0.55f, 0.22f), 2.4f, 8f);
        AddTorchLight(torchR.transform, "TorchLightR", new Color(1f, 0.5f, 0.18f), 2.2f, 8.5f);

        // Empujar backdrop más lejos para escala.
        var backdrop = GameObject.Find("/MenuBackdrop");
        if (backdrop != null)
        {
            Undo.RecordObject(backdrop.transform, "Push menu backdrop");
            backdrop.transform.position = new Vector3(1.2f, 3.2f, 18.5f);
            backdrop.transform.localScale = new Vector3(42f, 22f, 1f);
            EditorUtility.SetDirty(backdrop);
        }
    }

    static GameObject Place(GameObject prefab, Transform parent, string name, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        if (prefab == null)
            throw new System.Exception("Missing prefab for " + name);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        return go;
    }

    static void AddTorchLight(Transform parent, string name, Color color, float intensity, float range)
    {
        var lightGo = new GameObject(name);
        lightGo.transform.SetParent(parent, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    static void TuneAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.015f, 0.01f, 0.035f, 1f);
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.045f, 0.07f, 1f);

        var ambient = GameObject.Find("/Canvas")?.GetComponent<MenuAmbientLighting>();
        if (ambient != null)
        {
            Undo.RecordObject(ambient, "Tune menu ambient");
            ambient.backdropTint = new Color(0.38f, 0.36f, 0.48f, 1f);
            ambient.backdropMin = 0.35f;
            ambient.backdropMax = 0.75f;
            ambient.lightMin = 8f;
            ambient.lightMax = 12f;
            ambient.lightRange = 7.2f;
            EditorUtility.SetDirty(ambient);
        }
    }
}

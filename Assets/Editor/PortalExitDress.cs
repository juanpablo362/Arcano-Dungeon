using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PortalExitDress
{
    const string ArtDir = "Assets/_ArcanoDungeon/Art/Portal";
    const string Level1 = "Assets/_ArcanoDungeon/Scenes/Level1.unity";
    const string Catacombs = "Assets/_ArcanoDungeon/Scenes/CatacombsLevel.unity";

    [MenuItem("Tools/Arcano/Dress Exit Portals", false, 132)]
    public static void DressAll()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Portal] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        EnsureAssets();
        DressScene(Level1);
        DressScene(Catacombs);
        Debug.Log("[Portal] Portales del final con textura y efectos.");
    }

    public static void EnsureAssets()
    {
        Directory.CreateDirectory(ArtDir);
        WriteTexture(ArtDir + "/PortalSwirl.png", Swirl);
        WriteTexture(ArtDir + "/PortalRunes.png", Runes);
        WriteTexture(ArtDir + "/PortalCircle.png", CircleRunes);
        WriteTexture(ArtDir + "/PortalSpark.png", Spark);
        AssetDatabase.Refresh();
        MarkSpriteLike(ArtDir + "/PortalSwirl.png", true);
        MarkSpriteLike(ArtDir + "/PortalRunes.png", true);
        MarkSpriteLike(ArtDir + "/PortalCircle.png", true);
        MarkSpriteLike(ArtDir + "/PortalSpark.png", false);

        var swirl = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/PortalSwirl.png");
        var runes = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/PortalRunes.png");
        var circle = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/PortalCircle.png");
        var spark = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/PortalSpark.png");
        var shader = Shader.Find("Arcano/PortalSwirl");
        if (shader == null)
            throw new System.Exception("No compiló el shader Arcano/PortalSwirl.");

        SaveMesh("PortalDisc.asset", Disc(1f, 48));
        SaveMesh("PortalBand.asset", Band(1.02f, 1.2f, 56));

        var energy = Material(shader, "Mat_PortalSwirl.mat", swirl, new Color(0.45f, 0.95f, 1f, 1f), 0.85f, 2.4f, 0f);
        var glow = Material(shader, "Mat_PortalGlow.mat", swirl, new Color(0.25f, 0.55f, 1f, 0.55f), -0.35f, 1.3f, 0f);
        var ring = Material(shader, "Mat_PortalRunes.mat", runes, new Color(0.7f, 1f, 1f, 1f), 0.12f, 2.8f, 1f);
        Material(shader, "Mat_PortalCircle.mat", circle, new Color(0.65f, 1f, 1f, 1f), 0.22f, 2.2f, 0f);
        ParticleMaterial(spark);
        EditorUtility.SetDirty(energy);
        EditorUtility.SetDirty(glow);
        EditorUtility.SetDirty(ring);
        AssetDatabase.SaveAssets();
    }

    static void DressScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var exits = Object.FindObjectsByType<LevelExit>(FindObjectsInactive.Include);
        var count = 0;
        foreach (var exit in exits)
        {
            if (exit == null)
                continue;
            Dress(exit.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Portal] " + scenePath + " portales=" + count);
    }

    public static void DressExit(GameObject exit)
    {
        EnsureAssets();
        Dress(exit);
    }

    static void Dress(GameObject exit)
    {
        for (int i = exit.transform.childCount - 1; i >= 0; i--)
        {
            var child = exit.transform.GetChild(i);
            if (child.name == "PortalDress")
                Object.DestroyImmediate(child.gameObject);
        }

        foreach (var oldName in new[] { "PortalBase", "PortalRing", "PortalEnergy" })
        {
            var old = exit.transform.Find(oldName);
            if (old != null)
                old.gameObject.SetActive(false);
        }

        bool crypt = exit.name == "CryptExit";
        var dress = new GameObject("PortalDress");
        dress.transform.SetParent(exit.transform, false);
        dress.transform.localPosition = crypt ? new Vector3(0f, -1.15f, -0.4f) : Vector3.zero;
        dress.transform.localRotation = Quaternion.identity;
        dress.transform.localScale = Vector3.one;

        var dungeon = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Gridness Studios/Elementary Dungeon Pack Lite/Materials/Mat_Dungeon.mat");
        var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plinth.name = "Plinth";
        Object.DestroyImmediate(plinth.GetComponent<Collider>());
        plinth.transform.SetParent(dress.transform, false);
        plinth.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        plinth.transform.localScale = new Vector3(2.35f, 0.05f, 2.35f);
        if (dungeon != null)
            plinth.GetComponent<MeshRenderer>().sharedMaterial = dungeon;

        var sealMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ArtDir + "/PortalDisc.asset");
        var bandMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ArtDir + "/PortalBand.asset");
        var sealMat = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Mat_PortalRunes.mat");
        var circleMat = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Mat_PortalCircle.mat");
        var swirlMat = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Mat_PortalSwirl.mat");
        var glowMat = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Mat_PortalGlow.mat");

        var seal = Surface("FloorSeal", sealMesh, circleMat, dress.transform);
        seal.transform.localPosition = new Vector3(0f, 0.09f, 0f);
        seal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        seal.transform.localScale = Vector3.one * 1.45f;

        var face = new GameObject("Face");
        face.transform.SetParent(dress.transform, false);
        face.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        face.transform.localScale = new Vector3(1.2f, 1.6f, 1f);

        Surface("Glow", sealMesh, glowMat, face.transform);
        Surface("Swirl", sealMesh, swirlMat, face.transform);
        var ring = Surface("RuneRing", bandMesh, sealMat, face.transform);

        var sparks = new GameObject("Sparks");
        sparks.transform.SetParent(face.transform, false);
        var ps = sparks.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 1f, 1f, 0.9f),
            new Color(0.75f, 0.55f, 1f, 0.8f));
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = 28f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.92f;
        shape.radiusThickness = 0.15f;
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.6f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.8f, 0.5f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOver.color = gradient;
        var renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtDir + "/Mat_PortalSpark.mat");
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Light light = null;
        var existing = exit.transform.Find("PortalLight");
        if (existing != null)
            light = existing.GetComponent<Light>();
        if (light == null)
        {
            var lightGo = new GameObject("PortalLight");
            lightGo.transform.SetParent(dress.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.7f, -0.3f);
            light = lightGo.AddComponent<Light>();
        }
        light.type = LightType.Point;
        light.color = new Color(0.35f, 0.9f, 1f);
        light.intensity = crypt ? 3.2f : 6f;
        light.range = crypt ? 8f : 12f;
        light.shadows = LightShadows.None;

        var visual = dress.AddComponent<PortalVisual>();
        visual.runeRing = ring.transform;
        visual.glow = light;
        visual.runeSpeed = 26f;
    }

    static GameObject Surface(string name, Mesh mesh, Material material, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go;
    }

    static Material Material(Shader shader, string file, Texture2D texture, Color color, float spin, float power, float mode)
    {
        string path = ArtDir + "/" + file;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetTexture("_MainTex", texture);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Spin", spin);
        mat.SetFloat("_Power", power);
        mat.SetFloat("_Mode", mode);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ParticleMaterial(Texture2D spark)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        string path = ArtDir + "/Mat_PortalSpark.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetTexture("_BaseMap", spark);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(mat);
    }

    static void SaveMesh(string file, Mesh mesh)
    {
        string path = ArtDir + "/" + file;
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
    }

    static Mesh Disc(float radius, int segments)
    {
        var mesh = new Mesh { name = "PortalDisc" };
        var vertices = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
        var triangles = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            vertices.Add(new Vector3(x, y, 0f) * radius);
            uvs.Add(new Vector2(0.5f + x * 0.5f, 0.5f + y * 0.5f));
        }
        for (int i = 1; i <= segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh Band(float inner, float outer, int segments)
    {
        var mesh = new Mesh { name = "PortalBand" };
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            float u = i / (float)segments;
            vertices.Add(new Vector3(x, y, 0f) * inner);
            vertices.Add(new Vector3(x, y, 0f) * outer);
            uvs.Add(new Vector2(u, 0f));
            uvs.Add(new Vector2(u, 1f));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            triangles.Add(a);
            triangles.Add(a + 1);
            triangles.Add(a + 2);
            triangles.Add(a + 1);
            triangles.Add(a + 3);
            triangles.Add(a + 2);
        }
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void WriteTexture(string path, System.Func<int, int, int, Color> paint)
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = paint(x, y, size);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static Color Swirl(int x, int y, int size)
    {
        float u = (x + 0.5f) / size - 0.5f;
        float v = (y + 0.5f) / size - 0.5f;
        float dist = Mathf.Sqrt(u * u + v * v) * 2f;
        float angle = Mathf.Atan2(v, u);
        float arm = Mathf.Repeat(angle / (Mathf.PI * 2f) * 5f + dist * 3.2f, 1f);
        float streak = Mathf.Pow(Mathf.Abs(Mathf.Sin(arm * Mathf.PI)), 0.45f);
        float core = Mathf.Clamp01(1f - dist * 0.9f);
        float rim = Mathf.SmoothStep(0.55f, 0.86f, dist) * (1f - Mathf.SmoothStep(0.9f, 1.02f, dist));
        var deep = new Color(0.02f, 0.04f, 0.12f, 0.45f);
        var cyan = new Color(0.55f, 1f, 1f, 1f);
        var violet = new Color(0.62f, 0.28f, 1f, 0.9f);
        var color = Color.Lerp(deep, violet, core * 0.8f);
        color = Color.Lerp(color, cyan, Mathf.Clamp01(streak * 0.85f + rim));
        color.a = Mathf.Clamp01(0.2f + core * 0.45f + streak * 0.7f + rim);
        if (dist > 1f)
            color.a = 0f;
        return color;
    }

    static Color Runes(int x, int y, int size)
    {
        float u = (x + 0.5f) / size;
        float v = (y + 0.5f) / size;
        float along = Mathf.Repeat(u * 8f, 1f);
        float band = Mathf.SmoothStep(0.08f, 0.22f, v) * (1f - Mathf.SmoothStep(0.78f, 0.94f, v));
        float glyph = 0f;
        if (along > 0.18f && along < 0.32f && v > 0.28f && v < 0.72f)
            glyph = 1f;
        if (along > 0.48f && along < 0.78f && v > 0.4f && v < 0.58f)
            glyph = 1f;
        if (Mathf.Abs(along - 0.62f) < 0.03f && v > 0.25f && v < 0.75f)
            glyph = 1f;
        float dots = Mathf.Abs(Mathf.Repeat(u * 24f, 1f) - 0.5f) < 0.08f && Mathf.Abs(v - 0.5f) < 0.06f ? 1f : 0f;
        var stone = new Color(0.16f, 0.14f, 0.18f, band);
        var glow = new Color(0.65f, 1f, 1f, 1f);
        var color = Color.Lerp(stone, glow, Mathf.Clamp01(glyph + dots));
        color.a = Mathf.Clamp01(band * 0.85f + glyph + dots);
        return color;
    }

    static Color CircleRunes(int x, int y, int size)
    {
        float u = (x + 0.5f) / size - 0.5f;
        float v = (y + 0.5f) / size - 0.5f;
        float dist = Mathf.Sqrt(u * u + v * v) * 2f;
        float around = Mathf.Repeat(Mathf.Atan2(v, u) / (Mathf.PI * 2f) * 12f, 1f);
        float ring = Mathf.SmoothStep(0.58f, 0.7f, dist) * (1f - Mathf.SmoothStep(0.86f, 0.98f, dist));
        float glyph = (around > 0.16f && around < 0.3f) || Mathf.Abs(around - 0.55f) < 0.045f ? 1f : 0f;
        float dots = Mathf.Abs(Mathf.Repeat(around * 2.2f, 1f) - 0.5f) < 0.07f ? 1f : 0f;
        var stone = new Color(0.1f, 0.08f, 0.14f, ring * 0.8f);
        var glow = new Color(0.6f, 1f, 1f, 1f);
        var color = Color.Lerp(stone, glow, Mathf.Clamp01((glyph + dots * 0.7f) * ring * 2f));
        color.a = Mathf.Clamp01(ring * 0.7f + glyph * ring);
        return color;
    }

    static Color Spark(int x, int y, int size)
    {
        float u = (x + 0.5f) / size - 0.5f;
        float v = (y + 0.5f) / size - 0.5f;
        float dist = Mathf.Sqrt(u * u + v * v) * 2f;
        float alpha = Mathf.Clamp01(1f - dist);
        alpha *= alpha;
        return new Color(1f, 1f, 1f, alpha);
    }

    static void MarkSpriteLike(string path, bool repeat)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.sRGBTexture = true;
        importer.SaveAndReimport();
    }
}

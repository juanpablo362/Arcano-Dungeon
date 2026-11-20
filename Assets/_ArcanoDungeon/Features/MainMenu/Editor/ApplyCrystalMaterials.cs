using UnityEditor;
using UnityEngine;

static class ApplyCrystalMaterials
{
    const string CrystalPath = "Assets/_ArcanoDungeon/Art/Environments/Castle/Corazon_Arcano.fbx";
    const string BodyPath = "Assets/_ArcanoDungeon/Art/Materials/Resources/CrystalBody.mat";
    const string GlowPath = "Assets/_ArcanoDungeon/Art/Materials/Resources/CrystalGlow.mat";
    const string GoldPath = "Assets/_ArcanoDungeon/Art/Materials/Resources/CrystalGold.mat";

    [MenuItem("Arcano/Reparar materiales del cristal")]
    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        var body = AssetDatabase.LoadAssetAtPath<Material>(BodyPath);
        var glow = AssetDatabase.LoadAssetAtPath<Material>(GlowPath);
        var gold = AssetDatabase.LoadAssetAtPath<Material>(GoldPath);
        if (body == null || glow == null || gold == null)
            return;

        // Repara shaders rotos recreando desde el material por defecto URP.
        Repair(body, new Color(0.25f, 0.06f, 0.52f), new Color(0.55f, 0.12f, 1.1f), 0.92f, 0.05f, false);
        Repair(glow, new Color(0.62f, 0.22f, 1f), new Color(1.4f, 0.35f, 2.2f), 0.7f, 0f, false);
        Repair(gold, new Color(0.62f, 0.44f, 0.14f), new Color(0.08f, 0.03f, 0.01f), 0.35f, 0.55f, true);

        var crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPath);
        if (crystalPrefab == null)
            return;

        foreach (var renderer in crystalPrefab.GetComponentsInChildren<Renderer>(true))
        {
            var name = renderer.gameObject.name;
            var material = body;
            if (name.Contains("InnerGlow"))
                material = glow;
            else if (name.Contains("Gold") || name.Contains("Orbit"))
                material = gold;

            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        var sceneCrystal = GameObject.Find("Crystal");
        if (sceneCrystal != null)
        {
            CrystalMaterials.Apply(sceneCrystal);
            EditorUtility.SetDirty(sceneCrystal);
        }

        AssetDatabase.SaveAssets();
    }

    static void Repair(Material material, Color baseColor, Color emission, float smoothness, float metallic, bool isGold)
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        var shader = pipeline != null && pipeline.defaultMaterial != null
            ? pipeline.defaultMaterial.shader
            : Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            return;

        if (material.shader != shader)
            material.shader = shader;

        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetColor("_EmissionColor", emission);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        if (isGold)
        {
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/LowlyPoly/Free Stylized Textures/Textures/Vol_18_2_Metals_Normal.png");
            if (normal != null)
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.7f);
                material.SetFloat("_SpecularHighlights", 0f);
                material.SetFloat("_EnvironmentReflections", 0f);
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
        }

        EditorUtility.SetDirty(material);
    }
}

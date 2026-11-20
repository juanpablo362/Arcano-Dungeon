using UnityEngine;
using UnityEngine.Rendering;

public static class CrystalMaterials
{
    static Material body;
    static Material glow;
    static Material gold;

    public static void Apply(GameObject crystal)
    {
        EnsureMaterials();
        if (body == null)
            return;

        foreach (var renderer in crystal.GetComponentsInChildren<Renderer>(true))
        {
            var name = renderer.gameObject.name;
            if (name.Contains("InnerGlow"))
                renderer.sharedMaterial = glow;
            else if (name.Contains("Gold") || name.Contains("Orbit"))
                renderer.sharedMaterial = gold;
            else
                renderer.sharedMaterial = body;
        }
    }

    static void EnsureMaterials()
    {
        if (body != null)
            return;

        body = Create(
            "CrystalBody",
            new Color(0.25f, 0.06f, 0.52f),
            new Color(0.55f, 0.12f, 1.1f),
            0.92f,
            0.05f);

        glow = Create(
            "CrystalGlow",
            new Color(0.62f, 0.22f, 1f),
            new Color(1.4f, 0.35f, 2.2f),
            0.7f,
            0f);

        gold = Create(
            "CrystalGold",
            new Color(0.62f, 0.44f, 0.14f),
            new Color(0.08f, 0.03f, 0.01f),
            0.35f,
            0.55f);
    }

    static Material Create(string name, Color baseColor, Color emission, float smoothness, float metallic)
    {
        var fromResources = Resources.Load<Material>(name);
        if (fromResources != null &&
            fromResources.shader != null &&
            fromResources.shader.name != "Hidden/InternalErrorShader")
            return fromResources;

        var shader = ResolveUrpLit();
        if (shader == null)
        {
            Debug.LogError("No se pudo resolver el shader URP Lit para el cristal.");
            return null;
        }

        var material = new Material(shader)
        {
            name = name + " (Runtime)"
        };
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetColor("_EmissionColor", emission);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return material;
    }

    static Shader ResolveUrpLit()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null && pipeline.defaultMaterial != null && pipeline.defaultMaterial.shader != null)
            return pipeline.defaultMaterial.shader;

        return Shader.Find("Universal Render Pipeline/Lit")
               ?? Shader.Find("Universal Render Pipeline/Simple Lit");
    }
}

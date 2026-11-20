using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ToneDownCrystal
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";
    const string BodyPath = "Assets/_ArcanoDungeon/Art/Materials/Resources/CrystalBody.mat";
    const string GlowPath = "Assets/_ArcanoDungeon/Art/Materials/Resources/CrystalGlow.mat";
    const string GoldPath = "Assets/_ArcanoDungeon/Art/Materials/Resources/CrystalGold.mat";
    const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Emisión ~45-55% y menos brillo especular/reflejos.
        Soften(BodyPath,
            new Color(0.22f, 0.06f, 0.45f),
            new Color(0.28f, 0.07f, 0.55f),
            smoothness: 0.58f,
            metallic: 0.04f);

        Soften(GlowPath,
            new Color(0.48f, 0.18f, 0.82f),
            new Color(0.55f, 0.16f, 0.95f),
            smoothness: 0.42f,
            metallic: 0f);

        Soften(GoldPath,
            new Color(0.72f, 0.52f, 0.16f),
            new Color(0.12f, 0.05f, 0.015f),
            smoothness: 0.48f,
            metallic: 0.72f);

        var light = GameObject.Find("CrystalLight")?.GetComponent<Light>();
        if (light != null)
        {
            Undo.RecordObject(light, "Tone crystal light");
            light.intensity = 5.5f;
            light.range = 6.2f;
            EditorUtility.SetDirty(light);
        }

        var ambient = GameObject.Find("/Canvas")?.GetComponent<MenuAmbientLighting>();
        if (ambient != null)
        {
            Undo.RecordObject(ambient, "Tone crystal ambient pulse");
            ambient.lightMin = 4.2f;
            ambient.lightMax = 6.5f;
            ambient.lightRange = 6.2f;
            EditorUtility.SetDirty(ambient);
        }

        // Bloom del volumen global: menos bloom = menos “flare” en anillos/cristal.
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile != null && profile.TryGet(out Bloom bloom))
        {
            Undo.RecordObject(profile, "Tone bloom");
            bloom.intensity.Override(Mathf.Min(bloom.intensity.value, 0.22f));
            bloom.threshold.Override(Mathf.Max(bloom.threshold.value, 1.05f));
            bloom.scatter.Override(0.55f);
            EditorUtility.SetDirty(profile);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Crystal/rings reflections and glow toned down.";
    }

    static void Soften(string path, Color baseColor, Color emission, float smoothness, float metallic)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
            return;

        Undo.RecordObject(mat, "Tone crystal material");
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color", baseColor);
        mat.SetColor("_EmissionColor", emission);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_EnvironmentReflections", 1f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
    }
}

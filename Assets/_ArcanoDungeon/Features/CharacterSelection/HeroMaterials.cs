using System.Collections.Generic;
using UnityEngine;

public static class HeroMaterials
{
    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

    public static void Apply(GameObject hero)
    {
        foreach (var renderer in hero.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = Match(renderer.gameObject.name);
    }

    static Material Match(string name)
    {
        var hero = name.StartsWith("Lyra") ? "Lyra" : name.StartsWith("Kaede") ? "Kaede" : "Elara";

        if (name.Contains("_Gem"))
            return Gem(hero);
        if (name.Contains("_Gold"))
            return Make("Gold", new Color(0.96f, 0.72f, 0.28f), 1f, 0.72f);
        if (name.Contains("_Blade"))
            return Make("Blade", new Color(0.82f, 0.86f, 0.92f), 1f, 0.82f);
        if (name.Contains("_Grip"))
            return Make("Grip", new Color(0.28f, 0.16f, 0.1f), 0f, 0.35f);
        if (name.Contains("_Scarf"))
            return Make("Scarf", new Color(0.72f, 0.08f, 0.1f), 0f, 0.42f, new Color(0.45f, 0.02f, 0.03f));
        if (name.Contains("_Cape"))
            return hero == "Elara"
                ? Make("CapeElara", new Color(0.12f, 0.28f, 0.72f), 0f, 0.4f)
                : Make("CapeLyra", new Color(0.28f, 0.14f, 0.48f), 0f, 0.4f);
        if (name.Contains("_Hair"))
            return Hair(hero);
        if (name.Contains("_Hat"))
            return Make("Hat", new Color(0.16f, 0.08f, 0.28f), 0.15f, 0.45f);
        if (name.Contains("_Armor") || (hero == "Elara" && name.Contains("_Boot")))
            return Make("Armor", new Color(0.74f, 0.78f, 0.84f), 1f, 0.68f);
        if (name.Contains("_Leather") || name.Contains("_Boot"))
            return Make("Leather", new Color(0.08f, 0.07f, 0.08f), 0.35f, 0.42f);
        if (name.Contains("_Cloth"))
            return hero == "Lyra"
                ? Make("ClothLyra", new Color(0.22f, 0.12f, 0.4f), 0.05f, 0.48f)
                : Make("ClothElara", new Color(0.22f, 0.4f, 0.82f), 0f, 0.45f);
        if (name.Contains("_Iris"))
            return Iris(hero);
        if (name.Contains("_Pupil"))
            return Make("Pupil", new Color(0.02f, 0.02f, 0.025f), 0f, 0.4f);
        if (name.Contains("_Catch"))
            return Make("Catch", Color.white, 0f, 0.2f, Color.white * 2f);
        if (name.Contains("_Sclera"))
            return Make("Sclera", new Color(0.95f, 0.96f, 0.98f), 0f, 0.35f);
        if (name.Contains("_Mouth"))
            return Make("Mouth", new Color(0.55f, 0.22f, 0.24f), 0f, 0.4f);
        if (name.Contains("_Brow"))
            return Hair(hero);
        if (name.Contains("_Skin"))
            return Make("Skin", new Color(0.93f, 0.76f, 0.68f), 0f, 0.42f);

        return Make("Skin", new Color(0.93f, 0.76f, 0.68f), 0f, 0.42f);
    }

    static Material Hair(string hero)
    {
        if (hero == "Lyra")
            return Make("HairLyra", new Color(0.62f, 0.48f, 0.9f), 0.05f, 0.45f);
        if (hero == "Kaede")
            return Make("HairKaede", new Color(0.06f, 0.05f, 0.06f), 0.1f, 0.4f);
        return Make("HairElara", new Color(0.86f, 0.88f, 0.93f), 0.08f, 0.42f);
    }

    static Material Iris(string hero)
    {
        if (hero == "Lyra")
            return Make("IrisLyra", new Color(0.62f, 0.35f, 0.95f), 0f, 0.3f, new Color(0.2f, 0.05f, 0.35f));
        if (hero == "Kaede")
            return Make("IrisKaede", new Color(0.85f, 0.08f, 0.1f), 0f, 0.25f, new Color(0.6f, 0.02f, 0.02f));
        return Make("IrisElara", new Color(0.25f, 0.5f, 0.95f), 0f, 0.3f, new Color(0.05f, 0.15f, 0.4f));
    }

    static Material Gem(string hero)
    {
        if (hero == "Lyra")
            return Make("GemLyra", new Color(0.7f, 0.4f, 1f), 0f, 0.85f, new Color(0.45f, 0.15f, 1f) * 2.2f);
        if (hero == "Kaede")
            return Make("GemKaede", new Color(1f, 0.2f, 0.15f), 0f, 0.7f, new Color(0.8f, 0.05f, 0.04f));
        return Make("GemElara", new Color(0.35f, 0.65f, 1f), 0f, 0.8f, new Color(0.15f, 0.4f, 1f) * 1.6f);
    }

    static Material Make(string key, Color color, float metallic, float smoothness, Color emission)
    {
        var cacheKey = key;
        if (Cache.TryGetValue(cacheKey, out var existing) && existing != null)
            return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader);
        material.name = key;
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (emission.maxColorComponent > 0.01f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        Cache[cacheKey] = material;
        return material;
    }

    static Material Make(string key, Color color, float metallic, float smoothness)
    {
        return Make(key, color, metallic, smoothness, Color.black);
    }
}

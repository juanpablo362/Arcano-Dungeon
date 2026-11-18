using UnityEngine;

public static class AltarMaterials
{
    public static void Apply(GameObject altar)
    {
        var stone = Require("AltarStone");
        var rune = Require("AltarRune");
        var star = Require("AltarStar");
        var wax = Require("AltarWax");
        var flame = Require("AltarFlame");
        var wick = Require("AltarWick");

        foreach (var t in altar.GetComponentsInChildren<Transform>(true))
        {
            var n = t.gameObject.name;
            if (n.Contains("Calaveras") || n.Contains("Ojos"))
                t.gameObject.SetActive(false);
        }

        foreach (var renderer in altar.GetComponentsInChildren<Renderer>(true))
        {
            var name = renderer.gameObject.name;
            if (name.Contains("Runas"))
                renderer.sharedMaterial = rune;
            else if (name.Contains("Pentagrama") || name.Contains("Circulo_pentagrama"))
                renderer.sharedMaterial = star;
            else if (name.Contains("Llamas"))
                renderer.sharedMaterial = flame;
            else if (name.Contains("Velas"))
                renderer.sharedMaterial = wax;
            else if (name.Contains("Mechas"))
                renderer.sharedMaterial = wick;
            else
                renderer.sharedMaterial = stone;
        }
    }

    static Material Require(string resourceName)
    {
        var material = Resources.Load<Material>(resourceName);
        if (material == null)
            Debug.LogError($"No se encontró Resources/{resourceName}.mat");
        return material;
    }
}

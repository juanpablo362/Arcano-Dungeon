using UnityEngine;

public enum HeroId
{
    Mage = 0,
    Knight = 1,
    Misaki = 2,
    Yuko = 3,
}

public sealed class HeroProfile
{
    public HeroId Id;
    public string Name;
    public string Title;
    public string Summary;
    public string Ability;
    public string[] Abilities;
    public string PrefabPath;
    public string PortraitPath;
    public float StageScale = 0.78f;
    public int Health;
    public int Attack;
    public int Defense;
    public int Speed;
    public int Crit;
    public Color Accent;
}

public static class HeroRoster
{
    public static readonly HeroProfile[] All =
    {
        new HeroProfile
        {
            Id = HeroId.Mage,
            Name = "Magus",
            Title = "Mago",
            Summary = "Maestro de las artes arcanas. Lanza bolas de fuego y controla el campo desde la distancia.",
            Ability = "Bola de fuego",
            Abilities = new[] { "staff", "spark", "ward", "shadow" },
            PrefabPath = "Assets/WizardPolyArt/Prefabs/PolyArtWizardStandardMat.prefab",
            PortraitPath = "",
            StageScale = 0.78f,
            Health = 5,
            Attack = 9,
            Defense = 4,
            Speed = 5,
            Crit = 4,
            Accent = new Color(0.72f, 0.48f, 1f),
        },
        new HeroProfile
        {
            Id = HeroId.Knight,
            Name = "Aldric",
            Title = "Caballero",
            Summary = "Guerrero de primera línea. Domina espada y escudo para proteger al grupo cuerpo a cuerpo.",
            Ability = "Golpe de escudo",
            Abilities = new[] { "shield", "swords", "ward", "spark" },
            PrefabPath = "Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPolyart.prefab",
            PortraitPath = "",
            StageScale = 0.78f,
            Health = 10,
            Attack = 6,
            Defense = 10,
            Speed = 4,
            Crit = 3,
            Accent = new Color(0.95f, 0.78f, 0.28f),
        },
        new HeroProfile
        {
            Id = HeroId.Misaki,
            Name = "Misaki",
            Title = "Duelista",
            Summary = "Ágil y letal. Combina movilidad con cortes precisos para abrir brechas en la defensa enemiga.",
            Ability = "Filo doble",
            Abilities = new[] { "swords", "spark", "shadow", "ward" },
            PrefabPath = "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume03 Misaki.prefab",
            PortraitPath = "",
            StageScale = 0.62f,
            Health = 7,
            Attack = 8,
            Defense = 5,
            Speed = 9,
            Crit = 7,
            Accent = new Color(0.95f, 0.42f, 0.55f),
        },
        new HeroProfile
        {
            Id = HeroId.Yuko,
            Name = "Yuko",
            Title = "Exploradora",
            Summary = "Rápida y astuta. Domina el terreno del dungeon y golpea desde ángulos inesperados.",
            Ability = "Paso sombra",
            Abilities = new[] { "shadow", "spark", "swords", "ward" },
            PrefabPath = "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume02 Yuko.prefab",
            PortraitPath = "",
            StageScale = 0.62f,
            Health = 6,
            Attack = 7,
            Defense = 5,
            Speed = 10,
            Crit = 6,
            Accent = new Color(0.35f, 0.78f, 0.92f),
        },
    };

    public static HeroProfile Get(HeroId id)
    {
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return All[i];
        }

        return All[0];
    }
}

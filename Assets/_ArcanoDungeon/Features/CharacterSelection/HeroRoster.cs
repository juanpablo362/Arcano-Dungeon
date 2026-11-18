using UnityEngine;

public enum HeroId
{
    Elara,
    Lyra,
    Kaede,
}

public sealed class HeroProfile
{
    public HeroId Id;
    public string Name;
    public string Title;
    public string Summary;
    public string Ability;
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
            Id = HeroId.Elara,
            Name = "Elara",
            Title = "Caballera",
            Summary = "Defensa y combate cuerpo a cuerpo",
            Ability = "Golpe de escudo",
            Health = 10,
            Attack = 6,
            Defense = 10,
            Speed = 4,
            Crit = 3,
            Accent = new Color(0.45f, 0.72f, 1f),
        },
        new HeroProfile
        {
            Id = HeroId.Lyra,
            Name = "Lyra",
            Title = "Maga",
            Summary = "Magia y combate a distancia",
            Ability = "Nova de hielo",
            Health = 5,
            Attack = 9,
            Defense = 4,
            Speed = 5,
            Crit = 4,
            Accent = new Color(0.72f, 0.48f, 1f),
        },
        new HeroProfile
        {
            Id = HeroId.Kaede,
            Name = "Kaede",
            Title = "Asesina",
            Summary = "Velocidad y daño crítico",
            Ability = "Paso de sombra",
            Health = 6,
            Attack = 8,
            Defense = 3,
            Speed = 10,
            Crit = 10,
            Accent = new Color(1f, 0.28f, 0.24f),
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

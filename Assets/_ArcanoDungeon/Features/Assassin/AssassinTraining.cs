using UnityEngine;

namespace ArcanoDungeon.Features.Assassin
{
    [DefaultExecutionOrder(-200)]
    public sealed class AssassinTraining : MonoBehaviour
    {
        void Awake() => GameSession.SelectedHero = HeroId.Assassin;

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16 };
            GUI.Box(new Rect(12, 12, 590, 124),
                "Asesina — zona de práctica\nWASD: mover · Alt: correr · Shift: dash · Espacio: saltar\nC/Ctrl: sombra indetectable · Clic izquierdo: combo\nClic derecho: cuchillo envenenado · E: filo doble\nEsc: liberar cursor", style);
        }
    }
}

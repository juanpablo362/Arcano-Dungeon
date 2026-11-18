using UnityEditor;
using UnityEngine;

// Escena CharacterSelection ya bakeada.
// Conservado vacío para no regenerar el lineup al compilar.
static class CharacterSelectSceneBake
{
    [MenuItem("Arcano Dungeon/Bake Character Selection Scene", false, 100)]
    public static void Bake()
    {
        Debug.Log("Character Selection ya está precargada en la escena. No se regenera.");
    }
}

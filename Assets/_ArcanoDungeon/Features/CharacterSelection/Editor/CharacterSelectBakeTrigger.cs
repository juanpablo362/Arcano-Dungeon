using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Escribe/borra Assets/.../REQUEST_CHARACTER_SELECT_BAKE.txt para disparar el bake
/// sin abrir menús (seguro tras compile, no en InitializeOnLoad).
/// </summary>
static class CharacterSelectBakeTrigger
{
    const string FlagPath =
        "Assets/_ArcanoDungeon/Features/CharacterSelection/Editor/REQUEST_CHARACTER_SELECT_BAKE.txt";

    [InitializeOnLoadMethod]
    static void Hook()
    {
        EditorApplication.delayCall += CheckFlag;
        EditorApplication.projectChanged += () => EditorApplication.delayCall += CheckFlag;
    }

    static void CheckFlag()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)
            return;
        if (!File.Exists(FlagPath))
            return;

        try
        {
            File.Delete(FlagPath);
            if (File.Exists(FlagPath + ".meta"))
                File.Delete(FlagPath + ".meta");
        }
        catch
        {
            return;
        }

        AssetDatabase.Refresh();
        EditorApplication.delayCall += () =>
        {
            try
            {
                CharacterSelectSceneBake.Bake();
                EditorPrefs.SetInt("ArcanoDungeon.CharacterSelect.BakeVersion", 3);
                Debug.Log("[Arcano] Character Selection bake vía flag completado.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Arcano] Bake falló: " + ex);
            }
        };
    }

    [MenuItem("Arcano Dungeon/Request Character Selection Bake", false, 102)]
    static void Request()
    {
        File.WriteAllText(FlagPath, "bake");
        AssetDatabase.Refresh();
        Debug.Log("[Arcano] Flag de bake escrito.");
    }
}

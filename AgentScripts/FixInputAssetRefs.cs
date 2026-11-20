using ArcanoDungeon.Infrastructure.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class FixInputAssetRefs
{
    public static string Main()
    {
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/_ArcanoDungeon/Settings/Input/DungeonKnightPlayer.inputactions");
        if (input == null)
            return "ERROR missing input asset";

        string[] paths =
        {
            "Assets/_ArcanoDungeon/Prefabs/ElaraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/LyraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/KaedePlayer.prefab",
        };

        var report = "";
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            var reader = root.GetComponent<PlayerInputReader>();
            var so = new SerializedObject(reader);

            // Dump property names for ObjectReference fields
            var it = so.GetIterator();
            bool enter = true;
            string names = "";
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                    names += it.propertyPath + ";";
            }

            var prop = so.FindProperty("actions");
            if (prop == null)
                prop = so.FindProperty("m_Actions");

            bool before = prop != null && prop.objectReferenceValue != null;
            if (prop != null)
            {
                prop.objectReferenceValue = input;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(reader);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            root = PrefabUtility.LoadPrefabContents(path);
            so = new SerializedObject(root.GetComponent<PlayerInputReader>());
            prop = so.FindProperty("actions");
            bool after = prop != null && prop.objectReferenceValue != null;
            PrefabUtility.UnloadPrefabContents(root);

            report += System.IO.Path.GetFileNameWithoutExtension(path)
                      + " before=" + before + " after=" + after + " fields=[" + names + "] | ";
        }

        AssetDatabase.SaveAssets();
        return report;
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RemoveMenuButtonDots
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";

    public static string Main()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var panel = GameObject.Find("/Canvas/MenuPanel");
        if (panel == null)
            return "MenuPanel missing";

        int removed = 0;
        foreach (Transform button in panel.transform)
        {
            var icon = button.Find("Icon");
            if (icon != null)
            {
                Undo.RecordObject(icon.gameObject, "Hide menu button icon");
                icon.gameObject.SetActive(false);
                EditorUtility.SetDirty(icon.gameObject);
                removed++;
            }

            var row = button.GetComponent<MenuButtonRow>();
            if (row != null)
            {
                Undo.RecordObject(row, "Clear menu button icon refs");
                row.icon = null;
                row.iconNormal = null;
                row.iconActive = null;
                EditorUtility.SetDirty(row);
            }

            // Acercar el texto al borde izquierdo, ya sin el diamante.
            var label = button.Find("Label")?.GetComponent<RectTransform>();
            if (label != null)
            {
                Undo.RecordObject(label, "Nudge menu label left");
                var pos = label.anchoredPosition;
                if (pos.x > 40f)
                    label.anchoredPosition = new Vector2(28f, pos.y);
                EditorUtility.SetDirty(label);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Hidden icons on " + removed + " menu buttons.";
    }
}

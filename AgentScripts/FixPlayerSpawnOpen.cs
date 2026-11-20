using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixPlayerSpawnOpen
{
    public static string Main()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        var scene = EditorSceneManager.OpenScene(
            "Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);

        // Buscar spawner / player start
        var spawner = GameObject.Find("PlayerSpawner");
        var start = GameObject.Find("PlayerStart");
        Transform t = null;
        if (spawner != null) t = spawner.transform;
        else if (start != null) t = start.transform;

        // También CharacterSelection / spawn points
        if (t == null)
        {
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb.GetType().Name.Contains("Spawn") || mb.GetType().Name.Contains("PlayerSpawner"))
                {
                    t = mb.transform;
                    break;
                }
            }
        }

        string moved = "none";
        // Posición abierta típica del centro de la sala (según captura: hex floor delante del arco)
        Vector3 openPos = new Vector3(0f, 0.1f, 0f);

        if (t != null)
        {
            t.position = openPos;
            EditorUtility.SetDirty(t.gameObject);
            moved = t.name + " -> " + openPos;
        }

        // Si hay un player en escena (no prefab instance only)
        foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
        {
            p.transform.position = openPos;
            EditorUtility.SetDirty(p);
            moved += "; player " + p.name;
        }

        // Bajar un poco heightOffset para chibi (Cesar usa 1.4 en humanos más altos)
        var follow = Object.FindAnyObjectByType<CameraFollow>();
        if (follow != null)
        {
            var so = new SerializedObject(follow);
            so.FindProperty("heightOffset").floatValue = 1.1f;
            so.FindProperty("distance").floatValue = 6.5f;
            so.FindProperty("mouseSensitivity").floatValue = 0.25f;
            so.FindProperty("minPitch").floatValue = 5f;
            so.FindProperty("maxPitch").floatValue = 65f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(follow);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "spawn: " + moved;
    }
}

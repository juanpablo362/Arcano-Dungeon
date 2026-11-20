using System.Collections.Generic;
using System.IO;
using ArcanoDungeon.Features.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Amplía Level1 con más salas, rejas doradas estilo Soul Knight y minimapa de nodos.
/// </summary>
public static class SetupSoulKnightDungeon
{
    const string LevelPath = "Assets/_ArcanoDungeon/Scenes/Level1.unity";
    const string MatDir = "Assets/_ArcanoDungeon/Art/Materials";
    const string TexDir = "Assets/_ArcanoDungeon/Art/Textures";
    const string GoldMatPath = MatDir + "/Mat_SoulKnightGateGold.mat";
    const string FrameMatPath = MatDir + "/Mat_SoulKnightGateFrame.mat";
    const string GoldTexPath = TexDir + "/Tex_SoulKnightGateGold.png";

    [MenuItem("Tools/Arcano/Setup Soul Knight Dungeon (rooms + gates + minimap)", false, 130)]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[SoulKnightDungeon] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        EnsureFolders();
        var goldMat = EnsureGoldMaterial();
        var frameMat = EnsureFrameMaterial();

        var scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Skeleton.prefab");
        if (skeleton == null)
            skeleton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Skeleton/Prefab/Skeleton.prefab");

        UpgradeExistingGates(goldMat, frameMat);
        var rooms = BuildExtraRooms(skeleton, goldMat, frameMat);
        WireConnections(rooms);
        EnsurePlayerStartsInHub();
        EnsureMinimap();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SoulKnightDungeon] Listo: más salas, rejas doradas y minimapa.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Art"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Art");
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Art", "Materials");
        if (!AssetDatabase.IsValidFolder(TexDir))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Art", "Textures");
    }

    static Material EnsureGoldMaterial()
    {
        EnsureGoldTexture();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GoldMatPath);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (mat == null)
        {
            mat = new Material(shader) { name = "Mat_SoulKnightGateGold" };
            AssetDatabase.CreateAsset(mat, GoldMatPath);
        }

        mat.shader = shader;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(GoldTexPath);
        if (mat.HasProperty("_BaseMap") && tex != null)
            mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(1f, 0.82f, 0.28f, 1f));
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.85f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.72f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.55f, 0.35f, 0.05f) * 1.4f);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material EnsureFrameMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FrameMatPath);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (mat == null)
        {
            mat = new Material(shader) { name = "Mat_SoulKnightGateFrame" };
            AssetDatabase.CreateAsset(mat, FrameMatPath);
        }

        mat.shader = shader;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.18f, 0.16f, 0.14f, 1f));
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.35f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.35f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureGoldTexture()
    {
        if (File.Exists(GoldTexPath))
            return;

        const int w = 64;
        const int h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float stripe = ((x / 8) % 2 == 0) ? 1f : 0.72f;
                float tip = y > h - 18 ? Mathf.Lerp(1f, 0.4f, (y - (h - 18)) / 18f) : 1f;
                float v = stripe * tip;
                Color c = new Color(0.95f * v, 0.75f * v, 0.22f * v, 1f);
                if (x % 8 == 0 || x % 8 == 7)
                    c *= 0.55f;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        File.WriteAllBytes(GoldTexPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(GoldTexPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(GoldTexPath);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
    }

    static void UpgradeExistingGates(Material gold, Material frame)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t == null || !t.name.StartsWith("CombatDoor_"))
                continue;
            ConvertDoorToPortcullis(t.gameObject, gold, frame);
        }
    }

    static void ConvertDoorToPortcullis(GameObject door, Material gold, Material frame)
    {
        // Mantener transform/collider del bloqueo; reemplazar look por rejas.
        var mf = door.GetComponent<MeshFilter>();
        var mr = door.GetComponent<MeshRenderer>();
        if (mf != null) Object.DestroyImmediate(mf);
        if (mr != null) Object.DestroyImmediate(mr);

        for (int i = door.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(door.transform.GetChild(i).gameObject);

        BuildPortcullisVisual(door.transform, gold, frame);

        var col = door.GetComponent<BoxCollider>();
        if (col == null)
            col = door.AddComponent<BoxCollider>();
        col.isTrigger = false;
        col.size = new Vector3(1f, 1f, 1f);
        col.center = Vector3.zero;

        // Empieza abierta (Soul Knight); RoomController la cierra al pelear.
        door.SetActive(false);
        EditorUtility.SetDirty(door);
    }

    static void BuildPortcullisVisual(Transform parent, Material gold, Material frame)
    {
        // Barras a lo largo del eje local más ancho (X o Z).
        bool wideX = Mathf.Abs(parent.lossyScale.x) >= Mathf.Abs(parent.lossyScale.z);

        CreateGatePart("GateFrame_Top", parent, new Vector3(0f, 0.48f, 0f),
            wideX ? new Vector3(1.05f, 0.08f, 1.2f) : new Vector3(1.2f, 0.08f, 1.05f), frame);
        CreateGatePart("GateFrame_A", parent,
            wideX ? new Vector3(-0.48f, 0f, 0f) : new Vector3(0f, 0f, -0.48f),
            wideX ? new Vector3(0.08f, 1.02f, 1.15f) : new Vector3(1.15f, 1.02f, 0.08f), frame);
        CreateGatePart("GateFrame_B", parent,
            wideX ? new Vector3(0.48f, 0f, 0f) : new Vector3(0f, 0f, 0.48f),
            wideX ? new Vector3(0.08f, 1.02f, 1.15f) : new Vector3(1.15f, 1.02f, 0.08f), frame);

        const int bars = 7;
        for (int i = 0; i < bars; i++)
        {
            float t = bars == 1 ? 0.5f : i / (float)(bars - 1);
            float along = Mathf.Lerp(-0.38f, 0.38f, t);
            Vector3 barPos = wideX ? new Vector3(along, -0.02f, 0f) : new Vector3(0f, -0.02f, along);
            Vector3 barScale = wideX ? new Vector3(0.07f, 0.92f, 0.55f) : new Vector3(0.55f, 0.92f, 0.07f);
            CreateGatePart("GateBar_" + i, parent, barPos, barScale, gold);

            Vector3 tipPos = wideX ? new Vector3(along, 0.48f, 0f) : new Vector3(0f, 0.48f, along);
            var tip = CreateGatePart("GateSpike_" + i, parent, tipPos,
                wideX ? new Vector3(0.08f, 0.08f, 0.5f) : new Vector3(0.5f, 0.08f, 0.08f), gold);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }

    static GameObject CreateGatePart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Dictionary<string, RoomController> BuildExtraRooms(
        GameObject skeleton,
        Material gold,
        Material frame)
    {
        var map = new Dictionary<string, RoomController>();
        foreach (var room in Object.FindObjectsByType<RoomController>(FindObjectsInactive.Include))
            map[room.name] = room;

        // Clasificar salas existentes.
        if (map.TryGetValue("CombatRoom_1", out var r1))
        {
            r1.roomKind = RoomController.RoomKind.Start;
            r1.roomName = "Sala Inicial (Hub)";
            r1.skeletonPrefab = null;
            r1.additionalEnemyPrefabs = System.Array.Empty<GameObject>();
            r1.normalSpawnPoints = System.Array.Empty<Transform>();
            r1.spawnMiniBoss = false;
            r1.miniBossSpawnPoint = null;
            EditorUtility.SetDirty(r1);
        }

        if (map.TryGetValue("CombatRoom_4", out var r4))
        {
            r4.roomKind = RoomController.RoomKind.Boss;
            EditorUtility.SetDirty(r4);
        }

        // Plantilla: CombatRoom_2 (paredes completas).
        var template = GameObject.Find("CombatRoom_2");
        if (template == null)
        {
            Debug.LogError("[SoulKnightDungeon] No está CombatRoom_2 para clonar.");
            return map;
        }

        // Room 5: este del hub (tesoro).
        var room5 = CloneRoom(template, "CombatRoom_5", "Sala Este (Tesoro)",
            new Vector3(30f, 0f, 27.5f), skeleton, gold, frame, RoomController.RoomKind.Treasure, false);
        map[room5.name] = room5;
        EnsureCorridor("Corridor_5", new Vector3(15f, 0f, 27.5f), eastWest: true);
        EnsureDoor(room5.transform, "CombatDoor_5_West", new Vector3(20f, 4f, 27.5f),
            new Vector3(0.5f, 8f, 5f), Quaternion.identity, gold, frame);
        OpenWallGap(room5.gameObject, "West");

        // Room 6: sur del hub.
        var room6 = CloneRoom(template, "CombatRoom_6", "Sala Sur",
            new Vector3(0f, 0f, -2.5f), skeleton, gold, frame, RoomController.RoomKind.Combat, false);
        map[room6.name] = room6;
        EnsureCorridor("Corridor_6", new Vector3(0f, 0f, 12.5f), eastWest: false);
        EnsureDoor(room6.transform, "CombatDoor_6_North", new Vector3(0f, 4f, 7.5f),
            new Vector3(5f, 8f, 0.5f), Quaternion.identity, gold, frame);
        OpenWallGap(room6.gameObject, "North");

        // Room 7: este de la sala sur.
        var room7 = CloneRoom(template, "CombatRoom_7", "Sala Sureste",
            new Vector3(30f, 0f, -2.5f), skeleton, gold, frame, RoomController.RoomKind.Combat, false);
        map[room7.name] = room7;
        EnsureCorridor("Corridor_7", new Vector3(15f, 0f, -2.5f), eastWest: true);
        EnsureDoor(room7.transform, "CombatDoor_7_West", new Vector3(20f, 4f, -2.5f),
            new Vector3(0.5f, 8f, 5f), Quaternion.identity, gold, frame);
        EnsureDoor(room6.transform, "CombatDoor_6_East", new Vector3(10f, 4f, -2.5f),
            new Vector3(0.5f, 8f, 5f), Quaternion.identity, gold, frame);
        OpenWallGap(room7.gameObject, "West");
        OpenWallGap(room6.gameObject, "East");

        // Puertas nuevas del hub hacia este/sur.
        if (r1 != null)
        {
            EnsureDoor(r1.transform, "CombatDoor_1_East", new Vector3(10f, 4f, 27.5f),
                new Vector3(0.5f, 8f, 5f), Quaternion.identity, gold, frame);
            EnsureDoor(r1.transform, "CombatDoor_1_South", new Vector3(0f, 4f, 17.5f),
                new Vector3(5f, 8f, 0.5f), Quaternion.identity, gold, frame);
            RefreshExitDoors(r1);
            OpenWallGap(r1.gameObject, "East");
            OpenWallGap(r1.gameObject, "South");
        }

        RefreshExitDoors(room5);
        RefreshExitDoors(room6);
        RefreshExitDoors(room7);

        return map;
    }

    static RoomController CloneRoom(
        GameObject template,
        string name,
        string displayName,
        Vector3 position,
        GameObject skeleton,
        Material gold,
        Material frame,
        RoomController.RoomKind kind,
        bool boss)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var clone = Object.Instantiate(template);
        clone.name = name;
        clone.transform.position = position;
        clone.transform.rotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;

        // Limpiar spawns/puertas viejas del clon.
        var toDelete = new List<GameObject>();
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            if (child == clone.transform)
                continue;
            if (child.name.StartsWith("Spawn_")
                || child.name.StartsWith("CombatDoor_")
                || child.name.Contains("Decor"))
                toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete)
            Object.DestroyImmediate(go);

        // Renombrar piezas de piso/pared.
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("CombatRoom_2"))
                child.name = child.name.Replace("CombatRoom_2", name);
        }

        var room = clone.GetComponent<RoomController>();
        if (room == null)
            room = clone.AddComponent<RoomController>();

        room.roomName = displayName;
        room.roomKind = kind;
        room.spawnMiniBoss = boss;
        room.enemyScale = 1f;
        room.skeletonPrefab = skeleton;
        room.additionalEnemyPrefabs = System.Array.Empty<GameObject>();
        room.exitDoor = null;
        room.exitDoors = System.Array.Empty<GameObject>();
        room.connectedRooms = System.Array.Empty<RoomController>();

        // Spawns nuevos.
        var spawns = new List<Transform>();
        spawns.Add(CreateSpawn(clone.transform, "Spawn_" + name + "_1", position + new Vector3(-3.5f, 0.52f, 2f)));
        spawns.Add(CreateSpawn(clone.transform, "Spawn_" + name + "_2", position + new Vector3(3.5f, 0.52f, -2f)));
        if (kind != RoomController.RoomKind.Treasure)
            spawns.Add(CreateSpawn(clone.transform, "Spawn_" + name + "_3", position + new Vector3(0f, 0.52f, 3.5f)));
        room.normalSpawnPoints = spawns.ToArray();

        // Tesoro: sin enemigos (sala de botín).
        if (kind == RoomController.RoomKind.Treasure)
        {
            room.skeletonPrefab = null;
            room.normalSpawnPoints = System.Array.Empty<Transform>();
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "TreasurePedestal";
            marker.transform.SetParent(clone.transform, false);
            marker.transform.position = position + new Vector3(0f, 0.6f, 0f);
            marker.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.35f, 0.65f, 1f, 1f));
                marker.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        EditorUtility.SetDirty(room);
        return room;
    }

    static Transform CreateSpawn(Transform parent, string name, Vector3 worldPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.identity;
        return go.transform;
    }

    static void EnsureCorridor(string name, Vector3 position, bool eastWest)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);

        string templateName = eastWest ? "Corridor_2" : "Corridor_3";
        var template = GameObject.Find(templateName);
        if (template == null)
        {
            // Fallback: piso simple.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = eastWest
                ? new Vector3(10f, 0.25f, 5f)
                : new Vector3(5f, 0.25f, 10f);
            return;
        }

        var clone = Object.Instantiate(template);
        clone.name = name;
        clone.transform.position = position;
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith(templateName))
                child.name = child.name.Replace(templateName, name);
        }
    }

    static GameObject EnsureDoor(
        Transform room,
        string name,
        Vector3 worldPos,
        Vector3 scale,
        Quaternion worldRot,
        Material gold,
        Material frame)
    {
        var existing = room.Find(name);
        GameObject door;
        if (existing != null)
            door = existing.gameObject;
        else
        {
            door = new GameObject(name);
            door.transform.SetParent(room, true);
        }

        door.transform.position = worldPos;
        door.transform.rotation = worldRot;
        door.transform.localScale = scale;
        ConvertDoorToPortcullis(door, gold, frame);
        return door;
    }

    static void RefreshExitDoors(RoomController room)
    {
        if (room == null)
            return;
        var doors = new List<GameObject>();
        foreach (Transform child in room.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("CombatDoor_"))
                doors.Add(child.gameObject);
        }
        room.exitDoors = doors.ToArray();
        EditorUtility.SetDirty(room);
    }

    static void OpenWallGap(GameObject roomRoot, string side)
    {
        if (roomRoot == null)
            return;

        // CombatRoom_2 usa segmentos East_North/East_South y West / North / South.
        foreach (Transform child in roomRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.GetComponent<Renderer>() == null)
                continue;
            string n = child.name;
            if (n.Contains("Door") || n.Contains("Floor") || n.Contains("Decor") || n.Contains("Spawn"))
                continue;

            bool match = false;
            switch (side)
            {
                case "East":
                    match = n.Contains("_East");
                    break;
                case "West":
                    match = n.Contains("_West");
                    break;
                case "North":
                    match = n.Contains("_North") && !n.Contains("East") && !n.Contains("West");
                    break;
                case "South":
                    match = n.Contains("_South") && !n.Contains("East") && !n.Contains("West")
                            && !n.Contains("Left") && !n.Contains("Right");
                    break;
            }

            if (!match)
                continue;

            // Desactivar segmentos del vano (p.ej. East_North + East_South).
            child.gameObject.SetActive(false);
        }
    }

    static void WireConnections(Dictionary<string, RoomController> map)
    {
        Link(map, "CombatRoom_1", "CombatRoom_2");
        Link(map, "CombatRoom_1", "CombatRoom_3");
        Link(map, "CombatRoom_1", "CombatRoom_5");
        Link(map, "CombatRoom_1", "CombatRoom_6");
        Link(map, "CombatRoom_3", "CombatRoom_4");
        Link(map, "CombatRoom_6", "CombatRoom_7");
    }

    static void Link(Dictionary<string, RoomController> map, string a, string b)
    {
        if (!map.TryGetValue(a, out var ra) || !map.TryGetValue(b, out var rb))
            return;
        AddUnique(ra, rb);
        AddUnique(rb, ra);
        EditorUtility.SetDirty(ra);
        EditorUtility.SetDirty(rb);
    }

    static void AddUnique(RoomController room, RoomController other)
    {
        var list = new List<RoomController>();
        if (room.connectedRooms != null)
            list.AddRange(room.connectedRooms);
        if (!list.Contains(other))
            list.Add(other);
        room.connectedRooms = list.ToArray();
    }

    static void EnsurePlayerStartsInHub()
    {
        var hub = GameObject.Find("CombatRoom_1");
        var start = GameObject.Find("PlayerStart");
        if (hub == null || start == null)
            return;

        Vector3 hubPos = hub.transform.position;
        start.transform.position = new Vector3(hubPos.x, 0.3f, hubPos.z);
        foreach (var spawner in Object.FindObjectsByType<PlayerSpawner>(FindObjectsInactive.Include))
            spawner.startPosition = start.transform;
        EditorUtility.SetDirty(start);
    }

    static void EnsureMinimap()
    {
        var existing = Object.FindAnyObjectByType<DungeonMinimapUI>();
        if (existing != null)
            return;

        var go = new GameObject("DungeonMinimap");
        go.AddComponent<DungeonMinimapUI>();
        Undo.RegisterCreatedObjectUndo(go, "Create Dungeon Minimap");
    }
}

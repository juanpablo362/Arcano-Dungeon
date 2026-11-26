using System.Collections.Generic;
using System.IO;
using ArcanoDungeon.Features.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class SetupCatacombsLevel
{
    const string Kit =
        "Assets/_ArcanoDungeon/Features/Catatumba nivel 2/KayKit_HalloweenBits_1.0_FREE (1)/KayKit_HalloweenBits_1.0_FREE/Assets/";
    const string Fbx = Kit + "fbx(unity)/";
    const string TexPath = Kit + "textures/halloweenbits_texture.png";
    const string MatPath = "Assets/_ArcanoDungeon/Art/Materials/Mat_HalloweenBits.mat";
    const string NightSkyTexPath = "Assets/_ArcanoDungeon/Art/Textures/NightSky.png";
    const string NightSkyMatPath = "Assets/_ArcanoDungeon/Art/Materials/Mat_NightSky.mat";
    const string GoldMatPath = "Assets/_ArcanoDungeon/Art/Materials/Mat_SoulKnightGateGold.mat";
    const string FrameMatPath = "Assets/_ArcanoDungeon/Art/Materials/Mat_SoulKnightGateFrame.mat";
    const string Level1 = "Assets/_ArcanoDungeon/Scenes/Level1.unity";
    const string Level2 = "Assets/_ArcanoDungeon/Scenes/CatacombsLevel.unity";
    const string Skeleton = "Assets/Prefabs/Skeleton.prefab";
    const string Monsters = "Assets/_ArcanoDungeon/Prefabs/Monsters/";
    const string Chest = "Assets/Prefabs/TreasureChest.prefab";

    const float DoorGap = 5f;
    const float BoundaryHeight = 8f;

    static Material kitMat;
    static Material goldMat;
    static Material frameMat;
    static float floorY;

    struct RoomSize
    {
        public Vector3 Center;
        public float Width;
        public float Depth;
    }

    [MenuItem("Tools/Arcano/Setup Catacombs Level", false, 131)]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[Catacombs] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        var heroes = ReadHeroesAndRetargetExit();
        BuildLevel(heroes);
        Debug.Log("[Catacombs] Nivel 2 listo.");
    }

    struct Heroes
    {
        public GameObject Mage;
        public GameObject Knight;
        public GameObject Misaki;
        public GameObject Yuko;
        public GameObject Assassin;
    }

    public static void RebuildCatacombs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Sal de Play Mode antes de reconstruir Catacumbas.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new System.InvalidOperationException("Guarda los cambios abiertos antes de reconstruir Catacumbas.");

        var preview = EditorSceneManager.OpenPreviewScene(Level2);
        Heroes heroes;
        try
        {
            PlayerSpawner source = null;
            foreach (var root in preview.GetRootGameObjects())
            {
                source = root.GetComponentInChildren<PlayerSpawner>();
                if (source != null) break;
            }
            if (source == null)
                throw new System.InvalidOperationException("No se encontró el punto de inicio de Catacumbas.");
            heroes = new Heroes
            {
                Mage = source.magePrefab, Knight = source.knightPrefab, Misaki = source.misakiPrefab,
                Yuko = source.yukoPrefab, Assassin = source.assassinPrefab
            };
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }

        BuildLevel(heroes);
    }

    static Heroes ReadHeroesAndRetargetExit()
    {
        var scene = EditorSceneManager.OpenScene(Level1, OpenSceneMode.Single);
        var source = Object.FindAnyObjectByType<PlayerSpawner>();
        var heroes = new Heroes();
        if (source != null)
        {
            heroes.Mage = source.magePrefab;
            heroes.Knight = source.knightPrefab;
            heroes.Misaki = source.misakiPrefab;
            heroes.Yuko = source.yukoPrefab;
            heroes.Assassin = source.assassinPrefab;
        }

        foreach (var exit in Object.FindObjectsByType<LevelExit>(FindObjectsInactive.Include))
        {
            if (exit.nextSceneBuildIndex == 3)
            {
                exit.nextSceneBuildIndex = 4;
                EditorUtility.SetDirty(exit);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return heroes;
    }

    static void BuildLevel(Heroes heroes)
    {
        var scene = EditorSceneManager.OpenScene(Level2, OpenSceneMode.Single);
        var old = GameObject.Find("CatacombsRoot");
        if (old != null)
            Object.DestroyImmediate(old);

        kitMat = EnsureKitMaterial();
        goldMat = LoadOrColor(GoldMatPath, "Mat_SoulKnightGateGold", new Color(1f, 0.82f, 0.28f), 0.85f, 0.72f);
        frameMat = LoadOrColor(FrameMatPath, "Mat_SoulKnightGateFrame", new Color(0.18f, 0.16f, 0.14f), 0.35f, 0.35f);

        var root = new GameObject("CatacombsRoot");
        TuneLight();
        TuneAtmosphere();
        EnsureFollowCamera();

        var cemetery = Room("Cementerio", new Vector2(22f, 20f), Vector3.zero);
        var gallery = Room("Galeria", new Vector2(20f, 18f), new Vector3(0f, 0f, 25f));
        var ossuary = Room("Osario", new Vector2(18f, 18f), new Vector3(25f, 0f, 25f));
        var reliquary = Room("Relicario", new Vector2(18f, 16f), new Vector3(0f, 0f, 48f));
        var wake = Room("Velatorio", new Vector2(20f, 18f), new Vector3(0f, 0f, 71f));
        var chapel = Room("Capilla", new Vector2(18f, 18f), new Vector3(-25f, 0f, 71f));
        var crypt = Room("Cripta", new Vector2(30f, 28f), new Vector3(0f, 0f, 102f));

        var rooms = new Dictionary<string, RoomController>();
        rooms["Cementerio"] = MakeRoom(root.transform, cemetery, "Cementerio", RoomController.RoomKind.Start, false, true, false, false);
        rooms["Galeria"] = MakeRoom(root.transform, gallery, "Galería de nichos", RoomController.RoomKind.Combat, true, true, true, false);
        rooms["Osario"] = MakeRoom(root.transform, ossuary, "Osario", RoomController.RoomKind.Combat, false, false, false, true);
        rooms["Relicario"] = MakeRoom(root.transform, reliquary, "Relicario", RoomController.RoomKind.Treasure, true, true, false, false);
        rooms["Velatorio"] = MakeRoom(root.transform, wake, "Velatorio", RoomController.RoomKind.Combat, true, true, false, true);
        rooms["Capilla"] = MakeRoom(root.transform, chapel, "Capilla", RoomController.RoomKind.Combat, false, false, true, false);
        rooms["Cripta"] = MakeRoom(root.transform, crypt, "Cripta", RoomController.RoomKind.Boss, true, false, false, false);

        Connect(root.transform, rooms["Cementerio"], cemetery, rooms["Galeria"], gallery, north: true);
        Connect(root.transform, rooms["Galeria"], gallery, rooms["Osario"], ossuary, north: false);
        Connect(root.transform, rooms["Galeria"], gallery, rooms["Relicario"], reliquary, north: true);
        Connect(root.transform, rooms["Relicario"], reliquary, rooms["Velatorio"], wake, north: true);
        Connect(root.transform, rooms["Capilla"], chapel, rooms["Velatorio"], wake, north: false);
        Connect(root.transform, rooms["Velatorio"], wake, rooms["Cripta"], crypt, north: true);
        AddContinuousGround(root.transform, rooms, floorY);

        DressCemetery(rooms["Cementerio"].transform, cemetery);
        DressGallery(rooms["Galeria"].transform, gallery);
        DressOssuary(rooms["Osario"].transform, ossuary);
        DressReliquary(rooms["Relicario"].transform, reliquary);
        DressOssuary(rooms["Velatorio"].transform, wake);
        DressReliquary(rooms["Capilla"].transform, chapel);
        DressCrypt(rooms["Cripta"].transform, crypt);
        foreach (var room in rooms.Values)
            WarmLight(room.transform, room.transform.position + Vector3.up * (floorY + 4f), 8.5f, 3.5f);

        WireEnemies(rooms);

        var exit = new GameObject("CryptExit");
        exit.transform.SetParent(rooms["Cripta"].transform, true);
        exit.transform.position = crypt.Center + new Vector3(0f, floorY + 1.2f, -0.75f);
        var exitCol = exit.AddComponent<BoxCollider>();
        exitCol.isTrigger = true;
        exitCol.size = new Vector3(3.2f, 2.4f, 1.6f);
        exit.AddComponent<LevelExit>().nextSceneBuildIndex = 5;
        PortalExitDress.DressExit(exit);
        exit.SetActive(false);
        rooms["Cripta"].objectToActivateOnClear = exit;

        var start = new GameObject("PlayerStart");
        start.transform.SetParent(root.transform, true);
        start.transform.position = cemetery.Center + new Vector3(0f, floorY + 0.2f, -2f);
        var spawnerGo = new GameObject("PlayerSpawner");
        spawnerGo.transform.SetParent(root.transform, true);
        var spawner = spawnerGo.AddComponent<PlayerSpawner>();
        spawner.magePrefab = heroes.Mage;
        spawner.knightPrefab = heroes.Knight;
        spawner.misakiPrefab = heroes.Misaki;
        spawner.yukoPrefab = heroes.Yuko;
        spawner.assassinPrefab = heroes.Assassin;
        spawner.startPosition = start.transform;

        var pause = new GameObject("PauseMenu");
        pause.transform.SetParent(root.transform, true);
        pause.AddComponent<PauseMenuController>();

        var map = new GameObject("DungeonMinimap");
        map.transform.SetParent(root.transform, true);
        var ui = map.AddComponent<DungeonMinimapUI>();
        var so = new SerializedObject(ui);
        var label = so.FindProperty("floorLabel");
        if (label != null)
        {
            label.stringValue = "2-1";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static RoomSize Room(string name, Vector2 size, Vector3 center)
    {
        return new RoomSize { Center = center, Width = size.x, Depth = size.y };
    }

    static RoomController MakeRoom(Transform root, RoomSize size, string display, RoomController.RoomKind kind,
        bool south, bool north, bool east, bool west)
    {
        var go = new GameObject(display.Split(' ')[0]);
        if (display.StartsWith("Galer"))
            go.name = "Galeria";
        go.name = display == "Cementerio" ? "Cementerio"
            : display.StartsWith("Galer") ? "Galeria"
            : display == "Osario" ? "Osario"
            : display == "Relicario" ? "Relicario"
            : "Cripta";
        go.transform.SetParent(root, true);
        go.transform.position = size.Center;

        TileRect(go.transform, size.Center, size.Width, size.Depth, "floor_dirt.fbx");
        Perimeter(go.transform, size, south, north, east, west);

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 2.5f, 0f);
        col.size = new Vector3(size.Width - 1f, 5f, size.Depth - 1f);

        var room = go.AddComponent<RoomController>();
        room.roomName = display;
        room.roomKind = kind;
        room.enemyScale = 1f;
        room.exitDoors = System.Array.Empty<GameObject>();
        room.additionalEnemyPrefabs = System.Array.Empty<GameObject>();
        room.normalSpawnPoints = System.Array.Empty<Transform>();
        return room;
    }

    static void Perimeter(Transform parent, RoomSize size, bool south, bool north, bool east, bool west)
    {
        float y = floorY + BoundaryHeight * 0.5f;
        Side(parent, size.Center + new Vector3(0f, y, size.Depth * 0.5f), new Vector3(size.Width, BoundaryHeight, 0.45f), north, true);
        Side(parent, size.Center + new Vector3(0f, y, -size.Depth * 0.5f), new Vector3(size.Width, BoundaryHeight, 0.45f), south, true);
        Side(parent, size.Center + new Vector3(size.Width * 0.5f, y, 0f), new Vector3(0.45f, BoundaryHeight, size.Depth), east, false);
        Side(parent, size.Center + new Vector3(-size.Width * 0.5f, y, 0f), new Vector3(0.45f, BoundaryHeight, size.Depth), west, false);
        FenceRing(parent, size, south, north, east, west);
    }

    static void Side(Transform parent, Vector3 mid, Vector3 full, bool open, bool alongX)
    {
        if (!open)
        {
            Block(parent, mid, full);
            return;
        }

        float span = alongX ? full.x : full.z;
        float side = (span - DoorGap) * 0.5f;
        if (side < 0.4f)
            return;
        float offset = DoorGap * 0.5f + side * 0.5f;
        var size = full;
        if (alongX)
            size.x = side;
        else
            size.z = side;
        var shift = alongX ? Vector3.right : Vector3.forward;
        Block(parent, mid + shift * offset, size);
        Block(parent, mid - shift * offset, size);
    }

    static void Block(Transform parent, Vector3 world, Vector3 size)
    {
        var go = new GameObject("Boundary");
        go.transform.SetParent(parent, true);
        go.transform.position = world;
        go.AddComponent<BoxCollider>().size = size;
        var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "Base de piedra";
        trim.transform.SetParent(go.transform, false);
        trim.transform.localPosition = Vector3.down * (size.y * 0.5f - 0.55f);
        trim.transform.localScale = new Vector3(size.x, 1.1f, size.z);
        trim.GetComponent<MeshRenderer>().sharedMaterial = frameMat;
        Object.DestroyImmediate(trim.GetComponent<BoxCollider>());
    }

    static void FenceRing(Transform parent, RoomSize size, bool south, bool north, bool east, bool west)
    {
        PlaceLine(parent, size.Center + new Vector3(0f, 0f, size.Depth * 0.5f + 0.6f), size.Width, north, true, 0f);
        PlaceLine(parent, size.Center + new Vector3(0f, 0f, -size.Depth * 0.5f - 0.6f), size.Width, south, true, 180f);
        PlaceLine(parent, size.Center + new Vector3(size.Width * 0.5f + 0.6f, 0f, 0f), size.Depth, east, false, 90f);
        PlaceLine(parent, size.Center + new Vector3(-size.Width * 0.5f - 0.6f, 0f, 0f), size.Depth, west, false, -90f);
    }

    static void PlaceLine(Transform parent, Vector3 mid, float length, bool gap, bool alongX, float yaw)
    {
        var probe = Spawn("fence.fbx", mid, parent, yaw, solid: false);
        if (probe == null)
            return;
        var probeSize = BoundsOf(probe).size;
        float step = Mathf.Max(1.2f, Mathf.Max(probeSize.x, probeSize.z));
        Object.DestroyImmediate(probe);

        int count = Mathf.Max(1, Mathf.FloorToInt(length / step));
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : Mathf.Lerp(-length * 0.5f + step * 0.5f, length * 0.5f - step * 0.5f, i / (float)(count - 1));
            if (gap && Mathf.Abs(t) < DoorGap * 0.55f)
                continue;
            var pos = mid + (alongX ? Vector3.right : Vector3.forward) * t;
            Spawn("fence.fbx", pos, parent, yaw, solid: true);
        }
    }

    static void Connect(Transform root, RoomController first, RoomSize a, RoomController second, RoomSize b, bool north)
    {
        Vector3 from = north
            ? a.Center + new Vector3(0f, 0f, a.Depth * 0.5f)
            : a.Center + new Vector3(a.Width * 0.5f, 0f, 0f);
        Vector3 to = north
            ? b.Center + new Vector3(0f, 0f, -b.Depth * 0.5f)
            : b.Center + new Vector3(-b.Width * 0.5f, 0f, 0f);
        var mid = (from + to) * 0.5f;
        var delta = to - from;
        float length = delta.magnitude + 1.2f;
        bool eastWest = Mathf.Abs(delta.x) > Mathf.Abs(delta.z);
        var path = new GameObject(first.name + " - " + second.name);
        path.transform.SetParent(root, false);
        TileRect(path.transform, mid, eastWest ? length : DoorGap, eastWest ? DoorGap : length, "path_A.fbx");
        Spawn("arch.fbx", mid, path.transform, eastWest ? 90f : 0f, solid: false);
        Vector3 railOffset = (eastWest ? Vector3.forward : Vector3.right) * (DoorGap * 0.5f + 0.15f);
        Vector3 railSize = eastWest ? new Vector3(length, BoundaryHeight, 0.45f) : new Vector3(0.45f, BoundaryHeight, length);
        Vector3 railCenter = mid + Vector3.up * (floorY + BoundaryHeight * 0.5f);
        Block(path.transform, railCenter - railOffset, railSize);
        Block(path.transform, railCenter + railOffset, railSize);

        var gate = Gate(path.transform, "Reja de combate", Midpoint(from, to),
            eastWest ? new Vector3(0.45f, BoundaryHeight, DoorGap) : new Vector3(DoorGap, BoundaryHeight, 0.45f));
        AttachGate(first, gate);
        AttachGate(second, gate);
        AddLink(first, second);
        AddLink(second, first);
    }

    static void AttachGate(RoomController room, GameObject gate)
    {
        if (room.roomKind == RoomController.RoomKind.Start || room.roomKind == RoomController.RoomKind.Treasure)
            return;
        var gates = new List<GameObject>(room.exitDoors ?? System.Array.Empty<GameObject>()) { gate };
        room.exitDoors = gates.ToArray();
    }

    static void DressCemetery(Transform room, RoomSize size)
    {
        var c = size.Center;
        Spawn("arch_gate.fbx", c + new Vector3(0f, 0f, size.Depth * 0.5f - 0.4f), room, 0f, solid: false);
        Spawn("path_B.fbx", c + new Vector3(0f, 0f, -1f), room, 0f, solid: false);
        Spawn("grave_A.fbx", c + new Vector3(-5f, 0f, 2f), room, 15f, solid: true);
        Spawn("grave_B.fbx", c + new Vector3(-5.5f, 0f, -2f), room, -10f, solid: true);
        Spawn("grave_A_destroyed.fbx", c + new Vector3(5.2f, 0f, 1.5f), room, 20f, solid: true);
        Spawn("gravestone.fbx", c + new Vector3(4.6f, 0f, -2.4f), room, -8f, solid: true);
        Spawn("gravemarker_A.fbx", c + new Vector3(-2.2f, 0f, 3.4f), room, 0f, solid: false);
        Spawn("tree_dead_large.fbx", c + new Vector3(-7f, 0f, -5f), room, 20f, solid: true);
        Spawn("tree_dead_medium.fbx", c + new Vector3(7f, 0f, 4.5f), room, -30f, solid: true);
        Spawn("pumpkin_orange.fbx", c + new Vector3(2.4f, 0f, -4f), room, 0f, solid: false);
        Spawn("pumpkin_yellow_small.fbx", c + new Vector3(-3f, 0f, -4.2f), room, 40f, solid: false);
        WarmLight(room, c + new Vector3(-4f, 0f, 0f), 7f);
    }

    static void DressGallery(Transform room, RoomSize size)
    {
        var c = size.Center;
        Spawn("pillar.fbx", c + new Vector3(-5f, 0f, 4f), room, 0f, solid: true);
        Spawn("pillar.fbx", c + new Vector3(5f, 0f, 4f), room, 0f, solid: true);
        Spawn("pillar.fbx", c + new Vector3(-5f, 0f, -4f), room, 0f, solid: true);
        Spawn("pillar.fbx", c + new Vector3(5f, 0f, -4f), room, 0f, solid: true);
        Spawn("plaque.fbx", c + new Vector3(-6.2f, 0f, 1f), room, 90f, solid: false);
        Spawn("plaque_candles.fbx", c + new Vector3(6.2f, 0f, -1f), room, -90f, solid: false);
        Spawn("skull.fbx", c + new Vector3(-4f, 0f, 0.5f), room, 20f, solid: false);
        Spawn("ribcage.fbx", c + new Vector3(4f, 0f, 1.2f), room, -15f, solid: false);
        Spawn("lantern_standing.fbx", c + new Vector3(-2f, 0f, -3f), room, 0f, solid: false);
        WarmLight(room, c + new Vector3(-2f, 1.4f, -3f), 6f);
        WarmLight(room, c + new Vector3(4f, 1.6f, 2f), 6f);
    }

    static void DressOssuary(Transform room, RoomSize size)
    {
        var c = size.Center;
        Spawn("coffin.fbx", c + new Vector3(-2.4f, 0f, 2f), room, 20f, solid: true);
        Spawn("coffin_decorated.fbx", c + new Vector3(2.6f, 0f, -1.5f), room, -25f, solid: true);
        Spawn("bone_A.fbx", c + new Vector3(0.4f, 0f, 1.2f), room, 40f, solid: false);
        Spawn("bone_B.fbx", c + new Vector3(-0.6f, 0f, -0.4f), room, 10f, solid: false);
        Spawn("bone_C.fbx", c + new Vector3(1.2f, 0f, 0.2f), room, -30f, solid: false);
        Spawn("skull_candle.fbx", c + new Vector3(0f, 0f, -3f), room, 0f, solid: false);
        Spawn("ribcage.fbx", c + new Vector3(-3.2f, 0f, -2f), room, 70f, solid: false);
        WarmLight(room, c + new Vector3(0f, 1.2f, -3f), 5.5f);
    }

    static void DressReliquary(Transform room, RoomSize size)
    {
        var c = size.Center;
        Spawn("shrine_candles.fbx", c + new Vector3(0f, 0f, 2.2f), room, 180f, solid: true);
        Spawn("candle_triple.fbx", c + new Vector3(-2.2f, 0f, 1.4f), room, 0f, solid: false);
        Spawn("candle.fbx", c + new Vector3(2.2f, 0f, 1.4f), room, 0f, solid: false);
        Spawn("bench.fbx", c + new Vector3(0f, 0f, -2.4f), room, 0f, solid: true);
        var chest = AssetDatabase.LoadAssetAtPath<GameObject>(Chest);
        if (chest != null)
        {
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(chest);
            copy.transform.SetParent(room, true);
            copy.transform.position = c + new Vector3(0f, floorY, 0.4f);
            copy.transform.rotation = Quaternion.identity;
            var loot = copy.GetComponent<TreasureChest>();
            loot.dropTable = AssetDatabase.LoadAssetAtPath<ArcanoDungeon.Features.Loot.LootDropTable>(
                "Assets/_ArcanoDungeon/Features/Loot/Items/LootDropTable_Catacombs.asset");
            loot.allowMimic = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(loot);
        }
        WarmLight(room, c + new Vector3(0f, 1.6f, 2.2f), 6f);
    }

    static void DressCrypt(Transform room, RoomSize size)
    {
        var c = size.Center;
        var crypt = Spawn("crypt.fbx", c + new Vector3(0f, 0f, 3.2f), room, 180f, solid: true);
        FitWidth(crypt, 8f);
        Spawn("candle_melted.fbx", c + new Vector3(-3f, 0f, 0.5f), room, 0f, solid: false);
        Spawn("candle_thin.fbx", c + new Vector3(3f, 0f, 0.5f), room, 0f, solid: false);
        Spawn("skull.fbx", c + new Vector3(-1.4f, 0f, -1f), room, 30f, solid: false);
        Spawn("bone_A.fbx", c + new Vector3(1.6f, 0f, -0.6f), room, -20f, solid: false);
        WarmLight(room, c + new Vector3(-3f, 1.3f, 0.5f), 6f);
        WarmLight(room, c + new Vector3(3f, 1.3f, 0.5f), 6f);
    }

    static void AddContinuousGround(Transform parent, Dictionary<string, RoomController> rooms, float surfaceY)
    {
        var coverage = rooms["Cementerio"].GetComponent<BoxCollider>().bounds;
        foreach (var room in rooms.Values)
            coverage.Encapsulate(room.GetComponent<BoxCollider>().bounds);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Suelo continuo";
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(coverage.center.x, surfaceY - 0.55f, coverage.center.z);
        ground.transform.localScale = new Vector3(Mathf.Max(136f, coverage.size.x + 36f), 1f, Mathf.Max(175f, coverage.size.z + 36f));
        ground.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
            "Assets/_ArcanoDungeon/Art/Environments/Castle/CatacombsContinuousFloor.asset");
        ground.GetComponent<MeshRenderer>().sharedMaterial = kitMat;
    }

    static void WireEnemies(Dictionary<string, RoomController> rooms)
    {
        var skeleton = LoadPrefab(Skeleton);
        var warrior = LoadPrefab(Monsters + "EnemySkellWarrior.prefab");
        var orc = LoadPrefab(Monsters + "EnemyOrc.prefab");
        var packSkeleton = LoadPrefab(Monsters + "EnemyPackSkeleton.prefab");
        var wolf = LoadPrefab(Monsters + "EnemyWerewolf.prefab");
        var mage = LoadPrefab(Monsters + "EnemySkellMage.prefab");
        var monster4 = LoadPrefab(Monsters + "EnemyMonster4.prefab");

        AssignCombat(rooms["Galeria"], skeleton, new[] { warrior }, false, null);
        AssignCombat(rooms["Osario"], orc, new[] { packSkeleton }, false, null);
        AssignCombat(rooms["Velatorio"], warrior, new[] { mage }, false, null);
        AssignCombat(rooms["Capilla"], mage, new[] { packSkeleton }, false, null);
        AssignCombat(rooms["Cripta"], wolf, new[] { mage }, true, monster4);
    }

    static void AssignCombat(RoomController room, GameObject primary, GameObject[] extra, bool boss, GameObject mini)
    {
        room.skeletonPrefab = primary;
        room.additionalEnemyPrefabs = extra;
        var a = SpawnPoint(room.transform, room.name + "_A", room.transform.position + new Vector3(-3f, floorY + 0.15f, 1.2f));
        var b = SpawnPoint(room.transform, room.name + "_B", room.transform.position + new Vector3(3f, floorY + 0.15f, -1f));
        room.normalSpawnPoints = new[] { a, b };
        room.spawnMiniBoss = boss;
        if (!boss)
            return;
        room.miniBossPrefab = mini;
        room.miniBossScale = 1.5f;
        room.miniBossMaxHealth = 240;
        room.miniBossSpawnPoint = SpawnPoint(room.transform, "MiniBossSpawn",
            room.transform.position + new Vector3(0f, floorY + 0.15f, -4f));
    }

    static Transform SpawnPoint(Transform parent, string name, Vector3 world)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = world;
        return go.transform;
    }

    static Vector3 Midpoint(Vector3 a, Vector3 b)
    {
        var mid = (a + b) * 0.5f;
        mid.y = floorY + 4f;
        return mid;
    }

    static GameObject Gate(Transform parent, string name, Vector3 world, Vector3 scale)
    {
        var door = new GameObject(name);
        door.transform.SetParent(parent, true);
        door.transform.position = world;
        door.transform.rotation = Quaternion.identity;
        door.transform.localScale = scale;
        bool wideX = scale.x >= scale.z;
        GatePart(door.transform, "GateFrame_Top", new Vector3(0f, 0.48f, 0f),
            wideX ? new Vector3(1.05f, 0.08f, 1.2f) : new Vector3(1.2f, 0.08f, 1.05f), frameMat);
        GatePart(door.transform, "GateFrame_A",
            wideX ? new Vector3(-0.48f, 0f, 0f) : new Vector3(0f, 0f, -0.48f),
            wideX ? new Vector3(0.08f, 1.02f, 1.15f) : new Vector3(1.15f, 1.02f, 0.08f), frameMat);
        GatePart(door.transform, "GateFrame_B",
            wideX ? new Vector3(0.48f, 0f, 0f) : new Vector3(0f, 0f, 0.48f),
            wideX ? new Vector3(0.08f, 1.02f, 1.15f) : new Vector3(1.15f, 1.02f, 0.08f), frameMat);
        for (int i = 0; i < 7; i++)
        {
            float along = Mathf.Lerp(-0.38f, 0.38f, i / 6f);
            var barPos = wideX ? new Vector3(along, -0.02f, 0f) : new Vector3(0f, -0.02f, along);
            var barScale = wideX ? new Vector3(0.07f, 0.92f, 0.55f) : new Vector3(0.55f, 0.92f, 0.07f);
            GatePart(door.transform, "GateBar_" + i, barPos, barScale, goldMat);
        }

        var col = door.AddComponent<BoxCollider>();
        col.size = Vector3.one;
        door.SetActive(false);
        return door;
    }

    static void GatePart(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void AddLink(RoomController room, RoomController other)
    {
        var list = new List<RoomController>();
        if (room.connectedRooms != null)
            list.AddRange(room.connectedRooms);
        if (!list.Contains(other))
            list.Add(other);
        room.connectedRooms = list.ToArray();
        EditorUtility.SetDirty(room);
    }

    static void TileRect(Transform parent, Vector3 center, float width, float depth, string file)
    {
        var probe = Spawn(file, center, parent, 0f, solid: false);
        if (probe == null)
            return;
        var size = BoundsOf(probe);
        if (file.StartsWith("floor_dirt"))
            floorY = size.max.y;
        float tw = Mathf.Max(0.8f, size.size.x * 0.98f);
        float td = Mathf.Max(0.8f, size.size.z * 0.98f);
        Object.DestroyImmediate(probe);

        int nx = Mathf.Max(1, Mathf.CeilToInt(width / tw));
        int nz = Mathf.Max(1, Mathf.CeilToInt(depth / td));
        float x0 = center.x - (nx - 1) * tw * 0.5f;
        float z0 = center.z - (nz - 1) * td * 0.5f;
        for (int x = 0; x < nx; x++)
        {
            for (int z = 0; z < nz; z++)
                Spawn(file, new Vector3(x0 + x * tw, 0f, z0 + z * td), parent, 0f, solid: true);
        }
    }

    static GameObject Spawn(string file, Vector3 pos, Transform parent, float yaw, bool solid)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx + file);
        if (prefab == null)
        {
            Debug.LogWarning("[Catacombs] Falta " + file);
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, true);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.position = pos;
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
                slots[i] = kitMat;
            renderer.sharedMaterials = slots;
        }

        SnapToGround(go, pos.y);
        if (solid)
            AddSolid(go);
        return go;
    }

    static void SnapToGround(GameObject go, float ground)
    {
        var bounds = BoundsOf(go);
        if (bounds.size.y < 0.001f)
            return;
        go.transform.position += new Vector3(0f, ground - bounds.min.y, 0f);
    }

    static void FitWidth(GameObject go, float maxWidth)
    {
        if (go == null)
            return;
        var bounds = BoundsOf(go);
        float wide = Mathf.Max(bounds.size.x, bounds.size.z);
        if (wide <= maxWidth || wide < 0.01f)
            return;
        go.transform.localScale *= maxWidth / wide;
        SnapToGround(go, 0f);
    }

    static void AddSolid(GameObject go)
    {
        var bounds = BoundsOf(go);
        var solid = new GameObject("Solid");
        solid.transform.SetParent(go.transform, true);
        solid.transform.position = bounds.center;
        solid.transform.rotation = Quaternion.identity;
        solid.AddComponent<BoxCollider>().size = bounds.size;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static void WarmLight(Transform parent, Vector3 pos, float range, float intensity = 1.6f)
    {
        var go = new GameObject("CandleLight");
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.32f);
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    static void TuneLight()
    {
        Light light = null;
        foreach (var candidate in Object.FindObjectsByType<Light>())
        {
            if (candidate.type != LightType.Directional) continue;
            light = candidate;
            break;
        }
        if (light == null)
            return;
        light.type = LightType.Directional;
        light.color = new Color(0.62f, 0.74f, 1f);
        light.intensity = 0.16f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
    }

    static void EnsureFollowCamera()
    {
        Camera cam = null;
        foreach (var candidate in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (candidate.CompareTag("MainCamera"))
            {
                cam = candidate;
                break;
            }
        }
        if (cam == null)
            return;

        cam.depth = 0f;
        cam.fieldOfView = 60f;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.offset = new Vector3(0f, 14f, -7f);
        follow.mouseSensitivity = 0.25f;
        follow.distance = 6.5f;
        follow.heightOffset = 1.4f;
        follow.minPitch = 5f;
        follow.maxPitch = 65f;
        follow.collisionOffset = 0.3f;
        follow.minDistance = 0.5f;
        EditorUtility.SetDirty(follow);
    }

    static void TuneAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.09f);
        RenderSettings.fogDensity = 0.023f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.035f, 0.046f, 0.08f);
        RenderSettings.skybox = EnsureNightSky();
        if (RenderSettings.sun != null)
            RenderSettings.sun.intensity = 0.16f;
    }

    public static Material EnsureNightSky()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Art"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Art/Textures"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Art", "Textures");
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Art/Materials"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Art", "Materials");

        if (!File.Exists(NightSkyTexPath))
            WriteNightSkyTexture();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NightSkyTexPath);
        var shader = Shader.Find("Skybox/Panoramic");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(NightSkyMatPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "Mat_NightSky" };
            AssetDatabase.CreateAsset(mat, NightSkyMatPath);
        }

        mat.shader = shader;
        mat.SetTexture("_MainTex", tex);
        mat.SetColor("_Tint", new Color(0.72f, 0.78f, 1f));
        mat.SetFloat("_Exposure", 1.05f);
        mat.SetFloat("_Rotation", 18f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void WriteNightSkyTexture()
    {
        const int w = 1024;
        const int h = 512;
        var pixels = new Color[w * h];
        var horizon = new Color(0.08f, 0.1f, 0.18f);
        var zenith = new Color(0.012f, 0.016f, 0.045f);
        var ground = new Color(0.015f, 0.016f, 0.02f);
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            Color row = v >= 0.5f
                ? Color.Lerp(horizon, zenith, Mathf.SmoothStep(0f, 1f, (v - 0.5f) * 2f))
                : Color.Lerp(horizon, ground, (0.5f - v) * 2f);
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = row;
        }

        var rng = new System.Random(19);
        for (int i = 0; i < 640; i++)
        {
            int x = rng.Next(w);
            int y = rng.Next(h / 2 + 6, h - 6);
            float bright = 0.4f + (float)rng.NextDouble() * 0.6f;
            var star = rng.NextDouble() > 0.86
                ? new Color(bright, bright * 0.86f, 0.62f)
                : new Color(bright * 0.85f, bright * 0.9f, bright);
            pixels[y * w + x] = star;
            if (rng.NextDouble() > 0.9f)
            {
                if (x > 0) pixels[y * w + x - 1] = Color.Lerp(pixels[y * w + x - 1], star, 0.45f);
                if (x < w - 1) pixels[y * w + x + 1] = Color.Lerp(pixels[y * w + x + 1], star, 0.45f);
            }
        }

        int mx = (int)(w * 0.73f);
        int my = (int)(h * 0.74f);
        const int radius = 16;
        for (int y = my - radius - 8; y <= my + radius + 8; y++)
        {
            for (int x = mx - radius - 8; x <= mx + radius + 8; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h)
                    continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(mx, my));
                if (d <= radius)
                {
                    float edge = 1f - d / radius;
                    pixels[y * w + x] = Color.Lerp(new Color(0.85f, 0.88f, 0.78f), new Color(0.98f, 0.97f, 0.9f), edge);
                }
                else if (d < radius + 8f)
                {
                    float halo = 1f - (d - radius) / 8f;
                    pixels[y * w + x] = Color.Lerp(pixels[y * w + x], new Color(0.35f, 0.38f, 0.32f), halo * 0.55f);
                }
            }
        }

        var image = new Texture2D(w, h, TextureFormat.RGB24, false);
        image.SetPixels(pixels);
        image.Apply();
        File.WriteAllBytes(NightSkyTexPath, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(NightSkyTexPath);
    }

    static Material EnsureKitMaterial()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Art"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Art/Materials"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Art", "Materials");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "Mat_HalloweenBits" };
            AssetDatabase.CreateAsset(mat, MatPath);
        }

        mat.shader = shader;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.12f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material LoadOrColor(string path, string name, Color color, float metal, float smooth)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(shader) { name = name };
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metal);
        mat.SetFloat("_Smoothness", smooth);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogWarning("[Catacombs] Falta prefab " + path);
        return prefab;
    }
}

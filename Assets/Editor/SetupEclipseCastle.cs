using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcanoDungeon.Features.Dungeon;
using ArcanoDungeon.Features.Levels;
using ArcanoDungeon.Features.Loot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SetupEclipseCastle
{
    public const string ScenePath = "Assets/_ArcanoDungeon/Scenes/CastleLevel.unity";
    const string Source = "Assets/_ArcanoDungeon/Scenes/CatacombsLevel.unity";
    const string Kit = "Assets/chibi_asssets/KayKit_Dungeon_Pack_1.1_FREE/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    const string Art = "Assets/_ArcanoDungeon/Art/Environments/Castle/Eclipse/";
    const string Monsters = "Assets/_ArcanoDungeon/Prefabs/Monsters/";
    const float Opening = 5f;
    static Material stone, masonry, floor, kit, gold, iron, carpet, rune, shadow, dust;
    static LootDropTable loot;
    static float wallHeight;

    sealed class Chamber
    {
        public string Id;
        public string Title;
        public RoomController.RoomKind Kind;
        public Vector3 Center;
        public float Width = 18f;
        public float Depth = 18f;
        public RoomController Room;
        public readonly HashSet<int> Openings = new HashSet<int>(); // north, east, south, west
        public readonly List<GameObject> Doors = new List<GameObject>();
    }

    [MenuItem("Tools/Arcano/Build Castillo Eclipse", false, 133)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Sal de Play Mode antes de construir el castillo.");
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        BuildDungeonRuntimeAssets.Rebuild();
        var sourceScene = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(sourceScene, ScenePath, true);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var sourceSpawner = Object.FindAnyObjectByType<PlayerSpawner>();
        var heroes = new[] { sourceSpawner.magePrefab, sourceSpawner.knightPrefab, sourceSpawner.misakiPrefab, sourceSpawner.yukoPrefab };
        wallHeight = GetWallHeight(heroes);
        var sourceExit = Object.FindObjectsByType<LevelExit>(FindObjectsInactive.Include).First();
        var portal = Object.Instantiate(sourceExit.gameObject);
        portal.name = "EclipseVictoryPortal";
        portal.transform.SetParent(null);
        Object.DestroyImmediate(portal.GetComponent<LevelExit>());
        var old = GameObject.Find("CatacombsRoot");
        if (old != null) Object.DestroyImmediate(old);
        foreach (var pause in Object.FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include))
            Object.DestroyImmediate(pause.gameObject);

        PrepareMaterials();
        loot = BuildLootTable();
        var bossPrefab = BuildBoss();
        var root = new GameObject("CastilloEclipseRoot");
        var atrio = Room("Atrio", "Atrio de la Última Luz", new Vector3(0f, 0f, 0f), RoomController.RoomKind.Start);
        var guardia = Room("Guardia", "Guardia del Eclipse", new Vector3(0f, 0f, 27f));
        var armeria = Room("Armeria", "Armería · Sello del Sol", new Vector3(27f, 0f, 27f));
        var biblioteca = Room("Biblioteca", "Biblioteca de Ceniza", new Vector3(-27f, 0f, 27f), RoomController.RoomKind.Treasure);
        var galeria = Room("Galeria", "Galería de los Juramentos", new Vector3(0f, 0f, 54f));
        var capilla = Room("Capilla", "Capilla · Sello de la Luna", new Vector3(-27f, 0f, 54f));
        var trono = Room("Trono", "Trono del Eclipse", new Vector3(0f, 0f, 84f), RoomController.RoomKind.Boss);
        trono.Width = trono.Depth = 24f;
        var rooms = new[] { atrio, guardia, armeria, biblioteca, galeria, capilla, trono };
        var links = new[] { (atrio, guardia), (guardia, armeria), (guardia, biblioteca), (guardia, galeria),
            (biblioteca, capilla), (galeria, capilla), (galeria, trono) };
        foreach (var link in links) RegisterLink(link.Item1, link.Item2);
        foreach (var chamber in rooms) BuildRoom(root.transform, chamber);
        foreach (var link in links) Corridor(root.transform, link.Item1, link.Item2);
        AddBackdrop(root.transform);

        Combat(guardia, new[] { "EnemySkellWarrior", "EnemySkellMage" }, 3);
        Combat(armeria, new[] { "EnemyOrc", "EnemySkellWarrior" }, 3);
        Combat(galeria, new[] { "EnemyWerewolf", "EnemySkellMage", "EnemySkellWarrior" }, 4);
        Combat(capilla, new[] { "EnemySkellMage", "EnemyPackSkeleton" }, 3);
        capilla.Room.spawnMiniBoss = true;
        capilla.Room.miniBossPrefab = Load<GameObject>("Assets/_ArcanoDungeon/Prefabs/EnemyLichPolyart.prefab");
        capilla.Room.miniBossMaxHealth = 320;
        capilla.Room.miniBossScale = 1f;
        capilla.Room.miniBossSpawnPoint = Point(capilla.Room.transform, "LunarGuardian", capilla.Center + new Vector3(0f, 0.2f, 3f));
        Combat(trono, new[] { "EnemySkellWarrior", "EnemySkellMage" }, 2);
        trono.Room.spawnMiniBoss = true;
        trono.Room.miniBossPrefab = bossPrefab;
        trono.Room.miniBossMaxHealth = 680;
        trono.Room.miniBossScale = 1.1f;
        trono.Room.miniBossSpawnPoint = Point(trono.Room.transform, "RegenteSpawn", trono.Center + new Vector3(0f, 0.2f, 2f));
        trono.Room.secondBossPrefab = Load<GameObject>(Monsters + "EnemyMonsterLowPoly.prefab");
        trono.Room.secondBossSpawnPoint = Point(trono.Room.transform, "MonsterLowPolySpawn", trono.Center + new Vector3(5f, 0.2f, 1f));

        Chest(biblioteca, new Vector3(0f, 0f, 5f), false);
        Chest(armeria, new Vector3(5f, 0f, 4f), true);
        Chest(capilla, new Vector3(-5f, 0f, 4f), true);
        Chest(galeria, new Vector3(5f, 0f, 5f), true);
        var castle = root.AddComponent<EclipseCastleController>();
        castle.seals = new[] { Beacon(armeria.Room, armeria.Center + new Vector3(0f, 0f, 5.5f), "Sello del Sol"),
            Beacon(capilla.Room, capilla.Center + new Vector3(0f, 0f, 5.5f), "Sello de la Luna") };
        castle.throneRoom = trono.Room;
        castle.throneBarrier = Gate(root.transform, "Barrera de los dos sellos", new Vector3(0f, 0f, 67.5f), false, true);
        castle.throneBarrier.SetActive(true);

        portal.transform.SetParent(trono.Room.transform, true);
        portal.transform.position = trono.Center + new Vector3(0f, 1.2f, 6f);
        portal.transform.rotation = Quaternion.identity;
        var portalCollider = portal.GetComponent<BoxCollider>();
        portalCollider.isTrigger = true;
        portalCollider.size = new Vector3(3f, 2.4f, 1.6f);
        portal.AddComponent<CastleVictoryPortal>().castle = castle;
        portal.SetActive(false);
        trono.Room.objectToActivateOnClear = portal;

        var spawner = new GameObject("PlayerSpawner").AddComponent<PlayerSpawner>();
        spawner.transform.SetParent(root.transform);
        spawner.magePrefab = heroes[0]; spawner.knightPrefab = heroes[1];
        spawner.misakiPrefab = heroes[2]; spawner.yukoPrefab = heroes[3];
        spawner.startPosition = Point(root.transform, "PlayerStart", new Vector3(0f, 0.2f, -3f));
        new GameObject("PauseMenu").AddComponent<PauseMenuController>().transform.SetParent(root.transform);
        var map = new GameObject("DungeonMinimap").AddComponent<DungeonMinimapUI>();
        map.transform.SetParent(root.transform);
        var mapData = new SerializedObject(map);
        mapData.FindProperty("floorLabel").stringValue = "3-1 · Eclipse";
        mapData.ApplyModifiedPropertiesWithoutUndo();
        BuildHealthHud(root.transform);
        EnsureEventSystem();
        Lighting(root.transform);
        EclipseSky(root.transform, new Vector3(-20f, 7f, 123f));

        foreach (var chamber in rooms)
        {
            chamber.Room.exitDoors = chamber.Doors.ToArray();
            EditorUtility.SetDirty(chamber.Room);
        }
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (!buildScenes.Any(s => s.path == ScenePath))
            buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static Chamber Room(string id, string title, Vector3 center, RoomController.RoomKind kind = RoomController.RoomKind.Combat)
    {
        return new Chamber { Id = id, Center = center, Room = null, Title = title, Kind = kind };
    }

    static void RegisterLink(Chamber a, Chamber b)
    {
        bool horizontal = Mathf.Abs(a.Center.x - b.Center.x) > 1f;
        int side = horizontal ? (b.Center.x > a.Center.x ? 1 : 3) : (b.Center.z > a.Center.z ? 0 : 2);
        a.Openings.Add(side); b.Openings.Add((side + 2) % 4);
    }

    static void BuildRoom(Transform root, Chamber c)
    {
        var go = new GameObject(c.Id);
        go.transform.SetParent(root, false); go.transform.position = c.Center;
        Tiles(go.transform, c.Center, c.Width, c.Depth);
        c.Room = go.AddComponent<RoomController>();
        c.Room.roomName = c.Title; c.Room.roomKind = c.Kind;
        var trigger = go.GetComponent<BoxCollider>();
        trigger.isTrigger = true; trigger.center = new Vector3(0f, 2.5f, 0f);
        trigger.size = new Vector3(c.Width - 2f, 5f, c.Depth - 2f);
        for (int side = 0; side < 4; side++) Wall(go.transform, c, side);
        foreach (int side in c.Openings)
        {
            bool horizontal = side == 1 || side == 3;
            Vector3 direction = side == 0 ? Vector3.forward : side == 1 ? Vector3.right : side == 2 ? Vector3.back : Vector3.left;
            float extent = horizontal ? c.Width * 0.5f : c.Depth * 0.5f;
            var door = Gate(go.transform, c.Id + "_Gate_" + side, c.Center + direction * (extent + 0.4f), horizontal, false);
            c.Doors.Add(door);
        }
        Decorate(c);
        Motes(go.transform, c.Center, c.Width, c.Depth);
    }

    static void Tiles(Transform parent, Vector3 center, float width, float depth, bool horizontalRunner = false)
    {
        for (float x = -width * 0.5f + 1.5f; x < width * 0.5f; x += 3f)
            for (float z = -depth * 0.5f + 1.5f; z < depth * 0.5f; z += 3f)
            {
                var tile = Model("floor_tile_large.fbx", parent, center + new Vector3(x, 0f, z), 0f, 3f, 0f, floor);
                var bounds = BoundsOf(tile);
                tile.transform.position += Vector3.down * bounds.max.y;
            }
        var foundation = Block("Foundation", parent, center + new Vector3(0f, -0.48f, 0f), new Vector3(width, 0.8f, depth), stone, true);
        foundation.GetComponent<BoxCollider>().size = Vector3.one;
        foundation.GetComponent<BoxCollider>().center = Vector3.up * 0.1f;
        Block("Runner", parent, center + new Vector3(0f, 0.022f, 0f),
            horizontalRunner ? new Vector3(width, 0.025f, 3.3f) : new Vector3(3.3f, 0.025f, depth - 1f), carpet, false);
        foreach (float offset in new[] { -1.75f, 1.75f })
            Block("RunnerGoldTrim", parent, center + (horizontalRunner ? new Vector3(0f, 0.03f, offset) : new Vector3(offset, 0.03f, 0f)),
                horizontalRunner ? new Vector3(width, 0.025f, 0.06f) : new Vector3(0.06f, 0.025f, depth - 1f), gold, false);
    }

    static void Wall(Transform parent, Chamber c, int side)
    {
        bool horizontal = side == 0 || side == 2;
        float span = horizontal ? c.Width : c.Depth;
        Vector3 normal = side == 0 ? Vector3.forward : side == 1 ? Vector3.right : side == 2 ? Vector3.back : Vector3.left;
        Vector3 along = horizontal ? Vector3.right : Vector3.forward;
        float offset = horizontal ? c.Depth * 0.5f : c.Width * 0.5f;
        bool open = c.Openings.Contains(side);
        for (float distance = -span * 0.5f + 1.5f; distance < span * 0.5f; distance += 3f)
        {
            if (open && Mathf.Abs(distance) < Opening * 0.5f + 1.4f) continue;
            var position = c.Center + normal * offset + along * distance;
            Model(Mathf.Abs(distance) > span * 0.3f ? "wall_pillar.fbx" : "wall_archedwindow_gated.fbx",
                parent, position, horizontal ? 0f : 90f, 3f, wallHeight, stone);
            Block("WallCollision", parent, position + Vector3.up * (wallHeight * 0.5f),
                horizontal ? new Vector3(3f, wallHeight, 0.8f) : new Vector3(0.8f, wallHeight, 3f), null, true);
        }
        // Solid spans leave exactly five metres of passage, including at the edges of tiled wall models.
        float length = open ? (span - Opening) * 0.5f : span;
        foreach (int sign in open ? new[] { -1, 1 } : new[] { 0 })
        {
            var position = c.Center + normal * offset + along * (sign * (Opening * 0.5f + length * 0.5f));
            Block("MasonryBase", parent, position + Vector3.up * 0.3f,
                horizontal ? new Vector3(length, 0.6f, 0.9f) : new Vector3(0.9f, 0.6f, length), stone, true);
            Block("WallSafety", parent, position + Vector3.up * (wallHeight * 0.5f),
                horizontal ? new Vector3(length, wallHeight, 0.65f) : new Vector3(0.65f, wallHeight, length), null, true);
        }
        foreach (float alongOffset in new[] { -span * 0.5f, span * 0.5f })
            Model("pillar_decorated.fbx", parent, c.Center + normal * offset + along * alongOffset, 0f, 1.1f, wallHeight, stone);
    }

    static void Corridor(Transform root, Chamber a, Chamber b)
    {
        bool horizontal = Mathf.Abs(a.Center.x - b.Center.x) > 1f;
        var direction = (b.Center - a.Center).normalized;
        float halfA = (horizontal ? a.Width : a.Depth) * 0.5f;
        float halfB = (horizontal ? b.Width : b.Depth) * 0.5f;
        float length = Vector3.Distance(a.Center, b.Center) - halfA - halfB;
        var center = (a.Center + direction * halfA + b.Center - direction * halfB) * 0.5f;
        var parent = new GameObject(a.Id + "_" + b.Id + "_Passage").transform;
        parent.SetParent(root);
        Tiles(parent, center, horizontal ? length : 6f, horizontal ? 6f : length, horizontal);
        foreach (float sign in new[] { -1f, 1f })
        {
            var offset = horizontal ? Vector3.forward * (sign * 3.15f) : Vector3.right * (sign * 3.15f);
            Block("BridgeParapet", parent, center + offset + Vector3.up * (wallHeight * 0.5f),
                horizontal ? new Vector3(length, wallHeight, 0.45f) : new Vector3(0.45f, wallHeight, length), stone, true);
            Block("ParapetGold", parent, center + offset + Vector3.up * (wallHeight + 0.04f),
                horizontal ? new Vector3(length, 0.07f, 0.48f) : new Vector3(0.48f, 0.07f, length), gold, false);
        }
        var linkA = a.Room.connectedRooms?.ToList() ?? new List<RoomController>(); linkA.Add(b.Room); a.Room.connectedRooms = linkA.ToArray();
        var linkB = b.Room.connectedRooms?.ToList() ?? new List<RoomController>(); linkB.Add(a.Room); b.Room.connectedRooms = linkB.ToArray();
    }

    static void Decorate(Chamber c)
    {
        var parent = c.Room.transform;
        foreach (float x in new[] { -c.Width * 0.5f + 2f, c.Width * 0.5f - 2f })
            foreach (float z in new[] { -c.Depth * 0.5f + 2f, c.Depth * 0.5f - 2f })
            {
                var pos = c.Center + new Vector3(x, 0f, z);
                Model("pillar_decorated.fbx", parent, pos, 0f, 0.9f, 3.3f, stone);
                Model("torch_lit.fbx", parent, pos + Vector3.up * 2.5f, 0f, 0.28f, 0.8f, kit);
                Light(parent, pos + Vector3.up * 3.1f, new Color(1f, 0.57f, 0.23f), 2.4f, 9f);
            }
        foreach (float x in new[] { -5f, 5f })
            Model("banner_patternC_red.fbx", parent, c.Center + new Vector3(x, 0f, c.Depth * 0.5f - 0.6f), 180f, 1.5f, 3f, kit);

        if (c.Id == "Biblioteca" || c.Id == "Capilla")
        {
            foreach (float z in new[] { -4f, 1f, 5.5f })
                Model("wall_shelves.fbx", parent, c.Center + new Vector3(-7.6f, 0f, z), 90f, 3f, 3.6f, kit);
            Model("table_long_decorated_A.fbx", parent, c.Center + new Vector3(4.5f, 0f, 1f), 90f, 1.9f, 1.2f, kit);
        }
        else if (c.Id == "Armeria")
        {
            foreach (float z in new[] { -4f, 0f, 4f })
            {
                Model("barrel_large_decorated.fbx", parent, c.Center + new Vector3(-6.5f, 0f, z), 0f, 1.2f, 1.5f, kit);
                Model("banner_shield_red.fbx", parent, c.Center + new Vector3(7.7f, 0f, z), 90f, 1.6f, 2.6f, kit);
            }
        }
        else if (c.Id == "Trono")
        {
            Block("ThroneDais", parent, c.Center + new Vector3(0f, 0.16f, 8.8f), new Vector3(8f, 0.32f, 4f), stone, true);
            var throne = Model("chair.fbx", parent, c.Center + new Vector3(0f, 0.32f, 9f), 180f, 2.2f, 3.6f, kit);
            throne.name = "Trono del Regente";
            foreach (float x in new[] { -3.5f, 3.5f })
                Model("pillar_decorated.fbx", parent, c.Center + new Vector3(x, 0f, 9f), 0f, 1.5f, 7f, stone);
            Ring(parent, "SolarSeal", c.Center + new Vector3(0f, 0.065f, 1f), 6f, gold, false);
            Ring(parent, "LunarSeal", c.Center + new Vector3(0f, 0.07f, 1f), 4.5f, rune, false);
        }
        else
        {
            foreach (float x in new[] { -6f, 6f })
            {
                Model("column.fbx", parent, c.Center + new Vector3(x, 0f, 2f), 0f, 1.1f, 2f, stone);
                Model("barrel_small_stack.fbx", parent, c.Center + new Vector3(x, 0f, -4f), 0f, 1.3f, 1.5f, kit);
            }
        }
    }

    static void Combat(Chamber c, string[] names, int count)
    {
        c.Room.skeletonPrefab = Load<GameObject>(Monsters + names[0] + ".prefab");
        c.Room.additionalEnemyPrefabs = names.Skip(1).Select(n => Load<GameObject>(Monsters + n + ".prefab")).ToArray();
        var points = new List<Transform>();
        var positions = new[] { new Vector3(-3f, 0.2f, 2f), new Vector3(3f, 0.2f, 2f), new Vector3(0f, 0.2f, 4f), new Vector3(4f, 0.2f, -2f) };
        for (int i = 0; i < count; i++) points.Add(Point(c.Room.transform, "EnemySpawn_" + i, c.Center + positions[i]));
        c.Room.normalSpawnPoints = points.ToArray();
    }

    static void Chest(Chamber c, Vector3 offset, bool reward)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/Prefabs/TreasureChest.prefab"));
        go.transform.SetParent(c.Room.transform, false);
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Fit(go, 1.7f, 0f);
        go.transform.position += c.Center + offset;
        var chest = go.GetComponent<TreasureChest>(); chest.dropTable = loot; chest.allowMimic = false;
        if (reward) { go.SetActive(false); c.Room.objectToActivateOnClear = go; }
    }

    static EclipseSealCrystal Beacon(RoomController room, Vector3 position, string name)
    {
        var root = new GameObject(name); root.transform.SetParent(room.transform); root.transform.position = position;
        Block("SigilPedestal", root.transform, position + Vector3.up * 0.3f, new Vector3(1.6f, 0.6f, 1.6f), gold, true);
        var model = Load<GameObject>("Assets/_ArcanoDungeon/Art/Environments/Castle/Corazon_Arcano.fbx");
        var gem = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        gem.name = "EclipseCrystal";
        gem.transform.localPosition = Vector3.up * 1.7f;
        gem.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        gem.transform.localScale = Vector3.one * 0.7f;
        gem.AddComponent<CrystalFloat>();
        CrystalMaterials.Apply(gem);
        foreach (var renderer in gem.GetComponentsInChildren<Renderer>(true))
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        Ring(root.transform, "SealOrbit", position + Vector3.up * 1.7f, 1.1f, gold, false);
        Light(root.transform, position + Vector3.up * 2f, new Color(0.60f, 0.28f, 1f), 3f, 8f);
        return ConfigureSeal(gem, room);
    }

    public static EclipseSealCrystal ConfigureSeal(GameObject crystal, RoomController room)
    {
        var seal = Undo.AddComponent<EclipseSealCrystal>(crystal);
        var serialized = new SerializedObject(seal);
        serialized.FindProperty("room").objectReferenceValue = room;
        serialized.ApplyModifiedProperties();
        ConfigureSealShards(seal);
        var collider = seal.GetComponent<BoxCollider>();
        Undo.RecordObject(collider, "Size crystal hitbox");
        var scale = crystal.transform.lossyScale;
        // Use an even hitbox so the floating crystal can rotate freely above its pedestal.
        collider.size = new Vector3(1.25f / Mathf.Abs(scale.x), 1.25f / Mathf.Abs(scale.y), 1.25f / Mathf.Abs(scale.z));
        collider.enabled = false;
        return seal;
    }

    public static void ConfigureSealShards(EclipseSealCrystal seal)
    {
        var serialized = new SerializedObject(seal);
        serialized.FindProperty("shardMesh").objectReferenceValue = Load<Mesh>(Art + "EclipseCrystal.asset");
        serialized.FindProperty("shardMaterial").objectReferenceValue = Load<Material>(Art + "EclipseRune.mat");
        serialized.ApplyModifiedProperties();
    }

    public static float GetWallHeight(params GameObject[] heroes)
    {
        float tallest = heroes.Where(hero => hero != null)
            .Select(hero => hero.GetComponent<CharacterController>())
            .Where(controller => controller != null)
            .Max(controller => controller.height * Mathf.Abs(controller.transform.lossyScale.y));
        return Mathf.Max(8f, Mathf.Ceil(tallest * 4f));
    }

    public static GameObject AddBackdrop(Transform castleRoot)
    {
        var foundations = castleRoot.GetComponentsInChildren<Renderer>()
            .Where(renderer => renderer.name == "Foundation").ToArray();
        var bounds = foundations[0].bounds;
        foreach (var foundation in foundations.Skip(1))
            bounds.Encapsulate(foundation.bounds);

        var backdrop = new GameObject("Entorno del castillo");
        backdrop.transform.SetParent(castleRoot, false);
        var material = Load<Material>(Art + "EclipseMasonry.mat");
        AddBackdropSurface("Suelo exterior", backdrop.transform, material, bounds, bounds.min.y - 1f, 60f);
        AddBackdropSurface("Techo de piedra", backdrop.transform, material, bounds, bounds.max.y + 10f, 20f);
        return backdrop;
    }

    static void AddBackdropSurface(string name, Transform parent, Material material, Bounds bounds, float height, float margin)
    {
        var position = new Vector3(bounds.center.x, height, bounds.center.z);
        var size = new Vector3(bounds.size.x + 2f * margin, 0.8f, bounds.size.z + 2f * margin);
        var surface = Block(name, parent, position, size, material, false);
        surface.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    static GameObject Gate(Transform parent, string name, Vector3 position, bool horizontal, bool sealedGate)
    {
        var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = position;
        Vector3 along = horizontal ? Vector3.forward : Vector3.right;
        for (int i = -3; i <= 3; i++)
            Block("PortcullisBar", root.transform, position + along * (i * 0.71f) + Vector3.up * 2.5f,
                new Vector3(0.12f, 5f, 0.12f), sealedGate ? rune : gold, false);
        foreach (float y in new[] { 0.5f, 2.5f, 4.7f })
            Block("GateCrossbar", root.transform, position + Vector3.up * y,
                horizontal ? new Vector3(0.25f, 0.13f, Opening) : new Vector3(Opening, 0.13f, 0.25f), iron, false);
        var col = root.AddComponent<BoxCollider>(); col.center = Vector3.up * 2.5f;
        col.size = horizontal ? new Vector3(0.5f, 5f, Opening) : new Vector3(Opening, 5f, 0.5f);
        root.SetActive(false); return root;
    }

    static void Lighting(Transform root)
    {
        var sun = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
        sun.color = new Color(0.83f, 0.78f, 1f); sun.intensity = 1.2f;
        sun.transform.rotation = Quaternion.Euler(42f, -32f, 0f); sun.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.40f, 0.35f, 0.51f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.26f, 0.37f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.12f, 0.21f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.075f, 0.05f, 0.12f); RenderSettings.fogDensity = 0.0045f;
        var fillLight = new GameObject("EclipseSoftFill").AddComponent<Light>();
        fillLight.transform.SetParent(root); fillLight.type = LightType.Directional;
        fillLight.transform.rotation = Quaternion.Euler(28f, 140f, 0f);
        fillLight.color = new Color(1f, 0.78f, 0.58f); fillLight.intensity = 0.4f;
        fillLight.shadows = LightShadows.None;
        var sky = Load<Material>(Art + "EclipseSky.mat");
        if (sky == null && RenderSettings.skybox != null)
        { sky = new Material(RenderSettings.skybox); AssetDatabase.CreateAsset(sky, Art + "EclipseSky.mat"); }
        if (sky != null)
        { sky.SetColor("_Tint", new Color(0.35f, 0.22f, 0.52f)); sky.SetFloat("_Exposure", 0.65f); RenderSettings.skybox = sky; EditorUtility.SetDirty(sky); }
        var volume = Object.FindAnyObjectByType<Volume>();
        var profile = Load<VolumeProfile>(Art + "EclipseVolume.asset");
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, Art + "EclipseVolume.asset"); }
        var bloom = Override<Bloom>(profile); bloom.intensity.Override(0.32f); bloom.threshold.Override(1.1f);
        var tone = Override<Tonemapping>(profile); tone.mode.Override(TonemappingMode.Neutral);
        var vignette = Override<Vignette>(profile); vignette.intensity.Override(0.22f);
        var grade = Override<ColorAdjustments>(profile); grade.postExposure.Override(0.2f); grade.contrast.Override(7f); grade.saturation.Override(5f);
        foreach (var component in profile.components)
            if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
        volume.isGlobal = true; volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        var camera = Camera.main;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.transform.position = new Vector3(0f, 4.5f, -9f);
        camera.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
    }

    static T Override<T>(VolumeProfile profile) where T : VolumeComponent
    {
        return profile.TryGet<T>(out var component) ? component : profile.Add<T>(true);
    }

    static void EclipseSky(Transform parent, Vector3 position)
    {
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere); core.name = "Eclipse Obscura";
        core.transform.SetParent(parent); core.transform.position = position; core.transform.localScale = new Vector3(7f, 7f, 0.25f);
        Object.DestroyImmediate(core.GetComponent<Collider>()); core.GetComponent<Renderer>().sharedMaterial = shadow;
        Ring(parent, "Corona del Eclipse", position - Vector3.forward * 0.2f, 3.65f, gold, true);
        Ring(parent, "Halo de Penumbra", position - Vector3.forward * 0.18f, 3.95f, rune, true);
    }

    static void Ring(Transform parent, string name, Vector3 position, float radius, Material material, bool vertical)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent); line.transform.position = position;
        line.useWorldSpace = false; line.loop = true; line.positionCount = 80; line.widthMultiplier = vertical ? 0.18f : 0.055f;
        line.sharedMaterial = material;
        for (int i = 0; i < 80; i++)
        { float a = i * Mathf.PI * 2f / 80f; line.SetPosition(i, vertical ? new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f) : new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius)); }
    }

    static void Motes(Transform parent, Vector3 position, float width, float depth)
    {
        var ps = new GameObject("Cenizas del Eclipse").AddComponent<ParticleSystem>(); ps.transform.SetParent(parent); ps.transform.position = position + Vector3.up * 2f;
        var main = ps.main; main.startLifetime = 7f; main.startSpeed = 0.12f; main.startSize = 0.045f; main.maxParticles = 24;
        main.startColor = new Color(0.72f, 0.53f, 1f, 0.35f); main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = 2.5f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(width - 2f, 3f, depth - 2f);
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = dust;
    }

    static void BuildHealthHud(Transform parent)
    {
        var root = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); root.transform.SetParent(parent);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f);
        HudBar(root.transform, "Health", "VIDA", -49f, new Color(0.78f, 0.15f, 0.22f), 100f);
        HudBar(root.transform, "Shield", "ESCUDO", -91f, new Color(0.45f, 0.85f, 1f), 30f);
        HudBar(root.transform, "Stamina", "ESTAMINA", -133f, new Color(0.95f, 0.82f, 0.2f), 100f);
    }

    static void HudBar(Transform parent, string id, string title, float y, Color color, float maximum)
    {
        var bar = new GameObject("Player" + id + "Bar", typeof(RectTransform), typeof(Image), typeof(Slider)); bar.transform.SetParent(parent, false);
        var rt = bar.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = new Vector2(28f, y); rt.sizeDelta = new Vector2(290f, 13f);
        bar.GetComponent<Image>().color = new Color(0.07f, 0.05f, 0.10f, 0.95f);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(rt, false);
        var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = new Vector2(2f, 2f); fr.offsetMax = new Vector2(-2f, -2f);
        fill.GetComponent<Image>().color = color;
        var slider = bar.GetComponent<Slider>(); slider.fillRect = fr; slider.targetGraphic = fill.GetComponent<Image>(); slider.interactable = false; slider.maxValue = maximum; slider.value = maximum;
        var text = new GameObject("Player" + id + "Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)); text.transform.SetParent(parent, false);
        var tr = text.GetComponent<RectTransform>(); tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0f, 1f); tr.anchoredPosition = new Vector2(28f, y + 23f); tr.sizeDelta = new Vector2(290f, 22f);
        var label = text.GetComponent<TMPro.TextMeshProUGUI>(); label.text = maximum + " / " + maximum; label.fontSize = 17; label.color = new Color(0.94f, 0.91f, 0.82f); label.alignment = TMPro.TextAlignmentOptions.Right;
        var fontMaterial = Load<Material>(Art + "EclipseHUDText.mat");
        if (fontMaterial == null)
        {
            fontMaterial = new Material(label.fontSharedMaterial);
            fontMaterial.SetColor("_FaceColor", Color.white);
            fontMaterial.SetColor("_OutlineColor", new Color(0.015f, 0.01f, 0.03f));
            fontMaterial.SetFloat("_OutlineWidth", 0.12f);
            AssetDatabase.CreateAsset(fontMaterial, Art + "EclipseHUDText.mat");
        }
        label.fontSharedMaterial = fontMaterial;
        var caption = Object.Instantiate(text, parent); caption.name = id + "Caption";
        var captionText = caption.GetComponent<TMPro.TextMeshProUGUI>(); captionText.text = title; captionText.alignment = TMPro.TextAlignmentOptions.Left;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static GameObject BuildBoss()
    {
        var source = Load<GameObject>(Monsters + "EnemyMinotaur.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = "RegenteDelEclipse";
        go.GetComponent<EnemyHealth>().ConfigureHealth(680);
        var ai = go.GetComponent<EnemyAI>(); ai.attackDamage = 22; ai.moveSpeed = 2.6f;
        var nova = go.GetComponent<EclipseBossAttack>() ?? go.AddComponent<EclipseBossAttack>(); nova.warningMaterial = rune;
        var result = PrefabUtility.SaveAsPrefabAsset(go, Monsters + "EnemyEclipseRegent.prefab");
        Object.DestroyImmediate(go); return result;
    }

    static LootDropTable BuildLootTable()
    {
        const string path = "Assets/_ArcanoDungeon/Features/Loot/Items/LootDropTable_Castle.asset";
        var table = Load<LootDropTable>(path);
        if (table == null) { table = ScriptableObject.CreateInstance<LootDropTable>(); AssetDatabase.CreateAsset(table, path); }
        var source = Load<LootDropTable>("Assets/_ArcanoDungeon/Features/Loot/Items/LootDropTable_Catacombs.asset");
        table.entries = source.entries.Where(e => e?.item != null).Select(e => new LootDropEntry
        { item = e.item, weightOverride = e.item.type == ItemType.Passive ? 15 : e.item.type == ItemType.Weapon ? 10 : 8 }).ToList();
        EditorUtility.SetDirty(table); return table;
    }

    static void PrepareMaterials()
    {
        kit = Load<Material>("Assets/_ArcanoDungeon/Art/Materials/KayKit_Dungeon_URP.mat");
        stone = Material("EclipseStone", new Color(0.55f, 0.51f, 0.68f), kit);
        masonry = Material("EclipseMasonry", new Color(0.22f, 0.20f, 0.29f));
        floor = Material("EclipseFloor", new Color(0.72f, 0.72f, 0.83f), kit);
        gold = Material("EclipseGold", new Color(1f, 0.68f, 0.25f), null, true);
        iron = Material("EclipseIron", new Color(0.13f, 0.10f, 0.20f));
        carpet = Material("EclipseVelvet", new Color(0.25f, 0.035f, 0.075f));
        rune = Material("EclipseRune", new Color(0.65f, 0.25f, 1f) * 2.1f, null, true);
        shadow = Material("EclipseVoid", new Color(0.012f, 0.007f, 0.025f), null, true);
        dust = Load<Material>(Art + "EclipseAsh.mat");
        if (dust == null) { dust = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(dust, Art + "EclipseAsh.mat"); }
    }

    static Material Material(string name, Color color, Material template = null, bool unlit = false)
    {
        string path = Art + name + ".mat";
        var mat = Load<Material>(path);
        if (mat == null) { mat = template != null ? new Material(template) : new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.22f);
        EditorUtility.SetDirty(mat); return mat;
    }

    static GameObject Model(string file, Transform parent, Vector3 position, float yaw, float width, float height, Material material)
    {
        var asset = Load<GameObject>(Kit + file);
        if (asset == null) throw new InvalidOperationException("Falta el modelo " + file);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.SetParent(parent, false); go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Fit(go, width, height); go.transform.position += position;
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
        return go;
    }

    static void Fit(GameObject go, float width, float height)
    {
        var b = BoundsOf(go);
        float footprint = Mathf.Max(b.size.x, b.size.z);
        go.transform.localScale *= footprint > 0.001f ? width / footprint : 1f;
        if (height > 0f)
        {
            b = BoundsOf(go);
            var scale = go.transform.localScale; scale.y *= height / Mathf.Max(0.001f, b.size.y); go.transform.localScale = scale;
        }
        b = BoundsOf(go); go.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
    }

    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.one);
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool solid)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
        go.transform.position = position; go.transform.localScale = size;
        if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = material == stone ? masonry : material;
        else Object.DestroyImmediate(go.GetComponent<Renderer>());
        return go;
    }

    static Transform Point(Transform parent, string name, Vector3 position)
    { var point = new GameObject(name).transform; point.SetParent(parent); point.position = position; return point; }
    static void Light(Transform parent, Vector3 position, Color color, float intensity, float range)
    { var light = new GameObject("EclipseTorchLight").AddComponent<Light>(); light.transform.SetParent(parent); light.transform.position = position; light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None; }
    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class CharacterSelectSceneBake
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/CharacterSelection.unity";
    const string AltarPath = "Assets/_ArcanoDungeon/Art/Environments/Castle/Altar_Menu.fbx";
    const string VolumePath = "Assets/_ArcanoDungeon/Art/CharacterSelectVolume.asset";
    const string FirePrefabPath =
        "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire.prefab";
    const string AmbientGlowsPath =
        "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR3 Ambient Glows.prefab";
    const string MagicAuraPath =
        "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR3 Magic Aura A (Runic).prefab";
    const string MagicCirclePath =
        "Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)/Prefabs/FX_MagicCircle_Icearrow04.prefab";

    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f);
    static readonly Color PanelFill = new Color(0.06f, 0.07f, 0.11f, 0.94f);
    static readonly Color PanelBorder = new Color(0.78f, 0.62f, 0.24f, 1f);
    static readonly Color CardFill = new Color(0.04f, 0.05f, 0.09f, 0.92f);
    static readonly Color Ink = new Color(0.94f, 0.91f, 0.82f);
    static readonly Color InkDark = new Color(0.12f, 0.10f, 0.08f);
    static readonly Color InkMuted = new Color(0.72f, 0.68f, 0.58f);
    static readonly Color BarTrack = new Color(0.18f, 0.16f, 0.22f, 0.9f);
    static readonly Color BarFill = new Color(0.86f, 0.62f, 0.22f, 1f);

    [MenuItem("Arcano Dungeon/Bake Character Selection Scene", false, 100)]
    public static void Bake()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = EnsureRoot();
        var lineup = EnsureChild(root.transform, "Lineup");
        ClearChildren(lineup);

        BuildEnvironment(lineup);
        var altar = PlaceAltar(lineup);
        var standY = MeasureStandHeight(altar);
        if (altar != null)
        {
            BakeCandles(altar);
            BakeRitualFx(lineup, altar, standY);
        }

        BuildHeroStage(lineup, standY);
        BuildLights(lineup, standY);

        var ui = BuildUi(root.transform);
        EnsureCamera();
        EnsureVolume();
        EnsureEventSystem();

        var presenter = root.GetComponent<CharacterSelectPresenter>();
        if (presenter == null)
            presenter = root.AddComponent<CharacterSelectPresenter>();
        WireMenuMusic(presenter);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Character Selection rebuild: altar + UI mockup listos.");
    }

    const string MenuMusicPath =
        "Assets/_ArcanoDungeon/Audio/Music/mavopix-black-magic-159868.mp3";

    static void WireMenuMusic(CharacterSelectPresenter presenter)
    {
        if (presenter == null)
            return;

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath);
        if (clip == null)
        {
            Debug.LogWarning("No se encontró la música del menú: " + MenuMusicPath);
            return;
        }

        var so = new SerializedObject(presenter);
        so.FindProperty("menuMusic").objectReferenceValue = clip;
        so.FindProperty("menuMusicVolume").floatValue = 0.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(presenter);
    }

    static GameObject EnsureRoot()
    {
        var root = GameObject.Find("CharacterSelect");
        if (root == null)
            root = new GameObject("CharacterSelect");
        return root;
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
            return child;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void ClearChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    static void BuildEnvironment(Transform lineup)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(lineup, false);
        floor.transform.localScale = new Vector3(2.4f, 1f, 2.4f);
        Object.DestroyImmediate(floor.GetComponent<MeshCollider>());
        var floorMat = Resources.Load<Material>("AltarStone");
        if (floorMat != null)
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        else
            ApplyLit(floor.GetComponent<Renderer>(), new Color(0.035f, 0.04f, 0.055f), 0f);

        // Anillo ritual luminoso bajo el personaje.
        var glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glow.name = "RitualGlow";
        glow.transform.SetParent(lineup, false);
        glow.transform.localPosition = new Vector3(0f, 0.02f, 0.15f);
        glow.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
        Object.DestroyImmediate(glow.GetComponent<Collider>());
        ApplyUnlit(glow.GetComponent<Renderer>(), new Color(0.22f, 0.1f, 0.38f, 0.06f), true);

        var mist = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mist.name = "GroundMist";
        mist.transform.SetParent(lineup, false);
        mist.transform.localPosition = new Vector3(0f, 0.08f, 0.2f);
        mist.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        mist.transform.localScale = new Vector3(5.5f, 5.5f, 1f);
        Object.DestroyImmediate(mist.GetComponent<Collider>());
        ApplyUnlit(mist.GetComponent<Renderer>(), new Color(0.12f, 0.08f, 0.18f, 0.04f), true);

        var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "Backdrop";
        backdrop.transform.SetParent(lineup, false);
        backdrop.transform.localPosition = new Vector3(0f, 2.6f, 6.2f);
        backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        backdrop.transform.localScale = new Vector3(28f, 14f, 1f);
        Object.DestroyImmediate(backdrop.GetComponent<Collider>());
        ApplyLit(backdrop.GetComponent<Renderer>(), new Color(0.025f, 0.03f, 0.05f), 0.05f);

        var vignette = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vignette.name = "BackdropVignette";
        vignette.transform.SetParent(lineup, false);
        vignette.transform.localPosition = new Vector3(0f, 2.4f, 5.8f);
        vignette.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        vignette.transform.localScale = new Vector3(18f, 10f, 1f);
        Object.DestroyImmediate(vignette.GetComponent<Collider>());
        ApplyUnlit(vignette.GetComponent<Renderer>(), new Color(0.12f, 0.05f, 0.22f, 0.45f), true);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.04f);
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.045f, 0.06f);
        RenderSettings.ambientIntensity = 0.4f;
    }

    static GameObject PlaceAltar(Transform lineup)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(AltarPath);
        if (source == null)
        {
            Debug.LogError("No se encontró AltarRitual en " + AltarPath);
            return null;
        }

        var altar = (GameObject)PrefabUtility.InstantiatePrefab(source);
        altar.name = "Altar";
        altar.transform.SetParent(lineup, false);
        altar.transform.localPosition = Vector3.zero;
        altar.transform.localRotation = Quaternion.identity;
        altar.transform.localScale = Vector3.one * 0.0095f;
        ApplyAltarMenuMaterials(altar);
        PlaceStarOnFront(altar, lineup);
        return altar;
    }

    static void PlaceStarOnFront(GameObject altar, Transform lineup)
    {
        // El mesh FBX del pentagrama sale negro/rocoso; lo ocultamos y usamos la estrella del menú.
        foreach (var t in altar.GetComponentsInChildren<Transform>(true))
        {
            var n = t.name;
            if (n.Contains("Pentagrama") || n.Contains("Circulo_pentagrama"))
                t.gameObject.SetActive(false);
        }

        var existingMenuStar = lineup.Find("MenuStar");
        if (existingMenuStar != null)
            Object.DestroyImmediate(existingMenuStar.gameObject);

        var menuStarGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        menuStarGo.name = "MenuStar";
        menuStarGo.transform.SetParent(lineup, false);
        // Frente del altar, tamaño contenido, sin tapar al héroe.
        menuStarGo.transform.localPosition = new Vector3(0f, 0.42f, -1.35f);
        menuStarGo.transform.localRotation = Quaternion.Euler(12f, 180f, 0f);
        menuStarGo.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        Object.DestroyImmediate(menuStarGo.GetComponent<Collider>());
        menuStarGo.GetComponent<Renderer>().sharedMaterial = EnsureMenuStarUnlitMaterial();
    }

    static void ApplyAltarMenuMaterials(GameObject altar)
    {
        var stone = Resources.Load<Material>("AltarStone");
        var wax = Resources.Load<Material>("AltarWax");
        var flame = Resources.Load<Material>("AltarFlame");
        var wick = Resources.Load<Material>("AltarWick");
        var star = EnsureGoldStarMaterial();

        foreach (var renderer in altar.GetComponentsInChildren<Renderer>(true))
        {
            var name = renderer.gameObject.name;
            if (name.Contains("Pentagrama") || name.Contains("Circulo_pentagrama"))
                renderer.sharedMaterial = star;
            else if (name.Contains("Llama") || name.Contains("Fuego") || name.Contains("Llamas"))
            {
                if (flame != null)
                {
                    var flameInst = new Material(flame);
                    if (flameInst.HasProperty("_EmissionColor"))
                        flameInst.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.1f) * 0.15f);
                    renderer.sharedMaterial = flameInst;
                }
            }
            else if (name.Contains("Cera") || name.Contains("Velas"))
                renderer.sharedMaterial = wax != null ? wax : renderer.sharedMaterial;
            else if (name.Contains("Mecha") || name.Contains("Mechas"))
                renderer.sharedMaterial = wick != null ? wick : renderer.sharedMaterial;
            else
                renderer.sharedMaterial = stone != null ? stone : renderer.sharedMaterial;
        }
    }

    static Material EnsureGoldStarMaterial()
    {
        // Solo para meshes del altar; oro limpio sin textura de roca del AltarStar.
        const string path = "Assets/_ArcanoDungeon/Art/Materials/Resources/AltarMenuStar.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            existing = new Material(shader != null ? shader : Shader.Find("Standard"));
            existing.name = "AltarMenuStar";
            AssetDatabase.CreateAsset(existing, path);
        }

        if (existing.HasProperty("_BaseMap"))
            existing.SetTexture("_BaseMap", null);
        if (existing.HasProperty("_BaseColor"))
            existing.SetColor("_BaseColor", Gold);
        else
            existing.color = Gold;
        if (existing.HasProperty("_EmissionColor"))
        {
            existing.EnableKeyword("_EMISSION");
            existing.SetColor("_EmissionColor", Gold * 0.08f);
        }

        EditorUtility.SetDirty(existing);
        return existing;
    }

    static Material EnsureMenuStarUnlitMaterial()
    {
        const string path = "Assets/_ArcanoDungeon/Art/Materials/Resources/MenuStarUnlit.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            existing = new Material(shader != null ? shader : Shader.Find("Unlit/Transparent"));
            existing.name = "MenuStarUnlit";
            AssetDatabase.CreateAsset(existing, path);
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_ArcanoDungeon/Art/UI/MainMenu/IconDiamond.png")
                  ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_ArcanoDungeon/Art/UI/Resources/IconDiamond.png");
        if (existing.HasProperty("_BaseMap") && tex != null)
            existing.SetTexture("_BaseMap", tex);
        if (existing.HasProperty("_BaseColor"))
            existing.SetColor("_BaseColor", Gold);
        else
            existing.color = Gold;

        // Alpha del icono.
        existing.SetFloat("_Surface", 1f);
        existing.SetOverrideTag("RenderType", "Transparent");
        existing.renderQueue = 3000;
        if (existing.HasProperty("_Blend"))
            existing.SetFloat("_Blend", 0f);
        existing.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        EditorUtility.SetDirty(existing);
        return existing;
    }

    static float MeasureStandHeight(GameObject altar)
    {
        if (altar == null)
            return 0.35f;

        Renderer best = null;
        foreach (var renderer in altar.GetComponentsInChildren<Renderer>(true))
        {
            var n = renderer.gameObject.name;
            if (n == "Plataforma" || n.Contains("Plataforma_media") || n.Contains("Disco_ritual"))
            {
                best = renderer;
                break;
            }
        }

        if (best == null)
        {
            var bounds = new Bounds(altar.transform.position, Vector3.zero);
            var first = true;
            foreach (var renderer in altar.GetComponentsInChildren<Renderer>(true))
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            return Mathf.Max(0.2f, bounds.max.y - lineupY(altar) + 0.02f);
        }

        return best.bounds.max.y - lineupY(altar) + 0.02f;
    }

    static void StripGameplay(GameObject hero)
    {
        foreach (var behaviour in hero.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
                continue;
            var typeName = behaviour.GetType().Name;
            if (typeName.Contains("Player") || typeName.Contains("Motor") || typeName.Contains("Input") ||
                typeName.Contains("Camera") || typeName.Contains("Health") || typeName.Contains("Combat") ||
                typeName.Contains("Controller") || typeName.Contains("Bootstrap"))
                Object.DestroyImmediate(behaviour);
        }

        foreach (var rb in hero.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(rb);
        foreach (var col in hero.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        foreach (var audio in hero.GetComponentsInChildren<AudioSource>(true))
            Object.DestroyImmediate(audio);
    }

    static float lineupY(GameObject altar)
    {
        return altar.transform.parent != null ? altar.transform.parent.position.y : 0f;
    }

    static void BuildHeroStage(Transform lineup, float standY)
    {
        var stage = new GameObject("HeroStage");
        stage.transform.SetParent(lineup, false);
        stage.transform.localPosition = new Vector3(0f, standY, 0.2f);

        for (var i = 0; i < HeroRoster.All.Length; i++)
        {
            var profile = HeroRoster.All[i];
            var slot = new GameObject("Slot_" + profile.Name);
            slot.transform.SetParent(stage.transform, false);
            slot.transform.localPosition = Vector3.zero;
            slot.transform.localRotation = Quaternion.identity;

            var prefabPath = string.IsNullOrEmpty(profile.PrefabPath)
                ? "Assets/_ArcanoDungeon/Prefabs/" + profile.Name + "Player.prefab"
                : profile.PrefabPath;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("No hero prefab: " + prefabPath);
                continue;
            }

            var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            hero.name = profile.Name;
            hero.transform.SetParent(slot.transform, false);
            hero.transform.localPosition = Vector3.zero;
            hero.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var scale = profile.StageScale > 0.01f ? profile.StageScale : 0.78f;
            hero.transform.localScale = Vector3.one * scale;
            StripGameplay(hero);
            DimHeroEmissions(hero);
            slot.SetActive(i == 0);
        }
    }

    public static void ToneHeroesInOpenScene()
    {
        var stage = GameObject.Find("HeroStage");
        if (stage == null)
            return;

        for (var i = 0; i < stage.transform.childCount; i++)
        {
            var slot = stage.transform.GetChild(i);
            if (!slot.name.StartsWith("Slot_"))
                continue;
            DimHeroEmissions(slot.gameObject);
        }
    }

    static void DimHeroEmissions(GameObject hero)
    {
        foreach (var renderer in hero.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null)
                    continue;
                if (!src.HasProperty("_BaseColor") && !src.HasProperty("_Color") && !src.HasProperty("_EmissionColor"))
                    continue;

                var inst = new Material(src);
                SoftenHeroMaterial(inst);
                mats[i] = inst;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = mats;
        }
    }

    static void SoftenHeroMaterial(Material inst)
    {
        if (inst.HasProperty("_BaseColor"))
            inst.SetColor("_BaseColor", Soften(inst.GetColor("_BaseColor")));
        if (inst.HasProperty("_Color"))
            inst.SetColor("_Color", Soften(inst.GetColor("_Color")));
        if (inst.HasProperty("_EmissionColor"))
        {
            inst.DisableKeyword("_EMISSION");
            inst.SetColor("_EmissionColor", Color.black);
            inst.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        if (inst.HasProperty("_Smoothness"))
            inst.SetFloat("_Smoothness", Mathf.Min(inst.GetFloat("_Smoothness"), 0.2f));
        if (inst.HasProperty("_Metallic"))
            inst.SetFloat("_Metallic", Mathf.Min(inst.GetFloat("_Metallic"), 0.15f));
    }

    static Color Soften(Color color)
    {
        var white = color.r > 0.9f && color.g > 0.9f && color.b > 0.9f;
        if (white)
            return new Color(0.42f, 0.4f, 0.38f, color.a);
        return new Color(color.r * 0.68f, color.g * 0.68f, color.b * 0.68f, color.a);
    }

    static void BuildLights(Transform lineup, float standY)
    {
        // Apagar Directional Light de escena: era la principal fuente del reventón.
        foreach (var dir in Object.FindObjectsByType<Light>())
        {
            if (dir.type != LightType.Directional)
                continue;
            dir.intensity = 0.12f;
            dir.color = new Color(0.7f, 0.75f, 0.9f);
        }

        var key = new GameObject("StageLight");
        key.transform.SetParent(lineup, false);
        key.transform.localPosition = new Vector3(0.4f, standY + 2.1f, -1.4f);
        var light = key.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(0.85f, 0.9f, 1f);
        light.intensity = 0.35f;
        light.range = 9f;
        light.spotAngle = 50f;
        light.shadows = LightShadows.Soft;
        key.transform.LookAt(new Vector3(0f, standY + 0.9f, 0.2f));

        var fill = new GameObject("FillLight");
        fill.transform.SetParent(lineup, false);
        fill.transform.localPosition = new Vector3(-1.6f, standY + 1.4f, -0.6f);
        var fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(1f, 0.7f, 0.45f);
        fillLight.intensity = 0.12f;
        fillLight.range = 4f;

        var rim = new GameObject("RimLight");
        rim.transform.SetParent(lineup, false);
        rim.transform.localPosition = new Vector3(0f, standY + 1.6f, 1.8f);
        var rimLight = rim.AddComponent<Light>();
        rimLight.type = LightType.Point;
        rimLight.color = new Color(0.55f, 0.35f, 0.9f);
        rimLight.intensity = 0.15f;
        rimLight.range = 4.5f;

        var altarLight = new GameObject("AltarEmberLight");
        altarLight.transform.SetParent(lineup, false);
        altarLight.transform.localPosition = new Vector3(0f, standY + 0.35f, 0.15f);
        var ember = altarLight.AddComponent<Light>();
        ember.type = LightType.Point;
        ember.color = new Color(1f, 0.5f, 0.2f);
        ember.intensity = 0.12f;
        ember.range = 1.8f;
    }

    static void BakeCandles(GameObject altar)
    {
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
        if (firePrefab == null)
            return;

        const float worldScale = 0.028f;
        var placed = 0;
        foreach (var child in altar.GetComponentsInChildren<Transform>(true))
        {
            if (child == altar.transform)
                continue;
            var n = child.name;
            var isFlame = n.Contains("Llamas") || n.Contains("Llama") || n.Contains("Fuego") ||
                          n.Contains("Flame") || n.Contains("Fire") || n.Contains("Velas");
            if (!isFlame)
                continue;
            // Evitar duplicar FX en mechas/cera si ya hay llama hermana.
            if (n.Contains("Mecha") || n.Contains("Wick"))
                continue;

            var existing = child.Find("CandleFireFX");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var meshRenderer = child.GetComponent<Renderer>();
            if (meshRenderer != null && (n.Contains("Llama") || n.Contains("Fuego") || n.Contains("Flame") || n.Contains("Fire")))
                meshRenderer.enabled = false;

            var tip = GetCandleTip(child);
            PlaceCandleFx(firePrefab, child, tip, worldScale);
            placed++;
        }

        // Fallback: anillo de llamas si el FBX no expone meshes con nombres esperados.
        if (placed == 0)
        {
            var ring = new GameObject("CandleRingFX");
            ring.transform.SetParent(altar.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            const int count = 10;
            const float radius = 95f; // espacio local del FBX a escala ~0.014
            for (var i = 0; i < count; i++)
            {
                var ang = i * Mathf.PI * 2f / count;
                var local = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                var holder = new GameObject("CandleFX_" + i);
                holder.transform.SetParent(ring.transform, false);
                holder.transform.localPosition = local;
                var worldTip = holder.transform.position;
                PlaceCandleFx(firePrefab, holder.transform, worldTip, worldScale * 1.2f);
            }
        }
    }

    static void PlaceCandleFx(GameObject firePrefab, Transform parent, Vector3 tip, float worldScale)
    {
        var fx = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
        fx.name = "CandleFireFX";
        fx.transform.SetPositionAndRotation(tip + Vector3.down * 0.03f, Quaternion.identity);
        fx.transform.localScale = Vector3.one * worldScale;
        fx.transform.SetParent(parent, true);
        StripExtraCandleFx(fx);
        TuneCandleFlame(fx);

        var point = new GameObject("CandleLight");
        point.transform.SetParent(fx.transform, false);
        point.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var candleLight = point.AddComponent<Light>();
        candleLight.type = LightType.Point;
        candleLight.color = new Color(1f, 0.55f, 0.2f);
        candleLight.intensity = 0.08f;
        candleLight.range = 0.45f;
        candleLight.enabled = false; // evita bloom por luces de partícula
    }

    static Vector3 GetCandleTip(Transform flame)
    {
        var wickName = flame.name.Replace("Llamas_naranjas", "Mechas");
        Transform wick = null;
        Transform wax = null;
        if (flame.parent != null)
        {
            wick = flame.parent.Find(wickName);
            var waxName = flame.name.Replace("Llamas_naranjas", "Velas_rojas");
            wax = flame.parent.Find(waxName);
            if (wick == null || wax == null)
            {
                foreach (Transform sibling in flame.parent)
                {
                    if (sibling == flame)
                        continue;
                    if (wick == null && sibling.name.Contains("Mechas"))
                        wick = sibling;
                    if (wax == null && sibling.name.Contains("Velas"))
                        wax = sibling;
                }
            }
        }

        Renderer source = null;
        if (wick != null)
            source = wick.GetComponent<Renderer>();
        if (source == null && wax != null)
            source = wax.GetComponent<Renderer>();
        if (source == null)
            source = flame.GetComponent<Renderer>();
        if (source != null)
        {
            var b = source.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        return flame.position + Vector3.up * 0.05f;
    }

    static void StripExtraCandleFx(GameObject fx)
    {
        foreach (var t in fx.GetComponentsInChildren<Transform>(true))
        {
            var name = t.name;
            if (name.Contains("Smoke") || name.Contains("Ember") || name.Contains("Humo"))
                t.gameObject.SetActive(false);
        }

        foreach (var light in fx.GetComponentsInChildren<Light>(true))
        {
            light.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            light.enabled = false;
        }
    }

    static void TuneCandleFlame(GameObject fx)
    {
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!ps.gameObject.activeInHierarchy)
                continue;

            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startSizeMultiplier = Mathf.Min(main.startSizeMultiplier, 0.7f);
            main.startSpeedMultiplier = 0.4f;
            main.maxParticles = Mathf.Min(main.maxParticles, 48);

            var emission = ps.emission;
            emission.rateOverTimeMultiplier = Mathf.Min(emission.rateOverTimeMultiplier, 14f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.minParticleSize = 0f;
                psRenderer.maxParticleSize = 0.4f;
            }
        }
    }

    static void BakeRitualFx(Transform lineup, GameObject altar, float standY)
    {
        var magicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicCirclePath);
        if (magicPrefab != null)
        {
            var circle = (GameObject)PrefabUtility.InstantiatePrefab(magicPrefab);
            circle.name = "RitualMagicCircle";
            circle.transform.SetParent(lineup, false);
            circle.transform.localPosition = new Vector3(0f, 0.04f, 0.15f);
            circle.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            circle.transform.localScale = Vector3.one * 0.72f;
            foreach (var ps in circle.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.28f, 0.18f, 0.42f, 0.12f));
            }
        }

        // Cartoon FX Remaster Free: brillos ambientales + aura rúnica en el pedestal.
        PlaceCfxrLoop(
            AmbientGlowsPath,
            lineup,
            "RitualAmbientGlows",
            new Vector3(0f, standY + 0.55f, 0.15f),
            Quaternion.identity,
            0.55f,
            new Color(0.55f, 0.35f, 0.95f, 0.55f));

        PlaceCfxrLoop(
            MagicAuraPath,
            lineup,
            "RitualMagicAura",
            new Vector3(0f, standY + 0.08f, 0.15f),
            Quaternion.identity,
            0.85f,
            new Color(0.72f, 0.48f, 1f, 0.7f));

        // Emisión suave en la estrella del altar (sin quemar la imagen).
        foreach (var renderer in altar.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.name.Contains("Pentagrama") && !renderer.name.Contains("Circulo_pentagrama"))
                continue;
            var mat = renderer.sharedMaterial;
            if (mat == null || !mat.HasProperty("_EmissionColor"))
                continue;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Gold * 0.12f);
        }
    }

    static void PlaceCfxrLoop(
        string prefabPath,
        Transform parent,
        string name,
        Vector3 localPos,
        Quaternion localRot,
        float scale,
        Color tint)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("CFXR prefab no encontrado: " + prefabPath);
            return;
        }

        var fx = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        fx.name = name;
        fx.transform.SetParent(parent, false);
        fx.transform.localPosition = localPos;
        fx.transform.localRotation = localRot;
        fx.transform.localScale = Vector3.one * scale;

        foreach (var light in fx.GetComponentsInChildren<Light>(true))
            light.enabled = false;

        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startColor = new ParticleSystem.MinMaxGradient(tint);
            main.maxParticles = Mathf.Min(main.maxParticles, 64);

            var emission = ps.emission;
            if (emission.rateOverTimeMultiplier > 20f)
                emission.rateOverTimeMultiplier = 12f;
        }
    }

    static GameObject BuildUi(Transform root)
    {
        var old = root.Find("HeroUI");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var ui = new GameObject("HeroUI", typeof(RectTransform));
        ui.transform.SetParent(root, false);
        var canvas = ui.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = ui.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        ui.AddComponent<GraphicRaycaster>();

        var rt = ui.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        BuildTitle(ui.transform);
        BuildBack(ui.transform);
        BuildRoster(ui.transform);
        BuildDetail(ui.transform);
        BuildEnter(ui.transform);
        return ui;
    }

    static void BuildLogo(Transform ui)
    {
        var logo = CreatePanel(ui, "Logo", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -28f), new Vector2(360f, 70f));
        var img = logo.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        var star = CreateImage(logo.transform, "Star", SpriteDiamond(), Gold);
        Stretch(star, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(34f, 34f));

        var title = CreateText(logo.transform, "Brand", "ARCANO DUNGEON", 26, FontStyle.Bold, Gold, TextAnchor.MiddleLeft);
        Stretch(title, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 0f), new Vector2(-8f, 0f));
    }

    static void BuildTitle(Transform ui)
    {
        var title = CreateText(ui, "ScreenTitle", "SELECCIONAR PERSONAJE", 34, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
        Stretch(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(640f, 48f));
    }

    static void BuildBack(Transform ui)
    {
        var back = CreateButton(ui, "Back", "< MENÚ", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f), new Vector2(140f, 44f));
        var img = back.GetComponent<Image>();
        img.color = new Color(0.08f, 0.09f, 0.14f, 0.72f);
        var label = back.GetComponentInChildren<Text>();
        label.color = Gold;
        label.fontSize = 20;
    }

    static void BuildRoster(Transform ui)
    {
        var count = HeroRoster.All.Length;
        var cardH = count > 3 ? 88f : 110f;
        var gap = count > 3 ? 98f : 130f;
        var rosterH = Mathf.Max(420f, count * gap + 40f);
        var roster = CreatePanel(ui, "Roster", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 20f), new Vector2(300f, rosterH));
        roster.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        var startY = (count - 1) * 0.5f * gap;
        for (var i = 0; i < count; i++)
        {
            var profile = HeroRoster.All[i];
            var y = startY - i * gap;
            var card = CreateButton(roster.transform, "Card_" + profile.Name, "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(290f, cardH));
            var border = card.GetComponent<Image>();
            border.color = i == 0 ? Gold : new Color(Gold.r, Gold.g, Gold.b, 0.28f);

            var fill = CreateImage(card.transform, "Fill", null, CardFill);
            Stretch(fill, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

            var portrait = CreateImage(card.transform, "Portrait", LoadPortrait(profile.Name), Color.white);
            var pSize = count > 3 ? 64f : 78f;
            Stretch(portrait, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(pSize, pSize));

            var name = CreateText(card.transform, "Name", profile.Name, count > 3 ? 24 : 28, FontStyle.Bold, Gold, TextAnchor.MiddleLeft);
            Stretch(name, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(100f, -6f), new Vector2(-48f, -10f));

            var title = CreateText(card.transform, "Title", profile.Title.ToUpperInvariant(), 15, FontStyle.Normal, profile.Accent, TextAnchor.UpperLeft);
            Stretch(title, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(100f, 10f), new Vector2(-24f, -6f));

            var star = CreateImage(card.transform, "Star", SpriteDiamond(), Gold);
            Stretch(star, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 8f), new Vector2(20f, 20f));
            star.enabled = i == 0;
        }
    }

    static void BuildDetail(Transform ui)
    {
        var panel = CreatePanel(ui, "DetailPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 85f), new Vector2(430f, 470f));
        panel.GetComponent<Image>().color = PanelBorder;

        var fill = CreateImage(panel.transform, "Fill", null, PanelFill);
        Stretch(fill, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

        var name = CreateText(panel.transform, "HeaderName", "Elara", 40, FontStyle.Bold, Gold, TextAnchor.UpperLeft);
        Stretch(name, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -24f), new Vector2(-70f, -58f));

        var cls = CreateText(panel.transform, "HeaderClass", "CABALLERA", 18, FontStyle.Bold, new Color(0.72f, 0.58f, 1f), TextAnchor.UpperLeft);
        Stretch(cls, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -70f), new Vector2(-70f, -28f));

        var star = CreateImage(panel.transform, "Star", SpriteDiamond(), Gold);
        Stretch(star, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -42f), new Vector2(34f, 34f));

        var desc = CreateText(panel.transform, "Description", HeroRoster.All[0].Summary, 20, FontStyle.Normal, Ink, TextAnchor.UpperLeft);
        var descRt = desc.GetComponent<RectTransform>();
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        desc.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(desc, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -118f), new Vector2(-28f, -110f));

        string[] stats = { "Salud", "Ataque", "Defensa", "Velocidad" };
        int[] values = { 10, 6, 10, 4 };
        for (var i = 0; i < stats.Length; i++)
        {
            var top = 250f + i * 48f;
            var row = CreatePanel(panel.transform, "Stat_" + stats[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(374f, 40f));
            row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var label = CreateText(row.transform, "Label", stats[i], 16, FontStyle.Bold, Ink, TextAnchor.MiddleLeft);
            Stretch(label, new Vector2(0f, 0f), new Vector2(0.34f, 1f), Vector2.zero, Vector2.zero);

            var track = CreateImage(row.transform, "Track", null, BarTrack);
            Stretch(track, new Vector2(0.34f, 0.35f), new Vector2(0.82f, 0.65f), Vector2.zero, Vector2.zero);

            var bar = CreateImage(row.transform, "Fill", null, BarFill);
            var span = 0.48f * Mathf.Clamp01(values[i] / 10f);
            Stretch(bar, new Vector2(0.34f, 0.35f), new Vector2(0.34f + span, 0.65f), Vector2.zero, Vector2.zero);

            var value = CreateText(row.transform, "Value", values[i].ToString(), 16, FontStyle.Bold, Gold, TextAnchor.MiddleRight);
            Stretch(value, new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }

    }

    static void BuildCarousel(Transform ui)
    {
        var prev = CreateButton(ui, "Prev", "◀", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 86f), new Vector2(54f, 54f));
        StyleCarousel(prev);
        var next = CreateButton(ui, "Next", "▶", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(70f, 86f), new Vector2(54f, 54f));
        StyleCarousel(next);

        var star = CreateImage(ui, "CarouselStar", LoadRotateIcon(), Gold);
        Stretch(star, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(42f, 42f));
    }

    static void StyleCarousel(GameObject button)
    {
        button.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.55f);
        var label = button.GetComponentInChildren<Text>();
        label.color = Gold;
        label.fontSize = 26;
    }

    static void BuildEnter(Transform ui)
    {
        var enter = CreateButton(ui, "Enter", "COMENZAR AVENTURA  >>", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 42f), new Vector2(430f, 64f));
        enter.GetComponent<Image>().color = Gold;
        var label = enter.GetComponentInChildren<Text>();
        label.color = InkDark;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        cam.transform.position = new Vector3(0f, 1.45f, -7.2f);
        cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
        cam.fieldOfView = 26f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.035f, 0.05f);
    }

    static void EnsureVolume()
    {
        var volumeGo = GameObject.Find("Global Volume");
        if (volumeGo == null)
            volumeGo = new GameObject("Global Volume");
        var volume = volumeGo.GetComponent<Volume>();
        if (volume == null)
            volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumePath);
        }

        // Limpiar componentes rotos (refs nulas) del asset.
        profile.components.RemoveAll(c => c == null);

        if (!profile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
            bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);

        bloom.active = true;
        bloom.intensity.Override(0.22f);
        bloom.threshold.Override(1.35f);
        bloom.scatter.Override(0.32f);
        volume.priority = 20f;

        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        if (Mathf.Approximately(anchorMin.x, anchorMax.x) && Mathf.Approximately(anchorMin.y, anchorMax.y))
        {
            rt.pivot = new Vector2(anchorMin.x <= 0.01f ? 0f : anchorMin.x >= 0.99f ? 1f : 0.5f,
                anchorMin.y <= 0.01f ? 0f : anchorMin.y >= 0.99f ? 1f : 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
        }
        else
        {
            rt.offsetMin = new Vector2(anchored.x, anchored.y);
            rt.offsetMax = new Vector2(size.x, size.y);
        }

        go.GetComponent<Image>().color = Color.white;
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size)
    {
        var go = CreatePanel(parent, name, anchorMin, anchorMax, anchored, size);
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        if (!string.IsNullOrEmpty(label))
        {
            var text = CreateText(go.transform, "Label", label, 20, FontStyle.Bold, Ink, TextAnchor.MiddleCenter);
            Stretch(text, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        }

        return go;
    }

    static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = ResolveFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(Object target, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = null;
        if (target is RectTransform direct)
            rt = direct;
        else if (target is GameObject go)
            rt = go.GetComponent<RectTransform>();
        else if (target is Component component)
            rt = component.GetComponent<RectTransform>();

        if (rt == null)
            return;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        if (Mathf.Approximately(anchorMin.x, anchorMax.x) && Mathf.Approximately(anchorMin.y, anchorMax.y))
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offsetMin;
            rt.sizeDelta = offsetMax;
        }
        else
        {
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }

    static Font ResolveFont()
    {
        var cinzel = Resources.Load<Font>("Cinzel-Regular");
        if (cinzel != null)
            return cinzel;
        var lora = Resources.Load<Font>("Lora-Regular");
        if (lora != null)
            return lora;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Sprite LoadPortrait(string heroName)
    {
        HeroProfile profile = null;
        for (var i = 0; i < HeroRoster.All.Length; i++)
        {
            if (HeroRoster.All[i].Name == heroName)
            {
                profile = HeroRoster.All[i];
                break;
            }
        }

        if (profile != null && !string.IsNullOrEmpty(profile.PortraitPath))
        {
            var custom = AssetDatabase.LoadAssetAtPath<Sprite>(profile.PortraitPath);
            if (custom != null)
                return custom;
        }

        var path = "Assets/_ArcanoDungeon/Art/Textures/Portraits/" + heroName + "_Face.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
            return SpriteDiamond();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path)
               ?? Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    static Sprite LoadRotateIcon()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_ArcanoDungeon/Art/UI/IconRotate.png");
        if (sprite != null)
            return sprite;
        return SpriteDiamond();
    }

    static Sprite SpriteDiamond()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_ArcanoDungeon/Art/UI/MainMenu/IconDiamond.png");
        if (sprite != null)
            return sprite;
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_ArcanoDungeon/Art/UI/Resources/IconDiamond.png");
    }

    static void ApplyLit(Renderer renderer, Color color, float emission)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        if (emission > 0f && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }

        renderer.sharedMaterial = mat;
    }

    static void ApplyUnlit(Renderer renderer, Color color, bool transparent)
    {
        var shader = Shader.Find(transparent ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        var mat = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        if (transparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
        }

        renderer.sharedMaterial = mat;
    }
}

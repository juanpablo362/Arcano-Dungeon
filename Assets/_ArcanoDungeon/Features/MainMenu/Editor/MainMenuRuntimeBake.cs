using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

static class MainMenuRuntimeBake
{
    const string ScenePath = "Assets/_ArcanoDungeon/Scenes/MainMenu.unity";
    const string FirePrefabPath =
        "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire.prefab";

    [MenuItem("Arcano Dungeon/Bake Main Menu Runtime Objects")]
    public static void Bake()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var crystal = GameObject.Find("Crystal");
        if (crystal != null)
        {
            if (crystal.GetComponent<CrystalFloat>() == null)
                Undo.AddComponent<CrystalFloat>(crystal);
            CrystalMaterials.Apply(crystal);
        }

        var altar = GameObject.Find("Altar");
        if (altar != null)
        {
            AltarMaterials.Apply(altar);
            BakeCandles(altar);
            var candleFx = altar.GetComponent<CandleFlameFX>();
            if (candleFx != null)
                Undo.DestroyObjectImmediate(candleFx);
        }

        var ruins = GameObject.Find("CastleRuins");
        if (ruins != null)
        {
            var ruinsComp = ruins.GetComponent<MenuCastleRuins>();
            if (ruinsComp != null)
                Undo.DestroyObjectImmediate(ruinsComp);
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            if (canvas.GetComponent<MenuAmbientLighting>() == null)
                Undo.AddComponent<MenuAmbientLighting>(canvas);

            var controller = canvas.GetComponent<MainMenuController>();
            if (controller != null)
            {
                Undo.RecordObject(controller, "Wire MainMenu audio");
                EnsureAudio(controller, "MusicSource", ref controller.musicSource);
                EnsureAudio(controller, "SpellSource", ref controller.spellSource);
                EditorUtility.SetDirty(controller);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Main Menu runtime objects baked into scene.");
    }

    static void EnsureAudio(MainMenuController controller, string childName, ref AudioSource slot)
    {
        if (slot != null)
            return;

        var existing = controller.transform.Find(childName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
            go.transform.SetParent(controller.transform, false);
        }

        slot = go.GetComponent<AudioSource>();
        if (slot == null)
            slot = Undo.AddComponent<AudioSource>(go);

        slot.playOnAwake = false;
        slot.spatialBlend = 0f;
    }

    static void BakeCandles(GameObject altar)
    {
        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
        if (firePrefab == null)
        {
            Debug.LogError("No se encontró CFXR Fire prefab.");
            return;
        }

        const float worldScale = 0.045f;
        const float heightOffset = -0.01f;

        foreach (var child in altar.GetComponentsInChildren<Transform>(true))
        {
            if (child == altar.transform || !child.name.Contains("Llamas"))
                continue;

            var existing = child.Find("CandleFireFX");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var meshRenderer = child.GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                Undo.RecordObject(meshRenderer, "Disable mesh flame");
                meshRenderer.enabled = false;
            }

            var tip = GetWickTip(child, heightOffset);
            var fx = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
            Undo.RegisterCreatedObjectUndo(fx, "CandleFireFX");
            fx.name = "CandleFireFX";
            fx.transform.SetPositionAndRotation(tip + Vector3.down * 0.03f, Quaternion.identity);
            fx.transform.localScale = Vector3.one * worldScale;
            fx.transform.SetParent(child, true);
            StripExtraFx(fx);
            TuneMainFlame(fx);
        }
    }

    static Vector3 GetWickTip(Transform flame, float heightOffset)
    {
        var wickName = flame.name.Replace("Llamas_naranjas", "Mechas");
        var wick = FindSibling(flame, wickName) ?? FindSiblingContaining(flame, "Mechas");
        var waxName = flame.name.Replace("Llamas_naranjas", "Velas_rojas");
        var wax = FindSibling(flame, waxName) ?? FindSiblingContaining(flame, "Velas");

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
            return new Vector3(b.center.x, b.max.y, b.center.z) + Vector3.up * heightOffset;
        }

        return flame.position + Vector3.up * heightOffset;
    }

    static Transform FindSibling(Transform child, string exactName)
    {
        if (child.parent == null)
            return null;
        return child.parent.Find(exactName);
    }

    static Transform FindSiblingContaining(Transform child, string token)
    {
        if (child.parent == null)
            return null;
        foreach (Transform sibling in child.parent)
        {
            if (sibling != child && sibling.name.Contains(token))
                return sibling;
        }

        return null;
    }

    static void StripExtraFx(GameObject fx)
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
            light.range = 0.35f;
            light.intensity = 0.8f;
        }
    }

    static void TuneMainFlame(GameObject fx)
    {
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!ps.gameObject.activeInHierarchy)
                continue;

            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startSizeMultiplier = Mathf.Min(main.startSizeMultiplier, 0.65f);
            main.startSpeedMultiplier = 0.35f;
            main.maxParticles = Mathf.Min(main.maxParticles, 40);

            var emission = ps.emission;
            emission.rateOverTimeMultiplier = Mathf.Min(emission.rateOverTimeMultiplier, 12f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.minParticleSize = 0f;
                psRenderer.maxParticleSize = 0.35f;
            }
        }
    }
}

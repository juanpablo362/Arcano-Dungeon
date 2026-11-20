using ArcanoDungeon.Features.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupMeleeComboCombat
{
    const string CombatVfxFolder = "Assets/_ArcanoDungeon/Prefabs/Combat";
    const string CombatAnimFolder = "Assets/_ArcanoDungeon/Animators/CombatClips";

    [MenuItem("Tools/Arcano/Setup Melee Combo Combat")]
    public static void Setup()
    {
        EnsureFolders();
        var crescent = EnsureCrescentPrefab();
        var impact = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab");
        var slash = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Texts/CFXR _SLASH_.prefab");

        var knightClip = BuildClipWithEvents(
            "Assets/RPG Tiny Hero Duo/Animation/SwordAndShield/Attack01_SwordAndShiled.fbx",
            CombatAnimFolder + "/Knight_Attack01_Events.anim",
            "Knight_Attack01_Events");
        var mageClip = BuildClipWithEvents(
            "Assets/WizardPolyArt/Animations/Attack01.fbx",
            CombatAnimFolder + "/Mage_Attack01_Events.anim",
            "Mage_Attack01_Events");

        WireController(
            "Assets/Prefabs/KnightGameplay.controller",
            knightClip,
            new[] { 1.0f, 1.05f, 1.2f });
        WireController(
            "Assets/Prefabs/MageGameplay.controller",
            mageClip,
            new[] { 1.0f, 1.05f, 1.18f });

        SetupKnightPrefab(crescent, impact, slash);
        SetupMagePrefab(crescent, impact);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupMeleeComboCombat] Controllers, clips y prefabs listos.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Prefabs");
        if (!AssetDatabase.IsValidFolder(CombatVfxFolder))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Prefabs", "Combat");
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Animators"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Animators");
        if (!AssetDatabase.IsValidFolder(CombatAnimFolder))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Animators", "CombatClips");
    }

    static GameObject EnsureCrescentPrefab()
    {
        string path = CombatVfxFolder + "/MageCrescentWave.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "MageCrescentWave";
        Object.DestroyImmediate(root.GetComponent<Collider>());
        root.transform.localScale = new Vector3(1.2f, 0.25f, 0.7f);

        var renderer = root.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.35f, 0.75f, 1f, 0.55f));
        else
            mat.color = new Color(0.35f, 0.75f, 1f, 0.55f);
        renderer.sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, CombatVfxFolder + "/MageCrescentWave.mat");

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static AnimationClip BuildClipWithEvents(string fbxPath, string outAnimPath, string clipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip source = null;
        foreach (var a in assets)
        {
            if (a is AnimationClip c && !c.name.StartsWith("__preview"))
            {
                source = c;
                break;
            }
        }

        if (source == null)
        {
            Debug.LogError("[SetupMeleeComboCombat] No clip in " + fbxPath);
            return null;
        }

        var copy = new AnimationClip();
        EditorUtility.CopySerialized(source, copy);
        copy.name = clipName;

        float len = Mathf.Max(0.2f, copy.length);
        var hit = new AnimationEvent
        {
            functionName = "OnAttackHitFrame",
            time = len * 0.40f
        };
        var end = new AnimationEvent
        {
            functionName = "OnAttackEndFrame",
            time = len * 0.90f
        };
        AnimationUtility.SetAnimationEvents(copy, new[] { hit, end });

        var settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(copy, settings);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outAnimPath) != null)
            AssetDatabase.DeleteAsset(outAnimPath);
        AssetDatabase.CreateAsset(copy, outAnimPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(outAnimPath);
    }

    static void WireController(string ctrlPath, AnimationClip clip, float[] speeds)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null || clip == null)
        {
            Debug.LogError("[SetupMeleeComboCombat] Missing ctrl/clip " + ctrlPath);
            return;
        }

        EnsureParam(ctrl, "ComboIndex", AnimatorControllerParameterType.Int);
        EnsureParam(ctrl, "Attack", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null;
        foreach (var s in sm.states)
        {
            if (s.state.name == "Idle")
                idle = s.state;
        }

        string[] names = { "Attack1", "Attack2", "Attack3" };
        for (int i = 0; i < names.Length; i++)
        {
            AnimatorState state = FindState(sm, names[i]);
            if (state == null)
                state = sm.AddState(names[i]);

            state.motion = clip;
            state.speed = speeds[i];
            state.tag = "Attack";
            state.writeDefaultValues = true;

            // Limpiar transitions salientes y volver a Idle.
            var outs = state.transitions;
            for (int t = outs.Length - 1; t >= 0; t--)
                state.RemoveTransition(outs[t]);

            if (idle != null)
            {
                var toIdle = state.AddTransition(idle);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 0.92f;
                toIdle.duration = 0.08f;
                toIdle.hasFixedDuration = true;
            }
        }

        // Quitar Any→Attack viejo si apunta a estado Attack único; CrossFade lo maneja el script.
        EditorUtility.SetDirty(ctrl);
        Debug.Log("[SetupMeleeComboCombat] Wired " + ctrlPath);
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var s in sm.states)
        {
            if (s.state.name == name)
                return s.state;
        }
        return null;
    }

    static void EnsureParam(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in ctrl.parameters)
        {
            if (p.name == name)
                return;
        }
        ctrl.AddParameter(name, type);
    }

    static void SetupKnightPrefab(GameObject crescent, GameObject impact, GameObject slash)
    {
        string path = "Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPolyart.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var sword = FindChild(root.transform, "OHS03Polyart");
            if (sword == null)
                sword = FindChild(root.transform, "weapon_r");

            Transform weaponBase;
            Transform weaponTip;
            CreateWeaponSockets(sword != null ? sword : root.transform, out weaponBase, out weaponTip, 0.05f, 1.35f);

            var anim = root.GetComponentInChildren<Animator>(true);
            var melee = root.GetComponent<MeleeWeaponController>() ?? root.AddComponent<MeleeWeaponController>();
            var vfx = root.GetComponent<AttackArcVFX>() ?? root.AddComponent<AttackArcVFX>();
            // Relay solo si el Animator no está en el mismo GO que MeleeWeaponController.
            if (anim != null && anim.gameObject != melee.gameObject &&
                anim.GetComponent<AttackAnimEventRelay>() == null)
                anim.gameObject.AddComponent<AttackAnimEventRelay>();
            foreach (var r in root.GetComponentsInChildren<AttackAnimEventRelay>(true))
            {
                if (anim != null && r.gameObject == melee.gameObject)
                    Object.DestroyImmediate(r);
            }

            // Trail on tip — glow suave translúcido
            var trail = weaponTip.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = weaponTip.gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.14f;
                trail.widthMultiplier = 0.08f;
                trail.emitting = false;
                trail.minVertexDistance = 0.02f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.SetColor("_BaseColor", new Color(1f, 0.62f, 0.88f, 0.2f));
                    trail.sharedMaterial = mat;
                }
                ApplySoftTrailGradient(trail);
            }

            vfx.Configure(weaponTip, trail, slash, crescent, impact, weaponTip, root.transform);
            var vfxSo = new SerializedObject(vfx);
            vfxSo.FindProperty("owner").objectReferenceValue = root.transform;
            vfxSo.ApplyModifiedPropertiesWithoutUndo();
            melee.EditorAssignRefs(
                anim, weaponBase, weaponTip, vfx,
                MeleeWeaponController.AttackStyle.SwordArc, 1.7f, impact);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[SetupMeleeComboCombat] Knight prefab OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetupMagePrefab(GameObject crescent, GameObject impact)
    {
        string path = "Assets/WizardPolyArt/Prefabs/PolyArtWizardStandardMat.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var staff = FindChild(root.transform, "Staff01PolyArt");
            if (staff == null)
                staff = FindChild(root.transform, "Staff02Polyart");
            if (staff == null)
                staff = FindChild(root.transform, "Staff03PolyArt");
            if (staff == null)
                staff = FindChild(root.transform, "Weapon");

            Transform weaponBase;
            Transform weaponTip;
            CreateWeaponSockets(staff != null ? staff : root.transform, out weaponBase, out weaponTip, 0.05f, 1.55f);

            var anim = root.GetComponentInChildren<Animator>(true);
            var melee = root.GetComponent<MeleeWeaponController>() ?? root.AddComponent<MeleeWeaponController>();
            var vfx = root.GetComponent<AttackArcVFX>() ?? root.AddComponent<AttackArcVFX>();
            if (anim != null && anim.gameObject != melee.gameObject &&
                anim.GetComponent<AttackAnimEventRelay>() == null)
                anim.gameObject.AddComponent<AttackAnimEventRelay>();
            foreach (var r in root.GetComponentsInChildren<AttackAnimEventRelay>(true))
            {
                if (r.gameObject == melee.gameObject)
                    Object.DestroyImmediate(r);
            }

            var trail = weaponTip.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = weaponTip.gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.14f;
                trail.widthMultiplier = 0.08f;
                trail.emitting = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.SetColor("_BaseColor", new Color(1f, 0.62f, 0.88f, 0.2f));
                    trail.sharedMaterial = mat;
                }
                ApplySoftTrailGradient(trail);
            }

            vfx.Configure(weaponTip, trail, null, crescent, impact, weaponTip, root.transform);
            var vfxSo = new SerializedObject(vfx);
            vfxSo.FindProperty("owner").objectReferenceValue = root.transform;
            vfxSo.ApplyModifiedPropertiesWithoutUndo();
            melee.EditorAssignRefs(
                anim, weaponBase, weaponTip, vfx,
                MeleeWeaponController.AttackStyle.StaffCrescent, 2.0f, impact);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[SetupMeleeComboCombat] Mage prefab OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void CreateWeaponSockets(Transform parent, out Transform weaponBase, out Transform weaponTip, float baseLocalY, float tipLocalY)
    {
        weaponBase = parent.Find("WeaponBase");
        if (weaponBase == null)
        {
            var go = new GameObject("WeaponBase");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, baseLocalY, 0f);
            weaponBase = go.transform;
        }

        weaponTip = parent.Find("WeaponTip");
        if (weaponTip == null)
        {
            var go = new GameObject("WeaponTip");
            go.transform.SetParent(parent, false);
            // Muchos meshes de arma apuntan en +Y local.
            go.transform.localPosition = new Vector3(0f, tipLocalY, 0f);
            weaponTip = go.transform;
        }
    }

    static Transform FindChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return t;
        }
        return null;
    }

    static void ApplySoftTrailGradient(TrailRenderer trail)
    {
        if (trail == null)
            return;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.62f, 0.88f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.95f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.28f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.05f));
    }
}

using ArcanoDungeon.Features.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Genera prefabs jugables de Misaki (twin daggers) y Yuko (greatsword) desde Haon SD,
/// con Animator compatible con PlayerController/MeleeWeaponController, y los cablea al spawner.
/// </summary>
public static class SetupHaonPlayableHeroes
{
    const string OutPrefabDir = "Assets/_ArcanoDungeon/Prefabs";
    const string OutAnimDir = "Assets/_ArcanoDungeon/Animators";
    const string OutClipDir = "Assets/_ArcanoDungeon/Animators/CombatClips";
    const string KnightCtrl = "Assets/Prefabs/KnightGameplay.controller";
    const string LevelPath = "Assets/_ArcanoDungeon/Scenes/Level1.unity";
    const string ImpactVfx =
        "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab";
    const string SlashVfx =
        "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Texts/CFXR _SLASH_.prefab";
    const string CrescentVfx = "Assets/_ArcanoDungeon/Prefabs/Combat/MageCrescentWave.prefab";

    [MenuItem("Tools/Arcano/Setup Haon Playable Heroes", false, 130)]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[SetupHaon] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        EnsureFolders();

        var misakiCtrl = BuildController(
            "MisakiGameplay",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Twin dagger(WTD)/WTD_Stand.FBX",
            "WTD_Stand@loop",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Twin dagger(WTD)/WTD_Walk_Front.FBX",
            "WTD_Walk_Front@Loop",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Twin dagger(WTD)/WTD_Run_Front.FBX",
            "WTD_Run_Front@Loop",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Twin dagger(WTD)/WTD_AttackA np.FBX",
            "WTD_AttackA1 np",
            "WTD_AttackA2 np",
            "WTD_AttackA3 np",
            "Misaki");

        var yukoCtrl = BuildController(
            "YukoGameplay",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_Stand.FBX",
            "WGS_Stand@loop",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_Walk_Front.FBX",
            "WGS_Walk_Front@Loop",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_Run_Front.FBX",
            "WGS_Run_Front@Loop",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
            "WGS_attackA1",
            "WGS_attackA2",
            "WGS_attackA3",
            "Yuko");

        // Fallbacks si los nombres de clip WGS Stand/Walk/Run difieren.
        if (misakiCtrl == null || yukoCtrl == null)
        {
            Debug.LogError("[SetupHaon] Falló al crear controllers. Revisa nombres de clips Haon.");
            return;
        }

        var misaki = BuildPlayablePrefab(
            "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume03 Misaki.prefab",
            OutPrefabDir + "/MisakiPlayer.prefab",
            misakiCtrl,
            twinDaggers: true,
            maxHealth: 110,
            moveSpeed: 6.2f,
            attackReach: 1.65f);

        var yuko = BuildPlayablePrefab(
            "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume02 Yuko.prefab",
            OutPrefabDir + "/YukoPlayer.prefab",
            yukoCtrl,
            twinDaggers: false,
            maxHealth: 100,
            moveSpeed: 6.5f,
            attackReach: 1.85f);

        WirePlayerSpawner(misaki, yuko);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupHaon] Misaki/Yuko jugables listos.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Animators"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Animators");
        if (!AssetDatabase.IsValidFolder(OutClipDir))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Animators", "CombatClips");
        if (!AssetDatabase.IsValidFolder(OutPrefabDir))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Prefabs");
    }

    static AnimationClip FindClip(string fbxPath, string clipName)
    {
        AnimationClip fallback = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            var clip = a as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview"))
                continue;
            if (clip.name == clipName)
                return clip;
            if (fallback == null)
                fallback = clip;
            else if (clip.name.IndexOf(clipName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                fallback = clip;
        }

        if (fallback != null)
            Debug.LogWarning("[SetupHaon] Usando fallback clip '" + fallback.name + "' para " + clipName + " en " + fbxPath);
        return fallback;
    }

    static AnimationClip BuildEventClip(string fbxPath, string sourceName, string outPath)
    {
        var source = FindClip(fbxPath, sourceName);
        if (source == null)
        {
            Debug.LogWarning("[SetupHaon] No clip " + sourceName + " in " + fbxPath);
            return null;
        }

        var copy = new AnimationClip();
        EditorUtility.CopySerialized(source, copy);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(outPath);

        float len = Mathf.Max(0.2f, copy.length);
        AnimationUtility.SetAnimationEvents(copy, new[]
        {
            new AnimationEvent { functionName = "OnAttackHitFrame", time = len * 0.40f },
            new AnimationEvent
            {
                functionName = "OnAttackEndFrame",
                time = Mathf.Min(len * 0.90f, len - 0.02f)
            }
        });

        var settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(copy, settings);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null)
            AssetDatabase.DeleteAsset(outPath);
        AssetDatabase.CreateAsset(copy, outPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
    }

    static AnimatorController BuildController(
        string name,
        string idleFbx, string idleClip,
        string walkFbx, string walkClip,
        string runFbx, string runClip,
        string attackFbx,
        string atk1, string atk2, string atk3,
        string clipPrefix)
    {
        var idle = FindClip(idleFbx, idleClip);
        var walk = FindClip(walkFbx, walkClip);
        var run = FindClip(runFbx, runClip);
        if (idle == null)
            idle = FindClip("Assets/Haons SD series Pack/Animation/Common/StandA_idleA.FBX", "StandA_idleA");
        if (walk == null)
            walk = FindClip("Assets/Haons SD series Pack/Animation/Common/WalkA_Front.FBX", "Walk_front@loop");
        if (run == null)
            run = FindClip("Assets/Haons SD series Pack/Animation/Common/RunA_Front.FBX", "RunA_front@loop");

        var a1 = BuildEventClip(attackFbx, atk1, OutClipDir + "/" + clipPrefix + "_Combo1.anim");
        var a2 = BuildEventClip(attackFbx, atk2, OutClipDir + "/" + clipPrefix + "_Combo2.anim");
        var a3 = BuildEventClip(attackFbx, atk3, OutClipDir + "/" + clipPrefix + "_Combo3.anim");
        if (idle == null || walk == null || run == null || a1 == null || a2 == null || a3 == null)
            return null;

        string ctrlPath = OutAnimDir + "/" + name + ".controller";
        if (!AssetDatabase.CopyAsset(KnightCtrl, ctrlPath))
        {
            // Si ya existe, borrar y reintentar.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);
            if (!AssetDatabase.CopyAsset(KnightCtrl, ctrlPath))
            {
                Debug.LogError("[SetupHaon] No se pudo copiar KnightGameplay -> " + ctrlPath);
                return null;
            }
        }

        AssetDatabase.ImportAsset(ctrlPath);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
            return null;

        SetStateMotion(ctrl, "Idle", idle, true);
        SetStateMotion(ctrl, "Move", walk, true);
        SetStateMotion(ctrl, "Run", run, true);
        SetStateMotion(ctrl, "CrouchIdle", idle, true);
        SetStateMotion(ctrl, "CrouchWalk", walk, true);
        SetStateMotion(ctrl, "Attack1", a1, false, 1f);
        SetStateMotion(ctrl, "Attack2", a2, false, 1.05f);
        SetStateMotion(ctrl, "Attack3", a3, false, 1.1f);

        // Silenciar Attack del UpperBody (igual que knight).
        if (ctrl.layers.Length > 1)
        {
            var sm = ctrl.layers[1].stateMachine;
            foreach (var any in sm.anyStateTransitions)
            {
                if (any.destinationState != null && any.destinationState.name == "Attack")
                    any.mute = true;
            }

            foreach (var s in sm.states)
            {
                if (s.state.name == "Attack")
                    s.state.motion = null;
            }
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static void SetStateMotion(AnimatorController ctrl, string stateName, AnimationClip clip, bool loop, float speed = 1f)
    {
        if (clip == null)
            return;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);

        foreach (var s in ctrl.layers[0].stateMachine.states)
        {
            if (s.state.name != stateName)
                continue;
            s.state.motion = clip;
            s.state.speed = speed;
            if (stateName.StartsWith("Attack"))
                s.state.tag = "Attack";
            return;
        }
    }

    static GameObject BuildPlayablePrefab(
        string sourcePath,
        string outPath,
        AnimatorController ctrl,
        bool twinDaggers,
        int maxHealth,
        float moveSpeed,
        float attackReach)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
            throw new System.Exception("Missing " + sourcePath);

        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        root.name = System.IO.Path.GetFileNameWithoutExtension(outPath);
        // Haon SD ~1.0m de alto; subir a ~1.55 para igualar caballero.
        root.transform.localScale = Vector3.one * 1.55f;
        try { root.tag = "Player"; } catch { /* tag opcional */ }

        var anim = root.GetComponentInChildren<Animator>(true);
        if (anim == null)
            anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var cc = root.GetComponent<CharacterController>();
        if (cc == null)
            cc = root.AddComponent<CharacterController>();
        // Medidas en espacio local (después de scale 1.55 => altura mundo ~1.7).
        cc.height = 1.1f;
        cc.radius = 0.22f;
        cc.center = new Vector3(0f, 0.55f, 0f);
        cc.skinWidth = 0.05f;
        cc.stepOffset = 0.25f;
        cc.enabled = true;

        var box = root.GetComponent<BoxCollider>();
        if (box == null)
            box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.55f, 1.1f, 0.55f);
        box.center = new Vector3(0f, 0.55f, 0f);

        var health = root.GetComponent<PlayerHealth>();
        if (health == null)
            health = root.AddComponent<PlayerHealth>();
        health.maxHealth = maxHealth;
        health.currentHealth = maxHealth;

        var pc = root.GetComponent<PlayerController>();
        if (pc == null)
            pc = root.AddComponent<PlayerController>();
        pc.moveSpeed = moveSpeed;
        pc.runSpeedMultiplier = 1.55f;

        AttachWeapons(root, anim, twinDaggers, attackReach);

        var saved = PrefabUtility.SaveAsPrefabAsset(root, outPath);
        Object.DestroyImmediate(root);
        Debug.Log("[SetupHaon] Prefab " + outPath);
        return saved;
    }

    static void AttachWeapons(GameObject root, Animator anim, bool twinDaggers, float attackReach)
    {
        if (anim == null || !anim.isHuman)
            return;

        var rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        var leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        if (rightHand == null)
            return;

        Transform tipParent = rightHand;
        if (twinDaggers)
        {
            var rDag = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Haons SD series Pack/Prefab/Prop_AttachToHand/prf_Prop_R_TwinDagger1.prefab");
            var lDag = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Haons SD series Pack/Prefab/Prop_AttachToHand/prf_Prop_L_TwinDagger1.prefab");
            if (rDag != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(rDag);
                inst.name = "TwinDagger_R";
                inst.transform.SetParent(rightHand, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                tipParent = inst.transform;
            }

            if (lDag != null && leftHand != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(lDag);
                inst.name = "TwinDagger_L";
                inst.transform.SetParent(leftHand, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            var sword = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Haons SD series Pack/Prefab/Prop_AttachToHand/prf_Prop_R_GreatSword1.prefab");
            if (sword != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(sword);
                inst.name = "GreatSword_R";
                inst.transform.SetParent(rightHand, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                tipParent = inst.transform;
            }
        }

        Transform weaponBase;
        Transform weaponTip;
        CreateWeaponSockets(tipParent, out weaponBase, out weaponTip,
            twinDaggers ? 0.02f : 0.05f,
            twinDaggers ? 0.35f : 0.85f);

        var melee = root.GetComponent<MeleeWeaponController>() ?? root.AddComponent<MeleeWeaponController>();
        var vfx = root.GetComponent<AttackArcVFX>() ?? root.AddComponent<AttackArcVFX>();
        if (anim.gameObject != melee.gameObject && anim.GetComponent<AttackAnimEventRelay>() == null)
            anim.gameObject.AddComponent<AttackAnimEventRelay>();

        var trail = weaponTip.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = weaponTip.gameObject.AddComponent<TrailRenderer>();
            trail.time = twinDaggers ? 0.12f : 0.14f;
            trail.widthMultiplier = twinDaggers ? 0.06f : 0.08f;
            trail.emitting = false;
            trail.minVertexDistance = 0.02f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                // Rosa/cian suave y translúcido (alpha bajo).
                mat.SetColor("_BaseColor", twinDaggers
                    ? new Color(1f, 0.62f, 0.88f, 0.22f)
                    : new Color(0.7f, 0.85f, 1f, 0.22f));
                trail.sharedMaterial = mat;
            }

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.62f, 0.88f), 0f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.95f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.32f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
        }

        var impact = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactVfx);
        var slash = AssetDatabase.LoadAssetAtPath<GameObject>(SlashVfx);
        var crescent = AssetDatabase.LoadAssetAtPath<GameObject>(CrescentVfx);
        vfx.Configure(weaponTip, trail, slash, crescent, impact, weaponTip, root.transform);
        var vfxSo = new SerializedObject(vfx);
        var ownerProp = vfxSo.FindProperty("owner");
        if (ownerProp != null)
        {
            ownerProp.objectReferenceValue = root.transform;
            vfxSo.ApplyModifiedPropertiesWithoutUndo();
        }

        melee.EditorAssignRefs(
            anim, weaponBase, weaponTip, vfx,
            MeleeWeaponController.AttackStyle.SwordArc, attackReach, impact, 1.95f);

        var meleeSo = new SerializedObject(melee);
        var requireHit = meleeSo.FindProperty("requireHitToContinueCombo");
        if (requireHit != null) requireHit.boolValue = false;
        var buf = meleeSo.FindProperty("inputBufferWindow");
        if (buf != null) buf.floatValue = 0.35f;
        var reset = meleeSo.FindProperty("comboResetTime");
        if (reset != null) reset.floatValue = 0.7f;
        var reach = meleeSo.FindProperty("reachMultiplier");
        if (reach != null) reach.floatValue = 1.95f;
        var lunge = meleeSo.FindProperty("meleeLungeDistance");
        if (lunge != null) lunge.floatValue = 1.5f;
        var lungeDur = meleeSo.FindProperty("meleeLungeDuration");
        if (lungeDur != null) lungeDur.floatValue = 0.3f;
        var meleeMul = meleeSo.FindProperty("meleeAttackMoveMultiplier");
        if (meleeMul != null) meleeMul.floatValue = 0f;
        meleeSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateWeaponSockets(Transform parent, out Transform weaponBase, out Transform weaponTip, float baseY, float tipY)
    {
        weaponBase = parent.Find("WeaponBase");
        if (weaponBase == null)
        {
            var go = new GameObject("WeaponBase");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, baseY, 0f);
            weaponBase = go.transform;
        }

        weaponTip = parent.Find("WeaponTip");
        if (weaponTip == null)
        {
            var go = new GameObject("WeaponTip");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, tipY, 0f);
            weaponTip = go.transform;
        }
    }

    static void WirePlayerSpawner(GameObject misaki, GameObject yuko)
    {
        var scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        var spawners = Object.FindObjectsByType<PlayerSpawner>(FindObjectsInactive.Include);
        foreach (var spawner in spawners)
        {
            if (spawner == null)
                continue;
            spawner.misakiPrefab = misaki;
            spawner.yukoPrefab = yuko;
            EditorUtility.SetDirty(spawner);
            Debug.Log("[SetupHaon] PlayerSpawner cableado en " + spawner.gameObject.name);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

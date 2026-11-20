using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupLowPolyMonsters
{
    const string ModelRoot = "Assets/_ArcanoDungeon/Art/Monsters/Rigged/";
    const string PalettePath = "Assets/Monster Pack/Textures/Gradient Pallete.png";
    const string PrefabDir = "Assets/_ArcanoDungeon/Prefabs/Monsters";
    const string AnimDir = "Assets/_ArcanoDungeon/Animators/Monsters";
    const string ClipDir = "Assets/_ArcanoDungeon/Animators/Monsters/Clips";
    const string MaterialDir = "Assets/_ArcanoDungeon/Art/Materials/Monsters";
    const string HealthBarSource = "Assets/Prefabs/Skeleton.prefab";
    const string LevelPath = "Assets/_ArcanoDungeon/Scenes/Level1.unity";

    struct MonsterDef
    {
        public string Model;
        public string PrefabName;
        public float Height;
        public int Health;
        public int Damage;
        public float Speed;
        public float Range;
        public float Cooldown;
    }

    static readonly MonsterDef[] Monsters =
    {
        new MonsterDef { Model = "Skeleton", PrefabName = "EnemyPackSkeleton", Height = 1.7f, Health = 60, Damage = 10, Speed = 3.4f, Range = 1.5f, Cooldown = 1.2f },
        new MonsterDef { Model = "Skell_Warrior", PrefabName = "EnemySkellWarrior", Height = 2.2f, Health = 100, Damage = 16, Speed = 2.3f, Range = 2.1f, Cooldown = 1.6f },
        new MonsterDef { Model = "Skell_Mage", PrefabName = "EnemySkellMage", Height = 1.9f, Health = 70, Damage = 12, Speed = 2.6f, Range = 2.4f, Cooldown = 1.8f },
        new MonsterDef { Model = "Orc", PrefabName = "EnemyOrc", Height = 3.6f, Health = 140, Damage = 20, Speed = 2.1f, Range = 3.3f, Cooldown = 1.8f },
        new MonsterDef { Model = "WereWolf", PrefabName = "EnemyWerewolf", Height = 2.6f, Health = 85, Damage = 16, Speed = 4f, Range = 2.2f, Cooldown = 1.1f },
        new MonsterDef { Model = "Minotaur", PrefabName = "EnemyMinotaur", Height = 4.2f, Health = 240, Damage = 28, Speed = 1.9f, Range = 3.8f, Cooldown = 2f },
    };

    [MenuItem("Tools/Arcano/Setup Low Poly Monsters", false, 121)]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[LowPolyMonsters] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(AnimDir);
        Directory.CreateDirectory(MaterialDir);
        AssetDatabase.Refresh();

        var built = new Dictionary<string, GameObject>();
        foreach (var def in Monsters)
        {
            var prefab = BuildMonster(def);
            built[def.Model] = prefab;
        }

        WireRooms(built);
        AssetDatabase.SaveAssets();
        Debug.Log("[LowPolyMonsters] Prefabs listos en " + PrefabDir);
    }

    [MenuItem("Tools/Arcano/Rebuild Monster Scale And Clips", false, 122)]
    public static void RebuildPrefabs()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[LowPolyMonsters] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(AnimDir);
        EnsureClipDir();
        foreach (var def in Monsters)
            BuildMonster(def);
        AssetDatabase.SaveAssets();
        Debug.Log("[LowPolyMonsters] Escala y clips de caminar/muerte actualizados.");
    }

    static GameObject BuildMonster(MonsterDef def)
    {
        string modelPath = ModelRoot + def.Model + ".fbx";
        EnsureGeneric(modelPath);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (source == null)
            throw new Exception("No se encontró " + modelPath);

        EnsureClipDir();
        var temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
        temp.name = def.PrefabName;
        try { temp.tag = "Enemy"; } catch { /* el tag Enemy puede no existir */ }

        var clips = LoadClips(modelPath);
        var walkClip = MakeWalkClip(temp, def.Model);
        var deathClip = MakeDeathClip(temp, def.Model);
        var controller = BuildController(def.Model, clips, walkClip, deathClip);

        var renderers = temp.GetComponentsInChildren<Renderer>(true);
        var bounds = new Bounds(temp.transform.position, Vector3.zero);
        var hasBounds = false;
        foreach (var renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }

            var slots = renderer.sharedMaterials;
            for (var i = 0; i < slots.Length; i++)
                slots[i] = EnsureUrp(slots[i], def.Model, i);
            renderer.sharedMaterials = slots;
        }

        float height = hasBounds ? Mathf.Max(0.2f, bounds.size.y) : 1f;
        float scale = def.Height / height;
        temp.transform.localScale = Vector3.one * scale;

        var anim = temp.GetComponent<Animator>();
        if (anim == null)
            anim = temp.AddComponent<Animator>();
        if (controller != null)
            anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var cc = temp.GetComponent<CharacterController>();
        if (cc == null)
            cc = temp.AddComponent<CharacterController>();
        cc.height = height;
        cc.radius = hasBounds ? Mathf.Max(0.2f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.55f) : 0.35f;
        cc.center = hasBounds ? bounds.center - temp.transform.position : new Vector3(0f, height * 0.5f, 0f);
        cc.skinWidth = 0.05f;
        cc.stepOffset = Mathf.Min(0.3f, cc.height * 0.3f);

        var box = temp.GetComponent<BoxCollider>();
        if (box == null)
            box = temp.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = hasBounds ? bounds.size : new Vector3(0.6f, height, 0.6f);
        box.center = cc.center;

        var health = temp.GetComponent<EnemyHealth>();
        if (health == null)
            health = temp.AddComponent<EnemyHealth>();
        health.maxHealth = def.Health;
        health.currentHealth = def.Health;
        health.deathTrigger = "Die";
        health.hitTrigger = "Hit";
        health.deathStateName = "Death";
        health.deathDestroyDelay = 2.2f;

        var ai = temp.GetComponent<EnemyAI>();
        if (ai == null)
            ai = temp.AddComponent<EnemyAI>();
        ai.attackRange = def.Range;
        ai.attackDamage = def.Damage;
        ai.attackCooldown = def.Cooldown;
        ai.moveSpeed = def.Speed;
        ai.aggroRange = 40f;

        AttachHealthBar(temp, health, hasBounds ? bounds.max.y : height);

        string prefabPath = PrefabDir + "/" + def.PrefabName + ".prefab";
        var saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return saved;
    }

    static void AttachHealthBar(GameObject enemy, EnemyHealth health, float headY)
    {
        var source = PrefabUtility.LoadPrefabContents(HealthBarSource);
        try
        {
            var canvas = source.transform.Find("HealthBarCanvas");
            if (canvas == null)
                return;

            var copy = UnityEngine.Object.Instantiate(canvas.gameObject, enemy.transform);
            copy.name = "HealthBarCanvas";
            var rect = copy.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0f, headY + 0.35f, 0f);
            rect.localRotation = Quaternion.identity;
            float bar = 0.012f / Mathf.Max(0.35f, enemy.transform.localScale.x);
            rect.localScale = Vector3.one * bar;

            health.healthCanvasTransform = copy.transform;
            health.healthSlider = copy.GetComponentInChildren<Slider>(true);
            if (health.healthSlider != null && health.healthSlider.fillRect != null)
                health.healthFillImage = health.healthSlider.fillRect.GetComponent<Image>();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
        }
    }

    static Material EnsureUrp(Material source, string monster, int index)
    {
        if (source == null)
            return null;
        if (source.shader != null && source.shader.name.Contains("Universal Render Pipeline")
            && source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap") != null)
            return source;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            return source;

        string path = MaterialDir + "/" + monster + "_" + index + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = shader;
        Texture tex = null;
        if (source.HasProperty("_BaseMap"))
            tex = source.GetTexture("_BaseMap");
        if (tex == null && source.HasProperty("_MainTex"))
            tex = source.GetTexture("_MainTex");
        if (tex == null)
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        var color = Color.white;
        if (source.HasProperty("_BaseColor"))
            color = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            color = source.GetColor("_Color");

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Smoothness", 0.12f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureGeneric(string modelPath)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null || importer.animationType == ModelImporterAnimationType.Generic)
            return;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
    }

    struct Twist
    {
        public int Axis;
        public float Degrees;
        public Vector3 WorldDelta;
    }

    static void EnsureClipDir()
    {
        if (!AssetDatabase.IsValidFolder(AnimDir))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Animators", "Monsters");
        if (!AssetDatabase.IsValidFolder(ClipDir))
            AssetDatabase.CreateFolder(AnimDir, "Clips");
    }

    static AnimationClip MakeWalkClip(GameObject root, string model)
    {
        var pair = Limbs(root);
        if (pair.ThighL == null || pair.ThighR == null || pair.FootL == null)
            return null;

        var stride = Best(pair.ThighL, pair.FootL, d =>
        {
            d.y = 0f;
            return d.magnitude;
        });
        var forward = stride.WorldDelta;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        var clip = new AnimationClip { frameRate = 30f, name = model + "_Walk" };
        const float step = 40f;
        const float knee = 34f;
        const float arm = 32f;
        SwingLeg(clip, root.transform, pair.ThighL, pair.ShinL, pair.FootL, forward, step, knee, false);
        SwingLeg(clip, root.transform, pair.ThighR, pair.ShinR, pair.FootR != null ? pair.FootR : pair.ShinR, forward, step, knee, true);
        SwingArm(clip, root.transform, pair.ArmL, pair.HandL, forward, arm, true);
        SwingArm(clip, root.transform, pair.ArmR, pair.HandR, forward, arm, false);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return SaveClip(clip, ClipDir + "/" + model + "_Walk.anim");
    }

    static AnimationClip MakeDeathClip(GameObject root, string model)
    {
        var smr = root.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null || smr.rootBone == null)
            return null;

        var high = Highest(smr.bones);
        if (high == null)
            return null;

        var spine = smr.rootBone;
        var restE = spine.localEulerAngles;
        var restP = spine.localPosition;
        var fall = Best(spine, high, d => -d.y);
        float delta = Mathf.Sign(fall.Degrees == 0f ? 1f : fall.Degrees) * 88f;
        var fallenE = restE;
        fallenE[fall.Axis] += delta;
        spine.localEulerAngles = fallenE;

        float minY = float.MaxValue;
        foreach (var bone in smr.bones)
        {
            if (bone == null)
                continue;
            if (bone.position.y < minY)
                minY = bone.position.y;
        }

        var parent = spine.parent;
        var sink = parent != null
            ? parent.InverseTransformVector(new Vector3(0f, -(minY - 0.06f), 0f))
            : new Vector3(0f, -(minY - 0.06f), 0f);
        var fallenP = restP + sink;
        spine.localEulerAngles = restE;
        spine.localPosition = restP;

        var clip = new AnimationClip { frameRate = 30f, name = model + "_Death" };
        string path = BonePath(root.transform, spine);
        SetEuler(clip, path, 0f, restE);
        SetEuler(clip, path, 0.4f, Vector3.Lerp(restE, fallenE, 0.35f));
        SetEuler(clip, path, 1.05f, fallenE);
        SetEuler(clip, path, 1.7f, fallenE);
        SetPos(clip, path, 0f, restP);
        SetPos(clip, path, 0.4f, Vector3.Lerp(restP, fallenP, 0.25f));
        SetPos(clip, path, 1.05f, fallenP);
        SetPos(clip, path, 1.7f, fallenP);

        var limbs = Limbs(root);
        Droop(clip, root.transform, limbs.ArmL, limbs.HandL);
        Droop(clip, root.transform, limbs.ArmR, limbs.HandR);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return SaveClip(clip, ClipDir + "/" + model + "_Death.anim");
    }

    struct LimbSet
    {
        public Transform ThighL, ThighR, ShinL, ShinR, FootL, FootR;
        public Transform ArmL, ArmR, HandL, HandR;
    }

    static LimbSet Limbs(GameObject root)
    {
        var smr = root.GetComponentInChildren<SkinnedMeshRenderer>();
        var bones = smr != null ? smr.bones : System.Array.Empty<Transform>();
        return new LimbSet
        {
            ThighL = FindBone(bones, "thigh.L"),
            ThighR = FindBone(bones, "thigh.R"),
            ShinL = FindBone(bones, "shin.L"),
            ShinR = FindBone(bones, "shin.R"),
            FootL = FindBone(bones, "foot.L"),
            FootR = FindBone(bones, "foot.R"),
            ArmL = FindBone(bones, "upper_arm.L"),
            ArmR = FindBone(bones, "upper_arm.R"),
            HandL = FindBone(bones, "hand.L") ?? FindBone(bones, "forearm.L"),
            HandR = FindBone(bones, "hand.R") ?? FindBone(bones, "forearm.R"),
        };
    }

    static void SwingLeg(AnimationClip clip, Transform root, Transform thigh, Transform shin, Transform tip, Vector3 forward, float step, float knee, bool opposite)
    {
        if (thigh == null || tip == null)
            return;

        var swing = Best(thigh, tip, d => Vector3.Dot(d, forward));
        float sign = swing.Degrees < 0f ? -1f : 1f;
        if (opposite)
            sign = -sign;
        var rest = thigh.localEulerAngles;
        string path = BonePath(root, thigh);
        SetEuler(clip, path, 0f, Axis(rest, swing.Axis, sign * step));
        SetEuler(clip, path, 0.25f, rest);
        SetEuler(clip, path, 0.5f, Axis(rest, swing.Axis, -sign * step));
        SetEuler(clip, path, 0.75f, rest);
        SetEuler(clip, path, 1f, Axis(rest, swing.Axis, sign * step));

        if (shin == null)
            return;
        var bend = Best(shin, tip, d => d.y);
        float bendSign = bend.Degrees < 0f ? -1f : 1f;
        var shinRest = shin.localEulerAngles;
        string shinPath = BonePath(root, shin);
        SetEuler(clip, shinPath, 0f, shinRest);
        SetEuler(clip, shinPath, 0.25f, Axis(shinRest, bend.Axis, bendSign * knee));
        SetEuler(clip, shinPath, 0.5f, shinRest);
        SetEuler(clip, shinPath, 0.75f, Axis(shinRest, bend.Axis, bendSign * knee));
        SetEuler(clip, shinPath, 1f, shinRest);
    }

    static void SwingArm(AnimationClip clip, Transform root, Transform arm, Transform hand, Vector3 forward, float amount, bool backFirst)
    {
        if (arm == null || hand == null)
            return;
        var swing = Best(arm, hand, d => Vector3.Dot(d, forward));
        float sign = swing.Degrees < 0f ? -1f : 1f;
        if (backFirst)
            sign = -sign;
        var rest = arm.localEulerAngles;
        string path = BonePath(root, arm);
        SetEuler(clip, path, 0f, Axis(rest, swing.Axis, sign * amount));
        SetEuler(clip, path, 0.25f, rest);
        SetEuler(clip, path, 0.5f, Axis(rest, swing.Axis, -sign * amount));
        SetEuler(clip, path, 0.75f, rest);
        SetEuler(clip, path, 1f, Axis(rest, swing.Axis, sign * amount));
    }

    static void Droop(AnimationClip clip, Transform root, Transform arm, Transform hand)
    {
        if (arm == null || hand == null)
            return;
        var droop = Best(arm, hand, d => -d.y);
        float sign = droop.Degrees < 0f ? -1f : 1f;
        var rest = arm.localEulerAngles;
        var down = Axis(rest, droop.Axis, sign * 46f);
        string path = BonePath(root, arm);
        SetEuler(clip, path, 0f, rest);
        SetEuler(clip, path, 0.45f, Vector3.Lerp(rest, down, 0.45f));
        SetEuler(clip, path, 1.05f, down);
        SetEuler(clip, path, 1.7f, down);
    }

    static Vector3 Axis(Vector3 euler, int axis, float delta)
    {
        euler[axis] += delta;
        return euler;
    }

    static Twist Best(Transform bone, Transform tip, System.Func<Vector3, float> score)
    {
        var rest = bone.localEulerAngles;
        var origin = tip.position;
        var best = new Twist { Axis = 0, Degrees = 40f, WorldDelta = Vector3.zero };
        float bestScore = float.MinValue;
        for (int axis = 0; axis < 3; axis++)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            {
                bone.localEulerAngles = Axis(rest, axis, 40f * sign);
                var delta = tip.position - origin;
                float value = score(delta);
                if (value > bestScore)
                {
                    bestScore = value;
                    best = new Twist { Axis = axis, Degrees = 40f * sign, WorldDelta = delta };
                }
            }
        }

        bone.localEulerAngles = rest;
        return best;
    }

    static Transform Highest(Transform[] bones)
    {
        Transform high = null;
        float y = float.MinValue;
        if (bones == null)
            return null;
        foreach (var bone in bones)
        {
            if (bone == null || bone.position.y <= y)
                continue;
            y = bone.position.y;
            high = bone;
        }
        return high;
    }

    static Transform FindBone(Transform[] bones, string token)
    {
        Transform best = null;
        if (bones == null)
            return null;
        foreach (var bone in bones)
        {
            if (bone == null)
                continue;
            if (bone.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (best == null || bone.name.Length < best.name.Length)
                best = bone;
        }
        return best;
    }

    static string BonePath(Transform root, Transform bone)
    {
        var parts = new List<string>();
        while (bone != null && bone != root)
        {
            parts.Add(bone.name);
            bone = bone.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    static void SetEuler(AnimationClip clip, string path, float time, Vector3 euler)
    {
        SetFloat(clip, path, "localEulerAnglesRaw.x", time, euler.x);
        SetFloat(clip, path, "localEulerAnglesRaw.y", time, euler.y);
        SetFloat(clip, path, "localEulerAnglesRaw.z", time, euler.z);
    }

    static void SetPos(AnimationClip clip, string path, float time, Vector3 pos)
    {
        SetFloat(clip, path, "m_LocalPosition.x", time, pos.x);
        SetFloat(clip, path, "m_LocalPosition.y", time, pos.y);
        SetFloat(clip, path, "m_LocalPosition.z", time, pos.z);
    }

    static void SetFloat(AnimationClip clip, string path, string property, float time, float value)
    {
        var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
        var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
        curve.AddKey(new Keyframe(time, value));
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    }

    static List<AnimationClip> LoadClips(string modelPath)
    {
        var clips = new List<AnimationClip>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            var clip = asset as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview"))
                continue;
            clips.Add(clip);
        }
        return clips;
    }

    static AnimatorController BuildController(string model, List<AnimationClip> clips, AnimationClip authoredWalk, AnimationClip authoredDeath)
    {
        var idle = FindClip(clips, "idle");
        var walk = authoredWalk != null ? authoredWalk : (FindClip(clips, "walk") ?? FindClip(clips, "run"));
        var attack = FindClip(clips, "attack");
        var hit = FindClip(clips, "hit") ?? FindClip(clips, "damage") ?? FindClip(clips, "gethit");
        var death = authoredDeath != null ? authoredDeath : (FindClip(clips, "death") ?? FindClip(clips, "die"));
        if (idle == null || walk == null || attack == null)
        {
            Debug.Log("[LowPolyMonsters] " + model + " no trae clips de idle/walk/attack. Queda en pose de reposo.");
            return null;
        }

        string path = AnimDir + "/" + model + "Gameplay.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var machine = ctrl.layers[0].stateMachine;
        foreach (var state in machine.states)
            machine.RemoveState(state.state);

        var idleState = machine.AddState("Idle", new Vector3(200f, 0f, 0f));
        idleState.motion = idle;
        machine.defaultState = idleState;

        var walkState = machine.AddState("Walk", new Vector3(200f, 80f, 0f));
        walkState.motion = walk;

        var attackState = machine.AddState("Attack", new Vector3(450f, 40f, 0f));
        attackState.motion = attack;

        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.1f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var anyAttack = machine.AddAnyStateTransition(attackState);
        anyAttack.hasExitTime = false;
        anyAttack.duration = 0.05f;
        anyAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        var attackDone = attackState.AddTransition(idleState);
        attackDone.hasExitTime = true;
        attackDone.exitTime = 0.9f;
        attackDone.duration = 0.1f;

        if (hit != null)
        {
            var hitState = machine.AddState("Hit", new Vector3(450f, 140f, 0f));
            hitState.motion = hit;
            var anyHit = machine.AddAnyStateTransition(hitState);
            anyHit.hasExitTime = false;
            anyHit.duration = 0.05f;
            anyHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            var hitDone = hitState.AddTransition(idleState);
            hitDone.hasExitTime = true;
            hitDone.exitTime = 0.85f;
            hitDone.duration = 0.08f;
        }

        if (death != null)
        {
            var deathState = machine.AddState("Death", new Vector3(450f, 240f, 0f));
            deathState.motion = death;
            var anyDeath = machine.AddAnyStateTransition(deathState);
            anyDeath.hasExitTime = false;
            anyDeath.duration = 0.05f;
            anyDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static AnimationClip FindClip(List<AnimationClip> clips, string token)
    {
        foreach (var clip in clips)
        {
            if (clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return clip;
        }
        return null;
    }

    static void WireRooms(Dictionary<string, GameObject> built)
    {
        var scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        var rooms = UnityEngine.Object.FindObjectsByType<RoomController>(FindObjectsInactive.Include);
        foreach (var room in rooms)
        {
            if (room == null || string.IsNullOrEmpty(room.roomName))
                continue;

            if (room.roomName.Contains("Minijefe"))
                continue;

            if (room.roomName.Contains("Sala Norte"))
                SetAdds(room, built["Orc"], built["Skell_Warrior"]);
            else if (room.roomName.Contains("Ala Oeste"))
                SetAdds(room, built["WereWolf"], built["Skell_Mage"]);
            else if (room.roomName == "Sala Sur")
                SetAdds(room, built["Orc"], built["Skeleton"]);
            else if (room.roomName.Contains("Sureste"))
                SetSureste(room, built["WereWolf"], built["Skell_Warrior"], built["Minotaur"]);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SetAdds(RoomController room, params GameObject[] extras)
    {
        room.additionalEnemyPrefabs = extras;
        EditorUtility.SetDirty(room);
        Debug.Log("[LowPolyMonsters] " + room.roomName);
    }

    static void SetSureste(RoomController room, GameObject werewolf, GameObject warrior, GameObject minotaur)
    {
        room.skeletonPrefab = werewolf;
        room.additionalEnemyPrefabs = new[] { warrior };
        room.enemyScale = 1f;
        room.spawnMiniBoss = true;
        room.miniBossPrefab = minotaur;
        room.miniBossScale = 1f;
        room.miniBossMaxHealth = 240;
        if (room.miniBossSpawnPoint == null)
            room.miniBossSpawnPoint = CreateBossPoint(room);
        EditorUtility.SetDirty(room);
        Debug.Log("[LowPolyMonsters] " + room.roomName + " con minotauro");
    }

    static Transform CreateBossPoint(RoomController room)
    {
        var point = new GameObject("MiniBossSpawn").transform;
        point.SetParent(room.transform, false);
        if (room.normalSpawnPoints != null && room.normalSpawnPoints.Length > 0)
        {
            var sum = Vector3.zero;
            var count = 0;
            foreach (var spawn in room.normalSpawnPoints)
            {
                if (spawn == null)
                    continue;
                sum += spawn.position;
                count++;
            }
            if (count > 0)
                point.position = sum / count;
        }
        return point;
    }
}

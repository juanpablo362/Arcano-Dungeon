using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class GenerateEnemyHitDeathClips
{
    const string Dir = "Assets/_ArcanoDungeon/Animators/EnemyClips";

    [MenuItem("Tools/Arcano/Generate Enemy Hit Death Clips")]
    public static void Generate()
    {
        EnsureFolder();

        var kayHit = BuildKayKitHit();
        var kayDeath = BuildKayKitDeath();
        var classicHit = BuildClassicHit();
        var classicDeath = BuildClassicDeath();

        kayHit = Save(kayHit, Dir + "/KayKit_Enemy_Hit.anim");
        kayDeath = Save(kayDeath, Dir + "/KayKit_Enemy_Death.anim");
        classicHit = Save(classicHit, Dir + "/Classic_Enemy_Hit.anim");
        classicDeath = Save(classicDeath, Dir + "/Classic_Enemy_Death.anim");

        WireController(
            "Assets/_ArcanoDungeon/Animators/KayKitEnemyGameplay.controller",
            kayHit, kayDeath, "Idle");
        WireController(
            "Assets/Skeleton/FBX/Skeleton.controller",
            classicHit, classicDeath, "Blend Tree");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GenerateEnemyHitDeathClips] Clips created and controllers wired.");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Animators"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Animators");
        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Animators", "EnemyClips");
    }

    static AnimationClip Save(AnimationClip clip, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    }

    static Vector3 Rest(GameObject prefab, string bonePath)
    {
        var t = prefab.transform.Find(bonePath);
        return t != null ? t.localEulerAngles : Vector3.zero;
    }

    static void SetEuler(AnimationClip clip, string bonePath, float t, Vector3 euler)
    {
        void Add(string prop, float value)
        {
            var binding = EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), prop);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            int idx = -1;
            for (int i = 0; i < curve.keys.Length; i++)
            {
                if (Mathf.Abs(curve.keys[i].time - t) < 0.0001f)
                    idx = i;
            }

            var key = new Keyframe(t, value);
            if (idx >= 0) curve.MoveKey(idx, key);
            else curve.AddKey(key);

            for (int i = 0; i < curve.keys.Length; i++)
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        Add("localEulerAnglesRaw.x", euler.x);
        Add("localEulerAnglesRaw.y", euler.y);
        Add("localEulerAnglesRaw.z", euler.z);
    }

    static void NoLoop(AnimationClip clip)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static AnimationClip BuildKayKitHit()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_ArcanoDungeon/Prefabs/EnemySkeletonMinion.prefab");
        string spine = "Rig_Medium/root/hips/spine";
        string chest = "Rig_Medium/root/hips/spine/chest";
        string head = "Rig_Medium/root/hips/spine/chest/head";
        if (prefab.transform.Find("Rig_Medium/root/hips/spine/chest/neck_01/head") != null)
            head = "Rig_Medium/root/hips/spine/chest/neck_01/head";
        string armL = "Rig_Medium/root/hips/spine/chest/upperarm.l";
        string armR = "Rig_Medium/root/hips/spine/chest/upperarm.r";

        var kS = Rest(prefab, spine);
        var kC = Rest(prefab, chest);
        var kHd = Rest(prefab, head);
        var kAL = Rest(prefab, armL);
        var kAR = Rest(prefab, armR);
        float ht = 0.45f;

        var clip = new AnimationClip { name = "KayKit_Enemy_Hit", frameRate = 30f };
        SetEuler(clip, spine, 0f, kS);
        SetEuler(clip, spine, 0.12f, kS + new Vector3(-22f, 8f, 0f));
        SetEuler(clip, spine, 0.28f, kS + new Vector3(-12f, 4f, 0f));
        SetEuler(clip, spine, ht, kS);

        SetEuler(clip, chest, 0f, kC);
        SetEuler(clip, chest, 0.12f, kC + new Vector3(-18f, -6f, 0f));
        SetEuler(clip, chest, ht, kC);

        SetEuler(clip, head, 0f, kHd);
        SetEuler(clip, head, 0.1f, kHd + new Vector3(-25f, 10f, 0f));
        SetEuler(clip, head, ht, kHd);

        SetEuler(clip, armL, 0f, kAL);
        SetEuler(clip, armL, 0.12f, kAL + new Vector3(0f, -15f, 20f));
        SetEuler(clip, armL, ht, kAL);

        SetEuler(clip, armR, 0f, kAR);
        SetEuler(clip, armR, 0.12f, kAR + new Vector3(0f, 15f, -20f));
        SetEuler(clip, armR, ht, kAR);

        NoLoop(clip);
        return clip;
    }

    static AnimationClip BuildKayKitDeath()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_ArcanoDungeon/Prefabs/EnemySkeletonMinion.prefab");
        string hips = "Rig_Medium/root/hips";
        string spine = "Rig_Medium/root/hips/spine";
        string chest = "Rig_Medium/root/hips/spine/chest";
        string head = "Rig_Medium/root/hips/spine/chest/head";
        if (prefab.transform.Find("Rig_Medium/root/hips/spine/chest/neck_01/head") != null)
            head = "Rig_Medium/root/hips/spine/chest/neck_01/head";
        string armL = "Rig_Medium/root/hips/spine/chest/upperarm.l";
        string armR = "Rig_Medium/root/hips/spine/chest/upperarm.r";
        string root = "Rig_Medium/root";

        var kH = Rest(prefab, hips);
        var kS = Rest(prefab, spine);
        var kC = Rest(prefab, chest);
        var kHd = Rest(prefab, head);
        var kAL = Rest(prefab, armL);
        var kAR = Rest(prefab, armR);
        var rootPos = prefab.transform.Find(root).localPosition;
        float dt = 1.8f;

        var clip = new AnimationClip { name = "KayKit_Enemy_Death", frameRate = 30f };
        SetEuler(clip, hips, 0f, kH);
        SetEuler(clip, hips, 0.35f, kH + new Vector3(25f, 15f, 0f));
        SetEuler(clip, hips, 0.9f, kH + new Vector3(70f, 25f, 10f));
        SetEuler(clip, hips, dt, kH + new Vector3(88f, 30f, 12f));

        SetEuler(clip, spine, 0f, kS);
        SetEuler(clip, spine, 0.4f, kS + new Vector3(35f, 10f, 0f));
        SetEuler(clip, spine, dt, kS + new Vector3(55f, 15f, 5f));

        SetEuler(clip, chest, 0f, kC);
        SetEuler(clip, chest, 0.5f, kC + new Vector3(40f, -8f, 0f));
        SetEuler(clip, chest, dt, kC + new Vector3(50f, -10f, 0f));

        SetEuler(clip, head, 0f, kHd);
        SetEuler(clip, head, 0.3f, kHd + new Vector3(20f, 0f, 0f));
        SetEuler(clip, head, 1.0f, kHd + new Vector3(55f, 15f, 0f));
        SetEuler(clip, head, dt, kHd + new Vector3(70f, 20f, 0f));

        SetEuler(clip, armL, 0f, kAL);
        SetEuler(clip, armL, 0.6f, kAL + new Vector3(30f, -40f, 25f));
        SetEuler(clip, armL, dt, kAL + new Vector3(45f, -50f, 30f));

        SetEuler(clip, armR, 0f, kAR);
        SetEuler(clip, armR, 0.6f, kAR + new Vector3(30f, 40f, -25f));
        SetEuler(clip, armR, dt, kAR + new Vector3(45f, 50f, -30f));

        void SetPos(float t, float y)
        {
            void Add(string prop, float value)
            {
                var binding = EditorCurveBinding.FloatCurve(root, typeof(Transform), prop);
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
                curve.AddKey(new Keyframe(t, value));
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            Add("m_LocalPosition.x", rootPos.x);
            Add("m_LocalPosition.y", y);
            Add("m_LocalPosition.z", rootPos.z);
        }

        SetPos(0f, rootPos.y);
        SetPos(0.8f, rootPos.y - 0.15f);
        SetPos(dt, rootPos.y - 0.35f);

        NoLoop(clip);
        return clip;
    }

    static AnimationClip BuildClassicHit()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Skeleton.prefab");
        string spine = "DeformationSystem/Root_M/Spine1_M";
        string chest = "DeformationSystem/Root_M/Spine1_M/Chest_M";
        string head = "DeformationSystem/Root_M/Spine1_M/Chest_M/Neck1_M/Neck2_M/Head_M";
        string shL = "DeformationSystem/Root_M/Spine1_M/Chest_M/Scapula_L/Shoulder_L";
        string shR = "DeformationSystem/Root_M/Spine1_M/Chest_M/Scapula_R/Shoulder_R";

        var cS = Rest(prefab, spine);
        var cC = Rest(prefab, chest);
        var cHd = Rest(prefab, head);
        var cSL = Rest(prefab, shL);
        var cSR = Rest(prefab, shR);
        float ht = 0.45f;

        var clip = new AnimationClip { name = "Classic_Enemy_Hit", frameRate = 30f };
        SetEuler(clip, spine, 0f, cS);
        SetEuler(clip, spine, 0.12f, cS + new Vector3(0f, 0f, -20f));
        SetEuler(clip, spine, 0.28f, cS + new Vector3(0f, 0f, -10f));
        SetEuler(clip, spine, ht, cS);

        SetEuler(clip, chest, 0f, cC);
        SetEuler(clip, chest, 0.12f, cC + new Vector3(0f, 0f, -15f));
        SetEuler(clip, chest, ht, cC);

        SetEuler(clip, head, 0f, cHd);
        SetEuler(clip, head, 0.1f, cHd + new Vector3(0f, 0f, -22f));
        SetEuler(clip, head, ht, cHd);

        SetEuler(clip, shL, 0f, cSL);
        SetEuler(clip, shL, 0.12f, cSL + new Vector3(15f, 0f, 10f));
        SetEuler(clip, shL, ht, cSL);

        SetEuler(clip, shR, 0f, cSR);
        SetEuler(clip, shR, 0.12f, cSR + new Vector3(15f, 0f, 10f));
        SetEuler(clip, shR, ht, cSR);

        NoLoop(clip);
        return clip;
    }

    static AnimationClip BuildClassicDeath()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Skeleton.prefab");
        string root = "DeformationSystem/Root_M";
        string spine = "DeformationSystem/Root_M/Spine1_M";
        string chest = "DeformationSystem/Root_M/Spine1_M/Chest_M";
        string neck = "DeformationSystem/Root_M/Spine1_M/Chest_M/Neck1_M";
        string head = "DeformationSystem/Root_M/Spine1_M/Chest_M/Neck1_M/Neck2_M/Head_M";
        string shL = "DeformationSystem/Root_M/Spine1_M/Chest_M/Scapula_L/Shoulder_L";
        string shR = "DeformationSystem/Root_M/Spine1_M/Chest_M/Scapula_R/Shoulder_R";

        var cR = Rest(prefab, root);
        var cS = Rest(prefab, spine);
        var cC = Rest(prefab, chest);
        var cN = Rest(prefab, neck);
        var cHd = Rest(prefab, head);
        var cSL = Rest(prefab, shL);
        var cSR = Rest(prefab, shR);
        float dt = 1.8f;

        var clip = new AnimationClip { name = "Classic_Enemy_Death", frameRate = 30f };
        SetEuler(clip, root, 0f, cR);
        SetEuler(clip, root, 0.4f, cR + new Vector3(0f, 0f, 35f));
        SetEuler(clip, root, 1.0f, cR + new Vector3(0f, 20f, 70f));
        SetEuler(clip, root, dt, cR + new Vector3(0f, 30f, 95f));

        SetEuler(clip, spine, 0f, cS);
        SetEuler(clip, spine, 0.5f, cS + new Vector3(0f, 0f, 25f));
        SetEuler(clip, spine, dt, cS + new Vector3(0f, 0f, 40f));

        SetEuler(clip, chest, 0f, cC);
        SetEuler(clip, chest, 0.6f, cC + new Vector3(0f, 0f, 30f));
        SetEuler(clip, chest, dt, cC + new Vector3(0f, 0f, 45f));

        SetEuler(clip, neck, 0f, cN);
        SetEuler(clip, neck, 0.7f, cN + new Vector3(0f, 0f, 20f));
        SetEuler(clip, neck, dt, cN + new Vector3(0f, 0f, 35f));

        SetEuler(clip, head, 0f, cHd);
        SetEuler(clip, head, 0.8f, cHd + new Vector3(0f, 0f, 40f));
        SetEuler(clip, head, dt, cHd + new Vector3(0f, 0f, 55f));

        SetEuler(clip, shL, 0f, cSL);
        SetEuler(clip, shL, 0.7f, cSL + new Vector3(40f, 0f, 20f));
        SetEuler(clip, shL, dt, cSL + new Vector3(55f, 0f, 25f));

        SetEuler(clip, shR, 0f, cSR);
        SetEuler(clip, shR, 0.7f, cSR + new Vector3(40f, 0f, 20f));
        SetEuler(clip, shR, dt, cSR + new Vector3(55f, 0f, 25f));

        NoLoop(clip);
        return clip;
    }

    static void WireController(string ctrlPath, AnimationClip hit, AnimationClip death, string returnStateName)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            Debug.LogError("[GenerateEnemyHitDeathClips] Missing controller " + ctrlPath);
            return;
        }

        var sm = ctrl.layers[0].stateMachine;
        AnimatorState hitState = null;
        AnimatorState deathState = null;
        AnimatorState returnState = null;

        foreach (var s in sm.states)
        {
            if (s.state.name == "Hit") hitState = s.state;
            if (s.state.name == "Death") deathState = s.state;
            if (s.state.name == returnStateName) returnState = s.state;
        }

        if (hitState != null && hit != null)
        {
            hitState.motion = hit;
            hitState.tag = "Hit";
            hitState.writeDefaultValues = true;

            var transitions = hitState.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
                hitState.RemoveTransition(transitions[i]);

            if (returnState != null)
            {
                var t = hitState.AddTransition(returnState);
                t.hasExitTime = true;
                t.exitTime = 0.92f;
                t.duration = 0.08f;
                t.hasFixedDuration = true;
            }
        }

        if (deathState != null && death != null)
        {
            deathState.motion = death;
            deathState.tag = "Death";
            deathState.writeDefaultValues = false;

            var transitions = deathState.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
                deathState.RemoveTransition(transitions[i]);
        }

        EditorUtility.SetDirty(ctrl);
        Debug.Log("[GenerateEnemyHitDeathClips] Wired " + ctrlPath);
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Cablea KayKit Character Animations en héroes KayKit, enemigos y deja clips listos
/// para gestos de Character Select (loop + combo Attack1/2/3 + Hit/Death).
/// </summary>
public static class SetupKayKitCharacterAnims
{
    const string AnimRoot =
        "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
    const string General = AnimRoot + "Rig_Medium_General.fbx";
    const string MoveBasic = AnimRoot + "Rig_Medium_MovementBasic.fbx";
    const string MoveAdv = AnimRoot + "Rig_Medium_MovementAdvanced.fbx";
    const string Melee = AnimRoot + "Rig_Medium_CombatMelee.fbx";
    const string Ranged = AnimRoot + "Rig_Medium_CombatRanged.fbx";
    const string Special = AnimRoot + "Rig_Medium_Special.fbx";
    const string Simulation = AnimRoot + "Rig_Medium_Simulation.fbx";
    const string ClipDir = "Assets/_ArcanoDungeon/Animators/CombatClips";

    const string KnightCtrl = "Assets/_ArcanoDungeon/Animators/KayKitKnightGameplay.controller";
    const string MageCtrl = "Assets/_ArcanoDungeon/Animators/KayKitMageGameplay.controller";
    const string RogueCtrl = "Assets/_ArcanoDungeon/Animators/KayKitRogueGameplay.controller";
    const string EnemyCtrl = "Assets/_ArcanoDungeon/Animators/KayKitEnemyGameplay.controller";

    [MenuItem("Tools/Arcano/Setup KayKit Character Animations", false, 140)]
    public static void Setup()
    {
        EnsureFolder(ClipDir);

        EnableLoops(General, "Idle_A", "Idle_B", "Melee_Unarmed_Idle");
        EnableLoops(MoveBasic, "Walking_A", "Walking_B", "Walking_C", "Running_A", "Running_B");
        EnableLoops(MoveAdv, "Crouching", "Sneaking", "Crawling", "Walking_Backwards");
        EnableLoops(Melee, "Melee_Blocking", "Melee_2H_Idle", "Melee_Unarmed_Idle", "Melee_Block");
        EnableLoops(Special, "Skeletons_Idle", "Skeletons_Walking");
        EnableLoops(Simulation, "Cheering", "Waving", "Sit_Chair_Idle", "Sit_Floor_Idle");
        EnableLoops(Ranged, "Ranged_Bow_Idle", "Ranged_Bow_Aiming_Idle", "Ranged_Magic_Spellcasting",
            "Ranged_1H_Aiming", "Ranged_2H_Aiming");

        var idle = FindClip(General, "Idle_A");
        var walk = FindClip(MoveBasic, "Walking_A");
        var run = FindClip(MoveBasic, "Running_A");
        var crouch = FindClip(MoveAdv, "Crouching");
        var sneak = FindClip(MoveAdv, "Sneaking");
        var jump = FindClip(MoveBasic, "Jump_Full_Short") ?? FindClip(MoveBasic, "Jump_Start");
        var blockHold = FindClip(Melee, "Melee_Blocking") ?? FindClip(Melee, "Melee_Block");
        var hit = FindClip(General, "Hit_A");
        var death = FindClip(Special, "Skeletons_Death") ?? FindClip(General, "Death_A");
        var skelIdle = FindClip(Special, "Skeletons_Idle");
        var skelWalk = FindClip(Special, "Skeletons_Walking");
        var enemyAttack = FindClip(Melee, "Melee_1H_Attack_Chop");

        if (idle == null || walk == null || run == null || jump == null)
        {
            Debug.LogError("[KayKitAnims] Faltan clips base Idle/Walk/Run/Jump.");
            return;
        }

        // Knight: 1H chop → diagonal → horizontal finisher
        var k1 = EnsureEventClip("KayKit_Knight_Combo1.anim", Melee, "Melee_1H_Attack_Chop", 0.42f);
        var k2 = EnsureEventClip("KayKit_Knight_Combo2.anim", Melee, "Melee_1H_Attack_Slice_Diagonal", 0.40f);
        var k3 = EnsureEventClip("KayKit_Knight_Combo3.anim", Melee, "Melee_1H_Attack_Slice_Horizontal", 0.48f);

        // Mage: shoot → cast → raise
        var m1 = EnsureEventClip("KayKit_Mage_Combo1.anim", Ranged, "Ranged_Magic_Shoot", 0.35f);
        var m2 = EnsureEventClip("KayKit_Mage_Combo2.anim", Ranged, "Ranged_Magic_Spellcasting", 0.45f);
        var m3 = EnsureEventClip("KayKit_Mage_Combo3.anim", Ranged, "Ranged_Magic_Raise", 0.55f);

        // Rogue: stab → dual slice → dual chop
        var r1 = EnsureEventClip("KayKit_Rogue_Combo1.anim", Melee, "Melee_1H_Attack_Stab", 0.38f);
        var r2 = EnsureEventClip("KayKit_Rogue_Combo2.anim", Melee, "Melee_Dualwield_Attack_Slice", 0.40f);
        var r3 = EnsureEventClip("KayKit_Rogue_Combo3.anim", Melee, "Melee_Dualwield_Attack_Chop", 0.48f);

        WireHero(KnightCtrl, idle, walk, run, crouch, jump, blockHold, k1, k2, k3, useSneakCrouch: false);
        WireHero(MageCtrl, idle, walk, run, crouch, jump, blockHold, m1, m2, m3, useSneakCrouch: false);
        WireHero(RogueCtrl, idle, walk, run, sneak ?? crouch, jump, blockHold, r1, r2, r3, useSneakCrouch: true);
        WireEnemy(EnemyCtrl, skelIdle ?? idle, skelWalk ?? walk, enemyAttack, hit, death);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[KayKitAnims] Héroes + enemigos cableados con KayKit Character Animations.");
    }

    static void WireHero(
        string ctrlPath,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip crouch,
        AnimationClip jump,
        AnimationClip block,
        AnimationClip a1,
        AnimationClip a2,
        AnimationClip a3,
        bool useSneakCrouch)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            Debug.LogError("[KayKitAnims] Missing " + ctrlPath);
            return;
        }

        EnsureBool(ctrl, "isMoving");
        EnsureBool(ctrl, "isRunning");
        EnsureBool(ctrl, "isCrouching");
        EnsureBool(ctrl, "isBlocking");
        EnsureTrigger(ctrl, "Attack");
        EnsureTrigger(ctrl, "Jump");
        EnsureInt(ctrl, "ComboIndex");

        var sm = ctrl.layers[0].stateMachine;
        var idleSt = EnsureState(sm, "Idle", new Vector3(50f, 50f, 0f));
        var walkSt = EnsureState(sm, "Walk", new Vector3(250f, 50f, 0f));
        var runSt = EnsureState(sm, "Run", new Vector3(450f, 50f, 0f));
        var crouchSt = EnsureState(sm, "Crouch", new Vector3(250f, 180f, 0f));
        var jumpSt = EnsureState(sm, "Jump", new Vector3(450f, -80f, 0f));
        var blockSt = EnsureState(sm, "Block", new Vector3(50f, 180f, 0f));
        var attackLegacy = EnsureState(sm, "Attack", new Vector3(650f, 50f, 0f));

        SetMotion(idleSt, idle, true);
        SetMotion(walkSt, walk, true);
        SetMotion(runSt, run, true);
        SetMotion(crouchSt, crouch, true);
        SetMotion(jumpSt, jump, false);
        jumpSt.tag = "Jump";
        SetMotion(blockSt, block, true);
        SetMotion(attackLegacy, a1, false);
        attackLegacy.tag = "Attack";

        WireAttack(sm, idleSt, "Attack1", a1, 1f, new Vector3(650f, 120f, 0f));
        WireAttack(sm, idleSt, "Attack2", a2, 1.05f, new Vector3(650f, 190f, 0f));
        WireAttack(sm, idleSt, "Attack3", a3, 1.1f, new Vector3(650f, 260f, 0f));

        sm.defaultState = idleSt;
        ClearAllTransitions(sm);

        // Locomotion
        AddBool(idleSt, walkSt, false, 0.1f, ("isMoving", true), ("isRunning", false), ("isCrouching", false));
        AddBool(idleSt, runSt, false, 0.1f, ("isMoving", true), ("isRunning", true), ("isCrouching", false));
        AddBool(walkSt, idleSt, false, 0.1f, ("isMoving", false));
        AddBool(walkSt, runSt, false, 0.1f, ("isRunning", true), ("isCrouching", false));
        AddBool(runSt, walkSt, false, 0.1f, ("isRunning", false), ("isMoving", true), ("isCrouching", false));
        AddBool(runSt, idleSt, false, 0.1f, ("isMoving", false));

        AddBool(idleSt, crouchSt, false, 0.1f, ("isCrouching", true));
        AddBool(walkSt, crouchSt, false, 0.1f, ("isCrouching", true));
        AddBool(runSt, crouchSt, false, 0.1f, ("isCrouching", true));
        AddBool(crouchSt, idleSt, false, 0.1f, ("isCrouching", false), ("isMoving", false));
        AddBool(crouchSt, walkSt, false, 0.1f, ("isCrouching", false), ("isMoving", true), ("isRunning", false));
        AddBool(crouchSt, runSt, false, 0.1f, ("isCrouching", false), ("isMoving", true), ("isRunning", true));

        // Block
        var anyBlock = sm.AddAnyStateTransition(blockSt);
        anyBlock.hasExitTime = false;
        anyBlock.duration = 0.08f;
        anyBlock.canTransitionToSelf = false;
        anyBlock.AddCondition(AnimatorConditionMode.If, 0f, "isBlocking");
        AddBool(blockSt, idleSt, false, 0.1f, ("isBlocking", false));

        // Jump
        var anyJump = sm.AddAnyStateTransition(jumpSt);
        anyJump.hasExitTime = false;
        anyJump.duration = 0.05f;
        anyJump.canTransitionToSelf = false;
        anyJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
        var jumpBack = jumpSt.AddTransition(idleSt);
        jumpBack.hasExitTime = true;
        jumpBack.exitTime = 0.85f;
        jumpBack.duration = 0.1f;
        jumpBack.hasFixedDuration = true;

        // Legacy Attack trigger (PlayerController / special)
        var anyAttack = sm.AddAnyStateTransition(attackLegacy);
        anyAttack.hasExitTime = false;
        anyAttack.duration = 0.05f;
        anyAttack.canTransitionToSelf = false;
        anyAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        var atkBack = attackLegacy.AddTransition(idleSt);
        atkBack.hasExitTime = true;
        atkBack.exitTime = 0.9f;
        atkBack.duration = 0.08f;
        atkBack.hasFixedDuration = true;

        EditorUtility.SetDirty(ctrl);
        Debug.Log("[KayKitAnims] Hero OK " + Path.GetFileName(ctrlPath) +
                  (useSneakCrouch ? " (sneak crouch)" : ""));
    }

    static void WireEnemy(
        string ctrlPath,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip attack,
        AnimationClip hit,
        AnimationClip death)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            Debug.LogError("[KayKitAnims] Missing " + ctrlPath);
            return;
        }

        EnsureFloat(ctrl, "Speed");
        EnsureTrigger(ctrl, "Attack");
        EnsureTrigger(ctrl, "Die");
        EnsureTrigger(ctrl, "Hit");

        var sm = ctrl.layers[0].stateMachine;
        var idleSt = EnsureState(sm, "Idle", new Vector3(50f, 50f, 0f));
        var walkSt = EnsureState(sm, "Walk", new Vector3(250f, 50f, 0f));
        var attackSt = EnsureState(sm, "Attack", new Vector3(450f, 50f, 0f));
        var hitSt = EnsureState(sm, "Hit", new Vector3(250f, 180f, 0f));
        var deathSt = EnsureState(sm, "Death", new Vector3(450f, 180f, 0f));

        SetMotion(idleSt, idle, true);
        SetMotion(walkSt, walk, true);
        SetMotion(attackSt, attack, false);
        attackSt.tag = "Attack";
        SetMotion(hitSt, hit, false);
        hitSt.tag = "Hit";
        hitSt.writeDefaultValues = false;
        SetMotion(deathSt, death, false);
        deathSt.tag = "Death";
        deathSt.writeDefaultValues = false;

        sm.defaultState = idleSt;
        ClearAllTransitions(sm);

        AddFloat(idleSt, walkSt, false, 0.1f, "Speed", true, 0.1f);
        AddFloat(walkSt, idleSt, false, 0.1f, "Speed", false, 0.1f);

        var anyAtk = sm.AddAnyStateTransition(attackSt);
        anyAtk.hasExitTime = false;
        anyAtk.duration = 0.05f;
        anyAtk.canTransitionToSelf = false;
        anyAtk.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        var atkBack = attackSt.AddTransition(idleSt);
        atkBack.hasExitTime = true;
        atkBack.exitTime = 0.9f;
        atkBack.duration = 0.08f;

        var anyHit = sm.AddAnyStateTransition(hitSt);
        anyHit.hasExitTime = false;
        anyHit.duration = 0.05f;
        anyHit.canTransitionToSelf = false;
        anyHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
        var hitBack = hitSt.AddTransition(idleSt);
        hitBack.hasExitTime = true;
        hitBack.exitTime = 0.92f;
        hitBack.duration = 0.08f;

        var anyDie = sm.AddAnyStateTransition(deathSt);
        anyDie.hasExitTime = false;
        anyDie.duration = 0.05f;
        anyDie.canTransitionToSelf = false;
        anyDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");

        EditorUtility.SetDirty(ctrl);
        Debug.Log("[KayKitAnims] Enemy OK " + Path.GetFileName(ctrlPath));
    }

    static void WireAttack(
        AnimatorStateMachine sm,
        AnimatorState idle,
        string name,
        AnimationClip clip,
        float speed,
        Vector3 pos)
    {
        var state = EnsureState(sm, name, pos);
        SetMotion(state, clip, false);
        state.speed = speed;
        state.tag = "Attack";
        state.writeDefaultValues = true;

        // Solo transición de salida; MeleeWeaponController hace Play directo.
        var outs = state.transitions;
        for (int i = outs.Length - 1; i >= 0; i--)
            state.RemoveTransition(outs[i]);

        if (idle == null)
            return;

        var tr = state.AddTransition(idle);
        tr.hasExitTime = true;
        tr.exitTime = 0.92f;
        tr.duration = 0.08f;
        tr.hasFixedDuration = true;
    }

    static AnimationClip EnsureEventClip(string fileName, string fbxPath, string sourceName, float hitNorm)
    {
        string outPath = ClipDir + "/" + fileName;
        var source = FindClip(fbxPath, sourceName);
        if (source == null)
        {
            Debug.LogWarning("[KayKitAnims] No clip " + sourceName + " in " + fbxPath);
            return null;
        }

        var copy = new AnimationClip();
        EditorUtility.CopySerialized(source, copy);
        copy.name = Path.GetFileNameWithoutExtension(outPath);

        float len = Mathf.Max(0.25f, copy.length);
        float hitT = Mathf.Clamp(len * hitNorm, 0.05f, len - 0.05f);
        float endT = Mathf.Min(len * 0.92f, len - 0.02f);
        AnimationUtility.SetAnimationEvents(copy, new[]
        {
            new AnimationEvent { functionName = "OnAttackHitFrame", time = hitT },
            new AnimationEvent { functionName = "OnAttackEndFrame", time = endT }
        });

        var settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(copy, settings);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null)
            AssetDatabase.DeleteAsset(outPath);
        AssetDatabase.CreateAsset(copy, outPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
    }

    static void EnableLoops(string fbxPath, params string[] clipNames)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[KayKitAnims] No importer " + fbxPath);
            return;
        }

        var want = new HashSet<string>(clipNames);
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (!want.Contains(clips[i].name))
                continue;
            if (clips[i].loopTime)
                continue;
            clips[i].loopTime = true;
            changed = true;
        }

        if (!changed)
            return;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        Debug.Log("[KayKitAnims] Loop ON " + Path.GetFileName(fbxPath));
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
        }

        return null;
    }

    static void SetMotion(AnimatorState state, AnimationClip clip, bool loop)
    {
        if (state == null || clip == null)
            return;
        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        if (settings.loopTime != loop)
        {
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
    }

    static AnimatorState EnsureState(AnimatorStateMachine sm, string name, Vector3 pos)
    {
        foreach (var s in sm.states)
            if (s.state.name == name)
                return s.state;
        return sm.AddState(name, pos);
    }

    static void ClearAllTransitions(AnimatorStateMachine sm)
    {
        var any = sm.anyStateTransitions;
        for (int i = any.Length - 1; i >= 0; i--)
            sm.RemoveAnyStateTransition(any[i]);

        foreach (var s in sm.states)
        {
            var outs = s.state.transitions;
            for (int i = outs.Length - 1; i >= 0; i--)
                s.state.RemoveTransition(outs[i]);
        }
    }

    static void AddBool(AnimatorState from, AnimatorState to, bool hasExit, float dur, params (string name, bool value)[] conds)
    {
        if (from == null || to == null)
            return;
        var tr = from.AddTransition(to);
        tr.hasExitTime = hasExit;
        tr.duration = dur;
        tr.hasFixedDuration = true;
        foreach (var c in conds)
            tr.AddCondition(c.value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, c.name);
    }

    static void AddFloat(
        AnimatorState from,
        AnimatorState to,
        bool hasExit,
        float dur,
        string param,
        bool greater,
        float threshold)
    {
        if (from == null || to == null)
            return;
        var tr = from.AddTransition(to);
        tr.hasExitTime = hasExit;
        tr.duration = dur;
        tr.hasFixedDuration = true;
        tr.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, param);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parts = path.Split('/');
        var cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static void EnsureTrigger(AnimatorController ctrl, string name)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name)
                return;
        ctrl.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    static void EnsureBool(AnimatorController ctrl, string name)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name)
                return;
        ctrl.AddParameter(name, AnimatorControllerParameterType.Bool);
    }

    static void EnsureInt(AnimatorController ctrl, string name)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name)
                return;
        ctrl.AddParameter(name, AnimatorControllerParameterType.Int);
    }

    static void EnsureFloat(AnimatorController ctrl, string name)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name)
                return;
        ctrl.AddParameter(name, AnimatorControllerParameterType.Float);
    }
}

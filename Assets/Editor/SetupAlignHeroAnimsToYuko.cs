using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Alinea Run / Jump / Combo (1-2-3) de Magus, Aldric y Misaki al feel de Yuko (WGS).
/// </summary>
public static class SetupAlignHeroAnimsToYuko
{
    const string WgsRunFbx = "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_Run_Front.FBX";
    const string WgsWalkFbx = "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_Walk_Front.FBX";
    const string JumpFbx = "Assets/Haons SD series Pack/Animation/Action Adventure(Adv)/Jump_One-cycle-Type(root).FBX";
    const string ClipDir = "Assets/_ArcanoDungeon/Animators/CombatClips";

    static readonly string[] Controllers =
    {
        "Assets/Prefabs/KnightGameplay.controller",
        "Assets/Prefabs/MageGameplay.controller",
        "Assets/_ArcanoDungeon/Animators/MisakiGameplay.controller",
        "Assets/_ArcanoDungeon/Animators/YukoGameplay.controller",
    };

    [MenuItem("Tools/Arcano/Align Hero Anims To Yuko WGS", false, 131)]
    public static void Setup()
    {
        var run = FindClip(WgsRunFbx, "WGS_Run_Front@Loop");
        var walk = FindClip(WgsWalkFbx, "WGS_Walk_Front@Loop");
        var jump = FindClip(JumpFbx, "Jump_Medium root");
        if (jump == null)
            jump = FindClip(JumpFbx, "Jump_Big root");

        var c1 = EnsureSharedComboClip("Shared_WGS_Combo1.anim",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
            "WGS_attackA1");
        var c2 = EnsureSharedComboClip("Shared_WGS_Combo2.anim",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
            "WGS_attackA2");
        // Finisher más largo / legible (como el feel bueno de Yuko, reforzado).
        var c3 = EnsureSharedComboClip("Shared_WGS_Combo3.anim",
            "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
            "WGS_attackA5");

        if (run == null || jump == null || c1 == null || c2 == null || c3 == null)
        {
            Debug.LogError("[AlignHeroAnims] Faltan clips WGS Run/Jump/Combo.");
            return;
        }

        // Yuko también usa el mismo finisher A5 para consistencia total.
        foreach (var path in Controllers)
            AlignController(path, run, walk, jump, c1, c2, c3);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AlignHeroAnims] Run/Jump/Combo alineados al estilo WGS de Yuko.");
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

        return fallback;
    }

    static AnimationClip EnsureSharedComboClip(string fileName, string fbxPath, string sourceName)
    {
        string outPath = ClipDir + "/" + fileName;
        var source = FindClip(fbxPath, sourceName);
        if (source == null)
        {
            Debug.LogWarning("[AlignHeroAnims] No clip " + sourceName);
            return null;
        }

        var copy = new AnimationClip();
        EditorUtility.CopySerialized(source, copy);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(outPath);

        float len = Mathf.Max(0.25f, copy.length);
        // Hit un poco más tarde en el finisher para que se lea el swing.
        float hitT = fileName.Contains("Combo3") ? len * 0.48f : len * 0.40f;
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

    static void AlignController(
        string ctrlPath,
        AnimationClip run,
        AnimationClip walk,
        AnimationClip jump,
        AnimationClip a1,
        AnimationClip a2,
        AnimationClip a3)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            Debug.LogError("[AlignHeroAnims] Missing " + ctrlPath);
            return;
        }

        EnsureTrigger(ctrl, "Jump");
        EnsureBool(ctrl, "isRunning");
        EnsureBool(ctrl, "isMoving");

        var sm = ctrl.layers[0].stateMachine;
        var idle = FindState(sm, "Idle");
        var move = FindState(sm, "Move");
        var runState = FindState(sm, "Run") ?? sm.AddState("Run", new Vector3(200f, -80f, 0f));
        runState.motion = run;
        runState.speed = 1f;
        runState.writeDefaultValues = true;

        // Walk más cercano al WGS también (opcional pero ayuda a unificar feel).
        if (move != null && walk != null)
            move.motion = walk;

        // Combo idéntico al de Yuko/WGS.
        WireAttack(sm, idle, "Attack1", a1, 1f);
        WireAttack(sm, idle, "Attack2", a2, 1.05f);
        WireAttack(sm, idle, "Attack3", a3, 1.08f);

        // Jump state
        var jumpState = FindState(sm, "Jump") ?? sm.AddState("Jump", new Vector3(450f, -80f, 0f));
        jumpState.motion = jump;
        jumpState.speed = 1f;
        jumpState.writeDefaultValues = true;
        jumpState.tag = "Jump";

        // Limpiar Any→Jump duplicados y recrear.
        var anyList = sm.anyStateTransitions;
        for (int i = anyList.Length - 1; i >= 0; i--)
        {
            var t = anyList[i];
            if (t.destinationState == jumpState)
                sm.RemoveAnyStateTransition(t);
        }

        var anyJump = sm.AddAnyStateTransition(jumpState);
        anyJump.hasExitTime = false;
        anyJump.duration = 0.05f;
        anyJump.canTransitionToSelf = false;
        anyJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

        // Jump → Idle al terminar.
        var outs = jumpState.transitions;
        for (int i = outs.Length - 1; i >= 0; i--)
            jumpState.RemoveTransition(outs[i]);

        if (idle != null)
        {
            var back = jumpState.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.85f;
            back.duration = 0.1f;
            back.hasFixedDuration = true;
        }

        // Asegurar transición Move/Idle ↔ Run existe (por si algún controller quedó raro).
        EnsureRunLinks(sm, idle, move, runState);

        // Silenciar Attack del UpperBody.
        if (ctrl.layers.Length > 1)
        {
            var upper = ctrl.layers[1].stateMachine;
            foreach (var any in upper.anyStateTransitions)
            {
                if (any.destinationState != null && any.destinationState.name == "Attack")
                    any.mute = true;
            }

            foreach (var s in upper.states)
            {
                if (s.state.name == "Attack")
                    s.state.motion = null;
            }
        }

        EditorUtility.SetDirty(ctrl);
        Debug.Log("[AlignHeroAnims] OK " + ctrlPath);
    }

    static void WireAttack(AnimatorStateMachine sm, AnimatorState idle, string name, AnimationClip clip, float speed)
    {
        var state = FindState(sm, name) ?? sm.AddState(name);
        state.motion = clip;
        state.speed = speed;
        state.tag = "Attack";
        state.writeDefaultValues = true;

        var outs = state.transitions;
        for (int i = outs.Length - 1; i >= 0; i--)
            state.RemoveTransition(outs[i]);

        if (idle != null)
        {
            var tr = state.AddTransition(idle);
            tr.hasExitTime = true;
            tr.exitTime = 0.92f;
            tr.duration = 0.08f;
            tr.hasFixedDuration = true;
        }
    }

    static void EnsureRunLinks(AnimatorStateMachine sm, AnimatorState idle, AnimatorState move, AnimatorState run)
    {
        if (run == null)
            return;

        void Link(AnimatorState from, AnimatorState to, string param, bool greater)
        {
            if (from == null || to == null)
                return;
            foreach (var t in from.transitions)
            {
                if (t.destinationState == to)
                    return;
            }

            var tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.duration = 0.1f;
            if (greater)
                tr.AddCondition(AnimatorConditionMode.If, 0f, param);
            else
                tr.AddCondition(AnimatorConditionMode.IfNot, 0f, param);
        }

        // Idle/Move → Run cuando isRunning
        Link(idle, run, "isRunning", true);
        Link(move, run, "isRunning", true);

        // Run → Move si se mueve y no corre; Run → Idle si para.
        if (move != null)
        {
            bool has = false;
            foreach (var t in run.transitions)
                if (t.destinationState == move) has = true;
            if (!has)
            {
                var tr = run.AddTransition(move);
                tr.hasExitTime = false;
                tr.duration = 0.1f;
                tr.AddCondition(AnimatorConditionMode.IfNot, 0f, "isRunning");
                tr.AddCondition(AnimatorConditionMode.If, 0f, "isMoving");
            }
        }

        if (idle != null)
        {
            bool has = false;
            foreach (var t in run.transitions)
                if (t.destinationState == idle) has = true;
            if (!has)
            {
                var tr = run.AddTransition(idle);
                tr.hasExitTime = false;
                tr.duration = 0.1f;
                tr.AddCondition(AnimatorConditionMode.IfNot, 0f, "isRunning");
                tr.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMoving");
            }
        }
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var s in sm.states)
            if (s.state.name == name)
                return s.state;
        return null;
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
}

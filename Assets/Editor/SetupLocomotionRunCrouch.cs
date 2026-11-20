using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Añade estados Run / CrouchIdle / CrouchWalk a Knight y Mage gameplay controllers
/// usando clips Humanoid (Asuna) retargetables al avatar PolyArt.
/// </summary>
public static class SetupLocomotionRunCrouch
{
    const string AsunaRunFbx = "Assets/FreeTestCharacterAsuna/FBX/Animations/003Run.fbx";
    const string AsunaStealthFbx = "Assets/FreeTestCharacterAsuna/FBX/Animations/005Stealth.fbx";
    const string AsunaWalkFbx = "Assets/FreeTestCharacterAsuna/FBX/Animations/002Walk.fbx";

    [MenuItem("Tools/Arcano/Setup Run Crouch Locomotion")]
    public static void Setup()
    {
        var run = FindClip(AsunaRunFbx, "Run");
        var crouchIdle = FindClip(AsunaStealthFbx, "StealthIdle");
        var crouchWalk = FindClip(AsunaStealthFbx, "StealthWalk");
        var walkFallback = FindClip(AsunaWalkFbx, "Walk");

        if (run == null || crouchIdle == null || crouchWalk == null)
        {
            Debug.LogError("[SetupLocomotionRunCrouch] Faltan clips Asuna Run/Stealth.");
            return;
        }

        WireController(
            "Assets/Prefabs/KnightGameplay.controller",
            run,
            crouchIdle,
            crouchWalk,
            keepExistingMove: true);

        // Mage: Move actual es BattleRunForward → usarlo como Run; Walk con Asuna Walk.
        WireController(
            "Assets/Prefabs/MageGameplay.controller",
            runClip: null, // se reasigna Move→Run abajo
            crouchIdle,
            crouchWalk,
            keepExistingMove: true,
            promoteExistingMoveToRun: true,
            walkClip: walkFallback);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupLocomotionRunCrouch] Controllers actualizados.");
    }

    static AnimationClip FindClip(string fbxPath, string clipName)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (a is AnimationClip c && c.name == clipName && !c.name.StartsWith("__preview"))
                return c;
        }
        return null;
    }

    static void WireController(
        string ctrlPath,
        AnimationClip runClip,
        AnimationClip crouchIdle,
        AnimationClip crouchWalk,
        bool keepExistingMove,
        bool promoteExistingMoveToRun = false,
        AnimationClip walkClip = null)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            Debug.LogError("Missing " + ctrlPath);
            return;
        }

        EnsureBool(ctrl, "isRunning");
        EnsureBool(ctrl, "isCrouching");
        EnsureBool(ctrl, "isMoving");

        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = FindState(sm, "Idle");
        AnimatorState move = FindState(sm, "Move");

        if (promoteExistingMoveToRun && move != null && move.motion != null)
        {
            // Move actual (BattleRunForward) pasa a ser Run; Move usa walk humanoid.
            runClip = move.motion as AnimationClip;
            if (walkClip != null)
                move.motion = walkClip;
        }

        AnimatorState run = FindState(sm, "Run") ?? sm.AddState("Run");
        run.motion = runClip != null ? runClip : move?.motion;
        run.writeDefaultValues = true;
        run.speed = 1f;

        AnimatorState crouchIdleState = FindState(sm, "CrouchIdle") ?? sm.AddState("CrouchIdle");
        crouchIdleState.motion = crouchIdle;
        crouchIdleState.writeDefaultValues = true;

        AnimatorState crouchWalkState = FindState(sm, "CrouchWalk") ?? sm.AddState("CrouchWalk");
        crouchWalkState.motion = crouchWalk;
        crouchWalkState.writeDefaultValues = true;

        // Limpiar transitions de locomoción entre estos estados y recrear.
        RebuildLocomotionTransitions(sm, idle, move, run, crouchIdleState, crouchWalkState);

        EditorUtility.SetDirty(ctrl);
        Debug.Log("[SetupLocomotionRunCrouch] Wired " + ctrlPath);
    }

    static void RebuildLocomotionTransitions(
        AnimatorStateMachine sm,
        AnimatorState idle,
        AnimatorState move,
        AnimatorState run,
        AnimatorState crouchIdle,
        AnimatorState crouchWalk)
    {
        // Quitar transitions entre pares de locomoción (no tocar Attack*).
        ClearTransitionsBetween(idle, move, run, crouchIdle, crouchWalk);

        // Idle <-> Move
        if (idle != null && move != null)
        {
            AddBoolTrans(idle, move, "isMoving", true, "isRunning", false, "isCrouching", false);
            AddBoolTrans(move, idle, "isMoving", false);
            AddBoolTrans(move, run, "isRunning", true, "isCrouching", false);
        }

        // Idle <-> Run
        if (idle != null && run != null)
        {
            AddBoolTrans(idle, run, "isMoving", true, "isRunning", true, "isCrouching", false);
            AddBoolTrans(run, idle, "isMoving", false);
            if (move != null)
                AddBoolTrans(run, move, "isRunning", false, "isMoving", true, "isCrouching", false);
        }

        // Crouch
        if (idle != null && crouchIdle != null)
        {
            AddBoolTrans(idle, crouchIdle, "isCrouching", true, "isMoving", false);
            AddBoolTrans(move, crouchIdle, "isCrouching", true, "isMoving", false);
            AddBoolTrans(run, crouchIdle, "isCrouching", true, "isMoving", false);
            AddBoolTrans(crouchIdle, idle, "isCrouching", false, "isMoving", false);
            AddBoolTrans(crouchIdle, move, "isCrouching", false, "isMoving", true, "isRunning", false);
            AddBoolTrans(crouchIdle, run, "isCrouching", false, "isMoving", true, "isRunning", true);
        }

        if (crouchIdle != null && crouchWalk != null)
        {
            AddBoolTrans(crouchIdle, crouchWalk, "isMoving", true, "isCrouching", true);
            AddBoolTrans(crouchWalk, crouchIdle, "isMoving", false, "isCrouching", true);
            AddBoolTrans(move, crouchWalk, "isCrouching", true, "isMoving", true);
            AddBoolTrans(run, crouchWalk, "isCrouching", true, "isMoving", true);
            AddBoolTrans(idle, crouchWalk, "isCrouching", true, "isMoving", true);
            AddBoolTrans(crouchWalk, idle, "isCrouching", false, "isMoving", false);
            AddBoolTrans(crouchWalk, move, "isCrouching", false, "isMoving", true, "isRunning", false);
            AddBoolTrans(crouchWalk, run, "isCrouching", false, "isMoving", true, "isRunning", true);
        }
    }

    static void ClearTransitionsBetween(params AnimatorState[] states)
    {
        for (int i = 0; i < states.Length; i++)
        {
            var from = states[i];
            if (from == null) continue;
            var transitions = from.transitions;
            for (int t = transitions.Length - 1; t >= 0; t--)
            {
                var dest = transitions[t].destinationState;
                bool locoDest = false;
                for (int j = 0; j < states.Length; j++)
                    if (states[j] != null && dest == states[j])
                        locoDest = true;
                if (locoDest)
                    from.RemoveTransition(transitions[t]);
            }
        }
    }

    static void AddBoolTrans(
        AnimatorState from,
        AnimatorState to,
        string b1, bool v1,
        string b2 = null, bool v2 = false,
        string b3 = null, bool v3 = false)
    {
        if (from == null || to == null) return;
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = 0.12f;
        t.AddCondition(v1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, b1);
        if (!string.IsNullOrEmpty(b2))
            t.AddCondition(v2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, b2);
        if (!string.IsNullOrEmpty(b3))
            t.AddCondition(v3 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, b3);
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var s in sm.states)
            if (s.state.name == name)
                return s.state;
        return null;
    }

    static void EnsureBool(AnimatorController ctrl, string name)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name)
                return;
        ctrl.AddParameter(name, AnimatorControllerParameterType.Bool);
    }
}

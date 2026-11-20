using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AssignKnightCombatClips
{
    public static string Main()
    {
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/_ArcanoDungeon/Animators/KayKitKnightGameplay.controller");
        if (ac == null)
            return "no controller";

        AnimationClip FindClip(string path, string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o is AnimationClip c && c.name == name)
                    return c;
            }
            return null;
        }

        const string meleePath =
            "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatMelee.fbx";

        // Espadazo claro + escudo firme (equivalente a Attack01 / Defend de Cesar).
        var attackClip = FindClip(meleePath, "Melee_1H_Attack_Slice_Horizontal");
        var blockClip = FindClip(meleePath, "Melee_Block");
        if (attackClip == null) attackClip = FindClip(meleePath, "Melee_1H_Attack_Chop");
        if (blockClip == null) blockClip = FindClip(meleePath, "Melee_Blocking");

        int changed = 0;
        foreach (var layer in ac.layers)
        {
            foreach (var child in layer.stateMachine.states)
            {
                if (child.state.name == "Attack" && attackClip != null)
                {
                    child.state.motion = attackClip;
                    changed++;
                }
                if (child.state.name == "Block" && blockClip != null)
                {
                    child.state.motion = blockClip;
                    changed++;
                }
            }
        }

        // Asegurar AnyState -> Block
        bool hasBlockAny = false;
        foreach (var t in ac.layers[0].stateMachine.anyStateTransitions)
        {
            foreach (var c in t.conditions)
            {
                if (c.parameter == "isBlocking" && c.mode == AnimatorConditionMode.If)
                    hasBlockAny = true;
            }
        }

        if (!hasBlockAny)
        {
            AnimatorState blockState = null;
            foreach (var child in ac.layers[0].stateMachine.states)
            {
                if (child.state.name == "Block")
                    blockState = child.state;
            }
            if (blockState != null)
            {
                var t = ac.layers[0].stateMachine.AddAnyStateTransition(blockState);
                t.AddCondition(AnimatorConditionMode.If, 0, "isBlocking");
                t.hasExitTime = false;
                t.duration = 0.08f;
                t.canTransitionToSelf = false;
                changed++;
            }
        }

        // Layer weight
        var layers = ac.layers;
        layers[0].defaultWeight = 1f;
        ac.layers = layers;

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();

        return string.Format(
            "OK attack={0} block={1} changed={2} hasBlockAny={3}",
            attackClip != null ? attackClip.name : "null",
            blockClip != null ? blockClip.name : "null",
            changed,
            hasBlockAny);
    }
}

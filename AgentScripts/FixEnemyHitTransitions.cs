using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class FixEnemyHitTransitions
{
    public static string Main()
    {
        string ctrlPath = "Assets/_ArcanoDungeon/Animators/KayKitEnemyGameplay.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null) return "ERROR missing controller";

        var sm = ctrl.layers[0].stateMachine;
        var death = sm.states.First(s => s.state.name == "Death").state;
        var hit = sm.states.First(s => s.state.name == "Hit").state;
        var idle = sm.states.First(s => s.state.name == "Idle").state;
        var walk = sm.states.First(s => s.state.name == "Walk").state;
        var attack = sm.states.First(s => s.state.name == "Attack").state;

        // Remove AnyState -> Hit so Death can't be interrupted.
        foreach (var t in sm.anyStateTransitions.ToArray())
        {
            if (t.destinationState == hit)
                sm.RemoveAnyStateTransition(t);
        }

        // Keep only Die on AnyState.
        bool hasDie = sm.anyStateTransitions.Any(t => t.destinationState == death);
        if (!hasDie)
        {
            var toDeath = sm.AddAnyStateTransition(death);
            toDeath.hasExitTime = false;
            toDeath.duration = 0.05f;
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        }

        void EnsureHitFrom(AnimatorState from)
        {
            foreach (var t in from.transitions.ToArray())
            {
                if (t.destinationState == hit)
                    from.RemoveTransition(t);
            }
            var tr = from.AddTransition(hit);
            tr.hasExitTime = false;
            tr.duration = 0.05f;
            tr.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        }

        EnsureHitFrom(idle);
        EnsureHitFrom(walk);
        EnsureHitFrom(attack);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return "OK: Hit only from Idle/Walk/Attack; Die remains AnyState";
    }
}

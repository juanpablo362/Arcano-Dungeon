using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class WireEnemyDeath
{
    public static string Main()
    {
        string generalPath = "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
        string specialPath = "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_Special.fbx";
        string ctrlPath = "Assets/_ArcanoDungeon/Animators/KayKitEnemyGameplay.controller";

        AnimationClip deathClip = null;
        AnimationClip hitClip = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(specialPath))
        {
            if (a is AnimationClip c && c.name == "Skeletons_Death")
                deathClip = c;
        }
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(generalPath))
        {
            if (a is AnimationClip c && c.name == "Hit_A")
                hitClip = c;
        }

        if (deathClip == null) return "ERROR: Skeletons_Death not found";
        if (hitClip == null) return "ERROR: Hit_A not found";

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null) return "ERROR: Missing controller";

        if (!ctrl.parameters.Any(p => p.name == "Die"))
            ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        if (!ctrl.parameters.Any(p => p.name == "Hit"))
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        foreach (var s in sm.states.ToArray())
        {
            if (s.state.name == "Death" || s.state.name == "Hit")
                sm.RemoveState(s.state);
        }

        foreach (var t in sm.anyStateTransitions.ToArray())
        {
            if (t.destinationState != null && (t.destinationState.name == "Death" || t.destinationState.name == "Hit"))
                sm.RemoveAnyStateTransition(t);
        }

        var deathState = sm.AddState("Death", new Vector3(560, 100, 0));
        deathState.motion = deathClip;
        deathState.tag = "Death";

        var hitState = sm.AddState("Hit", new Vector3(560, 180, 0));
        hitState.motion = hitClip;

        var toDeath = sm.AddAnyStateTransition(deathState);
        toDeath.hasExitTime = false;
        toDeath.duration = 0.05f;
        toDeath.canTransitionToSelf = false;
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");

        var toHit = sm.AddAnyStateTransition(hitState);
        toHit.hasExitTime = false;
        toHit.duration = 0.05f;
        toHit.canTransitionToSelf = false;
        toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");

        var idle = sm.states.First(s => s.state.name == "Idle").state;
        var hitToIdle = hitState.AddTransition(idle);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.85f;
        hitToIdle.duration = 0.1f;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        string prefabPath = "Assets/_ArcanoDungeon/Prefabs/EnemySkeletonMinion.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var eh = root.GetComponent<EnemyHealth>();
        string hpMsg = "no EnemyHealth";
        if (eh != null)
        {
            var so = new SerializedObject(eh);
            so.FindProperty("maxHealth").intValue = 140;
            var cur = so.FindProperty("currentHealth");
            if (cur != null) cur.intValue = 140;
            var delay = so.FindProperty("deathDestroyDelay");
            if (delay != null) delay.floatValue = 2.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            hpMsg = "HP=140";
        }
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        string states = string.Join(", ", sm.states.Select(s => s.state.name + "=" + (s.state.motion != null ? s.state.motion.name : "null")));
        string parms = string.Join(", ", ctrl.parameters.Select(p => p.name));
        return "OK death=" + deathClip.name + " hit=" + hitClip.name + " " + hpMsg + " | params=[" + parms + "] states=[" + states + "]";
    }
}

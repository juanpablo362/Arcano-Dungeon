using UnityEditor;
using UnityEngine;

public static class WireKnightCombatFeel
{
    public static string Main()
    {
        // Verificar clips de combate KayKit y peso del layer.
        var ctrlPath = "Assets/_ArcanoDungeon/Animators/KayKitKnightGameplay.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
        var ac = ctrl as UnityEditor.Animations.AnimatorController;
        if (ac == null)
            return "no KayKitKnightGameplay";

        if (ac.layers.Length > 0 && Mathf.Abs(ac.layers[0].defaultWeight - 1f) > 0.01f)
        {
            var layers = ac.layers;
            layers[0].defaultWeight = 1f;
            ac.layers = layers;
            EditorUtility.SetDirty(ac);
        }

        // Asegurar clips Attack/Block existen en CombatMelee
        var melee = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatMelee.fbx");
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("layer0Weight=").Append(ac.layers[0].defaultWeight).Append(" clips=");
        int n = 0;
        foreach (var o in melee)
        {
            if (o is AnimationClip clip && !clip.name.StartsWith("__preview"))
            {
                sb.Append(clip.name).Append(",");
                n++;
            }
        }
        sb.Append(" count=").Append(n);

        // Elara usa KayKit controller (guid 8b5f6e53...)
        var elara = PrefabUtility.LoadPrefabContents("Assets/_ArcanoDungeon/Prefabs/ElaraPlayer.prefab");
        var anim = elara.GetComponentInChildren<Animator>();
        string animInfo = "null";
        if (anim != null)
        {
            anim.applyRootMotion = false;
            if (anim.runtimeAnimatorController == null
                || anim.runtimeAnimatorController.name != "KayKitKnightGameplay")
            {
                anim.runtimeAnimatorController = ac;
            }
            animInfo = anim.runtimeAnimatorController != null
                ? anim.runtimeAnimatorController.name
                : "none";
            EditorUtility.SetDirty(anim);
        }
        PrefabUtility.SaveAsPrefabAsset(elara, "Assets/_ArcanoDungeon/Prefabs/ElaraPlayer.prefab");
        PrefabUtility.UnloadPrefabContents(elara);

        AssetDatabase.SaveAssets();
        return sb.Append(" elaraCtrl=").Append(animInfo).ToString();
    }
}

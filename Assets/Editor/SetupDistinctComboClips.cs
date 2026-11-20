using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Asigna clips distintos a Attack1/2/3 (Humanoid retarget) e inyecta Animation Events.
/// Silencia el Attack del layer UpperBody que forzaba siempre la misma animación.
/// </summary>
public static class SetupDistinctComboClips
{
    const string OutDir = "Assets/_ArcanoDungeon/Animators/CombatClips";

    [MenuItem("Tools/Arcano/Setup Distinct Combo Attack Clips")]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder(OutDir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Animators"))
                AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Animators");
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon/Animators", "CombatClips");
        }

        // Knight: 3 ataques 1H distintas
        var k1 = BuildEventClip(
            "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx",
            "AttackRightHand",
            OutDir + "/Knight_Combo1_Right.anim");
        var k2 = BuildEventClip(
            "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx",
            "AttackLeftHand",
            OutDir + "/Knight_Combo2_Left.anim");
        var k3 = BuildEventClip(
            "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx",
            "AttackBothHands",
            OutDir + "/Knight_Combo3_Both.anim");

        // Mage: mismos swings 1H (bastón en mano), distintos de puñetazos unarmed
        var m1 = BuildEventClip(
            "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx",
            "AttackRightHand",
            OutDir + "/Mage_Combo1_StaffR.anim");
        var m2 = BuildEventClip(
            "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx",
            "AttackLeftHand",
            OutDir + "/Mage_Combo2_StaffL.anim");
        var m3 = BuildEventClip(
            "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx",
            "AttackBothHands",
            OutDir + "/Mage_Combo3_StaffBoth.anim");

        // Fallback Haons si Asuna falla
        if (k1 == null || k2 == null || k3 == null)
        {
            k1 = BuildEventClip(
                "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
                "WGS_attackA1", OutDir + "/Knight_Combo1_A1.anim");
            k2 = BuildEventClip(
                "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
                "WGS_attackA2", OutDir + "/Knight_Combo2_A2.anim");
            k3 = BuildEventClip(
                "Assets/Haons SD series Pack/Animation/WeaponMaster Greatsword(WGS)/WGS_AttackA_set.FBX",
                "WGS_attackA3", OutDir + "/Knight_Combo3_A3.anim");
        }

        WireAttacks("Assets/Prefabs/KnightGameplay.controller", k1, k2, k3, new[] { 1f, 1.05f, 1.1f });
        WireAttacks("Assets/Prefabs/MageGameplay.controller", m1, m2, m3, new[] { 1f, 1.05f, 1.1f });

        MuteUpperBodyAttack("Assets/Prefabs/KnightGameplay.controller");
        MuteUpperBodyAttack("Assets/Prefabs/MageGameplay.controller");

        // Prefabs: combo por timing (no exigir hit para ver Attack2/3)
        SetRequireHit("Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPolyart.prefab", false);
        SetRequireHit("Assets/WizardPolyArt/Prefabs/PolyArtWizardStandardMat.prefab", false);
        AttachMageStaffToRightHand("Assets/WizardPolyArt/Prefabs/PolyArtWizardStandardMat.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupDistinctComboClips] Attack1/2/3 con clips distintos listos.");
    }

    /// <summary>
    /// El bastón PolyArt vive en pelvis/Weapon; con anims Humanoid Asuna no sigue la mano.
    /// Se reparenta a hand_r/weapon_r compensando la escala ~0.01 del esqueleto.
    /// </summary>
    static void AttachMageStaffToRightHand(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var anim = root.GetComponentInChildren<Animator>(true);
            if (anim == null || !anim.isHuman)
            {
                Debug.LogWarning("[SetupDistinctComboClips] Mage sin Animator Humanoid.");
                return;
            }

            var hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null)
            {
                Debug.LogWarning("[SetupDistinctComboClips] Mage sin RightHand.");
                return;
            }

            Transform staff = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Staff01PolyArt" || t.name == "Staff02Polyart" || t.name == "Staff03PolyArt")
                {
                    if (t.gameObject.activeSelf) { staff = t; break; }
                    if (staff == null) staff = t;
                }
            }

            if (staff == null)
            {
                Debug.LogWarning("[SetupDistinctComboClips] No hay Staff* en el mago.");
                return;
            }

            var weaponR = hand.Find("weapon_r");
            if (weaponR == null)
            {
                var go = new GameObject("weapon_r");
                weaponR = go.transform;
                weaponR.SetParent(hand, false);
                // Misma convención que MaleCharacterPolyart (espada en mano derecha).
                weaponR.localPosition = new Vector3(0.09f, -0.01f, 0f);
                weaponR.localRotation = Quaternion.Euler(90f, 0f, 0f);
                weaponR.localScale = Vector3.one;
            }

            staff.SetParent(weaponR, false);
            float parentLossy = (Mathf.Abs(weaponR.lossyScale.x) + Mathf.Abs(weaponR.lossyScale.y) + Mathf.Abs(weaponR.lossyScale.z)) / 3f;
            float scale = 1f / Mathf.Max(0.0001f, parentLossy);
            staff.localScale = Vector3.one * scale;
            // Eje largo del bastón = +Y. Con weapon_r a 90°X, -90° local deja la punta arriba.
            staff.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            staff.localPosition = Vector3.zero;
            // Acercar el mango (WeaponBase) al origen de la mano.
            var weaponBase = staff.Find("WeaponBase");
            if (weaponBase != null)
            {
                Vector3 baseInSocket = weaponR.InverseTransformPoint(weaponBase.position);
                staff.localPosition -= baseInSocket;
            }

            staff.gameObject.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[SetupDistinctComboClips] Staff anclado a hand_r/weapon_r scale=" + scale.ToString("F1"));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static AnimationClip BuildEventClip(string fbxPath, string sourceName, string outPath)
    {
        AnimationClip source = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (a is AnimationClip c && c.name == sourceName && !c.name.StartsWith("__preview"))
            {
                source = c;
                break;
            }
        }

        if (source == null)
        {
            Debug.LogWarning("[SetupDistinctComboClips] No clip " + sourceName + " in " + fbxPath);
            return null;
        }

        var copy = new AnimationClip();
        EditorUtility.CopySerialized(source, copy);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(outPath);

        float len = Mathf.Max(0.2f, copy.length);
        AnimationUtility.SetAnimationEvents(copy, new[]
        {
            new AnimationEvent { functionName = "OnAttackHitFrame", time = len * 0.40f },
            new AnimationEvent { functionName = "OnAttackEndFrame", time = Mathf.Min(len * 0.90f, len - 0.02f) }
        });

        var settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(copy, settings);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null)
            AssetDatabase.DeleteAsset(outPath);
        AssetDatabase.CreateAsset(copy, outPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
    }

    static void WireAttacks(string ctrlPath, AnimationClip a1, AnimationClip a2, AnimationClip a3, float[] speeds)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null || a1 == null || a2 == null || a3 == null)
        {
            Debug.LogError("[SetupDistinctComboClips] Wire fail " + ctrlPath);
            return;
        }

        AnimationClip[] clips = { a1, a2, a3 };
        string[] names = { "Attack1", "Attack2", "Attack3" };
        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null;
        foreach (var s in sm.states)
            if (s.state.name == "Idle") idle = s.state;

        for (int i = 0; i < 3; i++)
        {
            AnimatorState state = null;
            foreach (var s in sm.states)
                if (s.state.name == names[i]) { state = s.state; break; }
            if (state == null) state = sm.AddState(names[i]);

            state.motion = clips[i];
            state.speed = speeds[i];
            state.tag = "Attack";
            state.writeDefaultValues = true;

            var outs = state.transitions;
            for (int t = outs.Length - 1; t >= 0; t--)
                state.RemoveTransition(outs[t]);

            if (idle != null)
            {
                var tr = state.AddTransition(idle);
                tr.hasExitTime = true;
                tr.exitTime = 0.92f;
                tr.duration = 0.08f;
                tr.hasFixedDuration = true;
            }
        }

        EditorUtility.SetDirty(ctrl);
        Debug.Log("[SetupDistinctComboClips] Wired " + ctrlPath +
                  " -> " + a1.name + " / " + a2.name + " / " + a3.name);
    }

    static void MuteUpperBodyAttack(string ctrlPath)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null || ctrl.layers.Length < 2)
            return;

        var sm = ctrl.layers[1].stateMachine;
        foreach (var any in sm.anyStateTransitions)
        {
            if (any.destinationState != null && any.destinationState.name == "Attack")
            {
                any.mute = true;
                Debug.Log("[SetupDistinctComboClips] Muted UpperBody Any->Attack in " + ctrlPath);
            }
        }

        // Vaciar motion del Attack de UpperBody para que no tape el combo de base.
        foreach (var s in sm.states)
        {
            if (s.state.name == "Attack")
                s.state.motion = null;
        }

        EditorUtility.SetDirty(ctrl);
    }

    static void SetRequireHit(string prefabPath, bool require)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var melee = root.GetComponent("MeleeWeaponController");
            if (melee == null)
                melee = root.GetComponent("ArcanoDungeon.Features.Combat.MeleeWeaponController");
            if (melee == null)
            {
                foreach (var mb in root.GetComponents<MonoBehaviour>())
                    if (mb != null && mb.GetType().Name == "MeleeWeaponController")
                    { melee = mb; break; }
            }
            if (melee == null) return;

            var so = new SerializedObject(melee);
            var prop = so.FindProperty("requireHitToContinueCombo");
            if (prop != null)
            {
                prop.boolValue = require;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var buf = so.FindProperty("inputBufferWindow");
            if (buf != null) buf.floatValue = 0.35f;
            var reset = so.FindProperty("comboResetTime");
            if (reset != null) reset.floatValue = 0.7f;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}

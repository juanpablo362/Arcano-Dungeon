using System.Linq;
using System.Text;
using ArcanoDungeon.Features.Player.Motor;
using ArcanoDungeon.Features.Player.View;
using ArcanoDungeon.Infrastructure.Input;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ValidatePhase1
{
    public static string Main()
    {
        var sb = new StringBuilder();

        bool cm = AssetDatabase.FindAssets("t:PackageInfo").Length >= 0;
        var cmPkg = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine");
        sb.AppendLine("Cinemachine=" + (cmPkg != null ? cmPkg.version : "MISSING"));

        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/_ArcanoDungeon/Settings/Input/DungeonKnightPlayer.inputactions");
        sb.AppendLine("InputAsset=" + (input != null));
        if (input != null)
        {
            var map = input.FindActionMap("Player");
            string[] need = { "Move", "Look", "Jump", "Dash", "Attack", "Secondary", "Pause" };
            foreach (var n in need)
                sb.AppendLine("  action " + n + "=" + (map != null && map.FindAction(n) != null));
        }

        var camPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_ArcanoDungeon/Prefabs/PlayerCameraRig.prefab");
        sb.AppendLine("CameraRigPrefab=" + (camPrefab != null));
        if (camPrefab != null)
        {
            sb.AppendLine("  ThirdPersonCameraRig=" + (camPrefab.GetComponent<ThirdPersonCameraRig>() != null));
            sb.AppendLine("  CinemachineCamera=" + (camPrefab.GetComponentInChildren<CinemachineCamera>(true) != null));
            sb.AppendLine("  ThirdPersonFollow=" + (camPrefab.GetComponentInChildren<CinemachineThirdPersonFollow>(true) != null));
        }

        EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
        var sceneRig = Object.FindFirstObjectByType<ThirdPersonCameraRig>();
        sb.AppendLine("Level1.ThirdPersonCameraRig=" + (sceneRig != null));
        sb.AppendLine("Level1.CinemachineBrain=" + (Object.FindFirstObjectByType<CinemachineBrain>() != null));

        string[] paths =
        {
            "Assets/_ArcanoDungeon/Prefabs/ElaraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/LyraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/KaedePlayer.prefab",
        };
        foreach (var p in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(p);
            sb.AppendLine(System.IO.Path.GetFileNameWithoutExtension(p)
                + " reader=" + (root.GetComponent<PlayerInputReader>() != null)
                + " motor=" + (root.GetComponent<PlayerMotor>() != null)
                + " aim=" + (root.GetComponent<AimRaycaster>() != null)
                + " bootstrap=" + (root.GetComponent<PlayerCameraBootstrap>() != null)
                + " camTarget=" + (root.transform.Find("CameraTarget") != null)
                + " layer=" + LayerMask.LayerToName(root.layer));
            var reader = root.GetComponent<PlayerInputReader>();
            if (reader != null)
            {
                var so = new SerializedObject(reader);
                sb.AppendLine("  actionsAssigned=" + (so.FindProperty("actions").objectReferenceValue != null));
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        sb.AppendLine("PlayerLayer=" + LayerMask.NameToLayer("Player"));
        sb.AppendLine("EnemyLayer=" + LayerMask.NameToLayer("Enemy"));
        sb.AppendLine("EnvironmentLayer=" + LayerMask.NameToLayer("Environment"));
        sb.AppendLine("ProjectileLayer=" + LayerMask.NameToLayer("Projectile"));

        return sb.ToString();
    }
}

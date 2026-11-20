using System.Collections.Generic;
using System.Linq;
using ArcanoDungeon.Features.Player.View;
using ArcanoDungeon.Features.Player.Motor;
using ArcanoDungeon.Infrastructure.Input;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class SetupPhase1PlayerCamera
{
    const string Level1Path = "Assets/_ArcanoDungeon/Scenes/Level1.unity";
    const string InputPath = "Assets/_ArcanoDungeon/Settings/Input/DungeonKnightPlayer.inputactions";
    const string CameraPrefabPath = "Assets/_ArcanoDungeon/Prefabs/PlayerCameraRig.prefab";

    public static string Main()
    {
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        if (inputAsset == null)
            return "ERROR: missing InputActionAsset at " + InputPath;

        // Ensure layers exist (already patched TagManager, refresh)
        EnsurePlayerLayers();

        string prefabMsg = CreateOrUpdateCameraRigPrefab();
        string sceneMsg = SetupLevel1Scene();
        string playersMsg = WirePlayerPrefabs(inputAsset);

        AssetDatabase.SaveAssets();
        return $"OK | {prefabMsg} | {sceneMsg} | {playersMsg}";
    }

    static void EnsurePlayerLayers()
    {
        // TagManager already edited; nothing else required here.
    }

    static string CreateOrUpdateCameraRigPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
        GameObject root;
        if (existing != null)
        {
            root = PrefabUtility.LoadPrefabContents(CameraPrefabPath);
        }
        else
        {
            root = new GameObject("PlayerCameraRig");
        }

        // Main camera + brain
        var camGo = root.transform.Find("Main Camera");
        Camera cam;
        if (camGo == null)
        {
            var go = new GameObject("Main Camera");
            go.transform.SetParent(root.transform, false);
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            go.AddComponent<CinemachineBrain>();
            camGo = go.transform;
        }
        else
        {
            cam = camGo.GetComponent<Camera>();
            if (camGo.GetComponent<CinemachineBrain>() == null)
                camGo.gameObject.AddComponent<CinemachineBrain>();
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.gameObject.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
        }

        // Disable legacy CameraFollow if present
        var legacy = camGo.GetComponent<CameraFollow>();
        if (legacy != null)
            Object.DestroyImmediate(legacy);

        // Cinemachine camera
        var vcamT = root.transform.Find("CM ThirdPerson");
        CinemachineCamera vcam;
        if (vcamT == null)
        {
            var go = new GameObject("CM ThirdPerson");
            go.transform.SetParent(root.transform, false);
            vcam = go.AddComponent<CinemachineCamera>();
            go.AddComponent<CinemachineThirdPersonFollow>();
            go.AddComponent<CinemachineRotationComposer>();
            vcamT = go.transform;
        }
        else
        {
            vcam = vcamT.GetComponent<CinemachineCamera>();
            if (vcam == null) vcam = vcamT.gameObject.AddComponent<CinemachineCamera>();
            if (vcamT.GetComponent<CinemachineThirdPersonFollow>() == null)
                vcamT.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            if (vcamT.GetComponent<CinemachineRotationComposer>() == null)
                vcamT.gameObject.AddComponent<CinemachineRotationComposer>();
        }

        var follow = vcamT.GetComponent<CinemachineThirdPersonFollow>();
        follow.ShoulderOffset = new Vector3(0.6f, 1.5f, 0f);
        follow.CameraDistance = 3.5f;
        follow.VerticalArmLength = 0.4f;
        follow.CameraSide = 1f;
        follow.Damping = new Vector3(0.1f, 0.2f, 0.3f);
        var obs = follow.AvoidObstacles;
        obs.Enabled = true;
        obs.IgnoreTag = "Player";
        obs.CameraRadius = 0.25f;
        follow.AvoidObstacles = obs;

        var rig = root.GetComponent<ThirdPersonCameraRig>();
        if (rig == null) rig = root.AddComponent<ThirdPersonCameraRig>();

        var so = new SerializedObject(rig);
        so.FindProperty("virtualCamera").objectReferenceValue = vcam;
        so.FindProperty("thirdPersonFollow").objectReferenceValue = follow;
        so.FindProperty("shoulderOffset").vector3Value = new Vector3(0.6f, 1.5f, 0f);
        so.FindProperty("cameraDistance").floatValue = 3.5f;
        so.FindProperty("minPitch").floatValue = -35f;
        so.FindProperty("maxPitch").floatValue = 60f;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/_ArcanoDungeon/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_ArcanoDungeon", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(root, CameraPrefabPath);
        if (existing != null)
            PrefabUtility.UnloadPrefabContents(root);
        else
            Object.DestroyImmediate(root);

        return "CameraRigPrefab saved";
    }

    static string SetupLevel1Scene()
    {
        var scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);

        // Remove old Main Camera CameraFollow-driven cameras (keep one brain)
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (var c in cameras)
        {
            if (c.CompareTag("MainCamera") || c.gameObject.name.Contains("Main Camera"))
            {
                var follow = c.GetComponent<CameraFollow>();
                if (follow != null) Object.DestroyImmediate(follow);
            }
        }

        GameObject rigInstance = GameObject.Find("PlayerCameraRig");
        if (rigInstance == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rigInstance.name = "PlayerCameraRig";
        }

        // Ensure only one AudioListener / MainCamera
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        bool kept = false;
        foreach (var l in listeners)
        {
            if (!kept && l.transform.root == rigInstance.transform)
            {
                kept = true;
                continue;
            }
            if (l.transform.root == rigInstance.transform) continue;
            // Disable extras outside rig
            if (l.gameObject != rigInstance && l.transform.root.name != "PlayerCameraRig")
                l.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Level1 camera rig ready";
    }

    static string WirePlayerPrefabs(InputActionAsset inputAsset)
    {
        string[] paths =
        {
            "Assets/_ArcanoDungeon/Prefabs/ElaraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/LyraPlayer.prefab",
            "Assets/_ArcanoDungeon/Prefabs/KaedePlayer.prefab",
        };

        int count = 0;
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var reader = root.GetComponent<PlayerInputReader>();
                if (reader == null) reader = root.AddComponent<PlayerInputReader>();
                var soReader = new SerializedObject(reader);
                soReader.FindProperty("actions").objectReferenceValue = inputAsset;
                soReader.ApplyModifiedPropertiesWithoutUndo();

                var motor = root.GetComponent<PlayerMotor>();
                if (motor == null) motor = root.AddComponent<PlayerMotor>();

                var aim = root.GetComponent<AimRaycaster>();
                if (aim == null) aim = root.AddComponent<AimRaycaster>();

                var bootstrap = root.GetComponent<PlayerCameraBootstrap>();
                if (bootstrap == null) bootstrap = root.AddComponent<PlayerCameraBootstrap>();

                // CameraTarget child
                var camTarget = root.transform.Find("CameraTarget");
                if (camTarget == null)
                {
                    var go = new GameObject("CameraTarget");
                    camTarget = go.transform;
                    camTarget.SetParent(root.transform, false);
                    camTarget.localPosition = new Vector3(0f, 1.4f, 0f);
                }

                // Wire PlayerController refs
                var pc = root.GetComponent<PlayerController>();
                if (pc != null)
                {
                    var soPc = new SerializedObject(pc);
                    soPc.FindProperty("inputReader").objectReferenceValue = reader;
                    soPc.FindProperty("playerMotor").objectReferenceValue = motor;
                    soPc.FindProperty("aimRaycaster").objectReferenceValue = aim;
                    soPc.ApplyModifiedPropertiesWithoutUndo();
                }

                var soMotor = new SerializedObject(motor);
                soMotor.FindProperty("input").objectReferenceValue = reader;
                soMotor.FindProperty("aimRaycaster").objectReferenceValue = aim;
                soMotor.FindProperty("character").objectReferenceValue = pc;
                soMotor.ApplyModifiedPropertiesWithoutUndo();

                // Layer Player
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                    SetLayerRecursive(root.transform, playerLayer);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return $"Wired {count} player prefabs";
    }

    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t)
            SetLayerRecursive(c, layer);
    }
}

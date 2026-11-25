using System;
using System.IO;
using System.Text;
using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Features.Player;
using ArcanoDungeon.Features.Player.Motor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcanoDungeon.Features.Assassin.Editor
{
    [InitializeOnLoad]
    public static class AssassinPlayValidation
    {
        const string Running = "Arcano.Assassin.Validation";
        const string SelectionOnly = Running + ".SelectionOnly";
        static int phase, combo;
        static double deadline, started;
        static GameObject player;
        static Animator animator;
        static MeleeWeaponController melee;
        static EnemyHealth target;
        static Vector3 origin;
        static float walkingDistance, beforeStamina;
        static int beforeHealth;
        static Key[] pendingKeys;
        static readonly StringBuilder report = new StringBuilder();

        static AssassinPlayValidation()
        {
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Running, false))
                {
                    SessionState.SetBool(Running, false);
                    if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(Running + ".Exit", 1));
                }
            };
        }

        [MenuItem("Tools/Arcano/Assassin/Validate gameplay in Level1")]
        public static void Run()
        {
            Directory.CreateDirectory(AssassinSetup.ReportDir);
            SessionState.SetBool(Running, true);
            SessionState.SetBool(SelectionOnly, false);
            SessionState.SetInt(Running + ".Exit", 1);
            EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/Level1.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Arcano/Assassin/Validate selection preview")]
        public static void RunSelection()
        {
            SessionState.SetBool(Running, true);
            SessionState.SetBool(SelectionOnly, true);
            SessionState.SetInt(Running + ".Exit", 1);
            EditorSceneManager.OpenScene("Assets/_ArcanoDungeon/Scenes/CharacterSelection.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnEnterPlayMode]
        static void SelectAssassin(EnterPlayModeOptions options)
        {
            if (SessionState.GetBool(Running, false)) GameSession.SelectedHero = HeroId.Assassin;
        }

        static void Require(bool condition, string detail)
        {
            if (!condition) throw new Exception(detail);
            report.AppendLine("PASS " + detail);
        }

        static void Keys(params Key[] keys)
        {
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>("AssassinValidationKeyboard");
            pendingKeys = keys;
        }

        static void FlushPendingKeys()
        {
            if (pendingKeys == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(pendingKeys));
            pendingKeys = null;
        }

        static void Wait(int next, double seconds)
        {
            phase = next; deadline = Time.timeAsDouble + seconds;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            try
            {
                var now = EditorApplication.timeSinceStartup;
                if (now - started > 100) throw new Exception("Gameplay validation timed out.");
                if (Time.timeAsDouble < deadline) return;
                switch (phase)
                {
                    case 0:
                        if (SessionState.GetBool(SelectionOnly, false)) { Wait(9, 2); break; }
                        player = GameObject.FindGameObjectWithTag("Player");
                        if (player == null) return;
                        Require(player.name.StartsWith("AssassinPlayer"), "Level1 PlayerSpawner resolves the assassin prefab");
                        animator = player.GetComponentInChildren<Animator>();
                        melee = player.GetComponent<MeleeWeaponController>();
                        Require(animator.isHuman && animator.avatar.isValid, "Valid humanoid animator at runtime");
                        InputSystem.onBeforeUpdate += FlushPendingKeys;
                        foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyAI>()) enemy.enabled = false;
                        Wait(1, 2);
                        break;
                    case 1:
                        CaptureFrame("Level1");
                        InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                        Application.runInBackground = true;
                        CameraFollow.SetCursorLocked(true);
                        // Exercise input on level ground after verifying the actual
                        // Level1 spawn, independently of dungeon walls and stairs.
                        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        ground.name = "Validation ground";
                        ground.transform.position = new Vector3(1000f, 0f, 1000f);
                        ground.transform.localScale = Vector3.one * 4f;
                        var cc = player.GetComponent<CharacterController>();
                        cc.enabled = false; player.transform.position = new Vector3(1000f, .15f, 1000f); cc.enabled = true;
                        Physics.SyncTransforms();
                        Wait(20, .4); break;
                    case 20:
                        origin = player.transform.position; Keys(Key.W); Wait(2, .65); break;
                    case 2:
                        walkingDistance = Vector3.Distance(origin, player.transform.position);
                        Require(walkingDistance > .4f, "WASD moves the character: " + walkingDistance.ToString("F2") + " m");
                        origin = player.transform.position; Keys(Key.W, Key.LeftAlt); Wait(3, .65); break;
                    case 3:
                        Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "Sprint enters Run");
                        Require(Vector3.Distance(origin, player.transform.position) > walkingDistance * 1.1f, "Sprint is faster than walking");
                        Keys(Key.C); Wait(4, .4); break;
                    case 4:
                        var inCrouch = animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchIdle");
                        if (!inCrouch && Time.timeAsDouble < deadline + 1f) return;
                        if (!inCrouch) report.AppendLine("Crouch diagnostics: grounded=" + player.GetComponent<CharacterController>().isGrounded + "; param=" + animator.GetBool("isCrouching") + "; moving=" + animator.GetBool("isMoving") + "; state=" + animator.GetCurrentAnimatorStateInfo(0).fullPathHash);
                        Require(inCrouch, "Crouch input enters CrouchIdle");
                        Keys(); Wait(21, .2); break;
                    case 21:
                        origin = player.transform.position; Keys(Key.Space); Wait(5, .25); break;
                    case 5:
                        Require(player.transform.position.y > origin.y + .1f, "Jump lifts the character");
                        var hipsOffset = animator.GetBoneTransform(HumanBodyBones.Hips).position - player.transform.position;
                        Require(new Vector2(hipsOffset.x, hipsOffset.z).magnitude < .7f, "Jump visual stays aligned with the character controller");
                        Keys(); Wait(6, 1.25); break;
                    case 6:
                        player.GetComponent<PlayerMotor>().enabled = false;
                        player.GetComponent<PlayerController>().enabled = false;
                        var settings = new SerializedObject(melee);
                        settings.FindProperty("meleeLungeDistance").floatValue = 0f;
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        var dummy = new GameObject("Assassin_Validation_Target"); dummy.tag = "Enemy";
                        dummy.transform.position = player.transform.position + player.transform.forward * .6f + Vector3.up * .8f;
                        var collider = dummy.AddComponent<CapsuleCollider>(); collider.height = 1.5f; collider.radius = .3f;
                        target = dummy.AddComponent<EnemyHealth>(); target.ConfigureHealth(10000);
                        Physics.SyncTransforms(); beforeHealth = target.currentHealth;
                        Require(melee.TryAttack(player.transform.forward), "Combo starts from the gameplay controller");
                        combo = 1; Wait(7, .1); break;
                    case 7:
                        if (melee.IsAttacking) return;
                        Require(target.currentHealth < beforeHealth, "Combo hit " + combo + " applies damage: " + (beforeHealth - target.currentHealth));
                        beforeHealth = target.currentHealth;
                        if (combo < 3)
                        {
                            combo++;
                            Require(melee.TryAttack(player.transform.forward), "Combo step " + combo + " starts");
                            Wait(7, .1);
                        }
                        else
                        {
                            beforeStamina = player.GetComponent<PlayerStamina>().Current;
                            Keys(Key.E); Wait(8, .25);
                        }
                        break;
                    case 8:
                        Require(player.GetComponent<PlayerStamina>().Current < beforeStamina, "Special ability consumes stamina");
                        Keys();
                        SceneManager.LoadScene("CharacterSelection");
                        Wait(9, 3); break;
                    case 9:
                        var card = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
                        var assassin = Array.Find(card, b => b.name == "Card_Asesina");
                        Require(assassin != null, "Assassin card exists in CharacterSelection");
                        assassin.onClick.Invoke();
                        Require(GameSession.SelectedHero == HeroId.Assassin, "Selection button chooses the assassin");
                        var slot = GameObject.Find("Slot_Asesina");
                        Require(slot != null && slot.activeInHierarchy, "Selection shows the animated assassin preview");
                        Require(!slot.GetComponentInChildren<Animator>().fireEvents, "Selection preview suppresses gameplay animation events");
                        Wait(10, 1); break;
                    case 10:
                        CaptureFrame("CharacterSelection");
                        Wait(11, .25); break;
                    case 11:
                        Finish(true); break;
                }
            }
            catch (Exception error)
            {
                report.AppendLine("FAIL " + error);
                Debug.LogError("[Assassin validation] " + error);
                Finish(false);
            }
        }

        static void CaptureFrame(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            AssassinIntegration.SaveRender(camera, AssassinSetup.ReportDir + "/" + name + ".png", 1280, 720);
        }

        static void Finish(bool success)
        {
            InputSystem.onBeforeUpdate -= FlushPendingKeys;
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            File.WriteAllText(AssassinSetup.ReportDir + (SessionState.GetBool(SelectionOnly, false) ? "/selection.txt" : "/gameplay.txt"), report.ToString());
            SessionState.SetInt(Running + ".Exit", success ? 0 : 1);
            EditorApplication.isPlaying = false;
            phase = 99;
        }
    }
}

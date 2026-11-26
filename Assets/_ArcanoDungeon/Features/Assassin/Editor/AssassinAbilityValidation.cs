using System;
using System.IO;
using System.Reflection;
using System.Text;
using ArcanoDungeon.Features.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ArcanoDungeon.Features.Assassin.Editor
{
    [InitializeOnLoad]
    public static class AssassinAbilityValidation
    {
        const string Running = "Arcano.Assassin.AbilitiesCheck";
        const string ScenePath = "Assets/_ArcanoDungeon/Tests/AssassinAbilitiesCheck.unity";
        static int phase, thrown;
        static float deadline;
        static double started;
        static GameObject player;
        static AssassinAbilities ability;
        static PlayerController controller;
        static EnemyHealth target, bossHealth;
        static EnemyAI guard;
        static EclipseBossAttack boss;
        static int previousHealth, targetHealth;
        static Vector3 guardPosition;
        static Key[] keys;
        static bool? mouse;
        static InputSettings originalInputSettings;
        static bool originalBackground;
        static readonly StringBuilder report = new StringBuilder();
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static AssassinAbilityValidation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Running, false)) return;
                SessionState.SetBool(Running, false);
                var previous = SessionState.GetString(Running + ".StartScene", "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                File.WriteAllText("AssassinValidation/abilities-editor-restored.txt", "Scene: " + SceneManager.GetActiveScene().path + "; dirty: " + SceneManager.GetActiveScene().isDirty);
            };
        }

        [MenuItem("Tools/Arcano/Assassin/Validate poison and stealth")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("The validation starts from Edit Mode.");
            Directory.CreateDirectory("AssassinValidation"); Directory.CreateDirectory("Assets/_ArcanoDungeon/Tests"); AssetDatabase.Refresh();
            var previousScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssassinIntegration.PlayerPath));
            actor.transform.position = Vector3.up * .08f;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "AbilityCheckGround"; ground.transform.localScale = Vector3.one * 4;
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule); dummy.name = "PoisonTarget"; dummy.tag = "Enemy"; dummy.transform.position = new Vector3(0, 1, 5);
            dummy.AddComponent<EnemyHealth>().ConfigureHealth(500);
            var guardObject = new GameObject("StealthGuard"); guardObject.tag = "Enemy"; guardObject.transform.position = new Vector3(5, .05f, 0);
            guardObject.AddComponent<EnemyHealth>().ConfigureHealth(1000);
            var ai = guardObject.AddComponent<EnemyAI>(); ai.enabled = false; ai.attackRange = 2; ai.attackCooldown = .6f; ai.attackDamage = 7;
            var guardCC = guardObject.GetComponent<CharacterController>(); guardCC.height = 1.7f; guardCC.center = Vector3.up * .85f; guardCC.radius = .25f;
            var bossObject = new GameObject("StealthBoss"); bossObject.transform.position = new Vector3(2.5f, 0, 0);
            bossObject.AddComponent<EnemyHealth>().ConfigureHealth(1000);
            var nova = bossObject.AddComponent<EclipseBossAttack>(); nova.enabled = false; nova.warningSeconds = .9f;
            var camera = new GameObject("AbilityCheckCamera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(0, 1.7f, -4); camera.transform.LookAt(new Vector3(0, 1, 5)); camera.nearClipPlane = .05f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .045f, .065f);
            var light = new GameObject("AbilityCheckLight").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.4f, .4f, .5f);
            EditorSceneManager.SaveScene(scene, ScenePath); EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previousScene);
            SessionState.SetString(Running + ".StartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(Running, true); SessionState.SetInt(Running + ".Result", 1);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            phase = 0; report.Clear(); started = EditorApplication.timeSinceStartup;
            EditorApplication.isPlaying = true;
        }

        static void InputBeforeUpdate()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (keys != null) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys)); keys = null; }
            if (mouse.HasValue) { InputSystem.QueueStateEvent(Mouse.current, mouse.Value ? new MouseState().WithButton(MouseButton.Right) : new MouseState()); mouse = null; }
        }

        static void Check(bool valid, string message)
        {
            if (!valid) throw new Exception(message);
            report.AppendLine("PASS " + message);
        }
        static void Wait(int next, float seconds) { phase = next; deadline = Time.time + seconds; }
        static bool PrivateBool(object instance, string name) => (bool)instance.GetType().GetField(name, Private).GetValue(instance);
        static void Tick()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            try
            {
                if (phase == 0)
                {
                    started = EditorApplication.timeSinceStartup;
                    player = GameObject.FindGameObjectWithTag("Player"); if (player == null) return;
                    ability = player.GetComponent<AssassinAbilities>(); controller = player.GetComponent<PlayerController>();
                    Check(ability != null, "Playable prefab includes assassin abilities");
                    target = GameObject.Find("PoisonTarget").GetComponent<EnemyHealth>(); guard = GameObject.Find("StealthGuard").GetComponent<EnemyAI>(); boss = GameObject.Find("StealthBoss").GetComponent<EclipseBossAttack>(); bossHealth = boss.GetComponent<EnemyHealth>();
                    originalInputSettings = InputSystem.settings;
                    originalBackground = Application.runInBackground;
                    InputSystem.settings = Object.Instantiate(originalInputSettings);
                    InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    Application.runInBackground = true;
                    if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>("AbilityCheckKeyboard");
                    if (Mouse.current == null) InputSystem.AddDevice<Mouse>("AbilityCheckMouse");
                    InputSystem.onBeforeUpdate += InputBeforeUpdate;
                    ability.KnifeThrown += knife => thrown++;
                    CameraFollow.SetCursorLocked(true); GameSession.SelectedHero = HeroId.Assassin;
                    Wait(1, .6f); return;
                }
                if (EditorApplication.timeSinceStartup - started > 90) throw new Exception("Ability validation timed out at phase " + phase);
                if (Time.time < deadline) return;
                switch (phase)
                {
                    case 1:
                        previousHealth = player.GetComponent<PlayerHealth>().currentHealth; mouse = true; Wait(2, .12f); break;
                    case 2:
                        Check(thrown == 1, "Right mouse button launches one poison knife");
                        Check(!controller.isBlocking, "Assassin secondary attack does not activate blocking");
                        Check(!ability.TryThrowAt(target.transform.position), "Throw cooldown prevents duplicate launches");
                        mouse = false; AssassinIntegration.SaveRender(Camera.main, "AssassinValidation/PoisonKnife.png", 960, 720); Wait(3, .4f); break;
                    case 3:
                        Check(target.currentHealth == 480, "Knife hits once for 20 direct damage");
                        Check(target.GetComponent<PoisonedEnemy>()?.IsPoisoned == true, "Knife applies poison to the enemy");
                        Check(player.GetComponent<PlayerHealth>().currentHealth == previousHealth, "Projectile ignores its owner");
                        Wait(4, 4.1f); break;
                    case 4:
                        Check(target.currentHealth == 456, "Poison deals four ticks of 6 damage");
                        Check(!target.GetComponent<PoisonedEnemy>().IsPoisoned, "Poison expires after its final tick");
                        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "KnifeTestWall"; wall.transform.position = new Vector3(0, 1, 2.5f); wall.transform.localScale = new Vector3(2.5f, 3, .04f); Physics.SyncTransforms();
                        targetHealth = target.currentHealth; Check(ability.TryThrowAt(target.transform.position), "Knife can be thrown after cooldown"); Wait(5, .6f); break;
                    case 5:
                        Check(target.currentHealth == targetHealth && !target.GetComponent<PoisonedEnemy>().IsPoisoned, "A thin wall stops the fast projectile and its poison");
                        Object.Destroy(GameObject.Find("KnifeTestWall"));
                        var cc = guard.GetComponent<CharacterController>(); cc.enabled = false; guard.transform.position = player.transform.position + Vector3.forward * 1.1f; cc.enabled = true; guard.enabled = true;
                        Wait(6, .05f); break;
                    case 6:
                        if (!PrivateBool(guard, "isAttacking")) return;
                        Check(guard.HasPlayerTarget, "Visible assassin is detected and targeted");
                        previousHealth = player.GetComponent<PlayerHealth>().currentHealth; keys = new[] { Key.C }; Wait(7, .2f); break;
                    case 7:
                        Check(ability.IsHidden && ability.ShadowVisible, "Crouch activates concealment and the shadow visual");
                        Check(!guard.HasPlayerTarget, "Enemy loses the concealed assassin");
                        Check(!PrivateBool(guard, "isAttacking"), "Stealth cancels an enemy attack already winding up");
                        guardPosition = guard.transform.position;
                        Check(ability.TryThrowAt(target.transform.position), "Poison knives remain usable from stealth");
                        Camera.main.transform.position = new Vector3(3, 1.8f, 4); Camera.main.transform.LookAt(player.transform.position + Vector3.up * .75f);
                        Wait(8, .6f); break;
                    case 8:
                        Check(ability.IsHidden && !guard.HasPlayerTarget, "Throwing from shadows does not expose the assassin");
                        Check(player.GetComponent<PlayerHealth>().currentHealth == previousHealth, "Cancelled enemy attacks do not deal delayed damage");
                        Check(new Vector2(guard.transform.position.x - guardPosition.x, guard.transform.position.z - guardPosition.z).magnitude < .05f, "Enemy stops pursuing the hidden assassin");
                        AssassinIntegration.SaveRender(Camera.main, "AssassinValidation/ShadowStealth.png", 960, 900);
                        boss.enabled = true; Wait(9, 5.4f); break;
                    case 9:
                        Check(!PrivateBool(boss, "casting"), "Boss does not start an attack against a hidden assassin");
                        Check(player.GetComponent<PlayerHealth>().currentHealth == previousHealth, "Enemy and boss cannot hit the concealed assassin");
                        keys = Array.Empty<Key>(); Wait(10, .6f); break;
                    case 10:
                        Check(!ability.IsHidden && !ability.ShadowVisible, "Leaving crouch restores the normal appearance and visibility");
                        Check(guard.HasPlayerTarget, "Enemy detects the assassin again after stealth ends");
                        if (!PrivateBool(boss, "casting")) return;
                        keys = new[] { Key.C }; previousHealth = player.GetComponent<PlayerHealth>().currentHealth; Wait(11, .25f); break;
                    case 11:
                        Check(ability.IsHidden && !PrivateBool(boss, "casting"), "Entering shadow cancels a boss attack during its warning");
                        Wait(12, 1.1f); break;
                    case 12:
                        Check(player.GetComponent<PlayerHealth>().currentHealth == previousHealth, "Cancelled boss warning causes no delayed damage");
                        guard.enabled = false; boss.enabled = false;
                        target.ConfigureHealth(100); var poison = target.GetComponent<PoisonedEnemy>(); poison.Apply(6, 2, .2f, null); poison.Apply(6, 2, .2f, null); Wait(13, .25f); break;
                    case 13:
                        Check(target.currentHealth == 94, "Reapplying poison refreshes it without doubling tick damage"); Wait(14, .25f); break;
                    case 14:
                        Check(target.currentHealth == 88 && !target.GetComponent<PoisonedEnemy>().IsPoisoned, "Refreshed poison ends cleanly");
                        target.ConfigureHealth(5); target.GetComponent<PoisonedEnemy>().Apply(6, 2, .2f, null); Wait(15, .3f); break;
                    case 15:
                        Check(target.IsDead && target.currentHealth == 0, "Poison can kill through the normal enemy death system");
                        ability.enabled = false; Check(!AssassinAbilities.IsConcealed(player.transform), "Concealment does not persist when the ability is disabled");
                        Finish(true); break;
                }
            }
            catch (Exception error) { report.AppendLine("FAIL " + error); Debug.LogException(error); Finish(false); }
        }

        static void Finish(bool success)
        {
            InputSystem.onBeforeUpdate -= InputBeforeUpdate;
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            InputSystem.settings = originalInputSettings;
            Application.runInBackground = originalBackground;
            CameraFollow.SetCursorLocked(false);
            File.WriteAllText("AssassinValidation/abilities-gameplay.txt", report.ToString());
            SessionState.SetInt(Running + ".Result", success ? 0 : 1); phase = 99;
            EditorApplication.isPlaying = false;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using ArcanoDungeon.Features.Assassin;
using ArcanoDungeon.Features.Assassin.Editor;
using ArcanoDungeon.Features.Levels;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArcanoDungeon.Features.HUD.Editor
{
    [InitializeOnLoad]
    public static class GamePresentationValidation
    {
        const string Running = "Arcano.Presentation.Validation";
        static readonly string[] Levels = { "Level1", "CatacombsLevel", "CastleLevel" };
        static readonly StringBuilder report = new StringBuilder();
        static int level, phase;
        static float readyAt;
        static double started;
        static GameObject player;
        static PlayerVitalsHud hud;

        static GamePresentationValidation()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Running, false)) return;
                SessionState.SetBool(Running, false);
                var previous = SessionState.GetString(Running + ".StartScene", "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                AudioListener.pause = SessionState.GetBool(Running + ".AudioPause", false);
                Application.runInBackground = SessionState.GetBool(Running + ".Background", false);
                File.WriteAllText("AssassinValidation/presentation-editor-restored.txt", "Scene: " + SceneManager.GetActiveScene().path + "; dirty: " + SceneManager.GetActiveScene().isDirty);
            };
        }

        [MenuItem("Tools/Arcano/Validate HUD in all three levels")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start this validation from Edit Mode.");
            Directory.CreateDirectory("AssassinValidation");
            SessionState.SetBool(Running, true); SessionState.SetBool(Running + ".AudioPause", AudioListener.pause);
            SessionState.SetBool(Running + ".Background", Application.runInBackground);
            SessionState.SetString(Running + ".StartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_ArcanoDungeon/Scenes/Level1.unity");
            level = phase = 0; report.Clear(); started = EditorApplication.timeSinceStartup;
            EditorApplication.isPlaying = true;
        }

        [InitializeOnEnterPlayMode]
        static void SelectAssassin(EnterPlayModeOptions options)
        {
            if (!SessionState.GetBool(Running, false)) return;
            GameSession.SelectedHero = HeroId.Assassin; AudioListener.pause = true;
            Application.runInBackground = true;
            Time.timeScale = 1f;
        }

        static void Check(bool valid, string message)
        {
            if (!valid) throw new Exception(message);
            report.AppendLine("PASS " + message);
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying) return;
            try
            {
                Application.runInBackground = true;
                EditorApplication.QueuePlayerLoopUpdate();
                if (EditorApplication.isPaused) return;
                if (started <= 0) started = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - started > 150) throw new Exception("Presentation validation timed out at " + level + "/" + phase);
                foreach (var enemy in Object.FindObjectsByType<EnemyAI>()) { enemy.StopAllCoroutines(); enemy.enabled = false; }
                foreach (var boss in Object.FindObjectsByType<EclipseBossAttack>()) boss.enabled = false;
                if (phase == 0)
                {
                    if (SceneManager.GetActiveScene().name != Levels[level]) return;
                    player = GameObject.FindGameObjectWithTag("Player"); hud = Object.FindAnyObjectByType<PlayerVitalsHud>();
                    if (player == null || hud == null) return;
                    CameraFollow.SetCursorLocked(false); readyAt = Time.time + .25f; phase = 1; return;
                }
                if (Time.time < readyAt) return;
                var health = player.GetComponent<PlayerHealth>(); var stats = player.GetComponent<PlayerStats>(); var stamina = player.GetComponent<PlayerStamina>();
                if (phase == 1)
                {
                    report.AppendLine("\nBARRAS — " + Levels[level]);
                    Check(Object.FindObjectsByType<PlayerVitalsHud>().Length == 1, "One shared vitals panel");
                    foreach (var id in new[] { "Health", "Shield", "Stamina" })
                        Check(Object.FindObjectsByType<Slider>().Count(s => s.name == "Player" + id + "Bar") == 1, "One active " + id + " bar; legacy copies are hidden");
                    var scaler = hud.GetComponent<CanvasScaler>();
                    Check(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution == new Vector2(1920, 1080), "Consistent screen scaling");
                    var bars = new[] { hud.HealthBar, hud.ShieldBar, hud.StaminaBar };
                    var colors = new[] { GamePresentationSetup.HealthColor, GamePresentationSetup.ShieldColor, GamePresentationSetup.StaminaColor };
                    for (int i = 0; i < bars.Length; i++)
                    {
                        var rect = bars[i].GetComponent<RectTransform>();
                        Check(rect.sizeDelta == new Vector2(328, 26) && Mathf.Abs(rect.anchoredPosition.x - 15f) < .01f, "Shared bar dimensions and alignment: " + bars[i].name);
                        Check(bars[i].fillRect.GetComponent<Image>().color == colors[i], "Shared color: " + bars[i].name);
                    }
                    if (level > 0)
                    {
                        Check(health.currentHealth == 80, "Health survives the level transition");
                        Check(Mathf.Abs(stats.CurrentShield - 20) < .1f, "Shield survives the level transition");
                        Check(stamina.Current >= 60 && stamina.Current < 80, "Stamina survives the level transition");
                    }
                    health.RestoreCurrentHealth(80); stats.RestoreTransitionState(new PlayerStats.TransitionState { Shield = 20, RegenDelayRemaining = 3f, SpeedMultiplier = 1f, SpeedSecondsRemaining = 0f }); stamina.RestoreCurrent(60);
                    readyAt = Time.time + .1f; phase = 2; return;
                }
                if (phase == 2)
                {
                    Check(Mathf.Abs(hud.HealthBar.value - health.currentHealth) < .01f && Mathf.Abs(hud.HealthBar.maxValue - health.maxHealth) < .01f, "Health fill and maximum match gameplay data");
                    Check(Mathf.Abs(hud.ShieldBar.value - stats.CurrentShield) < .1f, "Shield fill matches gameplay data");
                    Check(Mathf.Abs(hud.StaminaBar.value - stamina.Current) < .1f, "Stamina fill matches gameplay data");
                    health.TakeDamage(25); stamina.TryConsumeDash(); readyAt = Time.time + .1f; phase = 3; return;
                }
                if (phase == 3)
                {
                    Check(health.currentHealth == 75 && stats.CurrentShield < .1f, "Damage updates health and shield");
                    Check(Mathf.Abs(hud.HealthBar.value - 75f) < .01f && hud.ShieldBar.value < .1f && Mathf.Abs(hud.StaminaBar.value - stamina.Current) < .1f, "Bars respond to damage and stamina use");
                    report.AppendLine("TAMAÑO DE LA ASESINA — " + Levels[level]);
                    var visual = player.transform.Find("Visual"); var cc = player.GetComponent<CharacterController>();
                    Check(Mathf.Abs(visual.localScale.x - .713f) < .0001f, "Assassin visual scale increased by 15 percent");
                    Check(Mathf.Abs(cc.height - 1.6675f) < .001f && Mathf.Abs(cc.radius - .2645f) < .001f && Mathf.Abs(cc.center.y - .83375f) < .001f, "Collider matches the larger model and keeps the feet aligned");
                    Check(Mathf.Abs(player.GetComponent<PlayerController>().crouchHeight - 1.0925f) < .001f, "Crouch capsule scales proportionally");
                    var follow = Object.FindAnyObjectByType<CameraFollow>();
                    Check(follow != null && Mathf.Abs(follow.heightOffset - 1.265f) < .001f, "Camera targets the larger assassin at the proper height");
                    var animator = player.GetComponentInChildren<Animator>();
                    Check(animator.isHuman && animator.avatar.isValid && player.GetComponent<AssassinAbilities>() != null, "Humanoid animation and poison/shadow abilities remain connected");
                    var head = animator.GetBoneTransform(HumanBodyBones.Head);
                    var viewport = Camera.main.WorldToViewportPoint(head.position);
                    Check(viewport.z > 0 && viewport.y > .05f && viewport.y < .95f && viewport.x > 0 && viewport.x < 1, "The larger head stays inside the camera frame");
                    Capture(Levels[level]);
                    health.RestoreCurrentHealth(80); stats.RestoreTransitionState(new PlayerStats.TransitionState { Shield = 20, RegenDelayRemaining = 3f, SpeedMultiplier = 1f }); stamina.RestoreCurrent(60);
                    if (++level < Levels.Length)
                    {
                        Check(GameSession.PrepareLevelTransition(player), "Transition snapshot prepared");
                        phase = 0; player = null; hud = null; SceneManager.LoadScene(Levels[level]);
                    }
                    else Finish(true);
                }
            }
            catch (Exception error) { report.AppendLine("FAIL " + error); Debug.LogException(error); Finish(false); }
        }

        static void Capture(string name)
        {
            var camera = Camera.main;
            foreach (var canvas in Object.FindObjectsByType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            }
            Canvas.ForceUpdateCanvases();
            AssassinIntegration.SaveRender(camera, "AssassinValidation/HUD_" + name + ".png", 1280, 720);
        }

        static void Finish(bool success)
        {
            CameraFollow.SetCursorLocked(false);
            File.WriteAllText("AssassinValidation/presentation-gameplay.txt", report.ToString());
            SessionState.SetInt(Running + ".Result", success ? 0 : 1); phase = 99; EditorApplication.isPlaying = false;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ArcanoDungeon.Features.Combat;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player;
using ArcanoDungeon.Features.Player.Motor;
using ArcanoDungeon.Features.Player.View;
using ArcanoDungeon.Infrastructure.Input;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArcanoDungeon.Features.Assassin.Editor
{
    public static class AssassinIntegration
    {
        const string Art = "Assets/_ArcanoDungeon/Art/Characters/Assassin";
        const string Anim = "Assets/_ArcanoDungeon/Animators/Assassin";
        const string Wtd = "Assets/Haons SD series Pack/Animation/WeaponMaster Twin dagger(WTD)/";
        public const string PlayerPath = "Assets/_ArcanoDungeon/Resources/Characters/AssassinPlayer.prefab";
        public const string PreviewPath = "Assets/_ArcanoDungeon/Prefabs/AssassinPreview.prefab";
        public const string PreviewScene = "Assets/_ArcanoDungeon/Scenes/AssassinTraining.unity";
        const string SelectionScene = "Assets/_ArcanoDungeon/Scenes/CharacterSelection.unity";

        [MenuItem("Tools/Arcano/Assassin/Build playable assassin")]
        public static void Build()
        {
            Directory.CreateDirectory(Anim);
            Directory.CreateDirectory(Art + "/Materials");
            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPath));
            Directory.CreateDirectory(AssassinSetup.ReportDir);
            AssetDatabase.Refresh();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssassinSetup.ModelPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(AssassinSetup.ModelPath).OfType<Avatar>().First();
            if (!avatar.isValid || !avatar.isHuman) throw new Exception("Import the humanoid rig first.");
            var controller = BuildController();
            var preview = Object.Instantiate(model);
            preview.name = "AssassinPreview";
            ApplyMaterials(preview);
            HideCoveredSkin(preview);
            var animator = preview.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var grip = preview.AddComponent<AssassinDaggerGrip>();
            CaptureAuthoredGrip(preview, grip);
            foreach (var skin in preview.GetComponentsInChildren<SkinnedMeshRenderer>())
                skin.updateWhenOffscreen = true;
            PrefabUtility.SaveAsPrefabAsset(preview, PreviewPath);
            Object.DestroyImmediate(preview);
            BuildPlayer();
            AssassinAbilitySetup.Setup();
            RenderValidation();
            PatchCharacterSelection();
            BuildTrainingScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Assassin] Gameplay prefab, selection and training scene ready.");
        }

        static AnimationClip Clip(string path, string name)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);
            if (clip == null) throw new Exception("Missing animation: " + path + " | " + name);
            if (!clip.isHumanMotion) throw new Exception("Animation is not humanoid: " + name);
            return clip;
        }

        static AnimationClip CopyClip(string path, string name, string output, bool loop, bool attack = false)
        {
            var source = Clip(path, name);
            var targetPath = Anim + "/" + output + ".anim";
            var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath);
            if (copy == null) { copy = new AnimationClip(); AssetDatabase.CreateAsset(copy, targetPath); }
            EditorUtility.CopySerialized(source, copy);
            copy.name = output;
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AnimationUtility.SetAnimationEvents(copy, attack ? new[] {
                new AnimationEvent { functionName = "OnAttackHitFrame", time = copy.length * .40f },
                new AnimationEvent { functionName = "OnAttackEndFrame", time = copy.length * .90f }
            } : Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(copy);
            return copy;
        }

        static AnimatorController BuildController()
        {
            var path = Anim + "/AssassinGameplay.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                if (!AssetDatabase.CopyAsset("Assets/_ArcanoDungeon/Animators/MisakiGameplay.controller", path))
                    throw new Exception("Could not copy the existing gameplay controller.");
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            var clips = new Dictionary<string, AnimationClip> {
                {"Idle", CopyClip(Wtd + "WTD_Stand.FBX", "WTD_Stand@loop", "Assassin_Idle", true)},
                {"Move", CopyClip(Wtd + "WTD_Walk_Front.FBX", "WTD_Walk_Front@Loop", "Assassin_Walk", true)},
                {"Run", CopyClip(Wtd + "WTD_Run_Front.FBX", "WTD_Run_Front@Loop", "Assassin_Run", true)},
                {"CrouchIdle", CopyClip("Assets/FreeTestCharacterAsuna/FBX/Animations/005Stealth.fbx", "StealthIdle", "Assassin_CrouchIdle", true)},
                {"CrouchWalk", CopyClip("Assets/FreeTestCharacterAsuna/FBX/Animations/005Stealth.fbx", "StealthWalk", "Assassin_CrouchWalk", true)},
                {"Jump", CopyClip("Assets/Haons SD series Pack/Animation/Action Adventure(Adv)/Jump_One-cycle-Type(root).FBX", "Jump_Medium root", "Assassin_Jump", false)},
            };
            for (int i = 1; i <= 3; i++)
                clips["Attack" + i] = CopyClip(Wtd + "WTD_AttackA np.FBX", "WTD_AttackA" + i + " np", "Assassin_Combo" + i, false, true);
            UseDaggerArms(clips["CrouchIdle"], clips["Idle"]);
            UseDaggerArms(clips["CrouchWalk"], clips["Move"]);
            foreach (var state in controller.layers[0].stateMachine.states)
                if (clips.TryGetValue(state.state.name, out var clip))
                {
                    state.state.motion = clip;
                    state.state.speed = state.state.name.StartsWith("Attack") ? 1.08f : 1f;
                    state.state.iKOnFeet = !state.state.name.StartsWith("Attack") && state.state.name != "Jump";
                }
            // Only the full-body melee states drive this dual-dagger character.
            var layers = controller.layers;
            for (int i = 1; i < layers.Length; i++) layers[i].defaultWeight = 0f;
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void UseDaggerArms(AnimationClip crouch, AnimationClip source)
        {
            var ratio = crouch.length / source.length;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var name = binding.propertyName;
                if (binding.type != typeof(Animator) || (!name.StartsWith("Left") && !name.StartsWith("Right"))) continue;
                if (!name.Contains("Arm") && !name.Contains("Forearm") && !name.Contains("Shoulder") && !name.Contains("Hand")) continue;
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].time *= ratio;
                    keys[i].inTangent /= ratio;
                    keys[i].outTangent /= ratio;
                }
                AnimationUtility.SetEditorCurve(crouch, binding, new AnimationCurve(keys) { preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode });
            }
            EditorUtility.SetDirty(crouch);
        }

        static void ApplyMaterials(GameObject model)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new Exception("URP Lit shader missing.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Body.png");
            var cache = new Dictionary<string, Material>();
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source => {
                    if (cache.TryGetValue(source.name, out var found)) return found;
                    var path = Art + "/Materials/" + source.name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                    material.SetColor("_BaseColor", source.color);
                    material.SetFloat("_Smoothness", source.name.Contains("Acero") || source.name.Contains("Plata") ? .55f : .22f);
                    material.SetFloat("_Metallic", source.name.Contains("Acero") || source.name.Contains("Plata") ? .7f : 0f);
                    material.SetFloat("_Cull", 0f);
                    if (source.name == "Piel_Rostro_Referencia" || source.name == "Ojos_Detalles_Referencia")
                        material.SetTexture("_BaseMap", texture);
                    EditorUtility.SetDirty(material);
                    cache[source.name] = material;
                    return material;
                }).ToArray();
            }
        }

        static void BuildPlayer()
        {
            var root = new GameObject("AssassinPlayer");
            root.tag = "Player";
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath));
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * AssassinSizing.VisualScale;
            var animator = visual.GetComponent<Animator>();
            var cc = root.AddComponent<CharacterController>();
            cc.height = AssassinSizing.CapsuleHeight; cc.center = Vector3.up * (cc.height * .5f); cc.radius = AssassinSizing.CapsuleRadius; cc.skinWidth = .035f * AssassinSizing.Multiplier; cc.stepOffset = .25f * AssassinSizing.Multiplier;
            var body = root.AddComponent<BoxCollider>();
            body.isTrigger = true; body.size = new Vector3(.46f, 1.45f, .46f) * AssassinSizing.Multiplier; body.center = cc.center;
            var health = root.AddComponent<PlayerHealth>();
            health.maxHealth = health.currentHealth = 110;
            var input = root.AddComponent<PlayerInputReader>();
            input.ActionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_ArcanoDungeon/Settings/Input/DungeonKnightPlayer.inputactions");
            root.AddComponent<PlayerStamina>();
            root.AddComponent<AimRaycaster>();
            root.AddComponent<PlayerMotor>();
            var cameraBootstrap = root.AddComponent<PlayerCameraBootstrap>();
            var cameraSettings = new SerializedObject(cameraBootstrap);
            cameraSettings.FindProperty("followHeightOverride").floatValue = AssassinSizing.FollowHeight;
            cameraSettings.FindProperty("cameraTargetLocalPosition").vector3Value = Vector3.up * AssassinSizing.CrouchHeight;
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();
            root.AddComponent<PlayerInteract>();
            root.AddComponent<HeroSpecialAttack>();
            root.AddComponent<PlayerStats>();
            var player = root.AddComponent<PlayerController>();
            player.moveSpeed = 6.2f; player.runSpeedMultiplier = 1.55f; player.crouchHeight = AssassinSizing.CrouchHeight;
            var dagger = visual.GetComponentsInChildren<Transform>().First(t => t.name == "Dagger.R");
            var blade = visual.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Daga_R");
            var maxProjection = blade.sharedMesh.vertices.Max(v => Vector3.Dot(blade.transform.TransformPoint(v) - dagger.position, dagger.up));
            var weaponBase = Socket(dagger, "WeaponBase", dagger.position);
            var weaponTip = Socket(dagger, "WeaponTip", dagger.position + dagger.up * maxProjection);
            var vfx = root.AddComponent<AttackArcVFX>();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_ArcanoDungeon/Prefabs/MisakiPlayer.prefab");
            EditorUtility.CopySerialized(source.GetComponent<AttackArcVFX>(), vfx);
            var settings = new SerializedObject(vfx);
            settings.FindProperty("weaponTip").objectReferenceValue = weaponTip;
            settings.FindProperty("slashSpawnPoint").objectReferenceValue = weaponTip;
            settings.FindProperty("crescentSpawnPoint").objectReferenceValue = root.transform;
            settings.FindProperty("weaponTrail").objectReferenceValue = null;
            settings.FindProperty("owner").objectReferenceValue = root.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var melee = root.AddComponent<MeleeWeaponController>();
            melee.EditorAssignRefs(animator, weaponBase, weaponTip, vfx, MeleeWeaponController.AttackStyle.SwordArc, 1.65f, null, 1.95f);
            visual.AddComponent<AttackAnimEventRelay>();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
        }

        static void CaptureAuthoredGrip(GameObject model, AssassinDaggerGrip grip)
        {
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Asesina_Cuerpo");
            var bind = skin.sharedMesh.bindposes;
            var rest = new Dictionary<Transform, Matrix4x4>();
            for (int i = 0; i < skin.bones.Length; i++) rest[skin.bones[i]] = skin.transform.localToWorldMatrix * bind[i].inverse;
            var fingers = model.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("f_") || t.name.StartsWith("thumb.")).ToArray();
            var pose = fingers.Select(t => rest.ContainsKey(t) ? ((rest.ContainsKey(t.parent) ? rest[t.parent] : t.parent.localToWorldMatrix).inverse * rest[t]).rotation : t.localRotation).ToArray();
            var difference = fingers.Select((t, i) => Quaternion.Angle(t.localRotation, pose[i])).Max();
            grip.Capture(fingers, pose);
            File.WriteAllText(AssassinSetup.ReportDir + "/grip.txt", "Finger bones: " + fingers.Length + "; maximum import/bind rotation difference: " + difference.ToString("F3") + " degrees");
        }

        // Covered body surfaces are omitted only in the gameplay mesh. They can
        // poke through the independently weighted tunic and leggings in deep poses.
        // The approved source FBX and its editable body remain intact.
        static void HideCoveredSkin(GameObject model)
        {
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Asesina_Cuerpo");
            var source = skin.sharedMesh;
            var meshPath = Art + "/Assassin_BodyGameplay.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = Object.Instantiate(source); AssetDatabase.CreateAsset(mesh, meshPath); }
            else EditorUtility.CopySerialized(source, mesh);
            mesh.name = "Assassin_BodyGameplay";
            var vertices = source.vertices;
            var normals = source.normals;
            var garment = Object.Instantiate(source);
            var clothing = new HashSet<string> { "Tunica_cuero_flexible", "Leggings_carbon", "Cuero_botas", "Brazales_cuero" };
            var garmentIndices = Enumerable.Range(0, source.subMeshCount).Where(s => clothing.Contains(skin.sharedMaterials[s].name)).SelectMany(s => source.GetTriangles(s)).ToArray();
            garment.subMeshCount = 1;
            garment.SetTriangles(garmentIndices, 0);
            var probe = new GameObject("Garment coverage probe");
            probe.transform.SetParent(skin.transform, false);
            var collider = probe.AddComponent<MeshCollider>(); collider.sharedMesh = garment;
            Physics.SyncTransforms();
            var hidden = new bool[vertices.Length];
            var previousBackfaces = Physics.queriesHitBackfaces;
            try
            {
                Physics.queriesHitBackfaces = true;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var point = skin.transform.TransformPoint(vertices[i]);
                    var normal = skin.transform.TransformDirection(normals[i]).normalized;
                    hidden[i] = collider.Raycast(new Ray(point - normal * .002f, normal), out _, .09f);
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = previousBackfaces;
                Object.DestroyImmediate(probe); Object.DestroyImmediate(garment);
            }
            int removed = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                if (skin.sharedMaterials[sub].name != "Piel_Rostro_Referencia") continue;
                var indices = source.GetTriangles(sub);
                var kept = new List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    if (hidden[indices[i]] && hidden[indices[i + 1]] && hidden[indices[i + 2]]) { removed++; continue; }
                    kept.Add(indices[i]); kept.Add(indices[i + 1]); kept.Add(indices[i + 2]);
                }
                mesh.SetTriangles(kept, sub);
            }
            skin.sharedMesh = mesh; EditorUtility.SetDirty(mesh);
            File.WriteAllText(AssassinSetup.ReportDir + "/covered-skin.txt", "Hidden body triangles beneath tunic and leggings: " + removed);
        }

        static Transform Socket(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        static void PatchCharacterSelection()
        {
            var scene = EditorSceneManager.OpenScene(SelectionScene, OpenSceneMode.Single);
            var stage = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t => t.name == "HeroStage");
            var existing = stage.Find("Slot_Asesina");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var slot = new GameObject("Slot_Asesina");
            slot.transform.SetParent(stage, false);
            var sourceSlot = stage.GetChild(0);
            slot.transform.localPosition = sourceSlot.localPosition;
            var hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath));
            hero.name = "Asesina";
            hero.transform.SetParent(slot.transform, false);
            hero.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            hero.transform.localScale = Vector3.one * HeroRoster.Get(HeroId.Assassin).StageScale;
            ToneStagePreview(hero);
            slot.SetActive(false);
            var roster = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(t => t.name == "Roster");
            var card = roster.Find("Card_Asesina");
            if (card == null) { card = Object.Instantiate(roster.Find("Card_Yuko"), roster); card.name = "Card_Asesina"; }
            card.Find("Name").GetComponent<Text>().text = "Asesina";
            var title = card.Find("Title").GetComponent<Text>(); title.text = "ASESINA"; title.color = HeroRoster.Get(HeroId.Assassin).Accent;
            card.Find("Portrait").GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Portrait.png");
            var count = HeroRoster.All.Length;
            roster.sizeDelta = new Vector2(roster.sizeDelta.x, count * 98f + 40f);
            for (int i = 0; i < count; i++)
            {
                var rt = roster.Find("Card_" + HeroRoster.All[i].Name).GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, (count - 1) * 49f - i * 98f);
                rt.sizeDelta = new Vector2(290f, 88f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Arcano/Assassin/Update selection preview")]
        public static void UpdateSelectionPreview()
        {
            PatchCharacterSelection();
            AssetDatabase.SaveAssets();
        }

        static void ToneStagePreview(GameObject hero)
        {
            var path = Art + "/Materials/Stage";
            Directory.CreateDirectory(path); AssetDatabase.Refresh();
            var shader = Shader.Find("Arcano/Assassin Preview");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Assassin preview shader is not ready.");
            var materials = new Dictionary<string, Material>();
            foreach (var renderer in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source => {
                    if (materials.TryGetValue(source.name, out var cached)) return cached;
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path + "/" + source.name + ".mat");
                    if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path + "/" + source.name + ".mat"); }
                    material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
                    EditorUtility.SetDirty(material); materials[source.name] = material;
                    return material;
                }).ToArray();
            }
        }

        static void BuildTrainingScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));
            player.AddComponent<AssassinTraining>();
            MakeStudio();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TrainingGround"; floor.transform.localScale = Vector3.one * 4f;
            var camera = Object.FindAnyObjectByType<Camera>();
            camera.orthographic = false;
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.distance = 3.6f; follow.heightOffset = 1.0f;
            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "Blanco de practica"; dummy.tag = "Enemy";
            dummy.transform.position = new Vector3(0f, .85f, 2.4f);
            dummy.transform.localScale = new Vector3(.6f, .85f, .6f);
            var health = dummy.AddComponent<EnemyHealth>(); health.ConfigureHealth(10000);
            EditorSceneManager.SaveScene(scene, PreviewScene);
        }

        static Camera MakeStudio()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .65f, .8f);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.4f; key.transform.rotation = Quaternion.Euler(40, -35, 0);
            var rim = new GameObject("Rim").AddComponent<Light>();
            rim.type = LightType.Directional; rim.intensity = .7f; rim.color = new Color(.65f, .7f, 1f); rim.transform.rotation = Quaternion.Euler(20, 160, 0);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .065f, .10f);
            camera.transform.position = new Vector3(-2.2f, 1.65f, 4.2f);
            camera.transform.LookAt(new Vector3(0, .78f, 0));
            camera.orthographic = true; camera.orthographicSize = .95f;
            camera.nearClipPlane = .05f; camera.farClipPlane = 100;
            return camera;
        }

        [MenuItem("Tools/Arcano/Assassin/Render animation validation")]
        public static void RenderValidation()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = MakeStudio();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPath));
            model.transform.localScale = Vector3.one * .62f;
            var animator = model.GetComponent<Animator>();
            animator.fireEvents = false;
            animator.Rebind(); animator.Update(0f);
            var graph = PlayableGraph.Create("Assassin validation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "Pose", animator);
            var controller = (AnimatorController)animator.runtimeAnimatorController;
            var report = new System.Text.StringBuilder();
            foreach (var state in new[] { "Idle", "Move", "Run", "CrouchIdle", "CrouchWalk", "Jump", "Attack1", "Attack2", "Attack3" })
            {
                var clip = (AnimationClip)controller.layers[0].stateMachine.states.First(s => s.state.name == state).state.motion;
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                output.SetSourcePlayable(playable);
                graph.Play();
                for (int sample = 0; sample < 5; sample++)
                {
                    playable.SetTime(clip.length * sample * .2f); graph.Evaluate(0f);
                    model.GetComponent<AssassinDaggerGrip>().Apply();
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var baked = new Mesh(); skin.BakeMesh(baked);
                        if (baked.vertices.Any(v => float.IsNaN(v.x) || float.IsInfinity(v.x))) throw new Exception("Invalid skin: " + state);
                        report.AppendLine(state + " t=" + sample * .2f + " " + skin.name + " " + baked.bounds);
                        Object.DestroyImmediate(baked);
                    }
                    if (sample == 2)
                    {
                        SaveBakedRender(model, camera, AssassinSetup.ReportDir + "/" + state + ".png");
                    }
                }
                playable.Destroy();
            }
            graph.Destroy();
            animator.Play("Idle", 0, .2f); animator.Update(0f); model.GetComponent<AssassinDaggerGrip>().Apply();
            camera.transform.position = new Vector3(-.9f, 1.8f, 3f); camera.transform.LookAt(new Vector3(0, 1.22f, 0)); camera.orthographicSize = .43f;
            SaveBakedRender(model, camera, Art + "/Portrait.png", 512, 512);
            AssetDatabase.ImportAsset(Art + "/Portrait.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "/Portrait.png");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
            File.WriteAllText(AssassinSetup.ReportDir + "/animation-samples.txt", report.ToString());
        }

        // Force GPU matrix updates when rendering several poses in one callback.
        static void SaveBakedRender(GameObject model, Camera camera, string path, int width = 768, int height = 900)
        {
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins)
                skin.forceMatrixRecalculationPerRender = true;
            SaveRender(camera, path, width, height);
            SaveRender(camera, path, width, height);
        }

        internal static void SaveRender(Camera camera, string path, int width = 768, int height = 900)
        {
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(rt);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ArcanoDungeon.Features.Assassin.Editor
{
    public static class AssassinAbilitySetup
    {
        const string Folder = "Assets/_ArcanoDungeon/Art/Characters/Assassin/Abilities";
        const string Anim = "Assets/_ArcanoDungeon/Animators/Assassin/";
        public const string KnifePath = Folder + "/PoisonKnife.prefab";

        [MenuItem("Tools/Arcano/Assassin/Update poison and shadow abilities")]
        public static void Setup()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var texture = SoftTexture();
            var smoke = ParticleMaterial("ShadowSmoke", texture);
            var green = ParticleMaterial("PoisonMist", texture);
            var shadow = Effect("ShadowCloak", smoke, new Color(.09f, .025f, .19f, .55f), .28f, 28f, new Vector3(.27f, .65f, .25f));
            var poison = Effect("PoisonMotes", green, new Color(.18f, 1f, .12f, .8f), .075f, 12f, new Vector3(.24f, .6f, .24f));
            var projectile = BuildKnife(poison, green);
            BuildThrowAnimations();
            var root = PrefabUtility.LoadPrefabContents(AssassinIntegration.PlayerPath);
            try
            {
                var abilities = root.GetComponent<AssassinAbilities>() ?? root.AddComponent<AssassinAbilities>();
                var settings = new SerializedObject(abilities);
                settings.FindProperty("knifePrefab").objectReferenceValue = projectile;
                settings.FindProperty("shadowPrefab").objectReferenceValue = shadow;
                settings.FindProperty("shadowScale").floatValue = AssassinSizing.Multiplier;
                settings.FindProperty("throwingHand").objectReferenceValue = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Hand.R");
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, AssassinIntegration.PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(AssassinSetup.ReportDir);
            File.WriteAllText(AssassinSetup.ReportDir + "/abilities-setup.txt", "Ready: poison knife prefab, throw animations, shadow cloak and playable prefab.");
            Debug.Log("[Assassin] Poison knives and shadow stealth configured.");
        }

        static Texture2D SoftTexture()
        {
            var path = Folder + "/SoftSmoke.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null) return texture;
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "SoftSmoke", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                var delta = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f);
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - delta.sqrMagnitude), 2));
            }
            texture.SetPixels(pixels); texture.Apply(); AssetDatabase.CreateAsset(texture, path); return texture;
        }

        static Material ParticleMaterial(string name, Texture2D texture)
        {
            var path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            EditorUtility.SetDirty(material); return material;
        }

        static ParticleSystem Effect(string name, Material material, Color color, float size, float rate, Vector3 scale)
        {
            var go = new GameObject(name); go.transform.localPosition = Vector3.up * .75f;
            var particles = go.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.loop = true; main.startLifetime = .75f; main.startSize = size; main.startSpeed = .12f;
            main.startColor = color; main.maxParticles = 96; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission; emission.rateOverTime = rate;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1f; shape.scale = scale;
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Folder + "/" + name + ".prefab");
            Object.DestroyImmediate(go); return prefab.GetComponent<ParticleSystem>();
        }

        static PoisonKnifeProjectile BuildKnife(ParticleSystem poison, Material trailMaterial)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssassinIntegration.PreviewPath));
            model.transform.localScale = Vector3.one * AssassinSizing.VisualScale;
            var source = model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Daga_R");
            var socket = model.GetComponentsInChildren<Transform>().First(t => t.name == "Dagger.R");
            var rotation = Quaternion.FromToRotation(socket.up, Vector3.forward);
            var mesh = Object.Instantiate(source.sharedMesh); mesh.name = "ThrownPoisonKnife";
            var vertices = mesh.vertices; var weights = mesh.boneWeights; var bind = mesh.bindposes;
            var matrices = source.bones.Select((b, i) => b.localToWorldMatrix * bind[i]).ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i]; var v = vertices[i];
                var world = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                vertices[i] = rotation * (world - socket.position);
            }
            mesh.vertices = vertices; mesh.boneWeights = Array.Empty<BoneWeight>(); mesh.bindposes = Array.Empty<Matrix4x4>();
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var meshPath = Folder + "/PoisonKnifeMesh.asset";
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
            else { EditorUtility.CopySerialized(mesh, savedMesh); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(savedMesh); }
            var materials = source.sharedMaterials.ToArray();
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].name != "Acero_filo") continue;
                var path = Folder + "/PoisonBlade.mat"; var blade = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (blade == null) { blade = new Material(materials[i]); AssetDatabase.CreateAsset(blade, path); }
                blade.SetColor("_BaseColor", new Color(.25f, .8f, .4f)); blade.SetColor("_EmissionColor", new Color(.08f, .65f, .12f)); blade.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(blade);
                materials[i] = blade;
            }
            var go = new GameObject("PoisonKnife");
            go.AddComponent<MeshFilter>().sharedMesh = savedMesh; go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var projectile = go.AddComponent<PoisonKnifeProjectile>(); var settings = new SerializedObject(projectile);
            settings.FindProperty("poisonEffect").objectReferenceValue = poison; settings.ApplyModifiedPropertiesWithoutUndo();
            var trail = go.AddComponent<TrailRenderer>(); trail.sharedMaterial = trailMaterial; trail.time = .14f; trail.startWidth = .055f; trail.endWidth = 0f;
            trail.startColor = new Color(.15f, 1f, .25f, .8f); trail.endColor = new Color(.1f, .6f, .2f, 0); trail.minVertexDistance = .015f; trail.shadowCastingMode = ShadowCastingMode.Off;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, KnifePath); Object.DestroyImmediate(go); Object.DestroyImmediate(model);
            return prefab.GetComponent<PoisonKnifeProjectile>();
        }

        static void BuildThrowAnimations()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "AssassinGameplay.controller");
            var machine = controller.layers[0].stateMachine;
            foreach (var hidden in new[] { false, true })
            {
                var name = hidden ? "ThrowKnifeHidden" : "ThrowKnife";
                var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + (hidden ? "Assassin_CrouchIdle.anim" : "Assassin_Idle.anim"));
                var path = Anim + name + ".anim"; var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
                EditorUtility.CopySerialized(source, clip); clip.name = name;
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; settings.loopBlend = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
                AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
                Curve(clip, "Right Arm Down-Up", -.4f, .55f, .05f, -.4f);
                Curve(clip, "Right Arm Front-Back", -.15f, -.65f, .9f, -.15f);
                Curve(clip, "Right Arm Twist In-Out", -.1f, -.5f, .2f, -.1f);
                Curve(clip, "Right Forearm Stretch", -.1f, -.8f, .7f, -.1f);
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
                state.motion = clip; state.speed = clip.length / .45f; state.iKOnFeet = false;
                foreach (var old in state.transitions) state.RemoveTransition(old);
                var destination = machine.states.First(s => s.state.name == (hidden ? "CrouchIdle" : "Idle")).state;
                var transition = state.AddTransition(destination); transition.hasExitTime = true; transition.exitTime = .93f; transition.duration = .08f; transition.hasFixedDuration = true;
                EditorUtility.SetDirty(clip);
            }
            EditorUtility.SetDirty(controller);
        }

        static void Curve(AnimationClip clip, string muscle, float a, float b, float c, float d)
        {
            float duration = clip.length;
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), new AnimationCurve(new Keyframe(0, a), new Keyframe(duration * .3f, b), new Keyframe(duration * .52f, c), new Keyframe(duration, d)));
        }
    }
}

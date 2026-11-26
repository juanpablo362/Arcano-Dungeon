using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArcanoDungeon.Features.Assassin.Editor
{
    public static class AssassinSetup
    {
        public const string ModelPath = "Assets/_ArcanoDungeon/Art/Characters/Assassin/Assassin.fbx";
        public const string ReportDir = "AssassinValidation";

        [MenuItem("Tools/Arcano/Assassin/Import and validate rig")]
        public static void ImportRig()
        {
            Directory.CreateDirectory(ReportDir);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = true;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = Object.Instantiate(model);
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            var mapping = new Dictionary<string, string>
            {
                {"Hips", "Hips"}, {"Spine", "Spine"}, {"Chest", "Chest"}, {"UpperChest", "UpperChest"},
                {"Neck", "Neck"}, {"Head", "Head"}
            };
            foreach (var side in new[] { "Left", "Right" })
            {
                var suffix = side == "Left" ? ".L" : ".R";
                foreach (var part in new[] { "Shoulder", "UpperArm", "LowerArm", "Hand", "UpperLeg", "LowerLeg", "Foot", "Toes" })
                    mapping[side + part] = part + suffix;
                var fingers = new[] { "Thumb", "Index", "Middle", "Ring", "Little" };
                var source = new[] { "thumb", "f_index", "f_middle", "f_ring", "f_pinky" };
                var joints = new[] { "Proximal", "Intermediate", "Distal" };
                for (int f = 0; f < fingers.Length; f++)
                    for (int j = 0; j < joints.Length; j++)
                        mapping[side + fingers[f] + joints[j]] = source[f] + ".0" + (j + 1) + suffix;
            }
            var description = importer.humanDescription;
            description.human = mapping.Select(k => new HumanBone
            {
                humanName = HumanTrait.BoneName[(int)Enum.Parse(typeof(HumanBodyBones), k.Key)],
                boneName = k.Value,
                limit = new HumanLimit { useDefaultValues = true }
            }).ToArray();
            description.skeleton = transforms.Select(t => new SkeletonBone
            {
                name = t == instance.transform ? model.name : t.name,
                position = t.localPosition, rotation = t.localRotation, scale = t.localScale
            }).ToArray();
            description.armStretch = .05f;
            description.legStretch = .05f;
            description.upperArmTwist = .5f;
            description.lowerArmTwist = .5f;
            description.upperLegTwist = .5f;
            description.lowerLegTwist = .5f;
            Object.DestroyImmediate(instance);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.humanDescription = description;
            importer.SaveAndReimport();
            model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            var report = new System.Text.StringBuilder();
            report.AppendLine("Avatar valid: " + (avatar != null && avatar.isValid) + "; humanoid: " + (avatar != null && avatar.isHuman));
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                report.AppendLine("BONE " + t.name + " p=" + t.localPosition.ToString("F4") + " r=" + t.localEulerAngles.ToString("F2"));
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                report.AppendLine("MESH " + r.name + " bounds " + r.bounds + " vertices " + r.sharedMesh.vertexCount);
                foreach (var mat in r.sharedMaterials)
                    report.AppendLine("MAT " + mat.name + " color=" + mat.color + " texture=" + mat.mainTexture?.name);
            }
            File.WriteAllText(ReportDir + "/rig.txt", report.ToString());
            Debug.Log("[Assassin] " + report.ToString().Split('\n')[0]);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new Exception("Assassin avatar is invalid. See AssassinValidation/rig.txt.");
        }
    }
}

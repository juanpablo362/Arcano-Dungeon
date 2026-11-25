using System.IO;
using ArcanoDungeon.Features.Assassin;
using ArcanoDungeon.Features.Assassin.Editor;
using ArcanoDungeon.Features.Player.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArcanoDungeon.Features.HUD.Editor
{
    public static class GamePresentationSetup
    {
        public const string HudPath = "Assets/_ArcanoDungeon/Resources/UI/PlayerVitalsHUD.prefab";
        public static readonly Color HealthColor = new Color(.80f, .18f, .25f);
        public static readonly Color ShieldColor = new Color(.28f, .69f, .95f);
        public static readonly Color StaminaColor = new Color(.94f, .76f, .22f);

        [MenuItem("Tools/Arcano/Update shared HUD and assassin size")]
        public static void Apply()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HudPath)); AssetDatabase.Refresh();
            BuildHud(); ApplyAssassinSize(); AssassinAbilitySetup.Setup();
            AssetDatabase.SaveAssets(); Directory.CreateDirectory("AssassinValidation");
            File.WriteAllText("AssassinValidation/presentation-setup.txt", "Shared HUD ready for Level1, CatacombsLevel and CastleLevel. Assassin size multiplier: 1.15.");
        }

        static void BuildHud()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("PlayerVitalsHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                SceneManager.MoveGameObjectToScene(root, preview); root.layer = 5;
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
                var panel = Rect(root.transform, "VitalsPanel", new Vector2(360, 136));
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1); panel.anchoredPosition = new Vector2(24, -24);
                var border = panel.gameObject.AddComponent<Image>(); border.color = new Color(.55f, .44f, .24f, .95f); border.raycastTarget = false;
                var background = Rect(panel, "Background", Vector2.zero); Stretch(background, 1);
                var backgroundImage = background.gameObject.AddComponent<Image>(); backgroundImage.color = new Color(.025f, .03f, .055f, .94f); backgroundImage.raycastTarget = false;
                var component = root.AddComponent<PlayerVitalsHud>(); var settings = new SerializedObject(component);
                Row(background, settings, "Health", "VIDA", 14, HealthColor, 110);
                Row(background, settings, "Shield", "ESCUDO", 55, ShieldColor, 30);
                Row(background, settings, "Stamina", "ESTAMINA", 96, StaminaColor, 100);
                settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        static void Row(Transform parent, SerializedObject settings, string id, string caption, float offset, Color color, float maximum)
        {
            var rect = Rect(parent, "Player" + id + "Bar", new Vector2(328, 26));
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(15, -offset);
            var track = rect.gameObject.AddComponent<Image>(); track.color = new Color(.12f, .13f, .17f); track.raycastTarget = false;
            var area = Rect(rect, "FillArea", Vector2.zero); Stretch(area, 2);
            var fill = Rect(area, "Fill", Vector2.zero); Stretch(fill, 0);
            var image = fill.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            var slider = rect.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.interactable = false;
            slider.transition = Selectable.Transition.None; slider.navigation = new Navigation { mode = Navigation.Mode.None }; slider.maxValue = maximum; slider.value = maximum;
            Text(rect, id + "Caption", caption, false, 14);
            var value = Text(rect, "Player" + id + "Text", maximum + " / " + maximum, true, 17);
            var prefix = char.ToLowerInvariant(id[0]) + id.Substring(1);
            settings.FindProperty(prefix + "Bar").objectReferenceValue = slider;
            settings.FindProperty(prefix + "Text").objectReferenceValue = value;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string content, bool right, float size)
        {
            var rect = Rect(parent, name, Vector2.zero); Stretch(rect, 0); rect.offsetMin = new Vector2(10, 0); rect.offsetMax = new Vector2(-10, 0);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
            text.text = content; text.fontSize = size; text.fontStyle = FontStyles.Bold;
            text.alignment = right ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            text.color = new Color(.98f, .97f, .94f); text.raycastTarget = false;
            const string textMaterialPath = "Assets/_ArcanoDungeon/Resources/UI/PlayerVitalsText.mat";
            var textMaterial = AssetDatabase.LoadAssetAtPath<Material>(textMaterialPath);
            if (textMaterial == null) { textMaterial = new Material(text.fontSharedMaterial); AssetDatabase.CreateAsset(textMaterial, textMaterialPath); }
            textMaterial.SetColor("_FaceColor", Color.white);
            textMaterial.SetColor("_OutlineColor", new Color(.01f, .015f, .025f));
            textMaterial.SetFloat("_OutlineWidth", .12f);
            EditorUtility.SetDirty(textMaterial); text.fontSharedMaterial = textMaterial;
            var shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .85f); shadow.effectDistance = new Vector2(1, -1);
            return text;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = size; return rect;
        }

        static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin; rect.offsetMax = -Vector2.one * margin;
        }

        static void ApplyAssassinSize()
        {
            var root = PrefabUtility.LoadPrefabContents(AssassinIntegration.PlayerPath);
            try
            {
                root.transform.Find("Visual").localScale = Vector3.one * AssassinSizing.VisualScale;
                var cc = root.GetComponent<CharacterController>(); cc.height = AssassinSizing.CapsuleHeight; cc.radius = AssassinSizing.CapsuleRadius;
                cc.center = Vector3.up * (cc.height * .5f); cc.skinWidth = .035f * AssassinSizing.Multiplier; cc.stepOffset = .25f * AssassinSizing.Multiplier;
                var box = root.GetComponent<BoxCollider>(); box.size = new Vector3(.46f, 1.45f, .46f) * AssassinSizing.Multiplier; box.center = cc.center;
                root.GetComponent<PlayerController>().crouchHeight = AssassinSizing.CrouchHeight;
                var camera = new SerializedObject(root.GetComponent<PlayerCameraBootstrap>());
                camera.FindProperty("followHeightOverride").floatValue = AssassinSizing.FollowHeight;
                camera.FindProperty("cameraTargetLocalPosition").vector3Value = Vector3.up * (.95f * AssassinSizing.Multiplier);
                camera.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, AssassinIntegration.PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}

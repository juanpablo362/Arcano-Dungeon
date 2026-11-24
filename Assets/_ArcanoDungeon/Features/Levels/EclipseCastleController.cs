using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Infrastructure.Audio;
using ArcanoDungeon.Infrastructure.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcanoDungeon.Features.Levels
{
    public sealed class EclipseCastleController : MonoBehaviour
    {
        public RoomController[] sealRooms;
        public GameObject[] sealBeacons;
        public RoomController throneRoom;
        public GameObject throneBarrier;
        public bool Victory { get; private set; }
        public int SealsBroken { get; private set; }

        static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f);
        static readonly Color Ink = new Color(0.94f, 0.91f, 0.82f);
        static readonly Color Panel = new Color(0.055f, 0.045f, 0.10f, 0.94f);
        Canvas canvas;
        Text objective;
        Text bossLabel;
        Image bossFill;
        RectTransform bossPanel;
        EnemyHealth boss;

        void OnEnable() => RoomController.RoomCleared += OnRoomCleared;
        void OnDisable() => RoomController.RoomCleared -= OnRoomCleared;

        void Start()
        {
            BuildHud();
            RefreshSeals();
        }

        void OnDestroy()
        {
            if (Victory)
                Time.timeScale = 1f;
        }

        void OnRoomCleared(RoomController room)
        {
            RefreshSeals();
            if (room == throneRoom && objective != null)
                objective.text = "El Regente ha caído. Cruza el portal y disipa el eclipse.";
        }

        void RefreshSeals()
        {
            SealsBroken = 0;
            if (sealRooms != null)
                for (int i = 0; i < sealRooms.Length; i++)
                {
                    bool cleared = sealRooms[i] != null && sealRooms[i].IsCleared;
                    if (cleared) SealsBroken++;
                    if (sealBeacons != null && i < sealBeacons.Length && sealBeacons[i] != null)
                        sealBeacons[i].SetActive(!cleared);
                }
            bool unlocked = sealRooms != null && sealRooms.Length > 0 && SealsBroken == sealRooms.Length;
            if (throneBarrier != null)
                throneBarrier.SetActive(!unlocked);
            if (objective != null)
                objective.text = unlocked ? "El trono está abierto. Derrota al Regente del Eclipse."
                    : $"Rompe los sellos de la Armería y la Capilla  ·  {SealsBroken}/2";
        }

        void Update()
        {
            if (Victory || throneRoom == null || !throneRoom.HasTriggered)
                return;
            if (boss == null && !throneRoom.IsCleared)
            {
                var attack = FindAnyObjectByType<EclipseBossAttack>();
                if (attack != null) boss = attack.GetComponent<EnemyHealth>();
            }
            bool show = boss != null && !boss.IsDead && !throneRoom.IsCleared;
            bossPanel.gameObject.SetActive(show);
            if (show)
            {
                bossFill.fillAmount = boss.maxHealth > 0 ? (float)boss.currentHealth / boss.maxHealth : 0f;
                bossLabel.text = $"REGENTE DEL ECLIPSE   {boss.currentHealth} / {boss.maxHealth}";
            }
        }

        public void CompleteRun()
        {
            if (Victory || throneRoom == null || !throneRoom.IsCleared)
                return;
            Victory = true;
            foreach (var pause in FindObjectsByType<PauseMenuController>())
                pause.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>())
                input.enabled = false;
            var follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null) follow.enabled = false;
            Time.timeScale = 0f;
            CameraFollow.SetCursorLocked(false);
            var clip = DungeonSfxCatalog.Instance != null ? DungeonSfxCatalog.Instance.chestOpen : null;
            GameSfx.Play2D(clip, 0.8f);

            var backdrop = Rect("Victory", canvas.transform, new Vector2(1920f, 1080f));
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.sizeDelta = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.018f, 0.012f, 0.04f, 0.90f);
            var panel = Rect("ResultPanel", backdrop, new Vector2(680f, 390f));
            panel.gameObject.AddComponent<Image>().color = Panel;
            Label(panel, "III · CASTILLO ECLIPSE", 22, Gold, new Vector2(0f, 140f), new Vector2(620f, 35f));
            Label(panel, "ECLIPSE DISIPADO", 42, Ink, new Vector2(0f, 83f), new Vector2(640f, 70f));
            Label(panel, $"El castillo vuelve a ver la luz.\nTesoro de la expedición: {RunScore.Current} monedas", 23, Ink,
                new Vector2(0f, 8f), new Vector2(620f, 80f));
            Button(panel, "NUEVA PARTIDA", new Vector2(-157f, -113f), "CharacterSelection");
            Button(panel, "VOLVER AL MENÚ", new Vector2(157f, -113f), "MainMenu");
        }

        void BuildHud()
        {
            var root = new GameObject("EclipseObjectives", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 550;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var top = Rect("FloorTitle", root.transform, new Vector2(730f, 92f));
            top.anchorMin = top.anchorMax = new Vector2(0.5f, 1f);
            top.anchoredPosition = new Vector2(0f, -63f);
            var image = top.gameObject.AddComponent<Image>();
            image.color = Panel;
            image.raycastTarget = false;
            Label(top, "III  ·  CASTILLO ECLIPSE", 25, Gold, new Vector2(0f, 20f), new Vector2(710f, 35f));
            objective = Label(top, "", 20, Ink, new Vector2(0f, -20f), new Vector2(710f, 35f));
            bossPanel = Rect("BossHealth", root.transform, new Vector2(620f, 60f));
            bossPanel.anchorMin = bossPanel.anchorMax = new Vector2(0.5f, 0f);
            bossPanel.anchoredPosition = new Vector2(0f, 100f);
            bossPanel.gameObject.AddComponent<Image>().color = Panel;
            bossLabel = Label(bossPanel, "REGENTE DEL ECLIPSE", 19, Ink, new Vector2(0f, 13f), new Vector2(600f, 30f));
            var bar = Rect("Fill", bossPanel, new Vector2(590f, 9f));
            bar.anchoredPosition = new Vector2(0f, -15f);
            bossFill = bar.gameObject.AddComponent<Image>();
            bossFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            bossFill.color = Gold;
            bossFill.type = Image.Type.Filled;
            bossFill.fillMethod = Image.FillMethod.Horizontal;
            bossFill.fillOrigin = 0;
            bossPanel.gameObject.SetActive(false);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        static Text Label(Transform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var rect = Rect("Label", parent, dimensions);
            rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        static void Button(Transform parent, string text, Vector2 position, string scene)
        {
            var rect = Rect(text, parent, new Vector2(294f, 56f));
            rect.anchoredPosition = position;
            rect.gameObject.AddComponent<Image>().color = Gold;
            Label(rect, text, 20, Panel, Vector2.zero, new Vector2(286f, 52f));
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                GameSession.ResetRun();
                SceneManager.LoadScene(scene);
            });
        }
    }
}

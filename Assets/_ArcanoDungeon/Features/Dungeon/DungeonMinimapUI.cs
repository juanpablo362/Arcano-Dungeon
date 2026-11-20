using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcanoDungeon.Features.Dungeon
{
    /// <summary>
    /// Minimapa estilo Soul Knight: nodos conectados, sala actual resaltada, iconos especiales.
    /// </summary>
    public class DungeonMinimapUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] Vector2 panelSize = new Vector2(240f, 200f);
        [SerializeField] Vector2 panelOffset = new Vector2(-20f, -20f);
        [SerializeField] float nodeSize = 20f;
        [SerializeField] float mapPadding = 28f;
        [SerializeField] string floorLabel = "1-1";

        [Header("Colors")]
        [SerializeField] Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.78f);
        [SerializeField] Color unexploredColor = new Color(0.28f, 0.28f, 0.32f, 1f);
        [SerializeField] Color exploredColor = new Color(0.88f, 0.88f, 0.9f, 1f);
        [SerializeField] Color currentColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] Color bossColor = new Color(0.95f, 0.28f, 0.28f, 1f);
        [SerializeField] Color treasureColor = new Color(0.35f, 0.7f, 1f, 1f);
        [SerializeField] Color linkColor = new Color(0.6f, 0.6f, 0.65f, 0.95f);

        static Sprite whiteSprite;

        readonly Dictionary<RoomController, RectTransform> nodeByRoom = new Dictionary<RoomController, RectTransform>();
        readonly HashSet<RoomController> explored = new HashSet<RoomController>();

        RoomController current;
        RectTransform mapRoot;
        bool built;

        void OnEnable()
        {
            RoomController.RoomEntered += HandleRoomEntered;
            RoomController.RoomCleared += HandleRoomCleared;
        }

        void OnDisable()
        {
            RoomController.RoomEntered -= HandleRoomEntered;
            RoomController.RoomCleared -= HandleRoomCleared;
        }

        void Start()
        {
            EnsureUi();
            RebuildGraph();
            StartCoroutine(BootstrapWhenPlayerReady());
        }

        IEnumerator BootstrapWhenPlayerReady()
        {
            for (int i = 0; i < 30; i++)
            {
                if (GameObject.FindGameObjectWithTag("Player") != null)
                    break;
                yield return null;
            }

            BootstrapCurrentRoom();
        }

        void BootstrapCurrentRoom()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            Vector3 p = player.transform.position;
            RoomController best = null;
            float bestDist = float.MaxValue;
            foreach (var room in Object.FindObjectsByType<RoomController>(FindObjectsInactive.Exclude))
            {
                if (room == null)
                    continue;

                var box = room.GetComponent<BoxCollider>();
                if (box != null)
                {
                    Vector3 local = room.transform.InverseTransformPoint(p) - box.center;
                    Vector3 half = box.size * 0.5f;
                    if (Mathf.Abs(local.x) <= half.x
                        && Mathf.Abs(local.y) <= half.y + 2f
                        && Mathf.Abs(local.z) <= half.z)
                    {
                        HandleRoomEntered(room);
                        return;
                    }
                }

                float d = (room.transform.position - p).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = room;
                }
            }

            if (best != null && bestDist < 18f * 18f)
                HandleRoomEntered(best);
        }

        void EnsureUi()
        {
            if (built && mapRoot != null)
                return;

            // Limpiar builds rotos de intentos previos.
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            var canvasGo = new GameObject("DungeonMinimapCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = CreateUiObject("MinimapPanel", canvasGo.transform);
            var panel = panelGo.AddComponent<Image>();
            panel.sprite = WhiteSprite();
            panel.type = Image.Type.Simple;
            panel.color = panelColor;
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.sizeDelta = panelSize;
            panelRt.anchoredPosition = panelOffset;

            mapRoot = CreateUiObject("MapRoot", panelGo.transform).GetComponent<RectTransform>();
            mapRoot.anchorMin = new Vector2(0.5f, 0.56f);
            mapRoot.anchorMax = new Vector2(0.5f, 0.56f);
            mapRoot.pivot = new Vector2(0.5f, 0.5f);
            mapRoot.sizeDelta = panelSize - new Vector2(20f, 44f);
            mapRoot.anchoredPosition = Vector2.zero;

            var labelGo = CreateUiObject("FloorLabel", panelGo.transform);
            var floorText = labelGo.AddComponent<Text>();
            floorText.font = ResolveUiFont();
            floorText.fontSize = 18;
            floorText.alignment = TextAnchor.LowerCenter;
            floorText.color = new Color(0.92f, 0.92f, 0.95f, 0.95f);
            floorText.text = floorLabel;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.sizeDelta = new Vector2(0f, 28f);
            labelRt.anchoredPosition = new Vector2(0f, 6f);

            built = true;
        }

        static Font ResolveUiFont()
        {
            // Unity 6+: Arial.ttf ya no es válido y lanza excepción.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;

            try
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 16);
            }
            catch
            {
                font = null;
            }

            return font;
        }

        void RebuildGraph()
        {
            EnsureUi();
            if (mapRoot == null)
                return;

            foreach (Transform child in mapRoot)
                Destroy(child.gameObject);
            nodeByRoom.Clear();

            var rooms = Object.FindObjectsByType<RoomController>(FindObjectsInactive.Include);
            if (rooms == null || rooms.Length == 0)
                return;

            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var room in rooms)
            {
                Vector3 p = room.transform.position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }

            float worldW = Mathf.Max(1f, maxX - minX);
            float worldH = Mathf.Max(1f, maxZ - minZ);
            Vector2 center = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);

            Vector2 area = mapRoot.sizeDelta - Vector2.one * (mapPadding * 2f);
            float scale = Mathf.Min(area.x / worldW, area.y / worldH);
            scale = Mathf.Clamp(scale, 0.8f, 6f);

            foreach (var room in rooms)
            {
                Vector3 wp = room.transform.position;
                Vector2 local = new Vector2((wp.x - center.x) * scale, (wp.z - center.y) * scale);
                nodeByRoom[room] = CreateNode(room, local);
            }

            var linked = new HashSet<string>();
            foreach (var room in rooms)
            {
                if (room.connectedRooms == null)
                    continue;
                foreach (var other in room.connectedRooms)
                {
                    if (other == null || other == room)
                        continue;
                    string key = PackKey(room, other);
                    if (!linked.Add(key))
                        continue;
                    if (!nodeByRoom.TryGetValue(room, out var a) || !nodeByRoom.TryGetValue(other, out var b))
                        continue;
                    CreateLink(a.anchoredPosition, b.anchoredPosition);
                }
            }

            RefreshVisuals();
        }

        RectTransform CreateNode(RoomController room, Vector2 anchored)
        {
            var go = CreateUiObject("Node_" + room.name, mapRoot);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.type = Image.Type.Simple;
            img.color = unexploredColor;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = Vector2.one * nodeSize;
            rt.anchoredPosition = anchored;

            if (room.roomKind == RoomController.RoomKind.Boss
                || room.roomKind == RoomController.RoomKind.Treasure)
            {
                var badge = CreateUiObject("Badge", go.transform);
                var badgeImg = badge.AddComponent<Image>();
                badgeImg.sprite = WhiteSprite();
                badgeImg.color = room.roomKind == RoomController.RoomKind.Boss ? bossColor : treasureColor;
                var brt = badge.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = Vector2.one * (nodeSize * 0.45f);
                brt.anchoredPosition = Vector2.zero;
            }

            return rt;
        }

        void CreateLink(Vector2 from, Vector2 to)
        {
            var go = CreateUiObject("Link", mapRoot);
            go.transform.SetAsFirstSibling();
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = linkColor;
            var rt = go.GetComponent<RectTransform>();
            Vector2 delta = to - from;
            float dist = Mathf.Max(2f, delta.magnitude);
            rt.sizeDelta = new Vector2(dist, 3.5f);
            rt.anchoredPosition = (from + to) * 0.5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void HandleRoomEntered(RoomController room)
        {
            if (room == null)
                return;
            explored.Add(room);
            current = room;
            RefreshVisuals();
        }

        void HandleRoomCleared(RoomController room)
        {
            if (room == null)
                return;
            explored.Add(room);
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            foreach (var kv in nodeByRoom)
            {
                var room = kv.Key;
                var rt = kv.Value;
                if (rt == null)
                    continue;
                var img = rt.GetComponent<Image>();
                if (img == null)
                    continue;

                if (room == current)
                    img.color = currentColor;
                else if (explored.Contains(room) || room.IsCleared || room.HasTriggered)
                    img.color = exploredColor;
                else
                    img.color = unexploredColor;
            }
        }

        static string PackKey(RoomController a, RoomController b)
        {
            string na = a.name;
            string nb = b.name;
            return string.CompareOrdinal(na, nb) <= 0 ? na + "|" + nb : nb + "|" + na;
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Sprite WhiteSprite()
        {
            if (whiteSprite != null)
                return whiteSprite;

            var tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            whiteSprite.name = "UIWhite";
            return whiteSprite;
        }
    }
}

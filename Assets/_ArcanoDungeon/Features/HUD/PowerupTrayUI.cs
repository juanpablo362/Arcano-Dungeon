using ArcanoDungeon.Features.Loot;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcanoDungeon.Features.HUD
{
    /// <summary>
    /// Fila de iconos: pasivos + arma temporal con countdown, abajo a la izquierda.
    /// </summary>
    public class PowerupTrayUI : MonoBehaviour
    {
        const float Slot = 52f;
        const float Gap = 8f;

        static Sprite white;
        PlayerItemInventory boundInventory;
        TemporaryWeaponController boundWeapon;
        TextMeshProUGUI weaponTimerLabel;
        Image weaponTimerFill;
        GameObject weaponSlotRoot;
        float lastShownRemain = -1f;

        RectTransform row;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ListenForScenes()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<PowerupTrayUI>(FindObjectsInactive.Include) == null)
                CreateRuntime();
        }

        static void CreateRuntime()
        {
            var canvasGo = new GameObject("PowerupTrayCanvas");
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<PowerupTrayUI>();
        }

        void Awake()
        {
            var rowGo = new GameObject("Powerups");
            rowGo.transform.SetParent(transform, false);
            row = rowGo.AddComponent<RectTransform>();
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(0f, 0f);
            row.pivot = new Vector2(0f, 0f);
            row.anchoredPosition = new Vector2(28f, 28f);
            row.sizeDelta = new Vector2(720f, Slot);
        }

        void OnDestroy()
        {
            Unbind();
        }

        void Unbind()
        {
            if (boundInventory != null)
                boundInventory.Changed -= Rebuild;
            if (boundWeapon != null)
                boundWeapon.Changed -= Rebuild;
            boundInventory = null;
            boundWeapon = null;
        }

        void Update()
        {
            var inventory = FindInventory();
            var weapon = FindWeapon();

            if (inventory != boundInventory || weapon != boundWeapon)
            {
                Unbind();
                boundInventory = inventory;
                boundWeapon = weapon;
                if (boundInventory != null)
                    boundInventory.Changed += Rebuild;
                if (boundWeapon != null)
                    boundWeapon.Changed += Rebuild;
                Rebuild();
            }

            RefreshWeaponTimer();
        }

        static PlayerItemInventory FindInventory()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return null;
            return player.GetComponent<PlayerItemInventory>()
                   ?? player.GetComponentInParent<PlayerItemInventory>();
        }

        static TemporaryWeaponController FindWeapon()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return null;
            return player.GetComponent<TemporaryWeaponController>()
                   ?? player.GetComponentInParent<TemporaryWeaponController>();
        }

        void Rebuild()
        {
            if (row == null)
                return;

            for (int i = row.childCount - 1; i >= 0; i--)
                Destroy(row.GetChild(i).gameObject);

            weaponSlotRoot = null;
            weaponTimerLabel = null;
            weaponTimerFill = null;
            lastShownRemain = -1f;

            int index = 0;
            if (boundInventory != null)
            {
                var items = boundInventory.Passives;
                for (int i = 0; i < items.Count; i++)
                {
                    int stacks = boundInventory.GetStackCount(items[i]);
                    CreateSlot(items[i], index, false, null, stacks);
                    index++;
                }
            }

            if (boundWeapon != null && boundWeapon.IsActive && boundWeapon.ActiveItem != null)
                CreateSlot(boundWeapon.ActiveItem, index, true, boundWeapon, 1);
        }

        void CreateSlot(ItemData item, int index, bool isWeapon, TemporaryWeaponController weapon, int stacks)
        {
            var frame = new GameObject(item != null ? item.id : "slot");
            frame.transform.SetParent(row, false);
            var frameRect = frame.AddComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0f, 0f);
            frameRect.anchorMax = new Vector2(0f, 0f);
            frameRect.pivot = new Vector2(0f, 0f);
            frameRect.anchoredPosition = new Vector2(index * (Slot + Gap), 0f);
            frameRect.sizeDelta = new Vector2(Slot, isWeapon ? Slot + 18f : Slot);
            var border = frame.AddComponent<Image>();
            border.sprite = White();
            border.color = isWeapon ? new Color(0.92f, 0.72f, 0.28f, 1f) : FrameColor(item);
            border.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(frame.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, isWeapon ? 18f : 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = White();
            fill.color = new Color(0.08f, 0.07f, 0.06f, 0.94f);
            fill.raycastTarget = false;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(fillGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var icon = iconGo.AddComponent<Image>();
            var sprite = IconFor(item);
            icon.sprite = sprite != null ? sprite : White();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = sprite != null ? Color.white : FrameColor(item);

            if (!isWeapon && stacks > 1)
            {
                var badgeGo = new GameObject("Stack");
                badgeGo.transform.SetParent(frame.transform, false);
                var badgeRect = badgeGo.AddComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 0f);
                badgeRect.anchorMax = new Vector2(1f, 0f);
                badgeRect.pivot = new Vector2(1f, 0f);
                badgeRect.anchoredPosition = new Vector2(-2f, 2f);
                badgeRect.sizeDelta = new Vector2(22f, 16f);
                var badgeBg = badgeGo.AddComponent<Image>();
                badgeBg.sprite = White();
                badgeBg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
                badgeBg.raycastTarget = false;

                var badgeLabelGo = new GameObject("Label");
                badgeLabelGo.transform.SetParent(badgeGo.transform, false);
                var badgeLabelRect = badgeLabelGo.AddComponent<RectTransform>();
                badgeLabelRect.anchorMin = Vector2.zero;
                badgeLabelRect.anchorMax = Vector2.one;
                badgeLabelRect.offsetMin = Vector2.zero;
                badgeLabelRect.offsetMax = Vector2.zero;
                var badgeLabel = badgeLabelGo.AddComponent<TextMeshProUGUI>();
                badgeLabel.fontSize = 12f;
                badgeLabel.alignment = TextAlignmentOptions.Center;
                badgeLabel.color = Color.white;
                badgeLabel.raycastTarget = false;
                badgeLabel.text = $"x{stacks}";
            }

            if (!isWeapon || weapon == null)
                return;

            weaponSlotRoot = frame;

            var barBgGo = new GameObject("TimerBg");
            barBgGo.transform.SetParent(frame.transform, false);
            var barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0f);
            barBgRect.pivot = new Vector2(0.5f, 0f);
            barBgRect.anchoredPosition = new Vector2(0f, 3f);
            barBgRect.sizeDelta = new Vector2(-6f, 12f);
            var barBg = barBgGo.AddComponent<Image>();
            barBg.sprite = White();
            barBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            barBg.raycastTarget = false;

            var barFillGo = new GameObject("TimerFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.anchorMin = new Vector2(0f, 0f);
            barFillRect.anchorMax = new Vector2(1f, 1f);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            barFillRect.pivot = new Vector2(0f, 0.5f);
            weaponTimerFill = barFillGo.AddComponent<Image>();
            weaponTimerFill.sprite = White();
            weaponTimerFill.type = Image.Type.Filled;
            weaponTimerFill.fillMethod = Image.FillMethod.Horizontal;
            weaponTimerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            weaponTimerFill.color = new Color(0.95f, 0.78f, 0.28f, 1f);
            weaponTimerFill.raycastTarget = false;

            var labelGo = new GameObject("TimerLabel");
            labelGo.transform.SetParent(barBgGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            weaponTimerLabel = labelGo.AddComponent<TextMeshProUGUI>();
            weaponTimerLabel.fontSize = 11f;
            weaponTimerLabel.alignment = TextAlignmentOptions.Center;
            weaponTimerLabel.color = Color.white;
            weaponTimerLabel.raycastTarget = false;
            weaponTimerLabel.text = $"{Mathf.CeilToInt(weapon.Remaining)}s";
        }

        void RefreshWeaponTimer()
        {
            if (boundWeapon == null || !boundWeapon.IsActive)
            {
                if (weaponSlotRoot != null)
                    Rebuild();
                return;
            }

            if (weaponSlotRoot == null)
            {
                Rebuild();
                return;
            }

            float remain = boundWeapon.Remaining;
            if (weaponTimerFill != null)
                weaponTimerFill.fillAmount = boundWeapon.RemainingNormalized;

            int shown = Mathf.CeilToInt(remain);
            if (weaponTimerLabel != null && !Mathf.Approximately(shown, lastShownRemain))
            {
                lastShownRemain = shown;
                weaponTimerLabel.text = $"{shown}s";
            }
        }

        static Sprite IconFor(ItemData item)
        {
            if (item == null)
                return null;
            if (item.icon != null)
                return item.icon;

            string id = (item.id ?? item.name ?? "").ToLowerInvariant();
            string resource = "spark";
            if (item.type == ItemType.Weapon || id.Contains("sword") || id.Contains("arma") || id.Contains("mandoble")
                || id.Contains("gaunt") || id.Contains("ballesta") || id.Contains("grimorio") || id.Contains("ventisca"))
                resource = "swords";
            else if (id.Contains("shield") || id.Contains("escudo") || id.Contains("crystal_heart") || id.Contains("cristal"))
                resource = "shield";
            else if (id.Contains("holy") || id.Contains("aura") || id.Contains("fuego"))
                resource = "ward";
            else if (id.Contains("hermes") || id.Contains("bota"))
                resource = "shadow";
            else if (id.Contains("staff") || id.Contains("baston") || id.Contains("blizzard"))
                resource = "staff";
            else if (id.Contains("vampir") || id.Contains("colmillo"))
                resource = "spark";
            else if (id.Contains("multishot") || id.Contains("lente"))
                resource = "spark";

            var sprites = Resources.LoadAll<Sprite>(resource);
            if (sprites != null && sprites.Length > 0)
                return sprites[0];
            return null;
        }

        static Color FrameColor(ItemData item)
        {
            string id = item == null ? "" : (item.id ?? "").ToLowerInvariant();
            if (item != null && item.type == ItemType.Weapon)
                return new Color(0.92f, 0.72f, 0.28f, 1f);
            if (id.Contains("holy") || id.Contains("fuego"))
                return new Color(0.78f, 0.42f, 0.18f, 1f);
            if (id.Contains("chain") || id.Contains("rayo"))
                return new Color(0.85f, 0.72f, 0.28f, 1f);
            if (id.Contains("hermes") || id.Contains("bota"))
                return new Color(0.35f, 0.72f, 0.48f, 1f);
            if (id.Contains("shield") || id.Contains("escudo") || id.Contains("crystal"))
                return new Color(0.35f, 0.55f, 0.85f, 1f);
            if (id.Contains("vampir"))
                return new Color(0.75f, 0.2f, 0.28f, 1f);
            return new Color(0.45f, 0.75f, 0.82f, 1f);
        }

        static Sprite White()
        {
            if (white != null)
                return white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            white = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return white;
        }
    }
}

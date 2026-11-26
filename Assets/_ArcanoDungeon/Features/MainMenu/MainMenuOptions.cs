using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MainMenuOptions : MonoBehaviour
{
    const string VolumeKey = "Arcano.Settings.MasterVolume";
    const string FullscreenKey = "Arcano.Settings.Fullscreen";
    const string VSyncKey = "Arcano.Settings.VSync";

    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f);
    static readonly Color Ink = new Color(0.94f, 0.91f, 0.82f);
    static readonly Color Panel = new Color(0.05f, 0.06f, 0.1f, 0.98f);

    UnityEngine.UI.Slider volume;
    UnityEngine.UI.Text volumeText;
    UnityEngine.UI.Text displayText;
    UnityEngine.UI.Text vsyncText;

    static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
    static bool VSync => QualitySettings.vSyncCount > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySavedSettings()
    {
        if (PlayerPrefs.HasKey(VolumeKey))
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey));
        if (PlayerPrefs.HasKey(FullscreenKey))
            ApplyDisplayMode(Fullscreen);
        if (PlayerPrefs.HasKey(VSyncKey))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey) == 0 ? 0 : 1;
    }

    public static MainMenuOptions Create(Transform canvas, Action close)
    {
        var overlay = ControlsGuide.CreatePanel(canvas, "MainMenuOptions", Vector2.zero);
        ControlsGuide.Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.75f);
        var settings = overlay.AddComponent<MainMenuOptions>();
        settings.Build(close);
        return settings;
    }

    public void Show()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        volume.SetValueWithoutNotify(Mathf.RoundToInt(AudioListener.volume * 100f));
        RefreshLabels();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(volume.gameObject);
    }

    public void Hide()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    void Build(Action close)
    {
        var border = ControlsGuide.CreatePanel(transform, "Border", new Vector2(654f, 402f));
        border.GetComponent<UnityEngine.UI.Image>().color = Gold;
        var panel = ControlsGuide.CreatePanel(border.transform, "Panel", new Vector2(646f, 394f));
        panel.GetComponent<UnityEngine.UI.Image>().color = Panel;
        AddText(panel.transform, "Opciones", 36, 144f, 600f, 0f, TextAnchor.MiddleCenter, Gold);
        AddText(panel.transform, "Volumen general", 23, 66f, 250f, -173f, TextAnchor.MiddleLeft, Ink);
        AddText(panel.transform, "Modo de pantalla", 23, 0f, 250f, -173f, TextAnchor.MiddleLeft, Ink);
        AddText(panel.transform, "Sincronización vertical", 23, -66f, 270f, -163f, TextAnchor.MiddleLeft, Ink);
        volumeText = AddText(panel.transform, "", 20, 66f, 70f, 264f, TextAnchor.MiddleRight, Ink);
        volume = CreateVolumeSlider(panel.transform);
        volume.onValueChanged.AddListener(SetVolume);

        var display = ControlsGuide.CreateButton(panel.transform, "DisplayMode", "", new Vector2(163f, 176f), new Vector2(250f, 42f));
        displayText = display.GetComponentInChildren<UnityEngine.UI.Text>();
        display.onClick.AddListener(ToggleDisplay);
        var vsync = ControlsGuide.CreateButton(panel.transform, "VSync", "", new Vector2(163f, 110f), new Vector2(250f, 42f));
        vsyncText = vsync.GetComponentInChildren<UnityEngine.UI.Text>();
        vsync.onClick.AddListener(ToggleVSync);
        var back = ControlsGuide.CreateButton(panel.transform, "Back", "VOLVER", new Vector2(0f, 28f), new Vector2(200f, 48f));
        back.onClick.AddListener(() => close());
    }

    static UnityEngine.UI.Text AddText(Transform parent, string value, int fontSize, float y, float width, float x, TextAnchor alignment, Color color)
    {
        var text = ControlsGuide.CreateText(parent, "Label", value, fontSize, FontStyle.Normal, color, alignment);
        text.rectTransform.sizeDelta = new Vector2(width, 48f);
        text.rectTransform.anchoredPosition = new Vector2(x, y);
        return text;
    }

    static UnityEngine.UI.Slider CreateVolumeSlider(Transform parent)
    {
        var control = ControlsGuide.CreatePanel(parent, "Volume", new Vector2(178f, 44f));
        control.GetComponent<RectTransform>().anchoredPosition = new Vector2(128f, 66f);
        control.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        var track = ControlsGuide.CreatePanel(control.transform, "Track", new Vector2(172f, 8f));
        track.GetComponent<UnityEngine.UI.Image>().color = new Color(0.38f, 0.32f, 0.24f);
        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(control.transform, false);
        var area = handleArea.GetComponent<RectTransform>();
        ControlsGuide.Stretch(area);
        area.offsetMin = new Vector2(9f, 0f);
        area.offsetMax = new Vector2(-9f, 0f);
        var handle = ControlsGuide.CreatePanel(area, "Handle", new Vector2(18f, 22f));
        var graphic = handle.GetComponent<UnityEngine.UI.Image>();
        graphic.color = Gold;
        var slider = control.AddComponent<UnityEngine.UI.Slider>();
        slider.targetGraphic = graphic;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        return slider;
    }

    void SetVolume(float percent)
    {
        AudioListener.volume = percent / 100f;
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        RefreshLabels();
    }

    void ToggleDisplay()
    {
        bool enabled = !Fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
        ApplyDisplayMode(enabled);
        RefreshLabels();
    }

    static void ApplyDisplayMode(bool fullscreen) =>
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

    void ToggleVSync()
    {
        QualitySettings.vSyncCount = VSync ? 0 : 1;
        PlayerPrefs.SetInt(VSyncKey, QualitySettings.vSyncCount);
        RefreshLabels();
    }

    void RefreshLabels()
    {
        volumeText.text = $"{volume.value:0}%";
        displayText.text = Fullscreen ? "Pantalla completa" : "Ventana";
        vsyncText.text = VSync ? "Activada" : "Desactivada";
    }
}

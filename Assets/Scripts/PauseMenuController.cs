using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú de pausa in-game (Esc o Enter): Continuar / Salir del juego.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f);
    static readonly Color Panel = new Color(0.05f, 0.06f, 0.1f, 0.92f);
    static readonly Color Ink = new Color(0.94f, 0.91f, 0.82f);

    GameObject root;
    ControlsGuide guide;
    bool built;
    int lastToggleFrame = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (IsOpen)
                ResumeInternal(restoreCursor: false);
        }
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        var keyboard = Keyboard.current;
        if (!keyboard.escapeKey.wasPressedThisFrame
            && !keyboard.enterKey.wasPressedThisFrame
            && !keyboard.numpadEnterKey.wasPressedThisFrame)
            return;

        Toggle();
    }

    public void Toggle()
    {
        // Evita doble toggle si Esc llega por Keyboard y por InputAction en el mismo frame.
        if (Time.frameCount == lastToggleFrame)
            return;
        lastToggleFrame = Time.frameCount;

        if (guide != null && guide.IsShown)
        {
            guide.Hide();
            return;
        }

        if (IsOpen)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        EnsureUi();
        if (IsOpen)
            return;

        IsOpen = true;
        root.SetActive(true);
        Time.timeScale = 0f;
        CameraFollow.SetCursorLocked(false);
    }

    public void Resume()
    {
        if (!IsOpen)
            return;
        ResumeInternal(restoreCursor: true);
    }

    void ResumeInternal(bool restoreCursor)
    {
        IsOpen = false;
        if (guide != null)
            guide.Hide();
        if (root != null)
            root.SetActive(false);
        Time.timeScale = 1f;
        if (restoreCursor)
            CameraFollow.SetCursorLocked(true);
    }

    public void QuitGame()
    {
        GameSession.ResetRun();
        Time.timeScale = 1f;
        IsOpen = false;
        CameraFollow.SetCursorLocked(false);
        SceneManager.LoadScene("MainMenu");
    }

    void EnsureUi()
    {
        if (built)
            return;
        built = true;

        EnsureEventSystem();

        var canvasGo = new GameObject("PauseMenuCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        root = new GameObject("PauseRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvasGo.transform, false);
        StretchFull(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

        var panel = CreatePanel(root.transform, "Panel", new Vector2(420f, 360f));
        panel.GetComponent<Image>().color = Panel;

        var border = CreatePanel(panel.transform, "Border", new Vector2(420f, 360f));
        border.GetComponent<Image>().color = Gold;
        StretchFull(border.GetComponent<RectTransform>());
        border.transform.SetAsFirstSibling();
        var fill = CreatePanel(panel.transform, "Fill", new Vector2(408f, 348f));
        fill.GetComponent<Image>().color = Panel;

        var title = CreateText(panel.transform, "Title", "PAUSA", 36, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        titleRt.sizeDelta = new Vector2(360f, 48f);

        var continueBtn = CreateButton(panel.transform, "Continue", "CONTINUAR", new Vector2(0f, 46f), new Vector2(280f, 52f));
        continueBtn.onClick.AddListener(Resume);

        var quitBtn = CreateButton(panel.transform, "Quit", "SALIR DEL JUEGO", new Vector2(0f, -22f), new Vector2(280f, 52f));
        quitBtn.onClick.AddListener(QuitGame);

        var helpBtn = CreateButton(panel.transform, "Help", "AYUDA", new Vector2(0f, -90f), new Vector2(280f, 52f));
        helpBtn.onClick.AddListener(ShowHelp);

        guide = ControlsGuide.Ensure(canvasGo.transform);

        root.SetActive(false);
    }

    void ShowHelp()
    {
        if (guide == null || Time.frameCount == lastToggleFrame)
            return;
        guide.Show();
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 anchored, Vector2 size)
    {
        var go = CreatePanel(parent, name, size);
        go.GetComponent<RectTransform>().anchoredPosition = anchored;
        go.GetComponent<Image>().color = Gold;
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var text = CreateText(go.transform, "Label", label, 22, FontStyle.Bold, new Color(0.12f, 0.1f, 0.08f), TextAnchor.MiddleCenter);
        StretchFull(text.rectTransform);
        return button;
    }

    static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        return text;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

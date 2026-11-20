using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    public TextMeshProUGUI promptText;

    private string pendingMessage = null;
    private float bestDistance = float.MaxValue;

    void Awake()
    {
        Instance = this;
        ResolvePromptText();

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ListenForScenes()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindAnyObjectByType<InteractionPromptUI>(FindObjectsInactive.Include) == null)
            CreateRuntime();
    }

    public static void RequestPrompt(string message, float distance)
    {
        if (Instance == null)
        {
            Instance = FindAnyObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
            if (Instance == null)
                Instance = CreateRuntime();
        }

        if (Instance != null)
        {
            Instance.RegisterRequest(message, distance);
        }
    }

    private void RegisterRequest(string message, float distance)
    {
        if (distance < bestDistance)
        {
            bestDistance = distance;
            pendingMessage = message;
        }
    }

    void LateUpdate()
    {
        if (promptText == null)
        {
            ResolvePromptText();
            if (promptText == null) return;
        }

        if (!string.IsNullOrEmpty(pendingMessage))
        {
            if (!promptText.gameObject.activeSelf)
            {
                promptText.gameObject.SetActive(true);
            }

            if (promptText.text != pendingMessage)
            {
                promptText.text = pendingMessage;
            }
        }
        else if (promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }

        pendingMessage = null;
        bestDistance = float.MaxValue;
    }

    static InteractionPromptUI CreateRuntime()
    {
        var canvasGo = new GameObject("InteractionPromptCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var textGo = new GameObject("InteractionPromptText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var rect = textGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta = new Vector2(760f, 64f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 32f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        textGo.SetActive(false);

        var ui = canvasGo.AddComponent<InteractionPromptUI>();
        ui.promptText = tmp;
        return ui;
    }

    private void ResolvePromptText()
    {
        if (promptText != null) return;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in texts)
        {
            if (tmp.name == "InteractionPromptText")
            {
                promptText = tmp;
                return;
            }
        }
    }
}

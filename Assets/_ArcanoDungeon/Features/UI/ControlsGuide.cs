using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de controles compartido por el menú principal y la pausa.
/// </summary>
public class ControlsGuide : MonoBehaviour
{
    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f);
    static readonly Color Panel = new Color(0.05f, 0.06f, 0.1f, 0.96f);
    static readonly Color Ink = new Color(0.94f, 0.91f, 0.82f);
    static readonly Color InkDark = new Color(0.12f, 0.1f, 0.08f);

    const string MoveColumn =
        "<b>MOVIMIENTO</b>\n\n" +
        "<b>WASD</b>    Caminar según la cámara\n" +
        "<b>Ratón</b>    Mirar alrededor\n" +
        "<b>Clic derecho</b>    Fijar el cursor para poder moverte\n" +
        "<b>Mayús</b>    Dash. Gasta resistencia\n" +
        "<b>Alt</b>    Correr\n" +
        "<b>Ctrl o C</b>    Agacharse\n" +
        "<b>Espacio</b>    Saltar";

    const string ActionColumn =
        "<b>COMBATE</b>\n\n" +
        "<b>Clic izquierdo</b>    Ataque cuerpo a cuerpo\n" +
        "<b>Clic derecho</b>    Bloquear. Magus lanza una bola de fuego\n" +
        "<b>E</b>    Habilidad especial. Gasta resistencia\n" +
        "    Magus lanza tres bolas de fuego\n" +
        "    Aldric suelta una onda de choque\n" +
        "    Misaki encadena una ráfaga\n" +
        "    Yuko abre un tajo amplio\n\n" +
        "<b>MUNDO</b>\n\n" +
        "<b>F</b>    Recoger objetos, corazones y abrir cofres\n" +
        "<b>Q</b>    Bomba de pólvora, si tienes cargas. Explota a los 2 segundos\n" +
        "<b>Esc o Enter</b>    Pausa\n\n" +
        "Vacía la sala de enemigos para abrir la reja y activar el portal.";

    public bool IsShown => gameObject.activeSelf;

    public static ControlsGuide Ensure(Transform canvas)
    {
        var found = canvas.GetComponentInChildren<ControlsGuide>(true);
        if (found != null)
            return found;

        var go = new GameObject("ControlsGuide", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas, false);
        Stretch(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        var guide = go.AddComponent<ControlsGuide>();
        go.SetActive(false);
        return guide;
    }

    void Awake()
    {
        if (transform.childCount > 0)
            return;
        Build();
    }

    public void Show()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Build()
    {
        var panel = CreatePanel(transform, "Panel", new Vector2(980f, 640f));
        panel.GetComponent<Image>().color = Panel;

        var border = CreatePanel(panel.transform, "Border", new Vector2(980f, 640f));
        border.GetComponent<Image>().color = Gold;
        Stretch(border.GetComponent<RectTransform>());
        border.transform.SetAsFirstSibling();

        var fill = CreatePanel(panel.transform, "Fill", new Vector2(964f, 624f));
        fill.GetComponent<Image>().color = Panel;

        var title = CreateText(panel.transform, "Title", "CÓMO JUGAR", 36, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        titleRt.sizeDelta = new Vector2(860f, 48f);

        var left = CreateText(panel.transform, "Move", MoveColumn, 20, FontStyle.Normal, Ink, TextAnchor.UpperLeft);
        PlaceColumn(left.rectTransform, new Vector2(36f, -96f), new Vector2(440f, 430f));

        var right = CreateText(panel.transform, "Actions", ActionColumn, 20, FontStyle.Normal, Ink, TextAnchor.UpperLeft);
        PlaceColumn(right.rectTransform, new Vector2(500f, -96f), new Vector2(444f, 430f));

        var close = CreateButton(panel.transform, "Close", "CERRAR", new Vector2(0f, 36f), new Vector2(240f, 52f));
        close.onClick.AddListener(Hide);
    }

    static void PlaceColumn(RectTransform rt, Vector2 topLeft, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeft;
        rt.sizeDelta = size;
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return go;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredFromBottom, Vector2 size)
    {
        var go = CreatePanel(parent, name, size);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredFromBottom;
        go.GetComponent<Image>().color = Gold;
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var text = CreateText(go.transform, "Label", label, 22, FontStyle.Bold, InkDark, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.05f;
        text.supportRichText = true;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

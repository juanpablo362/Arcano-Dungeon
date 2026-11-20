using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Image highlight;
    public Image icon;
    public Image topLine;
    public Image bottomLine;
    public Text label;
    public Sprite iconNormal;
    public Sprite iconActive;
    public Color textNormal = new Color(0.96f, 0.9f, 0.72f, 1f);
    public Color textActive = new Color(1f, 0.94f, 0.75f, 1f);
    public Color textDisabled = new Color(0.55f, 0.62f, 0.72f, 0.35f);
    public float activeFontSize = 30f;
    public float normalFontSize = 26f;

    static readonly Color FillNormal = new Color(0.06f, 0.05f, 0.1f, 0.72f);
    static readonly Color FillActive = new Color(0.18f, 0.08f, 0.32f, 0.88f);
    static readonly Color Gold = new Color(0.82f, 0.68f, 0.32f, 0.9f);
    static readonly Color GoldSoft = new Color(0.72f, 0.58f, 0.28f, 0.45f);
    static readonly Color ArcaneGlow = new Color(0.55f, 0.28f, 1f, 0.28f);

    Button button;
    Image fill;
    Outline goldOutline;
    Image glow;
    bool hovered;

    void Awake()
    {
        button = GetComponent<Button>();
        fill = GetComponent<Image>();
        EnsureChrome();
        SetActiveVisual(false);
    }

    void OnEnable()
    {
        EnsureChrome();
        SetActiveVisual(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;
        hovered = true;
        SetActiveVisual(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        SetActiveVisual(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (button != null && !button.interactable)
            return;
        SetActiveVisual(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!hovered)
            SetActiveVisual(false);
    }

    void EnsureChrome()
    {
        if (fill == null)
            fill = GetComponent<Image>();

        if (fill != null && goldOutline == null)
        {
            goldOutline = fill.GetComponent<Outline>();
            if (goldOutline == null)
                goldOutline = fill.gameObject.AddComponent<Outline>();
            goldOutline.effectColor = GoldSoft;
            goldOutline.effectDistance = new Vector2(2f, -2f);
            goldOutline.useGraphicAlpha = true;
        }

        var glowTf = transform.Find("ArcaneGlow");
        if (glowTf == null)
        {
            var go = new GameObject("ArcaneGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowTf = go.transform;
            glowTf.SetParent(transform, false);
            glowTf.SetAsFirstSibling();
            var rt = (RectTransform)glowTf;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-8f, -6f);
            rt.offsetMax = new Vector2(8f, 6f);
            glow = go.GetComponent<Image>();
            glow.raycastTarget = false;
            glow.color = Color.clear;
        }
        else
        {
            glow = glowTf.GetComponent<Image>();
        }

        if (highlight != null)
        {
            // Degradado suave hacia la derecha: highlight a media opacidad.
            var c = highlight.color;
            if (c.a < 0.05f || c.maxColorComponent > 0.95f)
                highlight.color = new Color(0.72f, 0.45f, 1f, 0.18f);
        }
    }

    void SetActiveVisual(bool active)
    {
        bool disabled = button != null && !button.interactable;
        bool lit = active && !disabled;

        if (highlight != null)
            highlight.enabled = lit;

        if (topLine != null)
        {
            topLine.enabled = true;
            topLine.color = lit ? Gold : GoldSoft;
        }

        if (bottomLine != null)
        {
            bottomLine.enabled = true;
            bottomLine.color = lit ? Gold : new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, 0.28f);
        }

        if (fill != null)
            fill.color = lit ? FillActive : (disabled ? new Color(0.05f, 0.05f, 0.07f, 0.4f) : FillNormal);

        if (goldOutline != null)
        {
            goldOutline.enabled = !disabled;
            goldOutline.effectColor = lit ? Gold : GoldSoft;
            goldOutline.effectDistance = lit ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
        }

        if (glow != null)
            glow.color = lit ? ArcaneGlow : Color.clear;

        if (icon != null)
        {
            icon.sprite = lit && iconActive != null ? iconActive : iconNormal;
            icon.rectTransform.sizeDelta = lit
                ? new Vector2(34f, 34f)
                : new Vector2(16f, 16f);
            var c = icon.color;
            c.a = disabled ? 0.35f : 1f;
            icon.color = c;
        }

        if (label != null)
        {
            label.color = disabled ? textDisabled : (lit ? textActive : textNormal);
            label.fontSize = Mathf.RoundToInt(lit ? activeFontSize : normalFontSize);
        }
    }
}

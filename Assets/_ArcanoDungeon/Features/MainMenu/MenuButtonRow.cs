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

    Button button;
    bool hovered;

    void Awake()
    {
        button = GetComponent<Button>();
        SetActiveVisual(false);
    }

    void OnEnable()
    {
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

    void SetActiveVisual(bool active)
    {
        bool disabled = button != null && !button.interactable;
        if (highlight != null)
            highlight.enabled = active && !disabled;
        if (topLine != null)
            topLine.enabled = active && !disabled;
        if (bottomLine != null)
            bottomLine.enabled = active && !disabled;

        if (icon != null)
        {
            icon.sprite = active && !disabled && iconActive != null ? iconActive : iconNormal;
            icon.rectTransform.sizeDelta = active && !disabled
                ? new Vector2(34f, 34f)
                : new Vector2(16f, 16f);
            var c = icon.color;
            c.a = disabled ? 0.35f : 1f;
            icon.color = c;
        }

        if (label != null)
        {
            label.color = disabled ? textDisabled : (active ? textActive : textNormal);
            label.fontSize = Mathf.RoundToInt(active && !disabled ? activeFontSize : normalFontSize);
        }
    }
}

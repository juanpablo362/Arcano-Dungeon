using System.Collections;
using System.Collections.Generic;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectPresenter : MonoBehaviour
{
    [SerializeField] AudioClip menuMusic;
    [SerializeField] float menuMusicVolume = 0.5f;

    static readonly Color Gold = new Color(0.93f, 0.78f, 0.32f);
    static readonly Color GoldSoft = new Color(0.93f, 0.78f, 0.32f, 0.55f);
    static readonly Color Ink = new Color(0.93f, 0.90f, 0.82f);
    static readonly Color InkMuted = new Color(0.72f, 0.68f, 0.58f);
    static readonly Color PanelInk = new Color(0.94f, 0.91f, 0.82f);
    static readonly Color PanelMuted = new Color(0.72f, 0.68f, 0.58f);
    static readonly Color ClassReadable = new Color(0.96f, 0.86f, 0.58f);

    static readonly Dictionary<string, string> AbilityHints = new Dictionary<string, string>
    {
        { "staff", "Bastón. Lanza hechizos a distancia." },
        { "spark", "Chispa. Un estallido mágico rápido." },
        { "ward", "Amuleto. Refuerza la defensa." },
        { "shadow", "Sombra. Movimiento sigiloso." },
        { "shield", "Escudo. Bloquea los golpes de frente." },
        { "swords", "Espadas. Ataque cuerpo a cuerpo." },
    };

    // KayKit (Generic / Rig_Medium) — héroes Elara/Lyra/Kaede.
    static readonly string[] KayKitGestureClips =
    {
        "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatRanged.fbx|Ranged_Magic_Raise",
        "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatMelee.fbx|Melee_1H_Attack_Chop",
        "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_Simulation.fbx|Waving",
        "Assets/chibi_asssets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_Simulation.fbx|Cheering",
    };

    // Humanoid (Magus/Aldric/Misaki/Yuko) — Asuna retarget.
    static readonly string[] HumanoidGestureClips =
    {
        "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx|AttackRightHand",
        "Assets/FreeTestCharacterAsuna/FBX/Animations/009Conversation.fbx|ConversationYes",
        "Assets/FreeTestCharacterAsuna/FBX/Animations/022Unarmed.fbx|PunchRight",
        "Assets/FreeTestCharacterAsuna/FBX/Animations/009Conversation.fbx|ConversationYes",
    };

    static readonly string[] StatNames = { "Salud", "Ataque", "Defensa", "Velocidad" };

    Transform heroStage;
    Transform[] pivots;
    Quaternion[] pivotBase;
    Animator[] animators;
    float[] gestureLengths;
    Coroutine gestureRoutine;

    RectTransform[] cardRoots;
    Image[] cardBorders;
    Image[] cardPortraits;
    Text[] cardNames;
    Text[] cardTitles;
    Image[] cardStars;

    Text headerName;
    Text headerClass;
    Text description;
    Image[] statFills;
    Text[] statValues;
    Image[] abilityIcons;

    Light stageLight;
    int selected = -1;

    void OnEnable()
    {
        Rebind();
        EnsureMenuMusic();
    }

    void EnsureMenuMusic()
    {
        PersistentMenuMusic.EnsurePlaying(menuMusic, menuMusicVolume);
    }

    void Update()
    {
        if (!Application.isPlaying || pivots == null)
            return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
                Select(0);
            if (keyboard.digit2Key.wasPressedThisFrame)
                Select(1);
            if (keyboard.digit3Key.wasPressedThisFrame)
                Select(2);
            if (keyboard.digit4Key.wasPressedThisFrame)
                Select(3);
            if (keyboard.digit5Key.wasPressedThisFrame)
                Select(4);
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                Select(selected - 1);
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                Select(selected + 1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                EnterDungeon();
            if (keyboard.escapeKey.wasPressedThisFrame)
                BackToMenu();
        }

        if (pivots == null || selected < 0 || selected >= pivots.Length || pivots[selected] == null)
            return;

        var yaw = Mathf.Sin(Time.time * 0.45f) * 4f;
        pivots[selected].localRotation = pivotBase[selected] * Quaternion.Euler(0f, yaw, 0f);

        if (stageLight != null)
            stageLight.intensity = 0.32f + Mathf.Sin(Time.time * 2.1f) * 0.02f;
    }

    void Select(int index)
    {
        if (HeroRoster.All.Length == 0)
            return;

        selected = (index % HeroRoster.All.Length + HeroRoster.All.Length) % HeroRoster.All.Length;
        var profile = HeroRoster.All[selected];
        GameSession.SelectedHero = profile.Id;
        PlayerPrefs.SetInt("SelectedCharacter", (int)profile.Id);

        if (pivots != null)
        {
            for (var i = 0; i < pivots.Length; i++)
            {
                if (pivots[i] != null)
                    pivots[i].gameObject.SetActive(i == selected);
            }
        }

        if (cardRoots != null)
        {
            for (var i = 0; i < cardRoots.Length; i++)
            {
                var on = i == selected;
                if (cardBorders != null && cardBorders[i] != null)
                    cardBorders[i].color = on ? Gold : new Color(Gold.r, Gold.g, Gold.b, 0.28f);
                if (cardNames != null && cardNames[i] != null)
                    cardNames[i].color = on ? Gold : Ink;
                if (cardTitles != null && cardTitles[i] != null)
                    cardTitles[i].color = on ? ClassReadable : InkMuted;
                if (cardStars != null && cardStars[i] != null)
                    cardStars[i].enabled = on;
            }
        }

        if (headerName != null)
        {
            headerName.text = profile.Name;
            headerName.color = Gold;
        }
        if (headerClass != null)
        {
            headerClass.text = profile.Title.ToUpperInvariant();
            headerClass.color = ClassReadable;
        }

        if (description != null)
        {
            description.text = profile.Summary;
            description.color = PanelInk;
        }

        if (statFills != null && statValues != null)
        {
            int[] values = { profile.Health, profile.Attack, profile.Defense, profile.Speed };
            for (var i = 0; i < values.Length; i++)
            {
                if (statValues[i] != null)
                {
                    statValues[i].text = values[i].ToString();
                    statValues[i].color = Gold;
                }
                if (statFills[i] != null)
                    SetStatFill(statFills[i], values[i]);
            }
        }

        if (abilityIcons != null && profile.Abilities != null)
        {
            for (var i = 0; i < abilityIcons.Length; i++)
            {
                if (abilityIcons[i] == null)
                    continue;
                if (i < profile.Abilities.Length)
                {
                    var icon = Resources.Load<Sprite>(profile.Abilities[i]);
                    abilityIcons[i].sprite = icon;
                    abilityIcons[i].enabled = icon != null;
                    abilityIcons[i].color = new Color(0.55f, 0.78f, 1f, 1f);
                    abilityIcons[i].raycastTarget = true;
                    var hint = abilityIcons[i].GetComponent<AbilityHint>();
                    if (hint == null)
                        hint = abilityIcons[i].gameObject.AddComponent<AbilityHint>();
                    AbilityHints.TryGetValue(profile.Abilities[i], out var text);
                    hint.Text = text;
                }
                else
                {
                    abilityIcons[i].enabled = false;
                }
            }
        }

        ApplyPoses();
    }

    static void SetStatFill(Image fill, int value)
    {
        const float start = 0.34f;
        const float full = 0.48f;
        var rt = fill.rectTransform;
        var min = rt.anchorMin;
        var max = rt.anchorMax;
        min.x = start;
        max.x = start + full * Mathf.Clamp01(value / 10f);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        fill.type = Image.Type.Simple;
    }

    void ApplyPoses()
    {
        if (animators == null)
            return;

        if (gestureRoutine != null)
        {
            StopCoroutine(gestureRoutine);
            gestureRoutine = null;
        }

        for (var i = 0; i < animators.Length; i++)
        {
            var animator = animators[i];
            if (animator == null || !animator.gameObject.activeInHierarchy)
                continue;

            PrepareAnimator(i, animator);
            if (i == selected && Application.isPlaying && gestureLengths[i] > 0.05f)
            {
                var gestureState = ResolveGestureState(animator);
                if (!string.IsNullOrEmpty(gestureState))
                {
                    animator.speed = 1f;
                    animator.Play(gestureState, 0, 0f);
                    animator.Update(0f);
                    gestureRoutine = StartCoroutine(RestAfterGesture(i, gestureLengths[i]));
                    continue;
                }
            }

            HoldRest(animator);
        }
    }

    static string ResolveGestureState(Animator animator)
    {
        if (HasState(animator, "Attack1"))
            return "Attack1";
        if (HasState(animator, "Attack"))
            return "Attack";
        if (HasState(animator, "RifleAim"))
            return "RifleAim";
        return null;
    }

    IEnumerator RestAfterGesture(int index, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gestureRoutine = null;
        if (!Application.isPlaying || animators == null || index != selected || animators[index] == null)
            yield break;
        HoldRest(animators[index]);
    }

    static void HoldRest(Animator animator)
    {
        animator.speed = 1f;
        if (HasState(animator, "Idle"))
            animator.Play("Idle", 0, 0.12f);
        else if (HasState(animator, "Idle1"))
            animator.Play("Idle1", 0, 0.12f);
        else if (HasState(animator, "Idle_A"))
            animator.Play("Idle_A", 0, 0.12f);
        else if (HasState(animator, "Blend Tree"))
            animator.Play("Blend Tree", 0, 0f);
        animator.Update(0f);
    }

    static bool HasState(Animator animator, string stateName)
    {
        return animator != null && animator.runtimeAnimatorController != null &&
               animator.HasState(0, Animator.StringToHash(stateName));
    }

    void PrepareAnimator(int index, Animator animator)
    {
        animator.applyRootMotion = false;
        animator.fireEvents = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var gesture = ResolveGestureClip(index, animator);
        if (gesture != null)
            gestureLengths[index] = gesture.length;

        var source = animator.runtimeAnimatorController;
        if (source == null || source is AnimatorOverrideController || gesture == null)
            return;

        // Solo override si el controlador expone un clip de gesto reemplazable.
        var over = new AnimatorOverrideController(source);
        var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        over.GetOverrides(list);
        var replaced = false;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Key == null)
                continue;
            var key = list[i].Key.name;
            if (key != "RifleAim" && key != "Attack" && key != "Attack1" &&
                key != "KayKit_Knight_Combo1" && key != "KayKit_Mage_Combo1" && key != "KayKit_Rogue_Combo1")
                continue;
            list[i] = new KeyValuePair<AnimationClip, AnimationClip>(list[i].Key, gesture);
            replaced = true;
        }

        if (!replaced)
            return;

        over.ApplyOverrides(list);
        animator.runtimeAnimatorController = over;
    }

    static AnimationClip ResolveGestureClip(int index, Animator animator)
    {
        var table = animator != null && animator.isHuman ? HumanoidGestureClips : KayKitGestureClips;
        if (table == null || table.Length == 0)
            return null;

        var entry = table[Mathf.Clamp(index, 0, table.Length - 1)];
        var split = entry.Split('|');
        return LoadClip(split[0], split[1]);
    }

    static AnimationClip LoadClip(string path, string clipName)
    {
        return ArcanoDungeon.Infrastructure.Assets.DungeonRuntimeAssets.Load<AnimationClip>(path + "|" + clipName);
    }

    void Rebind()
    {
        var lineup = transform.Find("Lineup");
        var ui = transform.Find("HeroUI");
        if (lineup == null || ui == null)
        {
            Debug.LogError("CharacterSelect: falta Lineup o HeroUI precargados en la escena.");
            return;
        }

        heroStage = lineup.Find("HeroStage");
        stageLight = lineup.Find("StageLight")?.GetComponent<Light>();

        pivots = new Transform[HeroRoster.All.Length];
        pivotBase = new Quaternion[HeroRoster.All.Length];
        animators = new Animator[HeroRoster.All.Length];
        gestureLengths = new float[HeroRoster.All.Length];

        for (var i = 0; i < HeroRoster.All.Length; i++)
        {
            var profile = HeroRoster.All[i];
            Transform slot = null;
            if (heroStage != null)
                slot = heroStage.Find("Slot_" + profile.Name);
            if (slot == null)
                slot = lineup.Find("Slot_" + profile.Name);

            if (slot == null)
                continue;

            pivots[i] = slot;
            pivotBase[i] = slot.localRotation;
            var hero = slot.Find(profile.Name);
            if (hero != null)
            {
                if (profile.Id == HeroId.Assassin)
                    hero.localScale = Vector3.one * profile.StageScale;
                animators[i] = hero.GetComponentInChildren<Animator>();
            }
        }

        cardRoots = new RectTransform[HeroRoster.All.Length];
        cardBorders = new Image[HeroRoster.All.Length];
        cardPortraits = new Image[HeroRoster.All.Length];
        cardNames = new Text[HeroRoster.All.Length];
        cardTitles = new Text[HeroRoster.All.Length];
        cardStars = new Image[HeroRoster.All.Length];

        var roster = ui.Find("Roster");
        for (var i = 0; i < HeroRoster.All.Length; i++)
        {
            var profile = HeroRoster.All[i];
            var card = roster != null ? roster.Find("Card_" + profile.Name) : ui.Find("Card_" + profile.Name);
            if (card == null)
                continue;

            cardRoots[i] = card.GetComponent<RectTransform>();
            cardBorders[i] = card.GetComponent<Image>();
            cardPortraits[i] = FindImage(card, "Portrait");
            cardNames[i] = FindText(card, "Name");
            cardTitles[i] = FindText(card, "Title");
            cardStars[i] = FindImage(card, "Star");

            var button = card.GetComponent<Button>();
            if (button != null)
            {
                var captured = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Select(captured));
            }
        }

        var detail = ui.Find("DetailPanel");
        if (detail != null)
        {
            headerName = FindText(detail, "HeaderName");
            headerClass = FindText(detail, "HeaderClass");
            description = FindText(detail, "Description");

            statFills = new Image[StatNames.Length];
            statValues = new Text[StatNames.Length];
            for (var i = 0; i < StatNames.Length; i++)
            {
                var row = detail.Find("Stat_" + StatNames[i]);
                if (row == null)
                    continue;
                statFills[i] = FindImage(row, "Fill");
                statValues[i] = FindText(row, "Value");
            }

            abilityIcons = new Image[4];
            for (var i = 0; i < abilityIcons.Length; i++)
                abilityIcons[i] = FindImage(detail, "Ability_" + i);
        }
        else
        {
            headerName = FindText(ui, "HeaderName");
            headerClass = FindText(ui, "HeaderClass");
        }

        Hook(ui.Find("Back"), BackToMenu);
        Hook(ui.Find("Prev"), () => Select(selected - 1));
        Hook(ui.Find("Next"), () => Select(selected + 1));
        Hook(ui.Find("Enter"), EnterDungeon);
        AbilityHint.EnsureTooltip(ui);

        Select((int)GameSession.SelectedHero);
    }

    static void Hook(Transform target, UnityEngine.Events.UnityAction action)
    {
        if (target == null)
            return;
        var button = target.GetComponent<Button>();
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    static Text FindText(Transform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.GetComponent<Text>() : null;
    }

    static Image FindImage(Transform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.GetComponent<Image>() : null;
    }

    void EnterDungeon()
    {
        if (!Application.isPlaying)
            return;
        PlayerPrefs.SetInt("SelectedCharacter", (int)GameSession.SelectedHero);
        PlayerPrefs.Save();
        PersistentMenuMusic.StopAndDestroy();
        GameSession.ResetRun();
        SceneManager.LoadScene("Level1");
    }

    void BackToMenu()
    {
        if (!Application.isPlaying)
            return;
        SceneManager.LoadScene("MainMenu");
    }
}

public class AbilityHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string Text;

    static GameObject root;
    static Text label;

    public static void EnsureTooltip(Transform ui)
    {
        if (ui == null)
            return;

        var existing = ui.Find("AbilityTooltip");
        if (existing == null)
        {
            var go = new GameObject("AbilityTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(ui, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.07f, 0.05f, 0.94f);
            image.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(280f, 42f);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.9f, 0.72f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-10f, -4f);
            existing = go.transform;
        }

        root = existing.gameObject;
        label = root.GetComponentInChildren<Text>(true);
        root.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (root == null || label == null || string.IsNullOrEmpty(Text))
            return;

        label.text = Text;
        var tip = root.GetComponent<RectTransform>();
        var icon = (RectTransform)transform;
        var lift = icon.rect.height * icon.lossyScale.y * 0.7f + 14f;
        tip.position = icon.position + Vector3.up * lift;
        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (root != null)
            root.SetActive(false);
    }
}

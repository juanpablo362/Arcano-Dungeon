using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectPresenter : MonoBehaviour
{
    static readonly Vector3[] SlotPos =
    {
        new Vector3(-1.62f, 0f, 0.18f),
        new Vector3(0f, 0f, -0.08f),
        new Vector3(1.62f, 0f, 0.18f),
    };

    static readonly Vector3 SelectedStep = new Vector3(0f, 0f, -0.42f);
    static readonly Color Ink = new Color(0.1f, 0.12f, 0.16f);
    static readonly Color InkSoft = new Color(0.38f, 0.41f, 0.46f);

    static readonly string[] GestureClips =
    {
        "Assets/FreeTestCharacterAsuna/FBX/Animations/023OneHandMeleeWeapon.fbx|AttackRightHand",
        "Assets/FreeTestCharacterAsuna/FBX/Animations/009Conversation.fbx|ConversationYes",
        "Assets/FreeTestCharacterAsuna/FBX/Animations/022Unarmed.fbx|PunchRight",
    };

    static AnimationClip[] gestures;

    Transform[] pivots;
    Quaternion[] pivotBase;
    Light[] accentLights;
    Renderer[] rings;
    Animator[] animators;
    float[] gestureLengths;
    Coroutine gestureRoutine;
    RectTransform[] nameRoots;
    Text[] nameLabels;
    Text[] titleLabels;
    Text[] indexLabels;
    Image[] indexBars;
    RectTransform[] meterRoots;
    Image[] meterFills;
    Text[] meterRoles;
    Text statName;
    Text statClass;
    Text statSummary;
    Text statAbility;
    Text[] statValues;
    Image statsRule;
    RectTransform canvasRect;
    int selected = -1;

    void OnEnable()
    {
        Rebind();
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
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                Select(selected - 1);
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                Select(selected + 1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                EnterDungeon();
            if (keyboard.escapeKey.wasPressedThisFrame)
                BackToMenu();
        }

        var step = 6f * Time.deltaTime;
        for (var i = 0; i < pivots.Length; i++)
        {
            if (pivots[i] == null)
                continue;

            var forward = i == selected ? SelectedStep : Vector3.zero;
            pivots[i].localPosition = Vector3.Lerp(pivots[i].localPosition, SlotPos[i] + forward, step);

            var yaw = Mathf.Sin((Time.time + i * 1.4f) * 0.45f) * 0.8f;
            pivots[i].localRotation = pivotBase[i] * Quaternion.Euler(0f, yaw, 0f);

            if (accentLights[i] != null)
                accentLights[i].intensity = i == selected ? 1.5f : 0.15f;

            if (rings[i] != null)
            {
                var pulse = i == selected ? 1f + Mathf.Sin(Time.time * 2.2f) * 0.03f : 1f;
                var width = i == selected ? 1.42f : 1.12f;
                rings[i].transform.localScale = new Vector3(width * pulse, 0.003f, width * pulse);
            }
        }
    }

    void LateUpdate()
    {
        if (Application.isPlaying)
            FollowNames();
    }

    void Select(int index)
    {
        if (HeroRoster.All.Length == 0)
            return;

        selected = (index % HeroRoster.All.Length + HeroRoster.All.Length) % HeroRoster.All.Length;
        var profile = HeroRoster.All[selected];
        GameSession.SelectedHero = profile.Id;

        if (pivots != null)
        {
            for (var i = 0; i < pivots.Length; i++)
            {
                var on = i == selected;
                var accent = HeroRoster.All[i].Accent;

                if (rings != null && rings[i] != null)
                {
                    var mat = rings[i].sharedMaterial;
                    if (mat != null)
                    {
                        mat.SetColor("_BaseColor", accent * (on ? 1f : 0.45f));
                        mat.SetColor("_EmissionColor", accent * (on ? 1.8f : 0.25f));
                    }
                }

                if (nameLabels != null && nameLabels[i] != null)
                    nameLabels[i].color = on ? Ink : new Color(Ink.r, Ink.g, Ink.b, 0.38f);
                if (titleLabels != null && titleLabels[i] != null)
                    titleLabels[i].color = on ? InkSoft : new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.4f);
                if (meterRoles != null && meterRoles[i] != null)
                    meterRoles[i].text = on ? "Vanguardia" : "";
                if (meterFills != null && meterFills[i] != null)
                    meterFills[i].color = on ? new Color(0.16f, 0.48f, 0.95f) : new Color(0.62f, 0.66f, 0.72f);
                if (indexLabels != null && indexLabels[i] != null)
                {
                    indexLabels[i].color = on ? Ink : InkSoft;
                    indexLabels[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
                }

                if (indexBars != null && indexBars[i] != null)
                    indexBars[i].color = on ? new Color(1f, 1f, 1f, 0.82f) : new Color(1f, 1f, 1f, 0f);
            }
        }

        if (statName != null)
            statName.text = profile.Name;
        if (statClass != null)
        {
            statClass.text = profile.Title;
            statClass.color = InkSoft;
        }

        if (statSummary != null)
            statSummary.text = profile.Name;
        if (statAbility != null)
            statAbility.text = profile.Ability + "   ·   " + profile.Summary;

        if (statsRule != null)
            statsRule.color = profile.Accent;
        if (statValues != null)
        {
            int[] values = { profile.Health, profile.Attack, profile.Defense, profile.Speed, profile.Crit };
            for (var i = 0; i < statValues.Length && i < values.Length; i++)
                statValues[i].text = values[i].ToString();
        }

        FollowNames();
        ApplyPoses();
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
            if (animator == null)
                continue;

            PrepareAnimator(i, animator);
            var chosen = i == selected;
            Shade(animator.gameObject, !chosen);
            if (accentLights != null && accentLights[i] != null)
                accentLights[i].intensity = chosen ? 1.5f : 0.15f;

            if (chosen && Application.isPlaying && gestureLengths[i] > 0.05f)
            {
                animator.speed = 1f;
                animator.Play("RifleAim", 0, 0f);
                animator.Update(0f);
                gestureRoutine = StartCoroutine(RestAfterGesture(i, gestureLengths[i]));
            }
            else
            {
                HoldRest(animator);
            }
        }
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
        animator.Play("Idle1", 0, 0.12f);
        animator.Update(0f);
        animator.speed = 0f;
    }

    void PrepareAnimator(int index, Animator animator)
    {
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EnsureGestures();
        if (gestures[index] != null)
            gestureLengths[index] = gestures[index].length;

        if (animator.runtimeAnimatorController is AnimatorOverrideController || gestures[index] == null)
            return;

        var source = animator.runtimeAnimatorController;
        if (source == null)
            return;

        var over = new AnimatorOverrideController(source);
        var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        over.GetOverrides(list);
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Key != null && list[i].Key.name == "RifleAim")
                list[i] = new KeyValuePair<AnimationClip, AnimationClip>(list[i].Key, gestures[index]);
        }

        over.ApplyOverrides(list);
        animator.runtimeAnimatorController = over;
    }

    static void Shade(GameObject hero, bool shadowed)
    {
        foreach (var renderer in hero.GetComponentsInChildren<Renderer>(true))
        {
            if (!shadowed)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.62f, 0.64f, 0.68f, 1f));
            block.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(block);
        }
    }

    static void EnsureGestures()
    {
        if (gestures != null)
            return;

        gestures = new AnimationClip[GestureClips.Length];
        for (var i = 0; i < GestureClips.Length; i++)
        {
            var split = GestureClips[i].Split('|');
            gestures[i] = LoadClip(split[0], split[1]);
        }
    }

    static AnimationClip LoadClip(string path, string clipName)
    {
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip && clip.name == clipName)
                return clip;
        }
#endif
        return null;
    }

    void FollowNames()
    {
        var camera = Camera.main;
        if (camera == null || canvasRect == null || pivots == null || nameRoots == null)
            return;

        for (var i = 0; i < pivots.Length; i++)
        {
            if (pivots[i] == null || nameRoots[i] == null)
                continue;

            var world = new Vector3(pivots[i].position.x, HeadTop(pivots[i]), pivots[i].position.z);
            var screen = camera.WorldToScreenPoint(world);
            if (screen.z < 0f)
                continue;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local))
                nameRoots[i].anchoredPosition = local + new Vector2(86f, 18f);

            if (meterRoots == null || meterRoots[i] == null)
                continue;

            var feet = camera.WorldToScreenPoint(new Vector3(pivots[i].position.x, 0.08f, pivots[i].position.z));
            if (feet.z < 0f)
                continue;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, feet, null, out var feetLocal))
                meterRoots[i].anchoredPosition = feetLocal;
        }
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

        canvasRect = ui.GetComponent<RectTransform>();
        pivots = new Transform[HeroRoster.All.Length];
        pivotBase = new Quaternion[HeroRoster.All.Length];
        accentLights = new Light[HeroRoster.All.Length];
        rings = new Renderer[HeroRoster.All.Length];
        animators = new Animator[HeroRoster.All.Length];
        gestureLengths = new float[HeroRoster.All.Length];
        nameRoots = new RectTransform[HeroRoster.All.Length];
        nameLabels = new Text[HeroRoster.All.Length];
        titleLabels = new Text[HeroRoster.All.Length];
        indexLabels = new Text[HeroRoster.All.Length];
        indexBars = new Image[HeroRoster.All.Length];
        meterRoots = new RectTransform[HeroRoster.All.Length];
        meterFills = new Image[HeroRoster.All.Length];
        meterRoles = new Text[HeroRoster.All.Length];

        for (var i = 0; i < HeroRoster.All.Length; i++)
        {
            var profile = HeroRoster.All[i];
            var slot = lineup.Find("Slot_" + profile.Name);
            if (slot != null)
            {
                pivots[i] = slot;
                pivotBase[i] = slot.localRotation;
                var accent = slot.Find("Accent_" + profile.Name);
                if (accent != null)
                    accentLights[i] = accent.GetComponent<Light>();
                var ring = slot.Find("Ring");
                if (ring != null)
                    rings[i] = ring.GetComponent<Renderer>();
                var hero = slot.Find(profile.Name);
                if (hero != null)
                    animators[i] = hero.GetComponentInChildren<Animator>();
            }

            var plate = ui.Find("Name_" + profile.Name);
            if (plate != null)
            {
                nameRoots[i] = plate.GetComponent<RectTransform>();
                var name = plate.Find("Name");
                if (name != null)
                    nameLabels[i] = name.GetComponent<Text>();
                var title = plate.Find("Title");
                if (title != null)
                    titleLabels[i] = title.GetComponent<Text>();
            }

            var hit = ui.Find("Hit_" + profile.Name);
            if (hit != null)
            {
                var button = hit.GetComponent<Button>();
                if (button != null)
                {
                    var captured = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => Select(captured));
                }
            }

            var meter = ui.Find("Meter_" + profile.Name);
            if (meter != null)
            {
                meterRoots[i] = meter.GetComponent<RectTransform>();
                var fill = meter.Find("Track/Fill");
                if (fill != null)
                    meterFills[i] = fill.GetComponent<Image>();
                meterRoles[i] = FindText(meter, "Role");
            }

            var index = ui.Find("Index_" + i);
            if (index != null)
            {
                indexLabels[i] = index.GetComponentInChildren<Text>();
                indexBars[i] = index.GetComponent<Image>();
                var button = index.GetComponent<Button>();
                if (button != null)
                {
                    var captured = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => Select(captured));
                }
            }
        }

        statName = FindText(ui, "HeaderName");
        statClass = FindText(ui, "HeaderClass");
        var panel = ui.Find("StatsPanel");
        if (panel != null)
        {
            statSummary = FindText(panel, "HeroSummary");
            statAbility = FindText(panel, "Ability");
            var mark = panel.Find("Mark");
            if (mark != null)
                statsRule = mark.GetComponent<Image>();
        }

        Hook(ui.Find("Back"), BackToMenu);
        Hook(ui.Find("Home"), BackToMenu);
        Hook(ui.Find("Enter"), EnterDungeon);
        Select((int)GameSession.SelectedHero);
        FollowNames();
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

    void EnterDungeon()
    {
        if (!Application.isPlaying)
            return;
        SceneManager.LoadScene("ForestLevel");
    }

    void BackToMenu()
    {
        if (!Application.isPlaying)
            return;
        SceneManager.LoadScene("MainMenu");
    }

    static float HeadTop(Transform slot)
    {
        var skins = slot.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skins.Length == 0)
            return slot.position.y + 1.85f;

        var bounds = skins[0].bounds;
        for (var i = 1; i < skins.Length; i++)
            bounds.Encapsulate(skins[i].bounds);
        return bounds.max.y + 0.16f;
    }
}

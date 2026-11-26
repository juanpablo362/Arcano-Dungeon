using System.Collections;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource spellSource;
    public AudioClip backgroundMusic;
    public AudioClip spellVoice;
    public float spellDelay = 3f;
    public float musicVolume = 0.5f;
    public float spellVolume = 0.2f;

    ControlsGuide guide;
    MainMenuOptions settings;
    UnityEngine.UI.Button optionsButton;
    GameObject menuPanel;

    void Awake()
    {
        // Misma pista persistente para menú y selección de personajes.
        if (PersistentMenuMusic.IsPlaying)
        {
            if (musicSource != null)
                musicSource.Stop();
        }
        else
        {
            AudioClip clip = backgroundMusic != null
                ? backgroundMusic
                : musicSource != null ? musicSource.clip : null;
            if (clip != null)
                PersistentMenuMusic.EnsurePlaying(clip, musicVolume);
            if (musicSource != null)
                musicSource.Stop();
        }

        if (spellVoice != null)
            StartCoroutine(PlaySpellLater());

        EnsureHelpButton();
        StartCoroutine(ClearButtonSelection());
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;
        if (settings != null && settings.gameObject.activeSelf)
            CloseOptions();
        else if (guide != null && guide.IsShown)
            guide.Hide();
    }

    void EnsureHelpButton()
    {
        var panel = GameObject.Find("MenuPanel");
        if (panel == null)
            return;
        menuPanel = panel;

        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
            guide = ControlsGuide.Ensure(canvas.transform);

        var options = panel.transform.Find("Options");
        if (options == null)
            return;
        optionsButton = options.GetComponent<UnityEngine.UI.Button>();
        if (optionsButton != null && canvas != null)
        {
            optionsButton.interactable = true;
            optionsButton.onClick.AddListener(() => OpenOptions(canvas));
        }

        if (panel.transform.Find("Help") != null)
            return;

        var clone = Instantiate(options.gameObject, panel.transform);
        clone.name = "Help";
        var label = clone.transform.Find("Label");
        var text = label != null ? label.GetComponent<Text>() : clone.GetComponentInChildren<Text>();
        if (text != null)
            text.text = "AYUDA";

        var button = clone.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ShowHelp);
        }

        var rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + 70f);
    }

    void OpenOptions(Canvas canvas)
    {
        if (settings == null)
            settings = MainMenuOptions.Create(canvas.transform, CloseOptions);
        guide?.Hide();
        menuPanel.SetActive(false);
        settings.Show();
    }

    void CloseOptions()
    {
        settings.Hide();
        menuPanel.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
    }

    void ShowHelp()
    {
        if (guide != null)
            guide.Show();
    }

    IEnumerator ClearButtonSelection()
    {
        yield return null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    IEnumerator PlaySpellLater()
    {
        yield return new WaitForSeconds(spellDelay);
        if (spellSource == null || spellVoice == null)
            yield break;

        spellSource.clip = spellVoice;
        spellSource.loop = true;
        spellSource.playOnAwake = false;
        spellSource.spatialBlend = 0f;
        spellSource.volume = spellVolume;
        spellSource.Play();
    }

    public void StartNewGame()
    {
        PersistentMenuMusic.EnsurePlaying(
            backgroundMusic != null ? backgroundMusic : musicSource != null ? musicSource.clip : null,
            musicVolume);
        if (musicSource != null)
            musicSource.Stop();
        SceneManager.LoadScene("CharacterSelection");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

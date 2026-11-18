using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource spellSource;
    public AudioClip backgroundMusic;
    public AudioClip spellVoice;
    public float spellDelay = 3f;
    public float musicVolume = 0.5f;
    public float spellVolume = 0.2f;

    void Awake()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        if (spellVoice != null)
            StartCoroutine(PlaySpellLater());

        StartCoroutine(ClearButtonSelection());
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
        SceneManager.LoadScene("CharacterSelection");
    }
}

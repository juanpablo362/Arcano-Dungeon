using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviour
{
    public void SelectMage()
    {
        GameSession.ResetRun();
        GameSession.SelectedHero = HeroId.Mage;
        PlayerPrefs.SetInt("SelectedCharacter", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Level1");
    }

    public void SelectKnight()
    {
        GameSession.ResetRun();
        GameSession.SelectedHero = HeroId.Knight;
        PlayerPrefs.SetInt("SelectedCharacter", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Level1");
    }
}

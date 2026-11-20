using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class LevelExit : MonoBehaviour
{
    public int nextSceneBuildIndex = 3;
    private bool isLoading = false;

    void Awake()
    {
        BoxCollider triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLoading) return;

        if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
        {
            isLoading = true;
            var clip = DungeonSfxCatalog.Instance != null ? DungeonSfxCatalog.Instance.portalEnter : null;
            GameSfx.Play2D(clip, 0.8f);
            SceneManager.LoadScene(nextSceneBuildIndex);
        }
    }
}

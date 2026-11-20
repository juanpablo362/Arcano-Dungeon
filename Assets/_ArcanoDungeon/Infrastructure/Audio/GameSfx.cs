using UnityEngine;

namespace ArcanoDungeon.Infrastructure.Audio
{
    /// <summary>
    /// One-shots 2D sin tocar prefabs. DDOL con un AudioSource dedicado.
    /// </summary>
    public class GameSfx : MonoBehaviour
    {
        static GameSfx instance;
        AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
        }

        public static void Play2D(AudioClip clip, float volume = 0.6f)
        {
            if (clip == null)
                return;

            var sfx = Ensure();
            sfx.source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public static void PlayCombat(AudioClip clip)
        {
            float vol = DungeonSfxCatalog.Instance != null
                ? DungeonSfxCatalog.Instance.combatVolume
                : 0.6f;
            Play2D(clip, vol);
        }

        public static void PlayStinger(AudioClip clip)
        {
            float vol = DungeonSfxCatalog.Instance != null
                ? DungeonSfxCatalog.Instance.stingerVolume
                : 0.55f;
            Play2D(clip, vol);
        }

        static GameSfx Ensure()
        {
            if (instance != null)
                return instance;

            var go = new GameObject("GameSfx");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameSfx>();
            instance.source = go.AddComponent<AudioSource>();
            instance.source.playOnAwake = false;
            instance.source.loop = false;
            instance.source.spatialBlend = 0f;
            return instance;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}

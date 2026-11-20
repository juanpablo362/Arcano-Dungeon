using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcanoDungeon.Infrastructure.Audio
{
    /// <summary>
    /// Música del menú que sobrevive MainMenu → CharacterSelection y se apaga al entrar al dungeon.
    /// </summary>
    public class PersistentMenuMusic : MonoBehaviour
    {
        const string ClipResourceFallback = "mavopix-black-magic-159868";
        const float DefaultVolume = 0.5f;

        static PersistentMenuMusic instance;

        AudioSource source;
        AudioClip clip;

        public static bool IsPlaying =>
            instance != null && instance.source != null && instance.source.isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string name = scene.name ?? "";
            if (name.StartsWith("Level") || name.Contains("Catacomb") || name.Contains("Dungeon"))
                StopAndDestroy();
        }

        public static void EnsurePlaying(AudioClip preferredClip = null, float volume = DefaultVolume)
        {
            if (IsPlaying)
            {
                if (preferredClip != null && instance.source.clip != preferredClip)
                {
                    instance.clip = preferredClip;
                    instance.source.clip = preferredClip;
                    instance.source.Play();
                }
                return;
            }

            var music = EnsureInstance();
            music.Play(preferredClip, volume);
        }

        public static void Adopt(AudioSource from, AudioClip clip, float volume = DefaultVolume)
        {
            if (from == null && clip == null)
                return;

            var music = EnsureInstance();
            AudioClip useClip = clip != null ? clip : from != null ? from.clip : null;
            float time = from != null && from.isPlaying ? from.time : 0f;

            if (from != null)
                from.Stop();

            music.Play(useClip, volume, time);
        }

        public static void StopAndDestroy()
        {
            if (instance == null)
                return;

            if (instance.source != null)
                instance.source.Stop();

            var go = instance.gameObject;
            instance = null;
            if (go != null)
                Destroy(go);
        }

        static PersistentMenuMusic EnsureInstance()
        {
            if (instance != null)
                return instance;

            var go = new GameObject("PersistentMenuMusic");
            return go.AddComponent<PersistentMenuMusic>();
        }

        void Play(AudioClip preferred, float volume, float startTime = 0f)
        {
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
            }

            clip = preferred != null ? preferred : clip;
            if (clip == null)
                clip = Resources.Load<AudioClip>(ClipResourceFallback);

            if (clip == null)
                return;

            source.clip = clip;
            source.volume = volume;
            source.loop = true;
            source.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0.01f, clip.length - 0.05f));
            if (!source.isPlaying)
                source.Play();
        }
    }
}

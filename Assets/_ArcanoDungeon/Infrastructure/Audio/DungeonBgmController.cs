using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcanoDungeon.Infrastructure.Audio
{
    /// <summary>
    /// BGM de dungeon: Level1 = audio inicial → loop permanente;
    /// Catacombs = loop2 + whispers; stingers de sala/encuentro.
    /// </summary>
    public class DungeonBgmController : MonoBehaviour
    {
        static DungeonBgmController instance;
        static bool sceneHooked;

        AudioSource bgm;
        AudioSource whispers;
        Coroutine introRoutine;
        bool galleryStingerPlayed;
        string currentMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            sceneHooked = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            HookSceneLoaded();
            EnsureForScene(SceneManager.GetActiveScene());
        }

        static void HookSceneLoaded()
        {
            if (sceneHooked)
                return;

            SceneManager.sceneLoaded += StaticOnSceneLoaded;
            sceneHooked = true;
        }

        static void StaticOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        public static void EnsureForScene(Scene scene)
        {
            HookSceneLoaded();

            string name = scene.name ?? "";
            bool dungeon = name.StartsWith("Level")
                           || name.Contains("Catacomb")
                           || name.Contains("Dungeon")
                           || name.Contains("Castle");

            if (!dungeon)
            {
                StopAndDestroy();
                return;
            }

            var ctrl = EnsureInstance();
            ctrl.ApplyScene(name);
        }

        public static void StopAndDestroy()
        {
            if (instance == null)
                return;

            instance.Teardown();
            var go = instance.gameObject;
            instance = null;
            if (go != null)
                Object.Destroy(go);
        }

        static DungeonBgmController EnsureInstance()
        {
            if (instance != null)
                return instance;

            var go = new GameObject("DungeonBgmController");
            return go.AddComponent<DungeonBgmController>();
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

            bgm = gameObject.AddComponent<AudioSource>();
            bgm.playOnAwake = false;
            bgm.loop = true;
            bgm.spatialBlend = 0f;

            whispers = gameObject.AddComponent<AudioSource>();
            whispers.playOnAwake = false;
            whispers.loop = true;
            whispers.spatialBlend = 0f;

            RoomController.RoomEntered += OnRoomEntered;
            RoomController.EncounterStarted += OnEncounterStarted;
        }

        void OnDestroy()
        {
            RoomController.RoomEntered -= OnRoomEntered;
            RoomController.EncounterStarted -= OnEncounterStarted;
            if (instance == this)
                instance = null;
        }

        void Teardown()
        {
            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            if (bgm != null)
                bgm.Stop();
            if (whispers != null)
                whispers.Stop();

            currentMode = null;
        }

        void ApplyScene(string sceneName)
        {
            var catalog = DungeonSfxCatalog.Instance;
            if (catalog == null)
            {
                catalog = Resources.Load<DungeonSfxCatalog>("DungeonSfxCatalog");
                if (catalog != null)
                    catalog.BindAsInstance();
            }

            if (catalog == null)
            {
                Debug.LogWarning("DungeonBgmController: no hay DungeonSfxCatalog en Resources.");
                return;
            }

            PersistentMenuMusic.StopAndDestroy();

            if (sceneName == "Level1")
            {
                if (currentMode == "Level1" && bgm != null && bgm.isPlaying)
                    return;

                galleryStingerPlayed = false;
                StopWhispers();
                StartLevel1(catalog);
                currentMode = "Level1";
                return;
            }

            if (sceneName.Contains("Catacomb"))
            {
                if (currentMode == "Catacombs" && bgm != null && bgm.isPlaying)
                    return;

                galleryStingerPlayed = false;
                StartCatacombs(catalog);
                currentMode = "Catacombs";
                return;
            }

            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            if (bgm != null)
                bgm.Stop();
            StopWhispers();
            currentMode = sceneName;
        }

        void StartLevel1(DungeonSfxCatalog catalog)
        {
            if (introRoutine != null)
                StopCoroutine(introRoutine);

            // Audio inicial al empezar la primera sala, luego loop todo el nivel.
            if (catalog.level1Intro != null)
                introRoutine = StartCoroutine(PlayIntroThenLoop(catalog.level1Intro, catalog.level1Loop, catalog.bgmVolume));
            else
                PlayLoop(catalog.level1Loop, catalog.bgmVolume);
        }

        void StartCatacombs(DungeonSfxCatalog catalog)
        {
            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            PlayLoop(catalog.level2Loop, catalog.bgmVolume);

            if (catalog.catacombsWhispers != null)
            {
                whispers.clip = catalog.catacombsWhispers;
                whispers.volume = catalog.whispersVolume;
                whispers.loop = true;
                if (!whispers.isPlaying)
                    whispers.Play();
            }
            else
                StopWhispers();
        }

        IEnumerator PlayIntroThenLoop(AudioClip intro, AudioClip loop, float volume)
        {
            bgm.Stop();
            bgm.clip = intro;
            bgm.loop = false;
            bgm.volume = volume;
            bgm.Play();

            float wait = intro != null ? Mathf.Max(0.1f, intro.length) : 0.1f;
            yield return new WaitForSecondsRealtime(wait);

            introRoutine = null;
            PlayLoop(loop, volume);
        }

        void PlayLoop(AudioClip clip, float volume)
        {
            if (clip == null || bgm == null)
            {
                Debug.LogWarning("DungeonBgmController: falta clip de loop de Level1.");
                return;
            }

            if (bgm.isPlaying && bgm.clip == clip && bgm.loop)
                return;

            bgm.clip = clip;
            bgm.loop = true;
            bgm.volume = volume;
            bgm.Play();
        }

        void StopWhispers()
        {
            if (whispers != null && whispers.isPlaying)
                whispers.Stop();
        }

        void OnRoomEntered(RoomController room)
        {
            if (room == null || galleryStingerPlayed)
                return;

            string scene = SceneManager.GetActiveScene().name ?? "";
            if (!scene.Contains("Catacomb"))
                return;

            string roomName = room.roomName ?? "";
            if (!roomName.Contains("Galer") && !roomName.Contains("nichos"))
                return;

            var catalog = DungeonSfxCatalog.Instance;
            if (catalog == null || catalog.level2SecondRoomStinger == null)
                return;

            galleryStingerPlayed = true;
            GameSfx.PlayStinger(catalog.level2SecondRoomStinger);
        }

        void OnEncounterStarted(RoomController room)
        {
            var catalog = DungeonSfxCatalog.Instance;
            if (catalog == null || catalog.encounterScaryStinger == null)
                return;

            if (room != null &&
                (room.roomKind == RoomController.RoomKind.Combat
                 || room.roomKind == RoomController.RoomKind.Boss))
            {
                GameSfx.PlayStinger(catalog.encounterScaryStinger);
            }
        }
    }
}

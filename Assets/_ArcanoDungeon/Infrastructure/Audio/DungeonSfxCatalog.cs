using UnityEngine;

namespace ArcanoDungeon.Infrastructure.Audio
{
    [CreateAssetMenu(menuName = "Arcano/Audio/Dungeon SFX Catalog", fileName = "DungeonSfxCatalog")]
    public class DungeonSfxCatalog : ScriptableObject
    {
        public static DungeonSfxCatalog Instance { get; private set; }

        [Header("Level BGM")]
        public AudioClip level1Intro;
        public AudioClip level1Loop;
        public AudioClip level2Loop;
        public AudioClip level2SecondRoomStinger;

        [Header("Ambient")]
        public AudioClip catacombsWhispers;
        public AudioClip encounterScaryStinger;
        public AudioClip finalRoomCleared;

        [Header("Combat")]
        public AudioClip swordSwing;
        public AudioClip swordHit;
        public AudioClip skeletonHit;
        public AudioClip groundImpact;
        public AudioClip monsterAggroRoar;
        public AudioClip monsterAttackGrowlA;
        public AudioClip monsterAttackGrowlB;
        public AudioClip fireballWhoosh;
        public AudioClip portalEnter;
        public AudioClip chestOpen;
        public AudioClip knightSpecial;
        public AudioClip playerDeathScream;
        public AudioClip playerDeathWhistle;

        [Header("Volumes")]
        [Range(0f, 1f)] public float bgmVolume = 0.45f;
        [Range(0f, 1f)] public float whispersVolume = 0.18f;
        [Range(0f, 1f)] public float stingerVolume = 0.55f;
        [Range(0f, 1f)] public float combatVolume = 0.6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            Instance = Resources.Load<DungeonSfxCatalog>("DungeonSfxCatalog");
        }

        public void BindAsInstance()
        {
            Instance = this;
        }

        public AudioClip RandomAttackGrowl()
        {
            if (monsterAttackGrowlA == null)
                return monsterAttackGrowlB;
            if (monsterAttackGrowlB == null)
                return monsterAttackGrowlA;
            return Random.value < 0.5f ? monsterAttackGrowlA : monsterAttackGrowlB;
        }

        public AudioClip RandomPlayerDeath()
        {
            if (playerDeathScream == null)
                return playerDeathWhistle;
            if (playerDeathWhistle == null)
                return playerDeathScream;
            return Random.value < 0.5f ? playerDeathScream : playerDeathWhistle;
        }
    }
}

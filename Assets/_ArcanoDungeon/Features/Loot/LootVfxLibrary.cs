using ArcanoDungeon.Infrastructure.Assets;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Prefabs de VFX de los packs del proyecto (Hovl, Vefects Fire, 100 Best Effects).
    /// </summary>
    public static class LootVfxLibrary
    {
        public const string FireFloor =
            "Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_Floor_01.prefab";
        public const string FireFloorSimple =
            "Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_Floor_01_Simple.prefab";
        public const string FireSmall =
            "Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small.prefab";
        public const string FireMedium =
            "Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Medium.prefab";

        public const string MagicShieldBlue =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/Magic shields/Magic shield blue.prefab";
        public const string LightningAura =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/Character auras/Lightning aura.prefab";
        public const string ElectroHit =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/Hits and explosions/Electro hit.prefab";
        public const string GroundAoe =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/AoE effects/Ground AOE explosion.prefab";
        public const string AoESlashOrange =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/AoE effects/AoE slash orange.prefab";
        public const string MagicCircle =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/Magic circles/Magic circle.prefab";
        public const string HolyHit =
            "Assets/Hovl Studio/Magic effects pack/Prefabs/Hits and explosions/Holy hit.prefab";

        public const string Lightning1 =
            "Assets/100BestEffectPack/Effects/LightningEffect/LightningEffect1.prefab";
        public const string Lightning2 =
            "Assets/100BestEffectPack/Effects/LightningEffect/LightningEffect2.prefab";
        public const string FireEffect3 =
            "Assets/100BestEffectPack/Effects/FireEffect/FireEffect3.prefab";
        public const string FireEffect1 =
            "Assets/100BestEffectPack/Effects/FireEffect/FireEffect1.prefab";
        public const string MagicEffect8 =
            "Assets/100BestEffectPack/Effects/OtherMagicEffect/MagicEffect8.prefab";
        public const string Explosion1 =
            "Assets/100BestEffectPack/Effects/ExplosionEffect/ExplosionEffect.prefab";

        public static GameObject Load(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;
            return DungeonRuntimeAssets.Load<GameObject>(assetPath);
        }

        public static GameObject Spawn(
            string assetPath,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            float scale,
            float autoDestroySeconds = -1f)
        {
            var prefab = Load(assetPath);
            if (prefab == null)
                return null;

            var fx = Object.Instantiate(prefab, position, rotation, parent);
            fx.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                if (!ps.isPlaying)
                    ps.Play(true);
            }

            if (autoDestroySeconds > 0f)
                Object.Destroy(fx, autoDestroySeconds);

            return fx;
        }

        public static GameObject SpawnFirst(
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            float scale,
            float autoDestroySeconds,
            params string[] paths)
        {
            if (paths == null)
                return null;
            for (int i = 0; i < paths.Length; i++)
            {
                var fx = Spawn(paths[i], position, rotation, parent, scale, autoDestroySeconds);
                if (fx != null)
                    return fx;
            }

            return null;
        }
    }
}

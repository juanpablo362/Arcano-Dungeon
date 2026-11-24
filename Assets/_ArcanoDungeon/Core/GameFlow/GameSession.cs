using System.Collections.Generic;
using ArcanoDungeon.Features.Loot;
using ArcanoDungeon.Features.Player;
using UnityEngine;

public static class GameSession
{
    public static HeroId SelectedHero = HeroId.Mage;

    sealed class RunSnapshot
    {
        public int Health;
        public float Stamina;
        public int Bombs;
        public PlayerStats.TransitionState Stats;
        public ItemData Weapon;
        public float WeaponRemaining;
        public float WeaponDuration;
        public readonly List<ItemData> Passives = new List<ItemData>();
        public readonly List<LootItemKind> LegacyUpgrades = new List<LootItemKind>();
    }

    static RunSnapshot pending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetRun()
    {
        pending = null;
        RunScore.Reset();
    }

    public static bool PrepareLevelTransition(GameObject player)
    {
        var health = player != null ? player.GetComponent<PlayerHealth>() : null;
        if (health == null || health.currentHealth <= 0)
            return false;

        var stats = player.GetComponent<PlayerStats>();
        var weapon = player.GetComponent<TemporaryWeaponController>();
        var snapshot = new RunSnapshot
        {
            Health = health.currentHealth,
            Stamina = player.GetComponent<PlayerStamina>()?.Current ?? 100f,
            Bombs = player.GetComponent<PowderBombController>()?.Charges ?? 0,
            Stats = stats != null ? stats.CaptureTransitionState() : default,
            Weapon = weapon != null && weapon.IsActive ? weapon.ActiveItem : null,
            WeaponRemaining = weapon != null ? weapon.Remaining : 0f,
            WeaponDuration = weapon != null ? weapon.DurationTotal : 0f
        };
        var inventory = player.GetComponent<PlayerItemInventory>();
        if (inventory != null)
            foreach (var item in inventory.Passives)
                for (int i = 0; i < inventory.GetStackCount(item); i++)
                    snapshot.Passives.Add(item);

        var legacy = player.GetComponent<PlayerLootBuffs>();
        if (legacy != null)
            snapshot.LegacyUpgrades.AddRange(legacy.RunUpgrades);
        pending = snapshot;
        return true;
    }

    // The spawner calls this after the new player's Start methods have bound the HUD.
    public static void RestoreLevelTransition(GameObject player)
    {
        var snapshot = pending;
        pending = null;
        if (snapshot == null || player == null)
            return;

        var inventory = player.GetComponent<PlayerItemInventory>();
        foreach (var item in snapshot.Passives)
            inventory?.Collect(item);
        var legacy = player.GetComponent<PlayerLootBuffs>();
        foreach (var kind in snapshot.LegacyUpgrades)
            legacy?.Apply(kind);

        player.GetComponent<PlayerStats>()?.RestoreTransitionState(snapshot.Stats);
        player.GetComponent<PlayerHealth>()?.RestoreCurrentHealth(snapshot.Health);
        player.GetComponent<PlayerStamina>()?.RestoreCurrent(snapshot.Stamina);
        player.GetComponent<PowderBombController>()?.RestoreCharges(snapshot.Bombs);
        if (snapshot.Weapon != null && snapshot.WeaponRemaining > 0f)
            player.GetComponent<TemporaryWeaponController>()?.RestoreWeapon(
                snapshot.Weapon, snapshot.WeaponRemaining, snapshot.WeaponDuration);
    }
}

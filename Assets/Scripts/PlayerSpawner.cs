using System.Collections;
using ArcanoDungeon.Features.Loot;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject magePrefab;
    public GameObject knightPrefab;
    public GameObject misakiPrefab;
    public GameObject yukoPrefab;
    public Transform startPosition;

    IEnumerator Start()
    {
        var prefabToSpawn = ResolvePrefab();

        if (prefabToSpawn != null)
        {
            var spawnPos = startPosition != null ? startPosition.position : Vector3.zero;
            var spawnRot = startPosition != null ? startPosition.rotation : Quaternion.identity;
            var player = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            player.tag = "Player";
            if (player.GetComponent<CharacterController>() is CharacterController cc && !cc.enabled)
                cc.enabled = true;

            if (player.GetComponent<PlayerStats>() == null)
                player.AddComponent<PlayerStats>();
            if (player.GetComponent<PlayerItemInventory>() == null)
                player.AddComponent<PlayerItemInventory>();
            if (player.GetComponent<TemporaryWeaponController>() == null)
                player.AddComponent<TemporaryWeaponController>();
            if (player.GetComponent<PowderBombController>() == null)
                player.AddComponent<PowderBombController>();
            if (player.GetComponent<PlayerLootBuffs>() == null)
                player.AddComponent<PlayerLootBuffs>();

            yield return null;
            GameSession.RestoreLevelTransition(player);
        }
        else
        {
            Debug.LogError("PlayerSpawner: no hay prefab asignado para el héroe seleccionado.");
        }
    }

    GameObject ResolvePrefab()
    {
        switch (GameSession.SelectedHero)
        {
            case HeroId.Knight:
                return knightPrefab != null ? knightPrefab : magePrefab;
            case HeroId.Misaki:
                return misakiPrefab != null ? misakiPrefab : knightPrefab != null ? knightPrefab : magePrefab;
            case HeroId.Yuko:
                return yukoPrefab != null ? yukoPrefab : knightPrefab != null ? knightPrefab : magePrefab;
            case HeroId.Mage:
            default:
                return magePrefab != null ? magePrefab : knightPrefab;
        }
    }
}

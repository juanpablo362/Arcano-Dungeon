using ArcanoDungeon.Features.Loot.Behaviours;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    public float speed = 14f;
    public int damage = 45;
    public float lifetime = 4f;
    public float hitRadius = 0.45f;
    public GameObject impactEffect;

    Transform owner;
    bool consumed;
    bool enemyProjectile;
    static float nextWhooshTime;

    public bool IsEnemyProjectile => enemyProjectile;

    public void Launch(Transform shooter, Vector3 direction)
    {
        owner = shooter;
        enemyProjectile = false;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        PlayWhoosh();
        Destroy(gameObject, lifetime);
    }

    static void PlayWhoosh()
    {
        if (Time.time < nextWhooshTime)
            return;
        nextWhooshTime = Time.time + 0.12f;
        var clip = DungeonSfxCatalog.Instance != null ? DungeonSfxCatalog.Instance.fireballWhoosh : null;
        GameSfx.Play2D(clip, 0.75f);
    }

    public void LaunchAsEnemy(Transform shooter, Vector3 direction)
    {
        Launch(shooter, direction);
        enemyProjectile = true;
    }

    void Update()
    {
        if (consumed)
            return;

        transform.position += transform.forward * speed * Time.deltaTime;

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);
        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (owner != null && (hit.transform == owner || hit.transform.IsChildOf(owner)))
                continue;

            if (!hit.CompareTag("Enemy"))
                continue;

            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null)
                continue;

            enemyHealth.TakeDamage(damage);
            NotifyChain(enemyHealth, hit.ClosestPoint(transform.position));
            Explode();
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed || other == null)
            return;

        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                NotifyChain(enemyHealth, other.ClosestPoint(transform.position));
            }
            Explode();
            return;
        }

        if (!other.isTrigger && other.GetComponent<CharacterController>() == null)
            Explode();
    }

    void NotifyChain(EnemyHealth enemy, Vector3 hitPoint)
    {
        if (owner == null || enemyProjectile)
            return;

        var chain = owner.GetComponent<ChainLightningPassive>()
                    ?? owner.GetComponentInParent<ChainLightningPassive>();
        chain?.NotifyHit(enemy, hitPoint);
    }

    void Explode()
    {
        if (consumed)
            return;

        consumed = true;

        if (impactEffect != null)
        {
            GameObject fx = Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        Destroy(gameObject);
    }
}

using UnityEditor;
using UnityEngine;

public static class SetupCollisionMatrix
{
    public static string Main()
    {
        int player = LayerMask.NameToLayer("Player");
        int enemy = LayerMask.NameToLayer("Enemy");
        int environment = LayerMask.NameToLayer("Environment");
        int projectile = LayerMask.NameToLayer("Projectile");

        if (player < 0 || enemy < 0 || environment < 0 || projectile < 0)
            return "ERROR missing layers";

        // Proyectiles aliados no chocan con el jugador ni entre sí.
        Physics.IgnoreLayerCollision(projectile, player, true);
        Physics.IgnoreLayerCollision(projectile, projectile, true);

        // Asegurar que proyectiles sí chocan con enemigos y entorno.
        Physics.IgnoreLayerCollision(projectile, enemy, false);
        Physics.IgnoreLayerCollision(projectile, environment, false);

        return "OK collision matrix: Projectile ignores Player+Projectile";
    }
}

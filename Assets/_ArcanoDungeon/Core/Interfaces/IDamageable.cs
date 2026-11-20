namespace ArcanoDungeon.Core
{
    /// <summary>
    /// Contrato de daño compartido por jugador y enemigos.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}

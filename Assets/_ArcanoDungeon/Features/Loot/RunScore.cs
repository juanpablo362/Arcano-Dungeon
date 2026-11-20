using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Puntaje de la run actual (monedas/gemas).
    /// </summary>
    public static class RunScore
    {
        public static int Current { get; private set; }

        public static void Add(int amount)
        {
            if (amount == 0)
                return;
            Current = Mathf.Max(0, Current + amount);
        }

        public static void Reset()
        {
            Current = 0;
        }
    }
}

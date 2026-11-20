using UnityEngine;

namespace ArcanoDungeon.Features.Combat
{
    /// <summary>
    /// Colocar en el mismo GameObject que el Animator si MeleeWeaponController está en el root.
    /// Los Animation Events llaman aquí y se reenvían al controller.
    /// </summary>
    public class AttackAnimEventRelay : MonoBehaviour
    {
        [SerializeField] MeleeWeaponController melee;

        void Awake()
        {
            if (melee == null)
                melee = GetComponentInParent<MeleeWeaponController>();
        }

        public void OnAttackHitFrame()
        {
            if (melee == null)
                melee = GetComponentInParent<MeleeWeaponController>();
            melee?.OnAttackHitFrame();
        }

        public void OnAttackEndFrame()
        {
            if (melee == null)
                melee = GetComponentInParent<MeleeWeaponController>();
            melee?.OnAttackEndFrame();
        }
    }
}

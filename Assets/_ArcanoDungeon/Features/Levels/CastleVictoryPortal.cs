using UnityEngine;

namespace ArcanoDungeon.Features.Levels
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CastleVictoryPortal : MonoBehaviour
    {
        public EclipseCastleController castle;
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null)
                castle?.CompleteRun();
        }
    }
}

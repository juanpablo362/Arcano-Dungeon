using UnityEngine;

namespace ArcanoDungeon.Features.Player.View
{
    /// <summary>
    /// Raycast desde el centro exacto de la pantalla (retícula) hacia el mundo.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class AimRaycaster : MonoBehaviour
    {
        [SerializeField] Camera aimCamera;
        [SerializeField] float maxDistance = 50f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; }
        public bool HasHit { get; private set; }
        public RaycastHit AimHit { get; private set; }
        public Ray AimRay { get; private set; }

        void Awake()
        {
            if (aimCamera == null)
                aimCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (aimCamera == null)
                aimCamera = Camera.main;
            if (aimCamera == null)
                return;

            AimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            AimDirection = AimRay.direction.normalized;

            if (Physics.Raycast(AimRay, out RaycastHit hit, maxDistance, hitMask, triggerInteraction))
            {
                HasHit = true;
                AimHit = hit;
                AimPoint = hit.point;
            }
            else
            {
                HasHit = false;
                AimHit = default;
                AimPoint = AimRay.GetPoint(maxDistance);
            }
        }

        public Vector3 GetFlatAimDirection()
        {
            Vector3 flat = AimDirection;
            flat.y = 0f;
            return flat.sqrMagnitude > 0.001f ? flat.normalized : transform.forward;
        }
    }
}

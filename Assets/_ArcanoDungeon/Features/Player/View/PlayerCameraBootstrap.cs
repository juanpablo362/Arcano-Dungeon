using ArcanoDungeon.Features.Player.Motor;
using Unity.Cinemachine;
using UnityEngine;

namespace ArcanoDungeon.Features.Player.View
{
    /// <summary>
    /// Al spawnear el jugador, enlaza el CameraTarget con el rig Cinemachine de la escena.
    /// </summary>
    public class PlayerCameraBootstrap : MonoBehaviour
    {
        [SerializeField] string cameraTargetName = "CameraTarget";
        [SerializeField] Vector3 cameraTargetLocalPosition = new Vector3(0f, 0.95f, 0f);

        bool bound;

        void Start() => TryBind();

        void LateUpdate()
        {
            // Reintentar por si el rig spawnea después o el target se pierde.
            if (!bound)
                TryBind();
        }

        void TryBind()
        {
            // Preferir CameraFollow estilo Cesar; solo enlazar Cinemachine si el rig está activo.
            var follow = FindAnyObjectByType<CameraFollow>();
            var rig = FindAnyObjectByType<ThirdPersonCameraRig>();
            if (follow != null && (rig == null || !rig.isActiveAndEnabled))
            {
                EnsureCameraFollowIsMain(follow);
                bound = true;
                return;
            }

            Transform camTarget = transform.Find(cameraTargetName);
            if (camTarget == null)
            {
                var go = new GameObject(cameraTargetName);
                camTarget = go.transform;
                camTarget.SetParent(transform, false);
                camTarget.localPosition = cameraTargetLocalPosition;
            }

            if (rig != null && rig.isActiveAndEnabled)
            {
                rig.SetFollowTarget(transform);
                var motor = GetComponent<PlayerMotor>();
                motor?.SetCameraRig(rig);
                bound = rig.CameraTarget != null;
            }
        }

        static void EnsureCameraFollowIsMain(CameraFollow follow)
        {
            var cam = follow.GetComponent<Camera>();
            if (cam == null)
                return;

            cam.tag = "MainCamera";
            cam.enabled = true;
            follow.enabled = true;

            foreach (var brain in FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
                brain.enabled = false;

            foreach (var other in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (other == cam)
                    continue;
                if (other.CompareTag("MainCamera"))
                    other.tag = "Untagged";
                // No apagar cámaras de UI; solo brains Cinemachine ya desactivados.
            }
        }
    }
}

using UnityEngine;

namespace ArcanoDungeon.Features.Assassin
{
    // Keep the fitted closed grip while humanoid clips animate the wrists and arms.
    // Finger curves from the source animation otherwise open the hands around the daggers.
    [DefaultExecutionOrder(200)]
    public sealed class AssassinDaggerGrip : MonoBehaviour
    {
        [SerializeField] Transform[] fingers;
        [SerializeField] Quaternion[] rotations;

        public void Capture(Transform[] bones, Quaternion[] pose = null)
        {
            fingers = bones;
            rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                rotations[i] = pose != null ? pose[i] : bones[i].localRotation;
        }

        public void Apply()
        {
            if (fingers == null || rotations == null)
                return;
            for (int i = 0; i < fingers.Length && i < rotations.Length; i++)
                if (fingers[i] != null)
                    fingers[i].localRotation = rotations[i];
        }

        void LateUpdate() => Apply();
    }
}

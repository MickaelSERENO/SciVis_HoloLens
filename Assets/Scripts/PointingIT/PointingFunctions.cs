using Sereno.Unity.HandDetector;
using UnityEngine;

namespace Sereno.Pointing
{
    class PointingFunctions
    {
        public static Vector3 GetFingerOffset(Transform headset, Handedness h)
        {
            if (h == Handedness.NONE)
                return new Vector3(0, 0, 0);

            Vector3 cameraRight = headset.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            Vector3 cameraForward = headset.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            return 0.06f * (h == Handedness.LEFT ? cameraRight : -cameraRight) + 0.03f * Vector3.up + 0.11f * cameraForward;
        }
    }
}

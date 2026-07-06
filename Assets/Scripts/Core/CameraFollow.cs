using UnityEngine;

namespace Tribulation.Core
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new(0f, 18f, -13f);
        public float SmoothTime = 0.12f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (Target == null)
            {
                return;
            }

            var targetPosition = Target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, SmoothTime);
        }
    }
}

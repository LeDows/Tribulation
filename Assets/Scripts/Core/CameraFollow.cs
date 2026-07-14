using Tribulation.Config;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tribulation.Core
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 22f;
        public float Yaw = 0f;
        public float Pitch = 45f;
        public float MouseSensitivity = 0.22f;
        public float PositionSharpness = 18f;
        public float RotationSharpness = 22f;
        public Vector2 PitchLimits = new(30f, 75f);

        private float targetYaw;
        private float targetPitch;

        public void Configure(Transform target, CameraConfig config)
        {
            Target = target;
            Distance = config.orbitDistance;
            Yaw = config.yaw;
            Pitch = config.pitch;
            MouseSensitivity = config.mouseSensitivity;
            PositionSharpness = config.positionSharpness;
            RotationSharpness = config.rotationSharpness;
            PitchLimits = config.pitchLimits;
            targetYaw = Yaw;
            targetPitch = Pitch;
        }

        private void OnEnable()
        {
            targetYaw = Yaw;
            targetPitch = Pitch;
        }

        private void LateUpdate()
        {
            if (!GameManager.IsSimulationRunning || Target == null)
            {
                return;
            }

            ReadOrbitInput();
            ApplyOrbitSmoothing();

            var targetPosition = Target.position;
            var orbitRotation = Quaternion.Euler(Pitch, Yaw, 0f);
            var desiredPosition = targetPosition + orbitRotation * new Vector3(0f, 0f, -Distance);

            transform.SetPositionAndRotation(desiredPosition, orbitRotation);
        }

        private void ReadOrbitInput()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed)
            {
                return;
            }

            var delta = mouse.delta.ReadValue();
            targetYaw += delta.x * MouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch - delta.y * MouseSensitivity, PitchLimits.x, PitchLimits.y);
        }

        private void ApplyOrbitSmoothing()
        {
            var t = 1f - Mathf.Exp(-RotationSharpness * Time.deltaTime);
            Yaw = Mathf.LerpAngle(Yaw, targetYaw, t);
            Pitch = Mathf.Lerp(Pitch, targetPitch, t);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Tribulation.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerStats))]
    public sealed class PlayerController : MonoBehaviour
    {
        private Rigidbody body;
        private PlayerStats stats;
        private Vector3 moveDirection;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            stats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveDirection = Vector3.zero;
                return;
            }

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;

            input = Vector2.ClampMagnitude(input, 1f);
            moveDirection = GetCameraRelativeDirection(input);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = moveDirection * stats.MoveSpeed;
        }

        private static Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            var forward = camera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            var right = camera.transform.right;
            right.y = 0f;
            right.Normalize();

            return right * input.x + forward * input.y;
        }
    }
}

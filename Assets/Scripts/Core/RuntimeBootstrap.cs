using Tribulation.Combat;
using Tribulation.Enemies;
using Tribulation.Player;
using Tribulation.UI;
using UnityEngine;

namespace Tribulation.Core
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildPrototypeScene()
        {
            Time.timeScale = 1f;

            if (Object.FindFirstObjectByType<GameManager>() != null)
            {
                return;
            }

            var root = new GameObject("Prototype Runtime");
            root.AddComponent<GameManager>();

            EnsureGround();
            var player = CreatePlayer();
            CreateCamera(player.transform);

            var spawner = new GameObject("Wave Spawner");
            spawner.transform.SetParent(root.transform);
            spawner.AddComponent<WaveSpawner>();

            var hud = new GameObject("Prototype HUD");
            hud.transform.SetParent(root.transform);
            hud.AddComponent<PrototypeHud>();
        }

        private static PlayerStats CreatePlayer()
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Cultivator";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.GetComponent<Renderer>().material.color = new Color(0.2f, 0.75f, 1f);

            var body = player.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.useGravity = false;

            var stats = player.AddComponent<PlayerStats>();
            player.AddComponent<PlayerController>();
            player.AddComponent<AutoWeapon>();
            return stats;
        }

        private static void CreateCamera(Transform target)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            const float fieldOfView = 35f;
            const float orbitDistance = 35f;

            camera.transform.position = new Vector3(0f, 18f, -13f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.orthographic = false;
            camera.fieldOfView = fieldOfView;

            var follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraFollow>();
            }

            follow.Target = target;
            follow.Distance = orbitDistance;
            follow.Yaw = 0f;
            follow.Pitch = 45f;
            follow.MouseSensitivity = 0.22f;
        }

        private static void EnsureGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Qingyun Prototype Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<Renderer>().material.color = new Color(0.15f, 0.18f, 0.16f);
        }
    }
}
